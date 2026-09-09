using UnityEngine;

public class EnableColliderOnAnimationEnd : MonoBehaviour
{
    public Collider targetCollider;

    void Start()
    {
        if (targetCollider != null)
            targetCollider.enabled = false; // ?????
    }

    // ?????????? A ??
    public void EnableCollider()
    {
        if (targetCollider != null)
        {
            targetCollider.enabled = true;
            Debug.Log("?????Collider ???");
        }
    }
    public void PlaySound()
    {
    GetComponent<AudioSource>().Play();
    }
}
