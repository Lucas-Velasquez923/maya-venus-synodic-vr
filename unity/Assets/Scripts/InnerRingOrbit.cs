using UnityEngine;
using UnityEngine.InputSystem;
public class InnerRingMover : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float orbitRadius = 0.9f;
    public float angularSpeedDeg = 30f;

    [Header("Self Rotation")]
    public Transform innerRing;
    public float selfSpinMultiplier = 1.1f;

    [Header("VR Control")]
    public InputActionProperty toggleSpinAction;

    private float currentAngleRad = 0f;
    private bool spinning = false;
    float spinTimer = 0f;
    public float spinDuration = 4f;
    bool isSpinning = true;
    public GameObject nextObject;
    public GameObject currentObject;


    private void OnEnable()
    {
        if (toggleSpinAction != null && toggleSpinAction.action != null)
            toggleSpinAction.action.Enable();
    }

    private void OnDisable()
    {
        if (toggleSpinAction != null && toggleSpinAction.action != null)
            toggleSpinAction.action.Disable();
    }

    void Update()
    {
        

        if (!spinning)
            return;
        spinTimer += Time.deltaTime;
        if (spinTimer >= spinDuration)
    {
        spinning = false;
        if (currentObject != null)
            currentObject.SetActive(false);

        // ✅ APPEAR NEW (same position/rotation)
        if (nextObject != null && currentObject != null)
        {
            nextObject.transform.position = currentObject.transform.position;
            nextObject.transform.rotation = currentObject.transform.rotation;
            nextObject.SetActive(true);
        }
        return;
    }
        currentAngleRad += angularSpeedDeg * Mathf.Deg2Rad * Time.deltaTime;

        float x = Mathf.Cos(currentAngleRad) * orbitRadius;
        float y = Mathf.Sin(currentAngleRad) * orbitRadius;
        transform.localPosition = new Vector3(x, y, 0f);
        spinTimer += Time.deltaTime;
        if (spinTimer >= 30f)
        {
            isSpinning = false;
        }
        if (innerRing != null)
        {
            float selfSpinDeg = -angularSpeedDeg * selfSpinMultiplier * Time.deltaTime;
            innerRing.Rotate(0f, 0f, selfSpinDeg, Space.Self);
        }
    }
    public void StartSpin() => spinning = true;
    public void StopSpin() => spinning = false;
}