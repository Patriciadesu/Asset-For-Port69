using UnityEngine;

public class ForBox : ObjectScriptTointeract
{
    // You must override the abstract Interact method from the base class.
    public override void Interact()
    {
        // Add your interaction logic here.
        // For example, print a message to the console.
        Debug.Log("You have interacted with the box!");

        // Or you could make the box disappear.
        // Destroy(gameObject);
    }
}