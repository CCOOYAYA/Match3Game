using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PopupCloseButton : ButtonBase
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (0 < lockLevel)
            return;
        PopupManager.CloseCurrentPageAsync().Forget();
    }
}
