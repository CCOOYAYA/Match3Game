using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartPopupItemSelectButton : MonoBehaviour
{
    [SerializeField] private Image lockedIcon;
    [SerializeField] private SimpleButton unselectedButton;
    [SerializeField] private SimpleButton selectedButton;
    [SerializeField] private Image infiniteIcon;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image addIcon;
    [SerializeField] private Image numberIcon;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Image selectedIcon;
    private bool selected = false;
    private int itemID;

    public bool Selected => selected;

    public void SetLocked()
    {
        lockedIcon.gameObject.SetActive(true);
        unselectedButton.gameObject.SetActive(false);
        selectedButton.gameObject.SetActive(false);
        selected = false;
    }

    public void SetItemID(int i)
    {
        lockedIcon.gameObject.SetActive(false);
        itemID = i;
        selected = false;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (UserDataManager.InfiniteStartItem(itemID))
        {
            unselectedButton.gameObject.SetActive(false);
            selectedButton.gameObject.SetActive(true);
            selectedButton.enabled = false;
            infiniteIcon.gameObject.SetActive(true);
            selectedIcon.gameObject.SetActive(false);
            TimeSpan timeSpan = TimeSpan.FromSeconds(UserDataManager.InfiniteStartItemTime(itemID) - Time.unscaledTime);
            timerText.text = string.Format("{0:D2}m {1:D2}s", timeSpan.Minutes, timeSpan.Seconds);
        }
        else
        {
            int itemCnt = UserDataManager.StartItemCount(itemID);
            unselectedButton.gameObject.SetActive(!selected);
            selectedButton.gameObject.SetActive(selected);
            selectedButton.enabled = true;
            infiniteIcon.gameObject.SetActive(false);
            selectedIcon.gameObject.SetActive(true);
            if (itemCnt == 0)
            {
                addIcon.gameObject.SetActive(true);
                numberIcon.gameObject.SetActive(false);
            }
            else
            {
                addIcon.gameObject.SetActive(false);
                numberIcon.gameObject.SetActive(true);
                numberText.text = itemCnt.ToString();
            }
        }
    }

    public void OnSelect()
    {
        if (UserDataManager.StartItemCount(itemID) == 0)
        {
            PopupManager.BuyItem(itemID);
        }
        else
        {
            selected = true;
            UpdateDisplay();
        }
    }

    public void OnUnselect()
    {
        selected = false;
        UpdateDisplay();
    }

    public void Plus3min()
    {
        UserDataManager.AddStartItemTime(itemID, 3);
    }
}
