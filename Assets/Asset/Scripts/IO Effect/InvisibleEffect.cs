using UnityEngine;

public class InvisibleEffect : ObjectEffect
{
    private Renderer objectRenderer;

    public override void ApplyEffect(Collision playerCollision)
    {
        
    }

    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            objectRenderer.enabled = false;
            Debug.Log($"{gameObject.name} is now invisible");
        }
    }
}
