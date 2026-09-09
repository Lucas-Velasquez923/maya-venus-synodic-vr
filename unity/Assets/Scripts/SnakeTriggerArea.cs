using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SnakeTriggerArea : MonoBehaviour
{
    public SnakeMoveUp[] snakePieces;
    public GameObject uiCanvas;
    public float canvasDelay = 3.0f;

    [Header("Pressure Plate")]
    public PressurePlate pressurePlate;

    [Header("Puzzle Timer")]
    public PuzzleTimer puzzleTimer;

    [Header("Audio")]
    public AudioClip triggerSound;
    [Range(0f, 1f)] public float volume = 1f;

    private bool hasTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && triggerSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        if (audioSource != null && triggerSound != null)
            audioSource.PlayOneShot(triggerSound, volume);

        // Sink the pressure plate
        if (pressurePlate != null) pressurePlate.StartSinking();

        // Raise the snake pieces
        for (int i = 0; i < snakePieces.Length; i++)
        {
            if (snakePieces[i] != null) snakePieces[i].StartMovingUp();
        }

        StartCoroutine(ShowCanvasAfterDelay());
    }

    IEnumerator ShowCanvasAfterDelay()
    {
        yield return new WaitForSeconds(canvasDelay);
        if (uiCanvas != null) uiCanvas.SetActive(true);
    }

    public void ResetTriggerArea()
    {
        StopAllCoroutines();
        hasTriggered = false;

        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (pressurePlate != null) pressurePlate.ResetPlate();

        // A socket that has an object in it can still think it's "touching" that
        // object after the object is moved away without a normal physical exit
        // (documented XR Interaction Toolkit trigger-contact caching issue), and
        // will immediately reclaim it the moment it becomes selectable again.
        // Disabling each socket forces its OnDisable to release whatever it's
        // holding and clear that cache; re-enabling afterward makes it rescan
        // fresh, by which point every piece has already been moved away.
        // Search from the FullSnakeMiniGame root, not this object's own subtree -
        // the sockets live on separate branches of the hierarchy, not underneath
        // SnakeTriggerArea itself.
        XRSocketInteractor[] sockets = transform.root.GetComponentsInChildren<XRSocketInteractor>(true);
        foreach (XRSocketInteractor socket in sockets)
            socket.enabled = false;

        for (int i = 0; i < snakePieces.Length; i++)
        {
            if (snakePieces[i] != null) snakePieces[i].ResetPiece();
        }

        foreach (XRSocketInteractor socket in sockets)
            socket.enabled = true;

        if (puzzleTimer != null) puzzleTimer.ResetPuzzle();
    }
}