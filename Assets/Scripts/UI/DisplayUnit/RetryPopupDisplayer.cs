using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RetryPopupDisplayer : PopupPage, IUpdatePerSecond
{
    [SerializeField] PopupGoalUnit[] goals;
    [SerializeField] StartPopupItemSelectButton rocketButton;
    [SerializeField] StartPopupItemSelectButton bombButton;
    [SerializeField] StartPopupItemSelectButton rainbowButton;
    [SerializeField] SimpleSlideTweener streakTweener;
    [SerializeField] WinStreakDisplayer streakDisplayer;
    [SerializeField] AdRetryItemTweener itemTweener;

    public void Init()
    {   
        for (int i = 0; i < goals.Length; i++)
            goals[i].gameObject.SetActive(false);
        for (int i = 0; i < UserDataManager.GameLevel.targetInfo.Length; i++)
        {
            int goalnum = UserDataManager.GameLevel.targetInfo[i][1];
            if (0 < goalnum)
            {
                goals[i].RetryPopupMode(UserDataManager.GameLevel.targetInfo[i][0]);
                goals[i].gameObject.SetActive(true);
            }
        }

        rocketButton.SetItemID(0);
        bombButton.SetItemID(1);
        rainbowButton.SetItemID(2);

        streakDisplayer.UpdateDisplay();
        itemTweener.ShowMe();
    }

    public void ReStartLevel()
    {
        if (0 < UserDataManager.Life)
        {
            UserDataManager.UseStartItems(rocketButton.Selected, bombButton.Selected, rainbowButton.Selected);
            UserDataManager.IsRetryMode = true;
            DOTween.Clear();
            SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        }
        else
        {
            PopupManager.BuyLife();
        }
    }

    public void ReStartLevel_Ad()
    {
        if (0 < UserDataManager.Life)
        {
            UserDataManager.UseStartItems(rocketButton.Selected, bombButton.Selected, rainbowButton.Selected);
            UserDataManager.RetryPowerup = itemTweener.ItemID;
            UserDataManager.IsRetryMode = true;
            DOTween.Clear();
            SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        }
        else
        {
            PopupManager.BuyLife();
        }
    }

    public override void ShowMe()
    {
        MainClock.RegisterCUPS(this);
        Init();
        base.ShowMe();
    }

    protected async override void TweenPage()
    {
        if (gameObject.activeSelf)
            return;
        await transform.DOScale(Vector3.one * 1.03f, 0.1f).SetLoops(2, LoopType.Yoyo);
        await streakTweener.TweenIn();
    }

    public override async UniTask HideMe()
    {
        itemTweener.HideMe();
        await base.HideMe();
        MainClock.UnregisterCUPS(this);
    }

    public void Tick()
    {
        rocketButton.UpdateDisplay();
        bombButton.UpdateDisplay();
        rainbowButton.UpdateDisplay();
    }
}
