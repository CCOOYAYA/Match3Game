using AYellowpaper.SerializedCollections;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class BuyLifePopupDisplayer : PopupPage, IUpdatePerSecond
{
    [SerializeField] TextMeshProUGUI lifeCount;
    [SerializeField] TextMeshProUGUI lifeTimer;
    [SerializeField] GameObject adButton;
    [SerializeField] TextMeshProUGUI adTimer;
    [SerializeField] GameObject adCornerIcon;
    [SerializeField] TextMeshProUGUI adCount;

    public override void ShowMe()
    {
        MainClock.RegisterCUPS(this);
        UpdateDisplay();
        base.ShowMe();
    }

    public override UniTask HideMe()
    {
        MainClock.UnregisterCUPS(this);        
        return base.HideMe();
    }

    public void Tick()
    {
        if (UserDataManager.FullLife)
            PopupManager.CloseCurrentPageAsync().Forget();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        lifeCount.text = UserDataManager.Life.ToString();
        TimeSpan timeSpan = TimeSpan.FromSeconds(UserDataManager.LifeRegenTime - Time.unscaledTime);
        lifeTimer.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        if (UserDataManager.LifeAdCount == 0)
        {
            adButton.SetActive(false);
            adCornerIcon.SetActive(false);
            timeSpan = TimeSpan.FromSeconds(UserDataManager.LifeAdCountRegenTime - Time.unscaledTime);
        }
        else
        {
            adCornerIcon.SetActive(true);
            adCount.text = UserDataManager.LifeAdCount.ToString();
            if (Time.unscaledTime < UserDataManager.LifeAdCoolDownTime)
            {
                adButton.SetActive(false);
                timeSpan = TimeSpan.FromSeconds(UserDataManager.LifeAdCoolDownTime - Time.unscaledTime);
            }
            else
                adButton.SetActive(true);
        }
        if (1f < timeSpan.TotalHours)
            adTimer.text = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        else
            adTimer.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }

    public void Watch_Ad_Test()
    {
        UserDataManager.AddLifeByAd();
        UpdateDisplay();
    }

    public void Buy_life()
    {
        int lifePrice = 900;
        if (UserDataManager.Coin < lifePrice)
            PopupManager.OpenShop();
        else
        {
            UserDataManager.BuyLife(lifePrice);
            if (HomeSceneUIManager.Exist)
                HomeSceneUIManager.UpdateCoinCount();
        }
            
    }
}
