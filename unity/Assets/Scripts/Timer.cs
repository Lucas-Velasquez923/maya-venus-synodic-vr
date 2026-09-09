using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerScript : MonoBehaviour
{
    public float startTime = 60f;
    public float TimeLeft;
    public GameObject TimerCanvas;

    public TMP_Text TimerTxt;

    public Button StartButton;
    public GameObject UImenu;
    public TeleportController teleportController;
    public Transform startPoint;
    private bool started = false;

    
    
    
   
    void Start()
    {
        TimerCanvas.SetActive(false);
    }

    void Update()
{
    if (!started) return;

    if (TimeLeft > 0)
    {
        TimeLeft -= Time.deltaTime;
        TimerTxt.text = TimeLeft.ToString("0.00");
    }
    else
    {
        TimerTxt.text = "Time's Up!" + "\nRestarting...";
        started = false; 
        StartCoroutine(RestartDelay(3f));
    }
}
    private IEnumerator RestartDelay(float delay)
{

    yield return new WaitForSeconds(delay);
    TimerCanvas.SetActive(false);
    UImenu.SetActive(true);
    teleportController.TeleportTo(startPoint.position, startPoint.rotation);
}
    public void StartClicked()
{
    UImenu.SetActive(false);
    TimerCanvas.SetActive(true);
    TimeLeft = startTime;
    started = true; 
}

}