using UnityEngine;

public class HeightBasedDeactivator : MonoBehaviour
{
    [Header("References")]
    public Transform xrOrigin;
    public GameObject[] objectsToDeactivate;

    [Header("Skybox Settings")]
    public Material newSkybox; 

    [Header("Settings")]
    public float deactivateHeight = 50f;
    public bool deactivateOnce = true;

    private bool hasDeactivated = false;

    void Update()
    {
        if (xrOrigin == null)
            return;


        if (!hasDeactivated && xrOrigin.position.y >= deactivateHeight)
        {

            if (objectsToDeactivate != null)
            {
                foreach (GameObject obj in objectsToDeactivate)
                {
                    if (obj != null)
                        obj.SetActive(false);
                }
            }


            if (newSkybox != null)
            {
                RenderSettings.skybox = newSkybox;

                DynamicGI.UpdateEnvironment();
            }

            if (deactivateOnce)
                hasDeactivated = true; 
        }
    }
}