using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectSyncher : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] ScrollRect mainScrollRect;
    [SerializeField] ScrollRect subScrollRect;
    [SerializeField] UnityEvent dragEvent;

    public static bool locked = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        float dragAngle = Vector2.Angle(eventData.delta, Vector2.up);
        if ((45f < dragAngle) && (dragAngle < 135f))
        {
            mainScrollRect.enabled = true;
            subScrollRect.enabled = false;
        }
        else
        {
            mainScrollRect.enabled = false;
            subScrollRect.enabled = true;
        }
        mainScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragEvent?.Invoke();
        mainScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragEvent?.Invoke();
        mainScrollRect.OnEndDrag(eventData);
        mainScrollRect.enabled = true;
        subScrollRect.enabled = true;
    }
}
