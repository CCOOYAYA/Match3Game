using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeSceneUIManager : MonoBehaviour
{
    [SerializeField] CanvasInputSystem inputSystem;
    [SerializeField] PageManager pageManager;
    [SerializeField] TopBannerResizer areaArea;
    [SerializeField] TopAreaMarginer gameTopArea;
    [SerializeField] TopAreaMarginer buildTopArea;
    [SerializeField] LeaderboardManager leaderboardManager;

    [SerializeField] SlideTweenerGroup mainUITween;
    [SerializeField] BuildUIManager buildUIManager;
    [SerializeField] RewardManager rewardManager;
    [SerializeField] AvatarDisplayer avatarDisplayer;
    [SerializeField] TextMeshProUGUI coinText;
    [SerializeField] LifeDisplayer lifeIcon;
    [SerializeField] TextMeshProUGUI starText;
    [SerializeField] LevelButton levelButton;
    [SerializeField] GameObject buildButton;
    [SerializeField] TextMeshProUGUI buildButtonText;
    [SerializeField] LocalizedString l_Area;
    [SerializeField] GameObject buildCountIcon;
    [SerializeField] TextMeshProUGUI buildCountText;
    [SerializeField] GameObject newAreaButton;
    [SerializeField] SimpleSlideTweener reviewBackButton;
    [SerializeField] GameObject loadingIcon;

    [SerializeField] GameObject areaContent;
    [SerializeField] AreaDisplayer protoAreaDisplayer;
    [SerializeField] GameObject protoFrameLine;

    [SerializeField] PopupText popupText;
    [SerializeField] LocalizedString fullLifeWarning;
    [SerializeField] PopupPage starHintPopup;
    [SerializeField] PopupPage newAreaPopup;
    [SerializeField] BuyLifePopupDisplayer buyLifePopup;
    [SerializeField] LoadPopupDisplayer loadPopup;
    [SerializeField] PropUnlockPopupDisplayer propUnlockPopup;

    static HomeSceneUIManager Instance;
    public static bool Exist => Instance != null;

    private List<AreaDisplayer> areaDisplayers = new();
    private AreaDisplayer CurrentArea => areaDisplayers[UserDataManager.CurrentSceneID - 1];

    private void Awake()
    {
        Instance = this;
    }

    public static async void EnterBuildView()
    {
        ButtonBase.Lock();
        Instance.pageManager.DisableMe();
        await Instance.mainUITween.TweenOut();
        await Instance.buildUIManager.ShowMe();
        ButtonBase.Unlock();
    }

    public static async void ExitBuildView()
    {
        ButtonBase.Lock();
        Instance.CurrentArea.UpdateDisplay(UserDataManager.CurrentSceneID);
        UpdateStarCount();
        UpdateBuildButton();
        await Instance.buildUIManager.HideMe();
        await Instance.mainUITween.TweenIn();
        if (UserDataManager.TotalBuildStage == 15)
        {
            Instance.rewardManager.ClaimCallback = NewAreaPopupCheck;
            Instance.rewardManager.ShowTest();
        }
        Instance.pageManager.EnableMe();
        ButtonBase.Unlock();
    }

    public static void NewAreaPopupCheck()
    {
        if (UserDataManager.CurrentSceneID == 1)
            PopupManager.ShowPageAsync(Instance.newAreaPopup).Forget();
    }

    public static async void EnterReviewView(int sceneID)
    {
        SetLoadingIconActive(true);
        await BuildManager.LoadReviewScene(sceneID);
        SetLoadingIconActive(false);
        Instance.pageManager.PageSwitch(1);
        Instance.mainUITween.HideMe();
        Instance.reviewBackButton.TweenIn().Forget();
        
    }

    public static void ExitReviewView()
    {
        Instance.reviewBackButton.HideMe();
        Instance.mainUITween.ShowMe();
        Instance.pageManager.PageSwitch(0);
        BuildManager.UnloadReviewScene();
        Instance.pageManager.EnableMe();
    }

    public static void RefreshAreaDisplay()
    {
        for (int i = 0; i < UserDataManager.SceneCount; i++)
            Instance.areaDisplayers[i].UpdateDisplay(i + 1);
    }

    public void InitMe()
    {
        pageManager.Resize();
        areaArea.Resize();
        gameTopArea.Resize();
        buildTopArea.Resize();
        leaderboardManager.InitMe();

        popupText.Init();
        for (int i = 0; i < UserDataManager.SceneCount; i++)
        {
            if (0 < i)
                Instantiate(protoFrameLine, areaContent.transform);
            var newDisplayer = Instantiate<AreaDisplayer>(protoAreaDisplayer, areaContent.transform);
            newDisplayer.UpdateDisplay(i + 1);
            areaDisplayers.Add(newDisplayer);
        }
        UserDataManager.LevelFailed();
        UpdateAvatar();
        UpdateCoinCount();
        lifeIcon.UpdateLifeDisplay();
        MainClock.RegisterCUPS(lifeIcon);
        UpdateStarCount();
        
        UpdateLevelButton();
        UpdateBuildButton();
    }

    public async UniTask HomeSceneUIStartCheck()
    {
        ButtonBase.Lock();
        await mainUITween.TweenIn();
        await rewardManager.LevelStdRewardCheck();
        ButtonBase.Unlock();
    }

    public void OtherStartCheck()
    {
        if (UserDataManager.LevelEventCheck())
            propUnlockPopup.PopupCheck();
    }

    public static void UpdateAvatar()
    {
        Instance.avatarDisplayer.UpdateDisplay();
        Instance.leaderboardManager.UpdateUserInfo();
    }

    public static void UpdateCoinCount()
    {
        Instance.coinText.text = UserDataManager.Coin.ToString();
    }
    public static void UpdateStarCount()
    {
        Instance.starText.text = UserDataManager.Stars.ToString();
    }

    public static void UpdateLevelButton()
    {
        Instance.levelButton.UpdateDisplay();
    }

    public static void UpdateBuildButton()
    {
        if (UserDataManager.TotalBuildStage == 15)
        {
            Instance.buildButton.SetActive(false);
            Instance.newAreaButton.SetActive(true);
        }
        else
        {
            Instance.buildButton.SetActive(true);
            Instance.newAreaButton.SetActive(false);
            Instance.buildButtonText.text = Instance.l_Area.GetLocalizedString(UserDataManager.CurrentSceneID);
            if (0 < BuildManager.AvailableBuildCount)
            {
                Instance.buildCountText.text = BuildManager.AvailableBuildCount.ToString();
                Instance.buildCountIcon.SetActive(true);
            }
            else
                Instance.buildCountIcon.SetActive(false);
        }
    }

    public static void ShowStarHint()
    {
        PopupManager.ShowPageAsync(Instance.starHintPopup).Forget();
    }

    public void BuyLife()
    {
        if (UserDataManager.FullLife)
        {
            PopupText(fullLifeWarning.GetLocalizedString());
            return;
        }   
        if (UserDataManager.InfiniteLife)
        {
            PopupText(fullLifeWarning.GetLocalizedString());
            return;
        }
        PopupManager.ShowPageAsync(buyLifePopup).Forget();
    }

    public static void PopupText(string text)
    {
        Instance.popupText.ShowText(text);
    }

    public async static void SwitchScene()
    {
        UserDataManager.IsRetryMode = false;
        var op = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        op.allowSceneActivation = false;
        PopupManager.ShowPageAsync(Instance.loadPopup).Forget();
        await UniTask.Delay(1100);
        await UniTask.WaitUntil(() => 0.89f < op.progress);
        await Instance.loadPopup.FadeMe();
        await UniTask.Delay(200);
        op.allowSceneActivation = true;
    }

    public static void SetLoadingIconActive(bool value)
    {
        Instance.loadingIcon.SetActive(value);
    }

    public static async UniTask SimpleFlyingObject(TempFlyingObject prototype, Transform targetPos)
    {
        await Instance.rewardManager.SimpleFlyingObject(prototype, targetPos);
    }
}