using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerSpell : MonoBehaviour
{
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void CastFireball(GameObject fireballPrefab, Transform firePoint)
    {
        Vector2 direction = playerMovement.GetLastMoveDirection();

        // Fallback safety (in case something strange happens)
        if (direction == Vector2.zero)
            direction = Vector2.down;

        GameObject fireball = Instantiate(
            fireballPrefab,
            firePoint.position,
            Quaternion.identity
        );

        Fireball fb = fireball.GetComponent<Fireball>();
        fb.Initialize(direction);
    }
}