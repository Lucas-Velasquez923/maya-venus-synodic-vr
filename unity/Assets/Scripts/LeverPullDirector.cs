using UnityEngine;
using UnityEngine.SceneManagement;

public class LeverPullDetector : MonoBehaviour
{
    public string sceneToLoad;
    public float pullAngle = 60.0f;
    public float loadDelay = 1.0f;
    public float activationDelay = 1.0f;

    private bool hasTriggered = false;
    private bool isActive = false;
    private Quaternion startRotation;

    void Start()
    {
        Invoke(nameof(ActivateDetection), activationDelay);
    }

    void ActivateDetection()
    {
        startRotation = transform.localRotation;
        isActive = true;
    }

    void Update()
    {
        if (!isActive || hasTriggered) return;

        float angle = Quaternion.Angle(startRotation, transform.localRotation);

        if (angle >= pullAngle)
        {
            hasTriggered = true;
            Invoke(nameof(LoadNextScene), loadDelay);
        }
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}