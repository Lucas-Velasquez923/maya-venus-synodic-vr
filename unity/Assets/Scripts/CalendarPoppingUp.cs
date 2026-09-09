using System.Collections;
using UnityEngine;

public class CalendarPopUpOnEnter : MonoBehaviour
{
    [Header("References")]
    public Transform calendarRoot;     
    public Transform popUpTarget;    
    public GameObject newObjectPrefab;
    
      

    [Header("Motion")]
    public float popUpDuration = 0.8f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    bool hasPopped = false;

    [Header("Trigger Filtering")]
    public string xrRigTag = "Player"; // tag XR Origin as Player

    Vector3 hiddenPos;
    Vector3 shownPos;
    Coroutine anim;

    void Awake()
    {
        if (calendarRoot == null || popUpTarget == null) return;

        hiddenPos = calendarRoot.position;   // current (underground) position
        shownPos = popUpTarget.position;     // final (above ground) position
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasPopped) return;
    if (!other.CompareTag(xrRigTag)) return;

    hasPopped = true;
    StartPop(true);
    }

    void OnTriggerExit(Collider other)
    {
        
    }

    void StartPop(bool show)
    {
        if (calendarRoot == null) return;

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateTo(show ? shownPos : hiddenPos));
    }

    IEnumerator AnimateTo(Vector3 target)
    {
        Vector3 start = calendarRoot.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, popUpDuration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            calendarRoot.position = Vector3.Lerp(start, target, k);
            yield return null;
        }

        calendarRoot.position = target;
    }
}