using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SimpleButton : ButtonBase
{
    [SerializeField] public UnityEvent clickEvent;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (0 < lockLevel)
            return;
        clickEvent.Invoke();
    }
}
