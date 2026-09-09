using UnityEngine;

public class BushSFX : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] bushClips;

    [Range(0f, 1f)]
    public float volume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (audioSource == null || bushClips.Length == 0) return;

        int index = Random.Range(0, bushClips.Length);

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(bushClips[index], volume);
    }
}