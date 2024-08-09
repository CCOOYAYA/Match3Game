using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Events;

public class SettingSlider : ButtonBase
{
    [SerializeField] private UnityEvent<bool> switchEvent;
    [SerializeField] private float tweenTime = 0.2f;
    [SerializeField] private Transform lPos;
    [SerializeField] private Transform rPos;
    [SerializeField] private Transform slider;

    private bool isOn = true;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        isOn = !isOn;
        TweenSlider();
        switchEvent.Invoke(isOn);
    }

    private void TweenSlider()
    {
        if (isOn)
            slider.DOLocalMove(rPos.localPosition, tweenTime);
        else
            slider.DOLocalMove(lPos.localPosition, tweenTime);
    }

    public void SetValue(bool value)
    {
        isOn = value;
        if (isOn)
            slider.localPosition = rPos.localPosition;
        else
            slider.localPosition = lPos.localPosition;
    }
}
