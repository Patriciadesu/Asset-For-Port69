using UnityEngine;

public class PressEToST : PlayerExtension
{
    public Transform InteractorSource;
    public float InteractRange;

    void Update()
    {
        // Create a ray that starts at the InteractorSource and points forward.
        Ray r = new Ray(InteractorSource.position, InteractorSource.forward);

        // Perform a raycast. If it hits an object within InteractRange AND that object
        // has a component implementing IInteractable, the condition is true.
        if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange) && hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactObj))
        {
            // Check if the player presses the 'E' key.
            if (Input.GetKeyDown(KeyCode.E))
            {
                interactObj.Interact();
            }
        }
    }
}