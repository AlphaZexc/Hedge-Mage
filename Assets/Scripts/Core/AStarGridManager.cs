using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

public class AStarGridManager : MonoBehaviour
{
    private Node[,] grid;
    private int gridSizeX, gridSizeY;
    private Vector3Int gridOrigin;
    private List<Node> walkableNodesCache;

    private HashSet<Node> temporarilyUnwalkableNodes = new HashSet<Node>();
    private List<Node> lastPathNodes = null;

    [Header("Grid View in Editor")] 
    public bool showGridGizmos = true;
    public Color walkableColor = Color.white;
    public Color unwalkableColor = Color.red;
    public Color pathColor = Color.green;
    public Tilemap walkableTilemap;
    public Tilemap unwalkableTilemap;

    private void Awake()
    {
        if (walkableTilemap == null || unwalkableTilemap == null)
        {
            Debug.LogError("AStarGridManager: One or more tilemaps are not assigned!");
            return;
        }
        CreateGridFromTilemap();
    }

    // Mark a node at world position as temporarily unwalkable (for dynamic avoidance).
    public void SetNodeTemporarilyUnwalkable(Vector3 worldPosition, float duration = 1.0f)
    {
        Node node = NodeFromWorldPoint(worldPosition);
        if (node != null && node.walkable && !temporarilyUnwalkableNodes.Contains(node))
        {
            temporarilyUnwalkableNodes.Add(node);
            node.walkable = false;
            StartCoroutine(ResetNodeWalkableAfterDelay(node, duration));
        }
    }

    private IEnumerator ResetNodeWalkableAfterDelay(Node node, float delay)
    {
        yield return new WaitForSeconds(delay);
        node.walkable = true;
        temporarilyUnwalkableNodes.Remove(node);
    }


    // Public API for enemy queries
    public bool IsWorldPositionWalkable(Vector3 worldPosition)
    {
        Node node = NodeFromWorldPoint(worldPosition);
        return node != null && node.walkable && !temporarilyUnwalkableNodes.Contains(node);
    }

    public bool IsGridIndexWalkable(int x, int y)
    {
        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
        {
            Node node = grid[x, y];
            return node.walkable && !temporarilyUnwalkableNodes.Contains(node);
        }
        return false;
    }

    public Vector2Int GetGridSize()
    {
        return new Vector2Int(gridSizeX, gridSizeY);
    }

    public Node GetNodeByGridIndex(int x, int y)
    {
        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
            return grid[x, y];
        return null;
    }

    public List<Node> GetAllWalkableNodes()
    {
        return new List<Node>(walkableNodesCache);
    }


    public void CreateGridFromTilemap()
    {
        BoundsInt bounds = walkableTilemap.cellBounds;
        gridSizeX = bounds.size.x;
        gridSizeY = bounds.size.y;
        gridOrigin = bounds.min;
        grid = new Node[gridSizeX, gridSizeY];
        
        walkableNodesCache = new List<Node>(); // --- NEW: Initialize the cache

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3Int cellPos = new Vector3Int(gridOrigin.x + x, gridOrigin.y + y, 0);
                Vector3 worldPoint = walkableTilemap.GetCellCenterWorld(cellPos);
                bool walkable = walkableTilemap.HasTile(cellPos) && !unwalkableTilemap.HasTile(cellPos);
                grid[x, y] = new Node(walkable, worldPoint, x, y);

                // --- NEW: Populate the cache during grid creation ---
                if (walkable)
                {
                    walkableNodesCache.Add(grid[x,y]);
                }
            }
        }
        Debug.Log($"AStarGridManager: Grid created with size {gridSizeX} x {gridSizeY}. Found {walkableNodesCache.Count} walkable nodes.");
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        Vector3Int cellPos = walkableTilemap.WorldToCell(worldPosition);
        int x = cellPos.x - gridOrigin.x;
        int y = cellPos.y - gridOrigin.y;

        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
        {
            return grid[x, y];
        }
        return null;
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null || !startNode.walkable)
        {
            lastPathNodes = null;
            return null;
        }

        if (!targetNode.walkable)
        {
            Node closestNode = FindClosestWalkableNode(targetNode);
            if (closestNode != null)
            {
                targetNode = closestNode;
            }
            else
            {
                lastPathNodes = null;
                return null;
            }
        }

        BinaryHeap<Node> openSet = new BinaryHeap<Node>(gridSizeX * gridSizeY);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                lastPathNodes = RetracePathNodes(startNode, targetNode);
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                    else
                        openSet.UpdateItem(neighbor);
                }
            }
        }
        lastPathNodes = null;
        return null;
    }
    
    private List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        int[] x_directions = { 0, 0, 1, -1 };
        int[] y_directions = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int checkX = node.gridX + x_directions[i];
            int checkY = node.gridY + y_directions[i];

            if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
            {
                neighbors.Add(grid[checkX, checkY]);
            }
        }

        return neighbors;
    }

    private int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);
        return 10 * (dstX + dstY);
    }
    
    private Node FindClosestWalkableNode(Node node)
    {
        Queue<Node> queue = new Queue<Node>();
        HashSet<Node> visited = new HashSet<Node>();
        queue.Enqueue(node);
        visited.Add(node);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();
            if (current.walkable)
            {
                return current;
            }

            foreach (Node neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return null;
    }

    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Node> pathNodes = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            pathNodes.Add(currentNode);
            currentNode = currentNode.parent;
        }
        pathNodes.Reverse();
        List<Vector3> waypoints = new List<Vector3>();
        foreach(Node node in pathNodes)
        {
            waypoints.Add(node.worldPosition);
        }
        return waypoints;
    }

    private List<Node> RetracePathNodes(Node startNode, Node endNode)
    {
        List<Node> pathNodes = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            pathNodes.Add(currentNode);
            currentNode = currentNode.parent;
        }
        pathNodes.Reverse();
        return pathNodes;
    }

    // Draw grid and last path in the editor
    void OnDrawGizmos()
    {
        if (!showGridGizmos || grid == null) return;
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Node node = grid[x, y];
                Gizmos.color = node.walkable ? walkableColor : unwalkableColor;
                Gizmos.DrawCube(node.worldPosition, Vector3.one * 0.3f);
            }
        }
        if (lastPathNodes != null)
        {
            Gizmos.color = pathColor;
            foreach (Node node in lastPathNodes)
            {
                Gizmos.DrawSphere(node.worldPosition, 0.2f);
            }
        }
    }

    // Find a random walkable tile, using cached info
    public Node GetRandomWalkableNode()
    {
        if (walkableNodesCache != null && walkableNodesCache.Count > 0)
        {
            // Pick a random node from the pre-compiled list of walkable nodes.
            return walkableNodesCache[Random.Range(0, walkableNodesCache.Count)];
        }

        // This will only be reached if no walkable nodes exist on the entire map.
        Debug.LogWarning("AStarGridManager: No walkable nodes were found in the cache.");
        return null;
    }
}

// Helper Classes
public class Node : IHeapItem<Node>
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX, gridY;
    public int gCost, hCost;
    public Node parent;
    
    public int FCost => gCost + hCost;
    public int HeapIndex { get; set; }

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
    }

    public int CompareTo(Node other)
    {
        int compare = FCost.CompareTo(other.FCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(other.hCost);
        }
        return compare;
    }
}

public class BinaryHeap<T> where T : IHeapItem<T>
{
    T[] items;
    int count;

    public BinaryHeap(int maxSize)
    {
        items = new T[maxSize];
    }

    public void Add(T item)
    {
        item.HeapIndex = count;
        items[count] = item;
        SortUp(item);
        count++;
    }

    public T RemoveFirst()
    {
        T first = items[0];
        count--;
        items[0] = items[count];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return first;
    }

    public void UpdateItem(T item) => SortUp(item);
    public int Count => count;
    public bool Contains(T item) => Equals(items[item.HeapIndex], item);

    void SortDown(T item)
    {
        while (true)
        {
            int left = item.HeapIndex * 2 + 1;
            int right = item.HeapIndex * 2 + 2;
            int swap = item.HeapIndex;

            if (left < count)
            {
                if (items[left].CompareTo(items[swap]) < 0)
                {
                    swap = left;
                }
            }

            if (right < count)
            {
                if (items[right].CompareTo(items[swap]) < 0)
                {
                    swap = right;
                }
            }

            if (swap != item.HeapIndex)
            {
                Swap(item, items[swap]);
            }
            else
            {
                return;
            }
        }
    }

    void SortUp(T item)
    {
        int parent = (item.HeapIndex - 1) / 2;
        while (item.HeapIndex > 0 && item.CompareTo(items[parent]) < 0)
        {
            Swap(item, items[parent]);
            parent = (item.HeapIndex - 1) / 2;
        }
    }

    void Swap(T a, T b)
    {
        items[a.HeapIndex] = b;
        items[b.HeapIndex] = a;
        (a.HeapIndex, b.HeapIndex) = (b.HeapIndex, a.HeapIndex);
    }
}

public interface IHeapItem<T> : System.IComparable<T>
{
    int HeapIndex { get; set; }
}
