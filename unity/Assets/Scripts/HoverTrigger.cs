using UnityEngine;

public class HoverTrigger : MonoBehaviour
{
    public Animator animator; // ???? Animator
    public float hoverDuration = 3f;

    private float hoverTimer = 0f;
    private bool isHovering = false;
    private bool hasTriggered = false;

    private bool isUnlocked = false; // ????

    void Awake()
    {
        // ???? Animator?????????
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (!isHovering || hasTriggered)
            return;

        hoverTimer += Time.deltaTime;

        if (hoverTimer >= hoverDuration)
        {
            hasTriggered = true;

            if (animator != null)
            {
                animator.SetBool("PlayComplete", true);
            }

            // ?? ButtonsManager ?????
            ButtonsManager.Instance.NotifyButtonActivated();
        }
    }

    // ??????
    public void Unlock()
    {
        isUnlocked = true;
    }

    public void OnHoverEnter()
    {
        if (!isUnlocked || hasTriggered) return;

        isHovering = true;
        if (animator != null)
            animator.SetBool("Hovering", true);
        hoverTimer = 0f;
    }

    public void OnHoverExit()
    {
        if (hasTriggered) return;

        isHovering = false;
        if (animator != null)
            animator.SetBool("Hovering", false);
        hoverTimer = 0f;
    }
}