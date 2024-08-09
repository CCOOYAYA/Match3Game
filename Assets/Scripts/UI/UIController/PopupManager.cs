using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    [SerializeField] BuyLifePopupDisplayer buyLifePopup;
    [SerializeField] BuyItemPopupDisplayer buyItemPopup;
    [SerializeField] ShopPopupDisplayer shopPopup;

    private Stack<PopupPage> popupStack = new();

    private static PopupManager Instance;
    private static bool busyFlag = false;

    private void Awake()
    {
        Instance = this;
        popupStack.Clear();
        busyFlag = false;
    }

    public static async UniTask ShowPageAsync(PopupPage newPage, bool jumpTo = false, bool overlap = false)
    {
        if (busyFlag)
            return;
        busyFlag = true;
        ButtonBase.Lock();
        PopupPage currentPage;
        if (Instance.popupStack.TryPeek(out currentPage))
        {
            if (currentPage == newPage)
            {
                busyFlag = false;
                ButtonBase.Unlock();
                return;
            }
            if (jumpTo)
                await Instance.popupStack.Pop().HideMe();
            else if (!overlap)
                await currentPage.HideMe();
        }
        Instance.popupStack.Push(newPage);
        busyFlag = false;
        ButtonBase.Unlock();
        newPage.ShowMe();
    }

    public static async UniTask CloseCurrentPageAsync()
    {
        if (busyFlag)
            return;
        if (Instance.popupStack.Count == 0)
            return;
        busyFlag = true;
        ButtonBase.Lock();
        await Instance.popupStack.Pop().HideMe();
        busyFlag = false;
        ButtonBase.Unlock();
        PopupPage currentPage;
        if (Instance.popupStack.TryPeek(out currentPage))
            currentPage.ShowMe();
    }

    public static void BuyLife()
    {
        ShowPageAsync(Instance.buyLifePopup).Forget();
    }

    public static void BuyItem(int itemID)
    {
        UserDataManager.PurchaseComplete = false;
        Instance.buyItemPopup.SetItem(itemID);
        ShowPageAsync(Instance.buyItemPopup).Forget();
    }

    public static void OpenShop(int itemID = -1, int coinCount = 0)
    {
        ShowPageAsync(Instance.shopPopup).Forget();
    }
}
