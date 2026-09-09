using UnityEngine;
using UnityEngine.SceneManagement;

public class LeverSceneTrigger : MonoBehaviour
{
    public string sceneToLoad;
    public float loadDelay = 1.0f;
    public float activationDelay = 4.0f;

    private bool hasTriggered = false;
    private bool isActive = false;

    void Start()
    {
        Invoke(nameof(Activate), activationDelay);
    }

    void Activate()
    {
        isActive = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive || hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;
        Invoke(nameof(LoadNextScene), loadDelay);
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}