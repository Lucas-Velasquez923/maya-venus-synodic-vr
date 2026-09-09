using UnityEngine;
using UnityEngine.SceneManagement;

public class TempleAnimationController : MonoBehaviour
{
    public Animator reedsAnimator;
    public HoverTrigger[] buttonTriggers; // Inspector ??????? HoverTrigger ????

    public void OnTempleAnimFinished()
    {
        if (reedsAnimator != null)
            reedsAnimator.SetTrigger("Reveal");
    }

    public void OnMuralRevealFinished()
    {
        // ????????
        ButtonsManager.Instance.StartSequence(buttonTriggers);
    }
}