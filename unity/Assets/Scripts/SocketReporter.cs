using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketReporter : MonoBehaviour
{
    public PuzzleTimer puzzleTimer;

    [Header("Audio")]
    public AudioClip socketSound;
    [Range(0f, 1f)] public float volume = 1f;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    private AudioSource audioSource;

    void Start()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.selectEntered.AddListener(OnPieceSocketed);
        socket.selectExited.AddListener(OnPieceRemoved);

        // Set up AudioSource for socket placement sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && socketSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
    }

    void OnPieceSocketed(SelectEnterEventArgs args)
    {
        if (audioSource != null && socketSound != null)
            audioSource.PlayOneShot(socketSound, volume);

        if (puzzleTimer != null) puzzleTimer.SocketFilled();
    }

    void OnPieceRemoved(SelectExitEventArgs args)
    {
        if (puzzleTimer != null) puzzleTimer.SocketEmptied();
    }
}