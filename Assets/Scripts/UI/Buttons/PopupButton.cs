using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PopupButton : ButtonBase
{
    [SerializeField] PopupPage targetPage;
    [SerializeField] bool jumpTo = false;
    [SerializeField] bool overlap = false;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (0 < lockLevel)
            return;
        PopupManager.ShowPageAsync(targetPage, jumpTo, overlap).Forget();
    }
}
