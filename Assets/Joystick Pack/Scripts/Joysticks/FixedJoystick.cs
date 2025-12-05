using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedJoystick : Joystick
{
    protected override void Start()
    {
        base.Start();
        // Ensure the joystick is visible from the start
        if (background != null && !background.gameObject.activeSelf)
        {
            background.gameObject.SetActive(true);
        }
    }
}
