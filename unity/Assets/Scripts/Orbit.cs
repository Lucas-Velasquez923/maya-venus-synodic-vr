using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform aroundBoday;
    public float orbitPeriod = 30f;
    public float gametimePerDay = 24f;

    void Update()
    {
        float deltaAngle = (360f/(gametimePerDay*orbitPeriod))*Time.deltaTime;
        transform.RotateAround(aroundBoday.position, aroundBoday.up, deltaAngle);
    }
}
