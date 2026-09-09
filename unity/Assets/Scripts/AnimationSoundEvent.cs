using UnityEngine;

public class AnimationSoundEvent : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clip;

    public void PlayAnimationSound()
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
