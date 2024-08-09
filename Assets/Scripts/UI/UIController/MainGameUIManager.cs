using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;

public class MainGameUIManager : MonoBehaviour
{
    [Header("Level Targets && Reward")]
    [SerializeField]    private RectTransform levelTargetContainer;
    [SerializeField]    private GameObject[] levelTargetPrefabs;
//    [SerializeField]    private RectTransform rewardContainer;
    [SerializeField]    private Image coinImage;
    [SerializeField]    private TextMeshProUGUI coinCountText;
    [SerializeField]    private PieceConfigSO pieceConfigSO;
                        private Dictionary<int, (int leftCount, Image targetPieceImage, TMP_Text targetPieceLeftCountText, Image targetPieceCompleteMarkImage)> levelTargetDisplayDic = new();
                        private Dictionary<int, (int collectCount, Image rewardImage, TMP_Text rewardCollectCountText)> rewardDisplayDic = new();
                        private List<Collectable> executingCollectables = new();

    [Header("Others")]
    [SerializeField] private Canvas foregroundCanvas;
    [SerializeField] private TopAreaMarginer topArea;
    [SerializeField] private GameObject targetArea;
    [SerializeField] private GameObject rewardArea;
    [SerializeField] private TextMeshProUGUI movesCountText;
    [SerializeField] private AssetSwitcher[] difficultyRelatedAssets;
    [SerializeField] private PropButton[] propButtons;
    [SerializeField] private SimpleSlideTweener oomBanner;
    [SerializeField] private CongratsDisplayer congrats;
    [SerializeField] private PopupPage successPopup;
    [SerializeField] RevivePopupDisplayer revivePopup;
    [SerializeField] RetryPopupDisplayer retryPopup;
    [SerializeField] private GameObject foregroundShadow;
    [SerializeField] private LifeDisplayer lifeDisplayer;
    [SerializeField] private SlideTweenerGroup boardTweener;
    [SerializeField] private SimpleSlideTweener bottomButtonTweener;

    private static int buttonMuteLevel = 0;

    public bool IsAllLevelTargetDisplayCompleted
    {
        get
        {
            foreach (var kvp in levelTargetDisplayDic)
            {
                if (kvp.Value.leftCount > 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
    public bool IsAllExecutingCollectablesDestroyed => executingCollectables.Count <= 0;


    public static MainGameUIManager Instance;
    private void Awake()
    {
        Instance = this;
    }

    public void GameUIInit(int levelType, LevelTarget levelTarget, int moves)
    {
        topArea.Resize();
        UpdateDifficulty(levelType);
        InitializeLevelTargetDisplay(levelTarget);
        SetMoveCount(moves);
        UpdatePropButtonDisplay();
        buttonMuteLevel = 0;
        ButtonBase.ResetLockLevel();
        MainClock.RegisterCUPS(Instance.lifeDisplayer);
    }

    public static void UpdateDifficulty(int difficulty)
    {
        AssetSwitcher.DifficultyLevel difficultyLevel;
        switch (difficulty)
        {
            case 2:
                difficultyLevel = AssetSwitcher.DifficultyLevel.SuperHard;
                break;
            case 1:
                difficultyLevel = AssetSwitcher.DifficultyLevel.Hard;
                break;
            default:
                difficultyLevel = AssetSwitcher.DifficultyLevel.Normal;
                break;
        }
        foreach (var asset in Instance.difficultyRelatedAssets)
            asset?.SetDifficultyLevel(difficultyLevel);
    }


    #region Level Target
    public void InitializeLevelTargetDisplay(LevelTarget levelTarget)
    {
        targetArea.SetActive(true);
        rewardArea.SetActive(false);
        levelTargetDisplayDic.Clear();
        int insCount = 0;
        int targetCnt = levelTarget.TargetDic.Count;

        foreach (var kvp in levelTarget.TargetDic)
        {
            var rowOfLevelTargets = levelTargetContainer.GetChild(insCount / 2);
            rowOfLevelTargets.gameObject.SetActive(true);

            var insLevelTarget = Instantiate(levelTargetPrefabs[targetCnt - 1], rowOfLevelTargets);

            var collectId = kvp.Key;
            var targetPieceImage = insLevelTarget.transform.GetChild(0).GetComponent<Image>();
            foreach (var keyValuePair in pieceConfigSO.allRegisteredPieces)
            {
                var registeredPiece = keyValuePair.Value;
                if (collectId == registeredPiece.pieceTargetReference.collectId)
                {
                    targetPieceImage.sprite = registeredPiece.pieceTargetReference.pieceLevelTargetSprite;
                    //targetPieceImage.SetNativeSize();

                    break;
                }
            }

            var leftCount = kvp.Value;
            var targetPieceLeftCountText = insLevelTarget.transform.GetChild(1).GetComponent<TMP_Text>();
            targetPieceLeftCountText.text = leftCount.ToString();

            // 标记完成红勾Image
            var targetPieceCompleteMarkImage = insLevelTarget.transform.GetChild(2).GetComponent<Image>();
            targetPieceCompleteMarkImage.gameObject.SetActive(false);

            levelTargetDisplayDic.TryAdd(collectId, (leftCount, targetPieceImage, targetPieceLeftCountText, targetPieceCompleteMarkImage));
            insCount++;
        }
    }


    public Vector3 GetLevelTargetDisplayWorldPosition(int collectId)
    {
        if (!levelTargetDisplayDic.TryGetValue(collectId, out var tuple))
            throw new NullReferenceException();

        return tuple.targetPieceImage.transform.position;
    }

    public static bool TargetComplete(int collectID) => Instance.levelTargetDisplayDic[collectID].leftCount == 0;

    public void UpdateLevelTargetDisplay(int collectId)
    {
        if (levelTargetDisplayDic.ContainsKey(collectId) == false)
            throw new NullReferenceException();

        levelTargetDisplayDic.TryGetValue(collectId, out var tuple);
        levelTargetDisplayDic[collectId] = (--tuple.leftCount, tuple.targetPieceImage, tuple.targetPieceLeftCountText, tuple.targetPieceCompleteMarkImage);
        levelTargetDisplayDic[collectId].targetPieceLeftCountText.text = levelTargetDisplayDic[collectId].leftCount.ToString();

        if (levelTargetDisplayDic[collectId].leftCount == 0)
        {
            //判断其他Target的关联情况, 符合的需要标记为红勾
            levelTargetDisplayDic[collectId].targetPieceLeftCountText.gameObject.SetActive(false);
            levelTargetDisplayDic[collectId].targetPieceCompleteMarkImage.gameObject.SetActive(true);
        }
    }
    #endregion


    #region Reward
    public async UniTask ChangeLevelTargetDisplayToRewardDisplay(bool skip = false)
    {
        // TODO: animate this
        if (!skip)
            await UniTask.Delay(1);
        targetArea.SetActive(false);
        rewardArea.SetActive(true);

        // Initialize this
        rewardDisplayDic.Clear();

        //暂时只有金币
        rewardDisplayDic.TryAdd(9999, (0, coinImage, coinCountText));
    }

    public Vector3 GetRewardDisplayWorldPosition(int collectId)
    {
        if (!rewardDisplayDic.TryGetValue(collectId, out var tuple))
            throw new NullReferenceException();

        return tuple.rewardImage.transform.position;
    }

    public void UpdateRewardDisplay(int collectId, int count = 1, bool forceSet = false)
    {
        if (rewardDisplayDic.ContainsKey(collectId) == false)
            throw new NullReferenceException();

        rewardDisplayDic.TryGetValue (collectId, out var tuple);
        rewardDisplayDic[collectId] = (forceSet ? count : tuple.collectCount + count, tuple.rewardImage, tuple.rewardCollectCountText);
        rewardDisplayDic[collectId].rewardCollectCountText.text = rewardDisplayDic[collectId].collectCount.ToString();
    }
    #endregion


    private int CollectableSortOrder { get; set; } = 2;

    public void OnGenerateCollectable(Collectable newGenerateCollectable, out int sortOrder) 
    { 
        executingCollectables.Add(newGenerateCollectable);
        sortOrder = CollectableSortOrder;
        CollectableSortOrder += 3;
    }

    public void OnDestroyCollectable(Collectable toDestroyCollectable) => executingCollectables.Remove(toDestroyCollectable);




    public static void SetMoveCount(int count)
    {
        Instance.movesCountText.text = count.ToString();
    }

    public static void OnLevelSuccess(Action completeClbk, Action skipClbk, CancellationToken token)
    {
        UserDataManager.LevelSuccess();
        Instance.congrats.DisplayCongratulateAnimation(completeClbk, skipClbk, token);
    }

    public static void PopupSuccessPage()
    {
        UserDataManager.SaveRewards();
        Instance.congrats.gameObject.SetActive(false);
        PopupManager.ShowPageAsync(Instance.successPopup).Forget();
    }

    private async UniTask OOOM()
    {
        await oomBanner.TweenIn();
        await UniTask.Delay(1200);
        await oomBanner.TweenOut();
        await UniTask.Delay(200);
        UserDataManager.PurchaseComplete = false;
        await PopupManager.ShowPageAsync(Instance.revivePopup);
    }

    public static void OnOutOfMoves()
    {
        Instance.OOOM().Forget();
    }

    public static void OnLevelFailed()
    {
        UserDataManager.LevelFailed();
        PopupManager.ShowPageAsync(Instance.retryPopup).Forget();
    }

    public static void OnPointerDownOutOfBoard()
    {
        if (GameManager.CurrentProp != UsingProp.None)
            PropUsingOff();
    }

    public static void UpdatePropButtonDisplay()
    {
        foreach (var propButton in Instance.propButtons)
            propButton.UpdateDisplay();
    }

    public static void PropUsingOn(bool changeSortingLayer = true)
    {
        if (changeSortingLayer)
        {
            Instance.foregroundCanvas.sortingLayerName = "BackgroundUICanvas";
            Instance.foregroundCanvas.sortingOrder = 200;
        }
        Instance.foregroundShadow.SetActive(true);
    }

    public void OnShuffleButtonClick() => GameBoardManager.instance.UsingPropDice();

    public static void PropUsingOff()
    {
        Instance.foregroundCanvas.sortingLayerName = "ForegroundUICanvas";
        Instance.foregroundCanvas.sortingOrder = 0;
        Instance.foregroundShadow.SetActive(false);
        PropButton.PropUsingOff();
    }

    public static void MutePropButton()
    {
        buttonMuteLevel++;
        if (buttonMuteLevel == 1)
            foreach (var propButton in Instance.propButtons)
                propButton.SetActive(false);
    }

    public static void UnmutePropButton()
    {
        buttonMuteLevel--;
        if (buttonMuteLevel == 0)
            foreach (var propButton in Instance.propButtons)
                propButton.SetActive(true);
        if (buttonMuteLevel < 0)
            buttonMuteLevel = 0;
    }

    public static void HideBottomButtons()
    {
        Instance.bottomButtonTweener.TweenOut().Forget();
    }

    public static void ShowBottomButtons()
    {
        Instance.bottomButtonTweener.TweenIn().Forget();
    }

    public static async void HideBottomButtons(int hideTime)
    {
        await Instance.bottomButtonTweener.TweenOut();
        await UniTask.Delay(hideTime);
        await Instance.bottomButtonTweener.TweenIn();
    }

    public void LifePlusOne()
    {
        UserDataManager.AddLife(1);
        lifeDisplayer.UpdateLifeDisplay();
    }

    public void UseLife()
    {
        UserDataManager.UseLife_Test();
        lifeDisplayer.UpdateLifeDisplay();
    }

    public void Add1MinLife()
    {
        UserDataManager.AddLifeTime(1);
        lifeDisplayer.UpdateLifeDisplay();
    }

    public void ClearInfLife()
    {
        UserDataManager.ClearLifeTime_Test();
        lifeDisplayer.UpdateLifeDisplay();
    }

    public static async UniTask TweenInBoardUI()
    {
        ButtonBase.Lock();
        await Instance.boardTweener.TweenIn();
        ButtonBase.Unlock();
    }
}
