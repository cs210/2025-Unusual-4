using UnityEngine;

public class RollingBall : MonoBehaviour
{
    public float speed = 5f;
    
    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontalInput, 0, verticalInput);
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
