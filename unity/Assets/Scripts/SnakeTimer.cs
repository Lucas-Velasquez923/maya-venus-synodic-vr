using System.Collections;
using UnityEngine;
using TMPro;

public class PuzzleTimer : MonoBehaviour
{
    public GameObject instructionsCanvas;
    public GameObject timerCanvas;
    public GameObject timesUpCanvas;
    public GameObject snakePiecesParent;
    public GameObject completeSnake;
    public RewardRiseUp pedestalRewardObject;
    public RewardRiseUp leverRewardObject;
    public TextMeshProUGUI timerText;
    public float timeLimit = 60.0f;
    public int totalSockets = 5;
    public float completionDelay = 0.5f;

    [Header("Reset")]
    public SnakeMiniGameManager miniGameManager;

    [Header("Audio")]
    public AudioClip puzzleCompleteSound;
    [Range(0f, 1f)] public float completeVolume = 1f;

    private float timeRemaining;
    private bool timerRunning = false;
    private int filledSockets = 0;
    private AudioSource audioSource;

    void Start()
    {
        if (timerCanvas != null) timerCanvas.SetActive(false);
        if (timesUpCanvas != null) timesUpCanvas.SetActive(false);
        if (completeSnake != null) completeSnake.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && puzzleCompleteSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            timerRunning = false;
            OnTimeUp();
        }

        UpdateTimerDisplay();
    }

    public void StartPuzzle()
    {
        if (instructionsCanvas != null) instructionsCanvas.SetActive(false);
        if (timerCanvas != null) timerCanvas.SetActive(true);

        timeRemaining = timeLimit;
        timerRunning = true;
        filledSockets = 0;
        UpdateTimerDisplay();
    }

    public void SocketFilled()
    {
        if (!timerRunning) return;

        filledSockets++;

        if (filledSockets >= totalSockets)
        {
            OnPuzzleComplete();
        }
    }

    public void SocketEmptied()
    {
        if (!timerRunning) return;

        filledSockets--;
        if (filledSockets < 0) filledSockets = 0;
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    void OnTimeUp()
    {
        if (timerCanvas != null) timerCanvas.SetActive(false);
        if (timesUpCanvas != null) timesUpCanvas.SetActive(true);
    }

    void OnPuzzleComplete()
    {
        timerRunning = false;

        if (audioSource != null && puzzleCompleteSound != null)
            audioSource.PlayOneShot(puzzleCompleteSound, completeVolume);

        StartCoroutine(CompletePuzzleSequence());
    }

    IEnumerator CompletePuzzleSequence()
    {
        yield return new WaitForSeconds(completionDelay);

        if (timerCanvas != null) timerCanvas.SetActive(false);
        if (snakePiecesParent != null) snakePiecesParent.SetActive(false);
        if (completeSnake != null) completeSnake.SetActive(true);

        if (pedestalRewardObject != null) pedestalRewardObject.StartMovingUp();
        if (leverRewardObject != null) leverRewardObject.StartMovingUp();
    }

    public void RestartGame()
    {
        if (miniGameManager != null)
            miniGameManager.ResetMiniGame();
    }

    public void ResetPuzzle()
    {
        StopAllCoroutines();

        timerRunning = false;
        filledSockets = 0;
        timeRemaining = 0;

        if (timerCanvas != null) timerCanvas.SetActive(false);
        if (timesUpCanvas != null) timesUpCanvas.SetActive(false);
        if (completeSnake != null) completeSnake.SetActive(false);
        if (snakePiecesParent != null) snakePiecesParent.SetActive(true);
    }
}