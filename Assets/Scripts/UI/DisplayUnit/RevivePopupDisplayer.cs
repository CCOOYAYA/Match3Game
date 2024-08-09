using System;
using Random = System.Random;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class RevivePopupDisplayer : PopupPage
{
    [SerializeField] RevivePriceInfo[] revivePrice;
    [SerializeField] TextMeshProUGUI coinText;
    [SerializeField] GameObject phase1Root;
    [SerializeField] GameObject oneItemRoot;
    [SerializeField] Image itemImage;
    [SerializeField] GameObject otherItemRoot;
    [SerializeField] Image itemImageL;
    [SerializeField] Image itemImageR;
    [SerializeField] Sprite[] reviveItemSprite;
    [SerializeField] GameObject phase2Root;
    [SerializeField] GameObject p2Part1;
    [SerializeField] GameObject p2Part2;
    [SerializeField] Image streakImage;
    [SerializeField] Sprite[] streakSprite;
    [SerializeField] GameObject longButton;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] GameObject buttonL;
    [SerializeField] GameObject buttonR;

    private int reviveTimes = 0;
    private bool autoBuyFlag = false;

    [Serializable]
    private struct RevivePriceInfo
    {
        public int price;
        public int[] itemID;
    }

    private void UpdateDisplay()
    {
        coinText.text = UserDataManager.Coin.ToString();
        phase1Root.SetActive(true);
        var priceInfo = revivePrice[reviveTimes];
        if (priceInfo.itemID.Length == 1)
        {
            oneItemRoot.SetActive(true);
            otherItemRoot.SetActive(false);
            itemImage.sprite = reviveItemSprite[priceInfo.itemID[0]];
        }
        else
        {
            oneItemRoot.SetActive(false);
            otherItemRoot.SetActive(true);
            if (priceInfo.itemID.Length == 2)
            {
                itemImageL.gameObject.SetActive(true);
                itemImageL.sprite = reviveItemSprite[priceInfo.itemID[0]];
                itemImageR.gameObject.SetActive(true);
                itemImageR.sprite = reviveItemSprite[priceInfo.itemID[1]];
            }
            else
            {
                itemImageL.gameObject.SetActive(false);
                itemImageR.gameObject.SetActive(false);
            }
        }
        phase2Root.SetActive(false);
        p2Part1.transform.DOComplete();
        p2Part1.transform.localScale = Vector3.zero;
        p2Part2.transform.DOComplete();
        p2Part2.transform.localScale = Vector3.zero;
        if (reviveTimes == 0)
        {
            longButton.SetActive(false);
            buttonL.SetActive(true);
            buttonR.SetActive(true);
        }
        else
        {
            longButton.SetActive(true);
            buttonL.SetActive(false);
            buttonR.SetActive(false);
            priceText.text = priceInfo.price.ToString();
        }
    }

    public async void CloseCheck()
    {
        if ((0 < UserDataManager.WinStreak) && (!phase2Root.activeSelf))
        {
            ButtonBase.Lock();
            phase1Root.SetActive(false);
            phase2Root.SetActive(true);
            streakImage.sprite = streakSprite[UserDataManager.WinStreak - 1];
            await p2Part1.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
            await p2Part2.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
            ButtonBase.Unlock();
        }
        else
        {
            await PopupManager.CloseCurrentPageAsync();
            MainGameUIManager.OnLevelFailed();
        }
    }

    public void OpenShop()
    {
        autoBuyFlag = false;
        PopupManager.OpenShop();
    }

    private void ReviveMe()
    {
        UserDataManager.ReviveByCoin(revivePrice[reviveTimes].price);
        if (revivePrice[reviveTimes].itemID != null &&
            revivePrice[reviveTimes].itemID.Length > 0)
        {
            var converted = new int[revivePrice[reviveTimes].itemID.Length];
            for (int i = 0; i < revivePrice[reviveTimes].itemID.Length; i++)
            {
                converted[i] = UserDataManager.GetConvertedPieceId(revivePrice[reviveTimes].itemID[i]);
            }
            GameManager.instance.Revive(5, converted);
        }
        else GameManager.instance.Revive(5, null);
        if (reviveTimes < revivePrice.Length - 1)
            reviveTimes++;
        PopupManager.CloseCurrentPageAsync().Forget();
    }

    public void ReviveCheck()
    {
        if (UserDataManager.Coin < revivePrice[reviveTimes].price)
        {
            autoBuyFlag = true;
            PopupManager.OpenShop();
        }
        else
            ReviveMe();
    }

    public override void ShowMe()
    {
        if ((UserDataManager.PurchaseComplete) && (autoBuyFlag) && (revivePrice[reviveTimes].price <= UserDataManager.Coin))
        {
            ReviveMe();
            return;
        }
        UpdateDisplay();
        base.ShowMe();
    }
}
