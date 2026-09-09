using UnityEngine;

public class Spin : MonoBehaviour
{
    public float gametimePerDay = 24.0f;

    void Update()
    {
        float deltaAngle = (360/gametimePerDay)* Time.deltaTime;
        transform.Rotate(0f,deltaAngle,0f);
    }
}
