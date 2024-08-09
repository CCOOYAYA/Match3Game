using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Game_SettingButton : ButtonBase
{
    [SerializeField] private PopupPage targetPage;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (0 < lockLevel)
            return;
        if (targetPage.gameObject.activeSelf)
            PopupManager.CloseCurrentPageAsync().Forget();
        else
            PopupManager.ShowPageAsync(targetPage).Forget();
    }
}
