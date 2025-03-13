using UnityEngine;

public class RobotArmRotation : MonoBehaviour
{
    public GameObject shoulder;
    public GameObject elbow;
    public GameObject wrist1;
    public GameObject wrist2;
    public GameObject wrist3;

    void Start()
    {
        RotateArmParts();
    }

    void RotateArmParts()
    {
        RotatePart(shoulder);
        RotatePart(elbow);
        RotatePart(wrist1);
        RotatePart(wrist2);
        RotatePart(wrist3);
    }

    void RotatePart(GameObject part)
    {
        if (part != null)
        {
            part.transform.Rotate(new Vector3(0, 90, 0));
        }
    }
}
