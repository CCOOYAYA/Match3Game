using AYellowpaper.SerializedCollections;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class ShopPopupDisplayer : TopBannerPopup
{
    [SerializeField] private RectTransform shopArea;
    [SerializeField] private TextMeshProUGUI CoinText;
    [SerializeField] private GameObject removeAdBanner;
    [SerializeField] private Bundle[] bundleList;
    [SerializeField] private Bundle[] coinBundleList;
    [SerializeField] private Bundle focusBundle;
    [SerializeField] private Bundle focusCoinBundle;
    [SerializeField] private SimpleSlideTweener moreButtonTweener;
    [SerializeField] private SlideTweenerGroup tweenerGroup;

    private int itemFilter = -1;
    private int coinFilter = 0;
    private List<Bundle> fourBundles = new();

    public override void ShowMe()
    {
        shopArea.localPosition = Vector3.zero;
        CoinText.text = UserDataManager.Coin.ToString();
        Show4Bundles();
        base.ShowMe();
    }

    public override UniTask HideMe()
    {
        itemFilter = -1;
        coinFilter = 0;
        if (HomeSceneUIManager.Exist)
            HomeSceneUIManager.UpdateCoinCount();
        return base.HideMe();
    }

    private void Calc4Bundles()
    {
        fourBundles.Clear();
        fourBundles.Add(bundleList[0]);
        fourBundles.Add(bundleList[1]);
        fourBundles.Add(coinBundleList[0]);
        fourBundles.Add(coinBundleList[1]);
    }

    private void Show4Bundles()
    {
        Calc4Bundles();
        HideAllBundles();
        tweenerGroup.ClearTweener();
        for (int i = 0; i < 4; i++)
        {
            fourBundles[i].transform.SetSiblingIndex(i + 1);
            tweenerGroup.AddTweener(fourBundles[i].Tweener);
        }
        tweenerGroup.AddTweener(moreButtonTweener);
        tweenerGroup.TweenIn().Forget();
    }

    private void HideAllBundles()
    {
        for (int i = 0; i < bundleList.Length; i++)
            bundleList[i].Tweener.HideMe();
        for (int i = 0; i < coinBundleList.Length; i++)
            coinBundleList[i].Tweener.HideMe();
    }

    private void ShowAllBundles()
    {
        tweenerGroup.ClearTweener();
        for (int i = 0; i < bundleList.Length; i++)
            tweenerGroup.AddTweener(bundleList[i].Tweener);
        for (int i = 0; i < coinBundleList.Length; i++)
            tweenerGroup.AddTweener(coinBundleList[i].Tweener);
        tweenerGroup.TweenIn().Forget();
    }

    private void ReOrderBundles()
    {
        for (int i = 0; i < bundleList.Length; i++)
            bundleList[i].transform.SetSiblingIndex(i + 1);
        int l = bundleList.Length + 1;
        for (int i = 0; i < coinBundleList.Length; i++)
            coinBundleList[i].transform.SetSiblingIndex(i + l);
    }

    public void Show_All_Bundles()
    {
        ReOrderBundles();
        ShowAllBundles();
        moreButtonTweener.HideMe();
    }

    public void SetFilter(int itemID, int coin)
    {
        itemFilter = itemID;
        coinFilter = coin;
    }
}
