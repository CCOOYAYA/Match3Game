using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewAreaPopupDisplayer : PopupPage
{
    [SerializeField] Image newAreaImage;
    [SerializeField] TextMeshProUGUI newAreaName;
    [SerializeField] AreaInfoListSO areaInfoSO;

    private void UpdateDisplay()
    {
        int newareaID = Math.Min(UserDataManager.CurrentSceneID + 1, areaInfoSO.areaInfos.Count) - 1;
        newAreaImage.sprite = areaInfoSO.areaInfos[newareaID].emptySprite;
        newAreaName.text = areaInfoSO.areaInfos[newareaID].name.GetLocalizedString();
    }

    public override void ShowMe()
    {
        UpdateDisplay();
        base.ShowMe();
    }
}
