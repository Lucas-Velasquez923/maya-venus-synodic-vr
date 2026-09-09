using UnityEngine;

// Builds a ring (circle outline) out of small blocks, in the XY plane.
[ExecuteInEditMode]
public class RingBuilder : MonoBehaviour
{
    public GameObject segmentPrefab;     // cube prefab
    public int segmentCount = 32;        // more segments = smoother circle
    public float radius = 1.5f;          // distance from center to each cube
    public Vector3 segmentScale = new Vector3(0.1f, 0.1f, 0.1f);

    public void BuildRing()
    {
        if (segmentPrefab == null)
        {
            Debug.LogWarning("RingBuilder: no segmentPrefab on " + name, this);
            return;
        }

        // delete previous children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // place blocks in a circle in XY (vertical ring)
        for (int i = 0; i < segmentCount; i++)
        {
            float angleDeg = i * 360f / segmentCount;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * radius;
            float y = Mathf.Sin(angleRad) * radius;
            Vector3 localPos = new Vector3(x, y, 0f);

            GameObject seg = Instantiate(segmentPrefab, transform);
            seg.transform.localPosition = localPos;
            seg.transform.localRotation = Quaternion.identity;
            seg.transform.localScale = segmentScale;
        }
    }

    [ContextMenu("Build Ring Now")]
    private void BuildRingNow()
    {
        BuildRing();
    }
}
