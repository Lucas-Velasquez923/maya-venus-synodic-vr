using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SnakeMoveUp : MonoBehaviour
{
    public float moveSpeed = 2.0f;
    public float riseHeight = 5.0f;

    [Header("Audio")]
    public AudioClip snakeSound;
    public AudioClip riseSound;
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 1f;

    private bool isMovingUp = false;
    private float targetY;
    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private Vector3 startLocalScale;
    private Transform startParent;
    private XRGrabInteractable grabInteractable;

    void Start()
    {
        startParent = transform.parent;
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
        startLocalScale = transform.localScale;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        targetY = transform.position.y + riseHeight;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (riseSound != null || pickupSound != null || snakeSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnPickedUp);
        }
    }

    void OnPickedUp(SelectEnterEventArgs args)
    {
        if (audioSource != null && pickupSound != null)
            audioSource.PlayOneShot(pickupSound, volume);
    }

    void Update()
    {
        if (isMovingUp)
        {
            Vector3 targetPosition = new Vector3(transform.position.x, targetY, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (transform.position.y >= targetY)
            {
                isMovingUp = false;
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    public void StartMovingUp()
    {
        isMovingUp = true;
        if (audioSource != null && riseSound != null)
            audioSource.PlayOneShot(riseSound, volume);
        if (audioSource != null && snakeSound != null)
            audioSource.PlayOneShot(snakeSound, volume);
    }

    // Called by SnakeTriggerArea.ResetTriggerArea() while any socket that could be
    // holding this piece has already been disabled (which releases it cleanly and
    // clears the socket's own stale state - see SnakeTriggerArea for why).
    public void ResetPiece()
    {
        isMovingUp = false;

        // Belt-and-suspenders: drop any grab transformer (e.g. a socket's
        // snap-to-attach-point transformer) that might still be attached.
        if (grabInteractable != null)
            grabInteractable.ClearSingleGrabTransformers();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // XRGrabInteractable unparents an object on grab/select and is only
        // supposed to restore its original parent on a normal release. Our
        // forced release (disabling the socket) doesn't reliably go through
        // that path, so we restore the parent ourselves before reapplying the
        // local transform - otherwise the same local scale can produce a
        // different world-space size under the wrong parent.
        if (transform.parent != startParent)
            transform.SetParent(startParent, false);

        transform.localPosition = startLocalPosition;
        transform.localRotation = startLocalRotation;
        transform.localScale = startLocalScale;
        targetY = transform.position.y + riseHeight;

        // XRGrabInteractable caches its own private "target local scale" and
        // periodically reapplies it independent of the transformer system. If we
        // only change the Transform directly, that stale cached value (from
        // before the forced release) can overwrite our reset scale on a later
        // frame. SetTargetLocalScale is the documented way to override it.
        if (grabInteractable != null)
            grabInteractable.SetTargetLocalScale(startLocalScale);
    }
}