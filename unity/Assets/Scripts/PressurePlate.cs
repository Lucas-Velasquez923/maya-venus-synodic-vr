using UnityEngine;

/// <summary>
/// Sinks into the floor when triggered. Attach to a flat plate GameObject
/// positioned at ground level. When StartSinking() is called, it moves
/// down by sinkDepth units and stays there.
///
/// SETUP:
///   1. Create a flat box/cylinder (the plate visual) at ground level
///   2. Add this script
///   3. Optionally assign a sinkSound AudioClip
///   4. In SnakeTriggerArea, drag this into the pressurePlate field
/// </summary>
public class PressurePlate : MonoBehaviour
{
    public float sinkDepth = 0.15f;
    public float sinkSpeed = 1f;

    [Header("Audio")]
    public AudioClip sinkSound;
    [Range(0f, 1f)] public float volume = 1f;

    private bool isSinking = false;
    private float targetY;
    private float startY;
    private AudioSource audioSource;

    void Start()
    {
        startY = transform.position.y;
        targetY = transform.position.y - sinkDepth;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && sinkSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    void Update()
    {
        if (!isSinking) return;

        Vector3 target = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, sinkSpeed * Time.deltaTime);

        if (transform.position.y <= targetY)
            isSinking = false;
    }

    public void StartSinking()
    {
        isSinking = true;
        if (audioSource != null && sinkSound != null)
            audioSource.PlayOneShot(sinkSound, volume);
    }

    public void ResetPlate()
    {
        isSinking = false;
        Vector3 pos = transform.position;
        pos.y = startY;
        transform.position = pos;
    }
}