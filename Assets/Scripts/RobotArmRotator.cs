using UnityEngine;

public class RobotArmRotator : MonoBehaviour
{
    public ArticulationBody shoulderJoint;

    void Start()
    {
        RotateShoulder90Degrees();
    }

    void RotateShoulder90Degrees()
    {
        // Get current drive of the articulation body
        ArticulationDrive drive = shoulderJoint.xDrive;

        // Calculate the new target angle (current angle + 90 degrees)
        float newTarget = drive.target + 90f;

        // Ensure it remains within the joint's limits
        newTarget = Mathf.Clamp(newTarget, drive.lowerLimit, drive.upperLimit);

        // Set the new target
        drive.target = newTarget;

        // Apply the updated drive back to the articulation body
        shoulderJoint.xDrive = drive;
    }
}
