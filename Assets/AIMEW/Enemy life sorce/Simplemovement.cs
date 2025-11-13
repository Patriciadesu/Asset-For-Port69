using UnityEngine;

public class Simplemovement : MonoBehaviour
{
    public float runspeed = 50f;
    public float walkspeed = 20f;
    public float speed;

    void Start()
    {
        speed = walkspeed;
    }

    
 void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Get camera's forward and right vectors, ignoring y axis
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camRight = Camera.main.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        // Move relative to camera direction
        Vector3 movement = camForward * moveVertical + camRight * moveHorizontal;
        transform.Translate(movement * speed * Time.deltaTime, Space.World);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = runspeed;
        }
        else
        {
            speed = walkspeed;
        }
    }
}
