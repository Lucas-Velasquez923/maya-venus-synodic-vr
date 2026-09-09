using System.Collections;
using UnityEngine;

public class VoiceoverManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] clips;

    [Header("Settings")]
    public float gapBetweenClips = 0.5f;
    public bool playOnStart = true;

    void Start()
    {
        if (playOnStart && clips.Length > 0)
            StartCoroutine(PlayClipsInOrder());
    }

    public IEnumerator PlayClipsInOrder()
    {
        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;

            audioSource.clip = clip;
            audioSource.Play();

            yield return new WaitForSeconds(clip.length + gapBetweenClips);
        }
    }

    // Call this from another script or UI button to trigger playback manually
    public void StartVoiceover()
    {
        StopAllCoroutines();
        StartCoroutine(PlayClipsInOrder());
    }
}
