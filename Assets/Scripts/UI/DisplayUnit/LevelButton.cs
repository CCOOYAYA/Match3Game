using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

public class LevelButton : PopupButton
{
    [SerializeField] TextMeshProUGUI levelButtonText;
    [SerializeField] LocalizedString l_Level;
    [SerializeField] Vector2[] textPos;
    [SerializeField] TextMeshProUGUI difficultyText;
    [SerializeField] LocalizedString l_Hard;
    [SerializeField] LocalizedString l_SuperHard;
    [SerializeField] ImageSwitcher imageSwitcher;

    public void UpdateDisplay()
    {
        levelButtonText.text = l_Level.GetLocalizedString(UserDataManager.LevelID);
        switch (UserDataManager.GameLevel.levelType)
        {
            case 2:
                levelButtonText.transform.localPosition = textPos[1];
                difficultyText.text = l_SuperHard.GetLocalizedString();
                difficultyText.gameObject.SetActive(true);
                break;
            case 1:
                levelButtonText.transform.localPosition = textPos[1];
                difficultyText.text = l_Hard.GetLocalizedString();
                difficultyText.gameObject.SetActive(true);
                break;
            default:
                levelButtonText.transform.localPosition = textPos[0];
                difficultyText.gameObject.SetActive(false);
                break;
        }
        imageSwitcher.SetDifficultyLevel();
    }
}
