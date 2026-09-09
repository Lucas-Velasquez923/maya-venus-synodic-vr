using UnityEngine;

public class EnableWithInvoke : MonoBehaviour
{
    public MonoBehaviour scriptToEnable; // Drag the component directly here
    public float delay = 10f; // Time in seconds before enabling the script

    void Start()
    {
        Invoke("EnableTargetScript", delay);
    }

    void EnableTargetScript()
    {
        if (scriptToEnable != null)
        {
            scriptToEnable.enabled = true;
        }
    }
}
