using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartPopupDisplayer : PopupPage, IUpdatePerSecond
{
    [SerializeField] int streakBonusUnlockLevel = 2;
    [SerializeField] int startItemUnlockLevel = 2;
    [SerializeField] RectTransform labelTransform;
    [SerializeField] Vector2[] labelSize;
    [SerializeField] GameObject difficultyArea;
    [SerializeField] TextMeshProUGUI difficultyText;
    [SerializeField] LocalizedString l_Hard;
    [SerializeField] LocalizedString l_SuperHard;
    [SerializeField] GameObject goalArea;
    [SerializeField] PopupGoalUnit[] goals;
    [SerializeField] WinStreakDisplayer winStreakDisplayer;
    [SerializeField] StartPopupItemSelectButton rocketButton;
    [SerializeField] StartPopupItemSelectButton bombButton;
    [SerializeField] StartPopupItemSelectButton rainbowButton;
    [SerializeField] PopupPage streakHint;     

    public void PreStartLevel(int level = -1)
    {
        if (level < 0)
            level = UserDataManager.NextLevelID;
        else
            UserDataManager.LoadLevel(level);

        switch (UserDataManager.GameLevel.levelType)
        {
            case 2:
                labelTransform.sizeDelta = labelSize[1];
                difficultyText.text = l_SuperHard.GetLocalizedString();
                difficultyArea.SetActive(true);
                break;
            case 1:
                labelTransform.sizeDelta = labelSize[1];
                difficultyText.text = l_Hard.GetLocalizedString();
                difficultyArea.SetActive(true);
                break;
            default:
                labelTransform.sizeDelta = labelSize[0];
                difficultyArea.SetActive(false);
                break;
        }

        if (level < streakBonusUnlockLevel)
        {
            goalArea.SetActive(true);
            winStreakDisplayer.gameObject.SetActive(false);
            for (int i = 0; i < goals.Length; i++)
                goals[i].gameObject.SetActive(false);
            for (int i = 0; i < UserDataManager.GameLevel.targetInfo.Length; i++)
            {
                int goalnum = UserDataManager.GameLevel.targetInfo[i][1];
                if (0 < goalnum)
                {
                    goals[i].StartPopupMode(UserDataManager.GameLevel.targetInfo[i][0], goalnum);
                    goals[i].gameObject.SetActive(true);
                }
            }
        }
        else
        {
            goalArea.SetActive(false);
            winStreakDisplayer.gameObject.SetActive(true);
            winStreakDisplayer.UpdateDisplay();
        }

        if (level < startItemUnlockLevel)
        {
            rocketButton.SetLocked();
            bombButton.SetLocked();
            rainbowButton.SetLocked();
            MainClock.UnregisterCUPS(this);
        }
        else
        {
            rocketButton.SetItemID(0);
            bombButton.SetItemID(1);
            rainbowButton.SetItemID(2);
        }
    }

    public void ChangeLevel(string level)
    {
        int tmp;
        if (!int.TryParse(level, out tmp))
            return;
        MainClock.RegisterCUPS(this);
        PreStartLevel(tmp);
        base.ShowMe();
    }

    public void StartLevel()
    {
        if (UserDataManager.HaveLife)
        {
            UserDataManager.UseStartItems(rocketButton.Selected, bombButton.Selected, rainbowButton.Selected);
            HomeSceneUIManager.SwitchScene();
        }
        else
            PopupManager.BuyLife();
    }

    public override void ShowMe()
    {
        MainClock.RegisterCUPS(this);
        PreStartLevel();
        base.ShowMe();
    }

    public override async UniTask HideMe()
    {
        MainClock.UnregisterCUPS(this);
        HomeSceneUIManager.UpdateLevelButton();
        await base.HideMe();
    }

    public void Tick()
    {
        rocketButton.UpdateDisplay();
        bombButton.UpdateDisplay();
        rainbowButton.UpdateDisplay();
    }

    public void ShowStreakHint()
    {
        gameObject.SetActive(false);
        streakHint.ShowMe();
    }

    public void HideStreakHint()
    {
        streakHint.HideMe().Forget();
        base.ShowMe();
    }
}
