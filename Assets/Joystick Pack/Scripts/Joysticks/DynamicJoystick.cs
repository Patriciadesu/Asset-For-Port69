using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DynamicJoystick : Joystick
{
    public float MoveThreshold { get { return moveThreshold; } set { moveThreshold = Mathf.Abs(value); } }

    [SerializeField] private float moveThreshold = 1;

    protected override void Start()
    {
        MoveThreshold = moveThreshold;
        base.Start();
        
        EnsureRaycastTargets();
        
        if (background != null)
        {
            background.gameObject.SetActive(false);
        }
    }

    private void EnsureRaycastTargets()
    {
        RectTransform baseRect = GetComponent<RectTransform>();
        
        if (background != null)
        {
            Image bgImage = background.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.raycastTarget = true;
            }
            else
            {
                bgImage = background.gameObject.AddComponent<Image>();
                bgImage.color = new Color(1, 1, 1, 0.5f);
                bgImage.raycastTarget = true;
            }
        }
        
        Image baseImage = GetComponent<Image>();
        if (baseImage == null)
        {
            baseImage = gameObject.AddComponent<Image>();
            baseImage.color = new Color(0, 0, 0, 0);
        }
        baseImage.raycastTarget = true;
        
        if (baseRect != null && baseRect.sizeDelta.x < 200f)
        {
            baseRect.sizeDelta = new Vector2(200f, 200f);
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (background != null)
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            background.gameObject.SetActive(true);
            
            Vector2 center = new Vector2(0.5f, 0.5f);
            background.pivot = center;
        }
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (background != null)
        {
            background.gameObject.SetActive(false);
        }
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (magnitude > moveThreshold && background != null)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius;
            background.anchoredPosition += difference;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }
}