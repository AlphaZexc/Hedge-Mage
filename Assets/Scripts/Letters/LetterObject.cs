using UnityEngine;

public class LetterObject : MonoBehaviour
{
    public char letter { get; private set; }
    public SpriteRenderer spriteRenderer;

    [Header("Pickup Sound")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField][Range(0f, 1f)] private float pickupVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                Debug.LogError("LetterObject: SpriteRenderer is missing on the GameObject.");
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void SetLetter(char c)
    {
        letter = char.ToUpper(c);
        if (spriteRenderer != null)
            spriteRenderer.sprite = LetterSpriteDatabase.Instance.GetWorldSprite(letter);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayPickupSound();
            gameObject.SetActive(false);
        }
    }

    private void PlayPickupSound()
    {
        if (pickupClip == null) return;

        // Spawn a dedicated temporary GameObject to own the AudioSource
        GameObject soundObj = new GameObject("PickupSound");
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = pickupClip;
        source.spatialBlend = 0f;   // full 2D, no distance falloff
        source.volume = pickupVolume;
        source.Play();

        Destroy(soundObj, pickupClip.length + 0.1f);
    }
}