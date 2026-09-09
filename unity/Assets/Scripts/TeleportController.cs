using UnityEngine;


 
public class TeleportController : MonoBehaviour

{

    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider;
 
    void Start()

    {

        teleportationProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();

    }
 
    public void TeleportTo(Vector3 destination, Quaternion rotation)

    {

        var request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest()

        {

            destinationPosition = destination,

            destinationRotation = rotation,

            matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward

        };
 
        teleportationProvider.QueueTeleportRequest(request);

    }

}
 