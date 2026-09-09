using System.Collections.Generic;
using UnityEngine;

public class TriggerRevealAnimation : MonoBehaviour
{
    public List<Animator> targetAnimators;
    public string triggerName = "Reveal";

    public void PlayRevealAnimation()
    {
        if (targetAnimators == null || targetAnimators.Count == 0)
        {
            Debug.LogWarning("No animators assigned!");
            return;
        }

        foreach (Animator animator in targetAnimators)
        {
            if (animator != null)
            {
                animator.SetTrigger(triggerName);
            }
        }
    }
}