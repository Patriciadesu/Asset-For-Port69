using UnityEngine;

public class InvisibleEffect : ObjectEffect
{
    private Renderer objectRenderer;

    public override void ApplyEffect(Collision playerCollision)
    {
        // Invisible effect doesn't need player interaction, it's invisible from start
    }
    
    public override void ApplyEffect(Collision playerCollision, Player player)
    {
        // Invisible effect doesn't need player interaction, it's invisible from start
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
