using UnityEngine;

public class CourtyardTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var camFollow = FindFirstObjectByType<CameraFollow>();
            if (camFollow != null)
                camFollow.SetCourtyardState(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var camFollow = FindFirstObjectByType<CameraFollow>();
            if (camFollow != null)
                camFollow.SetCourtyardState(false);
        }
    }
}
