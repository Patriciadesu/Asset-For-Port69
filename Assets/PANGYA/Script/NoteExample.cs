using UnityEngine;
using UnityEngine.UI;

public class NoteExample : ObjectEffect,IInteractable
{
    public Sprite image;
    private GameObject canvas;
    public void Interact()
    {
        canvas = new GameObject();
        canvas.AddComponent<RectTransform>();
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        GameObject imageObject = new GameObject();
        imageObject.transform.SetParent(canvas.transform);
        RectTransform imageTransform = imageObject.AddComponent<RectTransform>();
        imageObject.AddComponent<CanvasRenderer>();
        imageTransform.anchorMin = Vector2.zero;
        imageTransform.anchorMax = Vector2.one;
        imageTransform.offsetMin = Vector2.zero;
        imageTransform.offsetMax = Vector2.zero;
        Image image = imageObject.AddComponent<Image>();
        image.sprite = image.sprite;
    }
}
