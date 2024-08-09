using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class LevelBanner : AssetSwitcher
{
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] LocalizedString l_Level;

    public override void SetDifficultyLevel(DifficultyLevel difficulty)
    {
        levelText.text = l_Level.GetLocalizedString(UserDataManager.LevelID);
    }
}
