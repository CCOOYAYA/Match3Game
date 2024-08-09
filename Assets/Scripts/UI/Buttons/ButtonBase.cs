using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;

public abstract class ButtonBase : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,IPointerClickHandler
{
    [SerializeField] protected bool SimpleZoomIn = true;

    protected static int lockLevel = 0;

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        ClearTween();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (SimpleZoomIn)
            ZoomInTween();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        ClearTween();
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        ClearTween();
    }

    protected virtual void ZoomInTween()
    {
        transform.DOScale(Vector3.one * 0.92f, 0.1f);
    }

    protected virtual void ClearTween()
    {
        if (SimpleZoomIn)
        {
            transform.DOComplete();
            transform.localScale = Vector3.one;
        }
    }

    public static void Lock()
    {
        lockLevel++;
    }

    public static void Unlock()
    {
        lockLevel--;
        if (lockLevel < 0)
            lockLevel = 0;
    }

    public static void ResetLockLevel()
    {
        lockLevel = 0;
    }
}
