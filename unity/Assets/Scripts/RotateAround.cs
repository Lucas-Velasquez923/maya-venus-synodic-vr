using UnityEngine;

public class RotateAround : MonoBehaviour
{

    public Transform target;
    public float orbitSpeed = 29f;
    
    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            transform.RotateAround(target.position, Vector3.up, orbitSpeed * Time.deltaTime);
        }
    }
}
