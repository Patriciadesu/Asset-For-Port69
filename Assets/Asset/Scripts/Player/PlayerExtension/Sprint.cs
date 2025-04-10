using UnityEngine;

public class Sprint : PlayerExtension
{
    public KeyCode activateKey = KeyCode.LeftShift;
    public float sprintSpeed = 8f;

    public void Update()
    {
        if (Input.GetKeyDown(activateKey))
        {
            _player.additionalSpeed += sprintSpeed;
            _player.animator.SetBool("isRunning", true);
        }
        if(Input.GetKeyUp(activateKey)) 
        {
            _player.additionalSpeed -= sprintSpeed;
            _player.animator.SetBool("isRunning", false);
        }
    }
}
