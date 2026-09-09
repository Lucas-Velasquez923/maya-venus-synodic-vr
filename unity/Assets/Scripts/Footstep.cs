using UnityEngine;
using UnityEngine.InputSystem; // ?????

public class Footstep : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] footstepClips;

    public InputActionProperty moveInput; 

    public float stepInterval = 0.5f;
    public float inputThreshold = 0.1f;

    private float timer;
    private int lastIndex = -1;

    void Update()
    {
        Vector2 input = moveInput.action.ReadValue<Vector2>();

        
        if (input.magnitude < inputThreshold)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= stepInterval)
        {
            PlayFootstep();
            timer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (audioSource == null || footstepClips.Length == 0) return;

        int index;

        // ??????
        do
        {
            index = Random.Range(0, footstepClips.Length);
        } while (index == lastIndex && footstepClips.Length > 1);

        lastIndex = index;

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(footstepClips[index]);
    }
}