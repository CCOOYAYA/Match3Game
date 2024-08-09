using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class LifeDisplayer : SimpleCountdown
{
    [SerializeField] private TextMeshProUGUI lifeCount;
    [SerializeField] private GameObject plusMark;

    public void UpdateLifeDisplay()
    {
        if (plusMark != null)
            plusMark.SetActive(false);
        if (UserDataManager.InfiniteLife)
        {
            lifeCount.text = "oo";
            TimeDisplay(UserDataManager.InfiniteLifeExpireTime - Time.unscaledTime);
        }
        else
        {
            lifeCount.text = UserDataManager.Life.ToString();
            if (UserDataManager.FullLife)
                text.text = "Full";
            else
            {
                TimeDisplay(Mathf.Max(0f, UserDataManager.LifeRegenTime - Time.unscaledTime));
                if (plusMark != null)
                    plusMark.SetActive(true);
            }
        }
    }

    public override void Tick()
    {
        UpdateLifeDisplay();
    }
}
