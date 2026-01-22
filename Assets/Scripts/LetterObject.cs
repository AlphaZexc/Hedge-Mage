// using UnityEngine;

// public class LetterObject : MonoBehaviour
// {
    
//     public char letter;
//     public SpriteRenderer spriteRenderer;
//     private AudioSource audioSource; 

//     private void Awake()
//     {
//         if (spriteRenderer == null)
//         {
//             spriteRenderer = GetComponent<SpriteRenderer>();
//             if (spriteRenderer == null)
//             {
//                 Debug.LogError("LetterObject: SpriteRenderer is missing on the GameObject.");
//             }
//         }

//         audioSource = GetComponent<AudioSource>();
//     }


//     public void SetLetter(char c)
//     {
//         letter = char.ToUpper(c);

//         if (spriteRenderer != null)
//         {
//             // Use world sprite only (new third set)
//             spriteRenderer.sprite = LetterSpriteDatabase.Instance.GetWorldSprite(letter);
//         }
//     }

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             // Play sound
//             if (audioSource != null && audioSource.clip != null)
//             {
//                 audioSource.Play();
//             }
            
//             if (WordProgressManager.Instance != null)
//             {
//                 WordProgressManager.Instance.CollectLetter(letter);
//             }

//             Debug.Log($"Letter '{letter}' collected by player at position {transform.position}.");

//             // Hide letter instead of destroying it, if reuse is possible
//             // gameObject.SetActive(false);

//             Destroy(gameObject, 0.5f);
//         }
//     }
// }

using UnityEngine;

public class LetterObject : MonoBehaviour
{
    public char letter;
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
            else
            {
                Debug.LogWarning("LetterObject: AudioSource or clip missing.");
            }

            if (WordProgressManager.Instance != null)
            {
                WordProgressManager.Instance.CollectLetter(letter);
            }

            Debug.Log($"Letter '{letter}' collected by player at position {transform.position}.");

            // Delay destroy to let sound play
            Destroy(gameObject, 0.5f);
        }
    }
}
