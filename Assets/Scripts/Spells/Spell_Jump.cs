using UnityEngine;
using System.Collections;

public class Spell_Jump : SpellBase
{
    private const float jumpDistance = 2f;   // How far to jump
    private const float jumpDuration = 0.3f;  // How long jump lasts

    public Spell_Jump()
    {
        spellName = "JUMP";
        GenerateMaskedSpell(NUMBER_MASKED_CHARS);
        spellType = SpellType.Mobility;
    }

    public override void Cast(GameObject player)
    {
        base.Cast(player);

        PlayerSpell spellComponent = player.GetComponent<PlayerSpell>();
        if (spellComponent != null)
        {
            spellComponent.StartCoroutine(
                JumpRoutine(player)
            );
        }

        Debug.Log("Jump spell cast!");
    }

    private IEnumerator JumpRoutine(GameObject player)
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (movement == null || rb == null)
            yield break;

        Vector2 direction = movement.GetLastMoveDirection();
        if (direction == Vector2.zero)
            yield break;

        movement.SetMovementEnabled(false);

        int playerLayer = player.layer;
        int hedgeLayer = LayerMask.NameToLayer("Obstacles");

        Physics2D.IgnoreLayerCollision(playerLayer, hedgeLayer, true);

        Vector2 startPos = rb.position;
        Vector2 targetPos = startPos + direction.normalized * jumpDistance;

        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;

            rb.MovePosition(Vector2.Lerp(startPos, targetPos, t));
            yield return new WaitForFixedUpdate(); // Important for physics consistency
        }

        rb.MovePosition(targetPos);

        Physics2D.IgnoreLayerCollision(playerLayer, hedgeLayer, false);

        movement.SetMovementEnabled(true);
    }
}