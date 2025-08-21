using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flashlight : PlayerExtension
{
    public GameObject on;
    public GameObject off;
    private bool isOn;


    // Start is called before the first frame update
    void Start()
    {
        on.SetActive(false);
        off.SetActive(true);
        isOn = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isOn = !isOn;
        }

        if (isOn)
        {
            on.SetActive(true);
            off.SetActive(false);
        }
        else
        {
            on.SetActive(false);
            off.SetActive(true);
        }
    }
}