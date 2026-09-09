using UnityEngine;

public class SnakeMiniGameManager : MonoBehaviour
{
    [Header("Player")]
    public TeleportController teleportController;
    public Transform playerStartPoint;

    [Header("Mini Game Instance")]
    public GameObject miniGameInstance;

    public void ResetMiniGame()
    {
        if (teleportController != null && playerStartPoint != null)
            teleportController.TeleportTo(playerStartPoint.position, playerStartPoint.rotation);

        if (miniGameInstance == null) return;

        SnakeTriggerArea triggerArea = miniGameInstance.GetComponentInChildren<SnakeTriggerArea>(true);
        if (triggerArea != null)
            triggerArea.ResetTriggerArea();
    }
}
