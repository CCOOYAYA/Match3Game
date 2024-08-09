using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameSettingPopupDisplayer : PopupPage
{
    [SerializeField] private GameObject subButtonGroup;
    [SerializeField] private Canvas buttonCanvas;

    private void UpdateDisplay()
    {
        buttonCanvas.overrideSorting = true;
        buttonCanvas.sortingLayerName = "PopupUICanvas";
        buttonCanvas.sortingOrder = 200;
        subButtonGroup.SetActive(true);
    }

    public override void ShowMe()
    {
        UpdateDisplay();
        base.ShowMe();
    }

    public override async UniTask HideMe()
    {
        await base.HideMe();
        buttonCanvas.overrideSorting = false;
        subButtonGroup.SetActive(false);
    }

    public void SetMusic(bool value)
    {
        UserDataManager.SetMusic(value);
    }

    public void SetSound(bool value)
    {
        UserDataManager.SetSound(value);
    }

    public void SetVibration(bool value)
    {
        UserDataManager.SetVibration(value);
    }

    public void SetHint(bool value)
    {
        UserDataManager.SetHint(value);
    }
}
