using UnityEngine;

public class RewardRiseUp : MonoBehaviour
{
    public float moveSpeed = 0.5f;
    public float riseHeight = 0.5f;

    [Header("Audio")]
    public AudioClip riseSound;
    [Range(0f, 1f)] public float volume = 1f;

    private bool isMovingUp = false;
    private float targetY;
    private Rigidbody rb;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        targetY = transform.position.y + riseHeight;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && riseSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    void Update()
    {
        if (!isMovingUp) return;

        Vector3 targetPosition = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (transform.position.y >= targetY)
            isMovingUp = false;
    }

    public void StartMovingUp()
    {
        isMovingUp = true;
        if (audioSource != null && riseSound != null)
            audioSource.PlayOneShot(riseSound, volume);
    }
}