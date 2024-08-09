using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TextTweenButton : SimpleButton
{
    [SerializeField] Transform textTransform;

    protected override void ZoomInTween()
    {
        textTransform.DOScale(Vector3.one * 0.92f, 0.1f);
    }

    protected override void ClearTween()
    {
        textTransform.DOComplete();
        textTransform.localScale = Vector3.one;
    }
}
