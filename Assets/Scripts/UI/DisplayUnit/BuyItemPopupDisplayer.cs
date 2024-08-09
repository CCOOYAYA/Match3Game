using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuyItemPopupDisplayer : PopupPage
{
    [SerializeField] ItemPriceInfo[] itemInfoList;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] Image itemImage;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI priceText;

    [Serializable]
    private struct ItemPriceInfo
    {
        public LocalizedString name;
        public Sprite sprite;
        public int setSize;
        public LocalizedString description;
        public int price;
    }

    private int itemID;

    public void SetItem(int id)
    {
        itemID = id;
    }

    public override void ShowMe()
    {
        if (UserDataManager.PurchaseComplete)
        {
            if (itemID < 3)
            {
                if (UserDataManager.StartItemCount(itemID) == 0)
                    UserDataManager.BuyItemByCoin(itemID, itemInfoList[itemID].setSize, itemInfoList[itemID].price);
                PopupManager.CloseCurrentPageAsync().Forget();
                return;
            }
            else
            {
                if (UserDataManager.PropCount(itemID - 3) == 0)
                    UserDataManager.BuyItemByCoin(itemID, itemInfoList[itemID].setSize, itemInfoList[itemID].price);
                PopupManager.CloseCurrentPageAsync().Forget();
                PropButton.PreActiveCheck();
                return;
            }
        }
        nameText.text = itemInfoList[itemID].name.GetLocalizedString();
        itemImage.sprite = itemInfoList[itemID].sprite;
        countText.text = "¡Á"+itemInfoList[itemID].setSize;
        descriptionText.text = itemInfoList[itemID].description.GetLocalizedString();
        priceText.text = itemInfoList[itemID].price.ToString();
        base.ShowMe();
    }

    public void Buy_Test()
    {
        if (UserDataManager.Coin < itemInfoList[itemID].price)
            PopupManager.OpenShop(itemID, itemInfoList[itemID].price - UserDataManager.Coin);
        else
        {
            UserDataManager.BuyItemByCoin(itemID, itemInfoList[itemID].setSize, itemInfoList[itemID].price);
            if (HomeSceneUIManager.Exist)
                HomeSceneUIManager.UpdateCoinCount();
            PopupManager.CloseCurrentPageAsync().Forget();
            if ((2 < itemID) && (0 < UserDataManager.PropCount(itemID - 3)))
                PropButton.PreActiveCheck();
        }
    }
}
