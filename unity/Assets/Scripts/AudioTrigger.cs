using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
    }

    // Update is called once per frame
    void OnTriggerEnter(Collider other)
    {
        audioSource.Play();
    }
}
