using UnityEngine;

public class LetterObject : MonoBehaviour
{
    public char letter { get; private set; }
    public SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("LetterObject: SpriteRenderer is missing on the GameObject.");
            }
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void SetLetter(char c)
    {
        letter = char.ToUpper(c);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = LetterSpriteDatabase.Instance.GetWorldSprite(letter);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }
            else Debug.LogWarning("LetterObject: AudioSource or clip missing.");

            gameObject.SetActive(false);
        }
    }
}
