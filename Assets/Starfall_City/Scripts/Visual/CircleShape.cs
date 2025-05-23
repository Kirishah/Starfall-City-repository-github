using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Mask))]
public class CircleShape : Button, ICanvasRaycastFilter
{
    private Mask _mask;
    private RectTransform _rectTransform;

    protected override void Awake()
    {
        base.Awake();
        _rectTransform = GetComponent<RectTransform>();
        _mask = GetComponent<Mask>();
        if (_mask != null)
            _mask.showMaskGraphic = false;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            screenPoint,
            eventCamera,
            out Vector2 localPoint))
            return false;

        // Circle equation: (x^2 + y^2) <= r^2
        float radius = _rectTransform.rect.width / 2;
        return localPoint.sqrMagnitude <= radius * radius;
    }
}
