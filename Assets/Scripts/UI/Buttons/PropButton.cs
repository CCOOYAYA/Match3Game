using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PropButton : ButtonBase
{
    [SerializeField] private int itemID;
    [SerializeField] private LevelConfigSO levelConfig;
    [SerializeField] private Image clickArea;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject itemIcon;
    [SerializeField] private GameObject addIcon;
    [SerializeField] private GameObject numberIcon;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private GameObject effectArea;
    

    private int itemCount;
    private bool isActive = false;
    private static PropButton activeButton = null;
    private static PropButton preActiveButton = null;

    public void UpdateDisplay()
    {
        if (UserDataManager.LevelID < levelConfig.PropUnlockLevel[itemID - 3])
        {
            clickArea.raycastTarget = false;
            lockIcon.SetActive(true);
            itemIcon.SetActive(false);
            addIcon.SetActive(false);
            numberIcon.SetActive(false);
        }
        else
        {
            itemCount = UserDataManager.PropCount(itemID - 3);
            if (0 < itemCount)
            {
                addIcon.SetActive(false);
                numberIcon.SetActive(true);
                countText.text = itemCount.ToString();
            }
            else
            {
                addIcon.SetActive(true);
                numberIcon.SetActive(false);
            }
        }
    }

    public static void PreActiveCheck()
    {
        if (preActiveButton == null)
            return;
        MainGameUIManager.UpdatePropButtonDisplay();
        if (0 < preActiveButton.itemCount)
            preActiveButton.PropUsingOn();
        preActiveButton = null;
    }

    private void PropUsingOn()
    {
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "PopupUICanvas";
        canvas.sortingOrder = 200;
        isActive = true;
        activeButton = this;
        GameManager.CurrentProp = (UsingProp)(itemID - 2);
        MainGameUIManager.PropUsingOn(itemID != 6);
        effectArea.SetActive(true);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        if ((!GameBoardManager.instance.GameBoardInactivity && !isActive) || 
            !GameBoardManager.instance.IsAllPiecesOnGameBoardStill ||
            GameManager.instance.AssigningPowerupTasks.Count > 0)
        {
            return;
        }

        if (0 < lockLevel)
            return;
        if (isActive)
            MainGameUIManager.PropUsingOff();
        else if (0 < itemCount)
            PropUsingOn();
        else
        {
            preActiveButton = this;
            PopupManager.BuyItem(itemID);
        }
    }

    public static void PropUsingOff()
    {
        activeButton.canvas.overrideSorting = false;
        activeButton.isActive = false;
        activeButton.UpdateDisplay();
        activeButton.effectArea.SetActive(false);
        GameManager.CurrentProp = UsingProp.None;
    }

    public void SetActive(bool value)
    {
        clickArea.raycastTarget = value;
    }
}
