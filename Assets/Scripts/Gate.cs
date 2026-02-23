using UnityEngine;

/// <summary>
/// Attach this to each gate sprite GameObject.
/// Requires: Animator, BoxCollider2D (trigger), and the "Gate" tag on the object.
///
/// ANIMATOR SETUP:
///   1. Create an Animator Controller and assign it to this object.
///   2. Add a Trigger parameter named "Open".
///   3. Create one animation clip for the open sequence (4 frames).
///      - In the clip settings, uncheck "Loop Time".
///   4. Set the default state to your closed/idle state.
///   5. Add a transition from the closed state to the open clip,
///      triggered by the "Open" parameter.
///      - Uncheck "Has Exit Time" so it fires immediately.
///      - Set Transition Duration to 0.
///
/// LEVEL PROGRESSION:
///   Set isLocked = true in the inspector (or via script) to prevent opening.
///   Unlock via gate.Unlock() when level progression allows it.
/// </summary>
public class Gate : MonoBehaviour
{
    public enum GateDirection { Top, Bottom, Left, Right }

    [Header("Gate Settings")]
    [Tooltip("Which cardinal direction this gate faces.")]
    public GateDirection gateDirection;

    [Tooltip("Prevent the gate from being opened until unlocked via script.")]
    public bool isLocked = false;

    [Header("References")]
    [Tooltip("Animator on this gate's sprite. Auto-found if left empty.")]
    public Animator animator;

    /// <summary>True once the open animation has been triggered.</summary>
    public bool IsOpen { get; private set; } = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Called by PlayerInteraction when the player presses the interact key
    /// while in range. Opens the gate if it is not locked and not already open.
    /// </summary>
    public void Interact()
    {
        if (IsOpen)
        {
            Debug.Log($"{gateDirection} Gate is already open.");
            return;
        }

        if (isLocked)
        {
            Debug.Log($"{gateDirection} Gate is locked.");
            return;
        }

        OpenGate();
    }

    /// <summary>Unlock this gate so the player is allowed to open it.</summary>
    public void Unlock()
    {
        isLocked = false;
        Debug.Log($"{gateDirection} Gate has been unlocked.");
    }

    /// <summary>Lock this gate again (e.g., for a reset).</summary>
    public void Lock()
    {
        isLocked = true;
    }

    private void OpenGate()
    {
        IsOpen = true;

        if (animator != null)
        {
            animator.SetTrigger("Open");
            Debug.Log($"{gateDirection} Gate is opening.");
        }
        else
        {
            Debug.LogWarning($"{gateDirection} Gate has no Animator assigned.");
        }
    }
}
