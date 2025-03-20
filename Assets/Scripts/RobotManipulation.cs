using UnityEngine;

public class RobotManipulation : MonoBehaviour
{
    public GameObject ur3; // Assign the UR3 game object in the Inspector

    void Start()
    {
        if (ur3 != null)
        {
            Transform shoulder = ur3.transform.Find("Shoulder");
            if (shoulder != null)
            {
                shoulder.Rotate(Vector3.right, 90f); // Rotate the shoulder 90 degrees down along the X-axis
            }
            else
            {
                Debug.LogWarning("Shoulder not found under UR3 object.");
            }
        }
        else
        {
            Debug.LogWarning("UR3 object not assigned.");
        }
    }
}
