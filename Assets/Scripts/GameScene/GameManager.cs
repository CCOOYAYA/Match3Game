using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-9999)]
public class GameManager : MonoBehaviour
{
    // Current Game State
    public static int Level { get; private set; }
    public static int LevelType { get; private set; }
    public static int CurrentScore { get; private set; }
    public static int Combo { get; private set; }
    public static int ThisMoveNewScore { get; private set; }
    public static int Disappointment { get; private set; }
    public static int Moves { get; private set; }
    public static LevelTarget LevelTarget { get; private set; }
    public static LevelSpawnerRule LevelSpawnerRule { get; private set; }
    public static LevelProgress LevelProgress { get; private set; }
    public static UsingProp CurrentProp { get; set; }



    [Header("Game Context Options")]
    [SerializeField]    private GameContextSettings gameContextSettings;
    [SerializeField]    private PieceConfigSO pieceConfigSO;

    [Header("Streak Box")]
    [SerializeField]    private StreakBox streakBox;
                        public bool PendingAssignPowerups { get; private set; }
                        public List<int> RevivePowerupIds { get; set; } = new();
                        public bool AssignedBoostAndRetryPowerups { get; private set; }
                        public bool AssignedStreakPowerups { get; private set; }
                        public bool AssignedRevivePowerups { get; private set; }
                        public List<UniTask> AssigningPowerupTasks { get; private set; } = new();

    [Header("Reward")]
    [SerializeField]    private RectTransform movesRectTransform;
    [SerializeField]    private GameObject moveParticlePrefab;
    [SerializeField]    private float moveParticleSpeed;
    [SerializeField]    private float moveParticleInterval;
    [SerializeField]    private uint basicEarnCoin;
                        private bool earnedBasicCoin;
                        public CancellationTokenSource TokenSource { get; private set; }

    [Header("Hint")]
    [SerializeField]    private GameBoardHintEntity hintEntity;

    [Header("User Data")]
    [SerializeField]    private UserDataManager userDataManager;


    [Header("Input")]
    [SerializeField]    private CanvasInputSystem canvasInputSystem;
                        private int muteInput;                  // 支持多来源的屏蔽
                        private IInputSystem _inputSystem;

                        private GridPosition downPosition;      // 按下的相对位置
                        private bool downMode;                  // 按下模式
                        private bool dragMode;                  // 拖拽模式

                        public float ScreenWidth => canvasInputSystem.ScreenWidth;
                        public float ScreenHeight => canvasInputSystem.ScreenHeight;

    [Header("Other")]
    [SerializeField] EventSystem eventSystem;
    [SerializeField] AudioListener audioListener;


    // Move event
    public delegate void MoveConsumeHandler(int movesLeft);
    public event MoveConsumeHandler OnMoveConsumed;
    

    private void Awake() => InitializeGame();
    private void Start() => SetGameContext();


    public static GameManager instance;
    private void InitializeGame()
    {
        if (instance != null)
            Destroy(instance);

        instance = this;

        Level = 0;
        LevelType = 0;
        CurrentScore = 0;
        Combo = 0;
        ThisMoveNewScore = 0;
        Disappointment = 0;
        Moves = 0;
        LevelTarget = null;
        LevelSpawnerRule = null;
        LevelProgress = LevelProgress.Initializing;
        CurrentProp = UsingProp.None;
    }


    private void SetGameContext()
    {
        Application.targetFrameRate = 60;
        LevelProgress = LevelProgress.Initializing;

        // 初始化用户数据 && 本局数据
        userDataManager.InitCheck();
        userDataManager.OnEnterGameSceneInitializeGameData();
        var gameLevel = UserDataManager.GameLevel ?? null;
        Level = gameLevel.level;
        LevelType = gameLevel.levelType;
        LevelTarget = new LevelTarget(gameLevel.targetInfo, pieceConfigSO);
        LevelSpawnerRule = new LevelSpawnerRule(gameLevel.spawnerRuleInfo, gameLevel.spawnerRuleAllocation);
        Moves = gameLevel.steps;

        // 初始化UI
        MainGameUIManager.Instance.GameUIInit(LevelType, LevelTarget, Moves);
        
        // 初始化棋盘
        GameBoardManager.instance.SetGameBoardEssentials(gameContextSettings.pieceAnimationSetting, gameContextSettings.powerupSetting, userDataManager.OnReleasePieceRecordData);
        GameBoardManager.instance.SetGridAndTargetGrid(gameLevel);

        OnMoveConsumed += MainGameUIManager.SetMoveCount;

        // 同步玩家设置
        hintEntity.HintOn = UserDataManager.HintOn;

        ActivateGameBoard();
    }


    private async void ActivateGameBoard()
    {
        await MainGameUIManager.TweenInBoardUI();

        // 初始化玩家输入
        _inputSystem = canvasInputSystem;
        ReceivePlayerInputOnGameBoard();
        LevelProgress = LevelProgress.Playing;

        // 尝试分发Powerups
        AssignedBoostAndRetryPowerups = !(UserDataManager.BoostPowerups.Count > 0 || UserDataManager.RetryPowerup != 0);
        AssignedStreakPowerups = UserDataManager.WinStreak == 0;
        AssignedRevivePowerups = RevivePowerupIds.Count <= 0;
        AssignPowerups();
    }


    public void MutePlayerInputOnGameBoard()
    {
        if (muteInput == 0)
        {
            _inputSystem.PointerDown -= OnPointerDown;
            _inputSystem.PointerUp -= OnPointerUp;
            _inputSystem.PointerDrag -= OnPointerDrag;
        }

        muteInput++;
    }

    public void ReceivePlayerInputOnGameBoard()
    {      
        muteInput = Math.Max(muteInput - 1, 0);

        if (muteInput <= 0)
        {
            _inputSystem.PointerDown += OnPointerDown;
            _inputSystem.PointerUp += OnPointerUp;
            _inputSystem.PointerDrag += OnPointerDrag;
        }
    }

    private void OnPointerDown(object sender, PointerEventArgs pointer)
    {
        // 重置
        downMode = false;
        dragMode = false;

        if (GameBoardManager.instance.IsPointerOnBoard(pointer.WorldPosition, out downPosition))
        {
            if (GameBoardManager.instance.IsSlotSelectable(downPosition))
            {
                downMode = true;
            }
            if (GameBoardManager.instance.IsSlotSwappable(downPosition))
            {
                dragMode = true;
            }
        }
        else MainGameUIManager.OnPointerDownOutOfBoard();
    }


    private void OnPointerUp(object sender, PointerEventArgs pointer)
    {
        // 当初始点击位置不在棋盘上
        if (downMode == false) { return; }

        // 当现在位置不在棋盘上
        // 或现在位置不允许点击
        if (!GameBoardManager.instance.IsPointerOnBoard(pointer.WorldPosition, out var slotPosition) || 
            !GameBoardManager.instance.IsSlotSelectable(slotPosition))
        {
            return;
        }

        // 当前位置和起始位置不同
        if (!GameBoardManager.instance.IsSlotSame(downPosition, slotPosition))
        {
            return;
        }

        // 执行棋子点击, 并关闭按下和拖拽
        GameBoardManager.instance.QueueClick(downPosition);
        downMode = false;
        dragMode = false;
    }


    private void OnPointerDrag(object sender, PointerEventArgs pointer)
    {
        // 当初始点击位置不在棋盘上
        if (dragMode == false) { return; }

        // 当现在位置不在棋盘上
        // 或现在位置不允许交换
        if (!GameBoardManager.instance.IsPointerOnBoard(pointer.WorldPosition, out var slotPosition) ||
            !GameBoardManager.instance.IsSlotSwappable(slotPosition))
        {
            return;
        }

        // 当槽位不同且非对角时
        // 或按下位置不允许交换
        if (GameBoardManager.instance.IsSlotSame(downPosition, slotPosition) ||
            GameBoardManager.instance.IsSlotDiagonal(downPosition, slotPosition) ||
            !GameBoardManager.instance.IsSlotSwappable(downPosition))
        {
            return;
        }

        // 执行棋子交换, 并关闭按下和拖拽
        GameBoardManager.instance.QueueSwap(downPosition, slotPosition);
        downMode = false;
        dragMode = false;
    }


    public async void AssignPowerups()
    {
        if (AssigningPowerupTasks.Count > 0)
            return;

        if (!AssignedBoostAndRetryPowerups &&
            (UserDataManager.BoostPowerups.Count > 0 || UserDataManager.RetryPowerup != 0))
        {
            GameBoardManager.instance.TryAssignBoostAndRetryPowerups(
                (argQueue) => AssignAllBoostAndRetryPowerups(argQueue),
                () => PendingAssignPowerups = true,
                () => 
                {
                    PendingAssignPowerups = false;
                    AssignedBoostAndRetryPowerups = true;
                });
        }
        else if (!AssignedStreakPowerups &&
                 UserDataManager.WinStreak > 0)
        {
            GameBoardManager.instance.TryAssignStreakPowerups(
                (argQueue) => AssigningPowerupTasks.Add(AssignAllStreakPowerups(UserDataManager.WinStreak, argQueue)),
                () => PendingAssignPowerups = true,
                () => 
                {
                    PendingAssignPowerups = false;
                    AssignedStreakPowerups = true;
                });
        }
        else if (!AssignedRevivePowerups &&
                 RevivePowerupIds.Count > 0)
        {
            GameBoardManager.instance.TryAssignRevivePowerups(
                (argQueue) => AssignAllRevivePowerups(argQueue),
                () => PendingAssignPowerups = true,
                () => 
                {
                    PendingAssignPowerups = false;
                    AssignedRevivePowerups = true; 
                });
        }

        await UniTask.WaitUntil(() => AssigningPowerupTasks.Count <= 0);
        if (!PendingAssignPowerups)
        {
            AssignPowerups();
        }
    }


    private async void AssignAllBoostAndRetryPowerups(Queue<(Slot assignReplaceSlot, int assignPowerupPieceId)> argQueue)
    {
        MutePlayerInputOnGameBoard();

        while (argQueue.Count > 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(gameContextSettings.assignPowerupSetting.assignBoostInterval));

            var (assignReplaceSlot, assignPowerupPieceId) = argQueue.Dequeue();
            AssigningPowerupTasks.Add(AssignBoostOrRetryPowerup(assignReplaceSlot, assignPowerupPieceId));
        }

        await UniTask.WhenAll(AssigningPowerupTasks);
        AssigningPowerupTasks.Clear();
        ReceivePlayerInputOnGameBoard();
    }


    private async UniTask AssignAllStreakPowerups(int streakLevel, Queue<(Slot assignReplaceSlot, int assignPowerupPieceId)> argQueue)
    {
        MutePlayerInputOnGameBoard();

        // 初始化
        streakBox.Initialize(streakLevel);

        // 等待盒子进入完成
        await UniTask.WaitUntil(() => streakBox.AssignProgress == StreakBox.AssignStreakBoxPowerupProgress.Assigning);

        // 分配Powerup
        int completeAssignCount = 0;
        while (argQueue.Count > 0)
        {
            var (assignReplaceSlot, assignPowerupPieceId) = argQueue.Dequeue();
            streakBox.AssignPowerup(assignReplaceSlot, assignPowerupPieceId,
                () =>
                {
                    GameBoardManager.instance.ReplacePiece(assignReplaceSlot.GridPosition, assignPowerupPieceId, SpawnTypeEnum.StreakSpwan);
                    completeAssignCount++;
                });
        }

        // 等待分配完成, 盒子离开
        await UniTask.WaitUntil(() => completeAssignCount == streakLevel);
        streakBox.Leave();

        // 盒子离开完毕后关闭屏蔽
        await UniTask.WaitUntil(() => streakBox.AssignProgress == StreakBox.AssignStreakBoxPowerupProgress.Complete);
        AssigningPowerupTasks.Clear();
        ReceivePlayerInputOnGameBoard();
    }


    private async void AssignAllRevivePowerups(Queue<(Slot assignReplaceSlot, int assignPowerupPieceId)> argQueue)
    {
        MutePlayerInputOnGameBoard();

        while (argQueue.Count > 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(gameContextSettings.assignPowerupSetting.assignReviveInterval));

            var (assignReplaceSlot, assignPowerupPieceId) = argQueue.Dequeue();
            AssigningPowerupTasks.Add(AssignRevivePowerup(assignReplaceSlot, assignPowerupPieceId));
        }

        await UniTask.WhenAll(AssigningPowerupTasks);
        AssigningPowerupTasks.Clear();
        ReceivePlayerInputOnGameBoard();
    }


    private async UniTask AssignBoostOrRetryPowerup(Slot replaceSlot, int replaceId)
    {
        bool spawnComplete = false;

        pieceConfigSO.allRegisteredPieces.TryGetValue(replaceId, out var registeredPiece);
        var replacedPiece = GameBoardManager.instance.ReplacePiece(replaceSlot.GridPosition, replaceId, SpawnTypeEnum.BoostSpawn);
        replacedPiece.OnSpawnCallback = () =>
        {
            spawnComplete = true;
        };

        await UniTask.WaitUntil(() => spawnComplete);
    }


    private async UniTask AssignRevivePowerup(Slot replaceSlot, int replaceId)
    {
        bool spawnComplete = false;

        pieceConfigSO.allRegisteredPieces.TryGetValue(replaceId, out var registeredPiece);
        var replacedPiece = GameBoardManager.instance.ReplacePiece(replaceSlot.GridPosition, replaceId, SpawnTypeEnum.BoostSpawn);
        replacedPiece.OnSpawnCallback = () =>
        {
            spawnComplete = true;
        };

        await UniTask.WaitUntil(() => spawnComplete);
    }


    public void ConsumeMove()
    {
        Moves = Math.Max(Moves - 1, 0);
        OnMoveConsumed?.Invoke(Moves);
        CalibrateDisappointmentWhenConsumeMove();

        if (Moves <= 0)
        {
            OnAllMovesConsumed();
        }
        else
        {
            if (PendingAssignPowerups)
            {
                OnMoveConsumedAssignPendingPowerups();
            }
        }
    }


    private void CalibrateDisappointmentWhenConsumeMove() 
    {
        Disappointment = ThisMoveNewScore switch
        {
            var x when x < 100 => Disappointment + 10,
            var x when x >= 100 && x < 400 => Disappointment,
            var x when x >= 400 && x < 1000 => Math.Max(Disappointment - 20, 0),
            var x when x >= 1000 => Math.Max(Disappointment - 1000, 0),
            _ => Disappointment
        };
        ThisMoveNewScore = 0;
        Combo = 0;
    }


    public void AddScore(int score, bool countCombo)
    {
        var addScore = score;
        if (countCombo)
        {
            Combo++;
            addScore *= Combo;
        }
        ThisMoveNewScore += addScore;
        CurrentScore += addScore;
    }


    public async UniTask OnAllLevelTargetsCompleted()
    {
        if (LevelProgress == LevelProgress.Initializing || 
            LevelProgress != LevelProgress.Playing) 
        { 
            return; 
        }

        MutePlayerInputOnGameBoard();           // 屏蔽玩家继续输入
        hintEntity.HintOn = false;              // 不再需要hint 

        // Wait for all the pieces on the gameboard are still && gameboard is inactivity &&
        // all level targets to be cleared &&  && no executing collectables exist
        await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
        while (true)
        {
            var firstCheck = GameBoardManager.instance.IsAllPiecesOnGameBoardStill &&
                             GameBoardManager.instance.GameBoardInactivity &&
                             MainGameUIManager.Instance.IsAllLevelTargetDisplayCompleted &&
                             MainGameUIManager.Instance.IsAllExecutingCollectablesDestroyed;

            await UniTask.DelayFrame(2);
            if (firstCheck)
            {
                var doubleCheck = GameBoardManager.instance.IsAllPiecesOnGameBoardStill &&
                                  GameBoardManager.instance.GameBoardInactivity &&
                                  MainGameUIManager.Instance.IsAllLevelTargetDisplayCompleted &&
                                  MainGameUIManager.Instance.IsAllExecutingCollectablesDestroyed;

                if (doubleCheck)
                {
                    break;
                }
            }
            continue;
        }
        await UniTask.Delay(TimeSpan.FromSeconds(0.25f));

        // 目标完成, 游戏胜利
        TokenSource = new ();
        MainGameUIManager.OnLevelSuccess(OnCongratulateComplete, OnCongratulateSkip, TokenSource.Token);
    }


    /// <summary>
    /// 当完成庆祝动画的回调
    /// </summary>
    private async void OnCongratulateComplete()
    {
        // 发放基础金币
        if (!earnedBasicCoin)
        {
            userDataManager.EarnCoin(basicEarnCoin);
            MainGameUIManager.Instance.UpdateRewardDisplay(9999, (int)basicEarnCoin, true);
            earnedBasicCoin = true;
        }

        var spawnTaskList = new List<UniTask>();
        while (!TokenSource.Token.IsCancellationRequested &&
               Moves > 0)
        {
            var random = UnityEngine.Random.Range(0f, 1f);
            var powerupId = random switch
            {
                var x when x >= 0.000f && x < 0.333f => Constants.PieceBombId,
                var x when x >= 0.333f && x < 0.666f => Constants.PieceHRocketId,
                var x when x >= 0.666f && x < 1.000f => Constants.PieceVRocketId,
                _ => throw new InvalidOperationException()
            };
            spawnTaskList.Add(ConsumeMoveToSpawnPowerup(powerupId));

            await UniTask.Delay(TimeSpan.FromSeconds(moveParticleInterval));
        }

        // 等待生成动画播放完成
        await UniTask.WhenAll(spawnTaskList);
        if (!TokenSource.Token.IsCancellationRequested)
        {
            LevelProgress = LevelProgress.Rewarding;

            // wait for activating all powerups
            await GameBoardManager.instance.HandleAllPowerupsStandanloneActive();

            // 等待直到棋盘完全静止且未被取消
            await UniTask.WaitUntil(() => !TokenSource.Token.IsCancellationRequested &&
                                          GameBoardManager.instance.IsAllPiecesOnGameBoardStill &&
                                          GameBoardManager.instance.GameBoardInactivity &&
                                          GameBoardManager.instance.GetGameBoardPowerupCountDic().Count <= 0 &&
                                          MainGameUIManager.Instance.IsAllExecutingCollectablesDestroyed, PlayerLoopTiming.PostLateUpdate);

            LevelProgress = LevelProgress.Finished;
            MainGameUIManager.PopupSuccessPage();
        }
    }


    private async UniTask ConsumeMoveToSpawnPowerup(int powerupId)
    {
        Moves = Moves - 1 <= 0 ? 0 : Moves - 1;
        OnMoveConsumed?.Invoke(Moves);

        var replaceSlot = GameBoardManager.instance.GetRandomSlotToReplace();
        if (replaceSlot == null) { return; }

        // 生成粒子Prefab, 运动到位后替换棋子
        var insParticle = Instantiate(moveParticlePrefab);
        insParticle.transform.position = movesRectTransform.position;
        TokenSource.Token.Register(() =>
        {
            if (insParticle != null)
            {
                Destroy(insParticle);
            }
        });

        var targetPosition = replaceSlot.transform.position;
        while (!TokenSource.IsCancellationRequested &&
               Vector3.Distance(insParticle.transform.position, targetPosition) >= 0.025f)
        {
            float maxDistance = moveParticleSpeed * Time.deltaTime;
            insParticle.transform.position = Vector3.MoveTowards(insParticle.transform.position, targetPosition, maxDistance);
            await UniTask.NextFrame();
        }

        // 在未被Cancel的情况下
        if (!TokenSource.IsCancellationRequested)
        {
            // 到达后关闭粒子循环
            var particleSystem = insParticle.GetComponent<ParticleSystem>();
            var emission = particleSystem.emission;
            var mainModule = particleSystem.main;
            emission.enabled = false;
            mainModule.loop = false;

            // 替换棋子, 并等待生成动画完毕
            bool spawnComplete = false;
            Piece replacedPiece = GameBoardManager.instance.ReplacePiece(replaceSlot.GridPosition, powerupId);
            replacedPiece.OnSpawnCallback = () => { spawnComplete = true; };
            await UniTask.WaitUntil(() => spawnComplete);
        }
    }

    
    /// <summary>
    /// 当跳过庆祝动画 || 级联阶段的回调
    /// </summary>
    private void OnCongratulateSkip()
    {
        LevelProgress = LevelProgress.Finished;
        TokenSource?.Cancel();

        var powerupDic = GameBoardManager.instance.GetGameBoardUnclaimedPowerupCountDic();
        // 新增Bomb
        int newBomb = Moves / 3;
        if (powerupDic.ContainsKey(Constants.PieceBombId))
        {
            powerupDic[Constants.PieceBombId] += newBomb;
        }
        else powerupDic.TryAdd(Constants.PieceBombId, newBomb);

        // 新增Rocket
        int newRocket = Moves - newBomb;
        if (powerupDic.ContainsKey(Constants.PieceHRocketId))
        {
            powerupDic[Constants.PieceHRocketId] += newRocket;
        }
        else powerupDic.TryAdd(Constants.PieceHRocketId, newRocket);

        // 新增金币
        uint newCoin = earnedBasicCoin ? 0 : basicEarnCoin;
        foreach (var kvp in powerupDic)
        {
            newCoin += (uint)kvp.Value * pieceConfigSO.allRegisteredPieces[kvp.Key].pieceRewardReference.rewardClearCoin;
        }
        userDataManager.EarnCoin(newCoin);

        // 关卡完成
        MainGameUIManager.Instance.UpdateRewardDisplay(9999, UserDataManager.NewCoin, true);
        MainGameUIManager.SetMoveCount(0);
        MainGameUIManager.PopupSuccessPage();
    }


    private async void OnAllMovesConsumed()
    {
        if (LevelProgress == LevelProgress.Initializing || 
            LevelProgress != LevelProgress.Playing) 
        { 
            return; 
        }
 
        MutePlayerInputOnGameBoard();           // 屏蔽玩家继续输入
        hintEntity.HintOn = false;              // 不再需要hint 

        // Wait for some frame to finish the final swap and/or activate some powerups
        // then wait for all the pieces on the are still && gameboard is inactivity &&
        // no executing collectables
        await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
        while (true)
        {
            var firstCheck = GameBoardManager.instance.IsAllPiecesOnGameBoardStill &&
                             GameBoardManager.instance.GameBoardInactivity &&
                             MainGameUIManager.Instance.IsAllExecutingCollectablesDestroyed;

            await UniTask.DelayFrame(2);
            if (firstCheck)
            {
                bool doubleCheck = GameBoardManager.instance.IsAllPiecesOnGameBoardStill &&
                                   GameBoardManager.instance.GameBoardInactivity &&
                                   MainGameUIManager.Instance.IsAllExecutingCollectablesDestroyed;
                if (doubleCheck)
                {
                    break;
                }
            }
            continue;
        }
        await UniTask.Delay(TimeSpan.FromSeconds(0.25f));

        LevelProgress = LevelProgress.Finished;
        if (LevelTarget.IsAllTargetsCompleted == false)
        {
            // 目标未完成, 游戏失败
            MainGameUIManager.OnOutOfMoves();
        } 
    }


    private async void OnMoveConsumedAssignPendingPowerups()
    {
        MutePlayerInputOnGameBoard();

        await UniTask.WaitUntil(() => GameBoardManager.instance.IsAllPiecesOnGameBoardStill &&
                                      GameBoardManager.instance.GameBoardInactivity);
        ReceivePlayerInputOnGameBoard();
        AssignPowerups();
    }


    /// <summary>
    /// 按照规则获取一个生成的棋子的id
    /// </summary>
    private bool basicRuleActivate = true;
    private HashSet<int> highWeightPieceIdsDic = new();
    private int highWeightPieceBouns = 0;
    public int GetPieceIdFromSpawner(GridPosition spawnerPosition)
    {
        if (LevelSpawnerRule.SpawnerRuleDic.TryGetValue(spawnerPosition, out var spawnerRuleQueue) == false)
            throw new NullReferenceException($"Spawner at {spawnerPosition} does not have any SpawnerRule");

        var spawnerRule = spawnerRuleQueue.Peek();
        var sourceRule = (spawnerRule?.rule) ?? throw new NullReferenceException("Rule is null");

        bool findRule = false;
        var dynamicRule = new List<int>();
        if (Level >= gameContextSettings.dynamicDifficultySetting.closeEndActivateSinceLevel)
        {
            // close-end strategy has the highest priority
            if (Moves <= 3 &&
                Level >= gameContextSettings.dynamicDifficultySetting.closeEndLastThreeActivateSinceLevel &&
                GameBoardManager.instance.CanTargetBeCompletedWithOneProp())
            {
                for (int i = 0; i < sourceRule.Count; i += 2)
                {
                    var sourcePieceId = sourceRule[i];
                    var sourcePieceWeight = sourceRule[i + 1];

                    dynamicRule.Add(sourcePieceId);
                    dynamicRule.Add(Constants.BasicPieceIds.Contains(sourcePieceId) ? 100 : sourcePieceWeight);
                }
                findRule = true;
            }
            else if (Moves <= 5 && 
                     Level >= gameContextSettings.dynamicDifficultySetting.closeEndLastFiveActivateSinceLevel &&
                     LevelTarget.TargetProgression <= 0.8f)
            {
                for (int i = 0; i < sourceRule.Count; i += 2)
                {
                    var sourcePieceId = sourceRule[i];
                    var sourcePieceWeight = sourceRule[i + 1];

                    dynamicRule.Add(sourcePieceId);
                    dynamicRule.Add(highWeightPieceIdsDic.Contains(sourcePieceId) ? sourcePieceWeight + 5 * GameBoardManager.instance.GetGameBoardFreePieceCountByPieceId(sourcePieceId) : sourcePieceWeight);
                }
                findRule = true;
            }
        }
        else if (!findRule && 
                 Level >= gameContextSettings.dynamicDifficultySetting.inGameActivateSinceLevel)
        {
            // inGame strategy has the middle priority
            var disappointmentOffset = ThisMoveNewScore switch
            {
                var x when x < 100 => 10,
                var x when x >= 100 && x < 400 => 0,
                var x when x >= 400 && x < 1000 => -20,
                var x when x >= 1000 => -1000,
                _ => 0
            };

            var currentDisappointment = Math.Max(Disappointment + disappointmentOffset, 0);
            if (currentDisappointment >= 30)
            {
                for (int i = 0; i < sourceRule.Count; i += 2)
                {
                    var sourcePieceId = sourceRule[i];
                    var sourcePieceWeight = sourceRule[i + 1];

                    dynamicRule.Add(sourcePieceId);
                    dynamicRule.Add(highWeightPieceIdsDic.Contains(sourcePieceId) ? sourcePieceWeight + 5 * GameBoardManager.instance.GetGameBoardFreePieceCountByPieceId(sourcePieceId) : sourcePieceWeight);
                }
            }
        }
        else if (basicRuleActivate &&
                 !findRule && 
                 Level >= gameContextSettings.dynamicDifficultySetting.basicActivateSinceLevel)
        {
            // basic strategy has the lowest priority
            // initialize the dictionary
            if (highWeightPieceIdsDic == null || highWeightPieceIdsDic.Count == 0)
            {
                var pieceIdList = new List<int>();
                var weightMapping = new List<int>(6);

                foreach (var kvp in LevelSpawnerRule.SpawnerRuleDic)
                {
                    var rule = kvp.Value.Peek().rule;

                    for (int i = 0; i < rule.Count; i += 2)
                    {
                        var id = rule[i];
                        var weight = rule[i + 1];

                        if (Constants.BasicPieceIds.Contains(id))
                        {
                            if (pieceIdList.Contains(id))
                            {
                                pieceIdList.Add(id);
                            }
                            weightMapping[id] += weight;
                        }
                    }
                }

                pieceIdList.Sort((a, b) => weightMapping[a].CompareTo(weightMapping[b]));
                highWeightPieceIdsDic = pieceIdList.Count switch
                {
                    3 or 4 => pieceIdList.Take(2).ToHashSet(),
                    5 => pieceIdList.Take(3).ToHashSet(),
                    _ => new()
                };

                if (highWeightPieceIdsDic.Count < 2)
                {
                    basicRuleActivate = false;
                }
                else highWeightPieceBouns = Math.Min(UserDataManager.RecentlyWeight, 200);
            }

            if (basicRuleActivate) 
            {
                for (int i = 0; i < sourceRule.Count; i += 2)
                {
                    var sourcePieceId = sourceRule[i];
                    var sourcePieceWeight = sourceRule[i + 1];

                    dynamicRule.Add(sourcePieceId);
                    dynamicRule.Add(highWeightPieceIdsDic.Contains(sourcePieceId) ? sourcePieceWeight + highWeightPieceBouns : sourcePieceWeight);
                }
            }
        }

        // 按照权重生成
        int pieceId = 0;
        List<int> finalRule = findRule ? dynamicRule : sourceRule;
        if (finalRule.Count % 2 == 0)
        {
            int totalWeight = finalRule
                .Where((value, index) => index % 2 != 0)
                .Sum();
            int randomWeight = GetRandomWeightInt(totalWeight);

            for (int i = 1; i < finalRule.Count; i += 2)
            {
                randomWeight -= finalRule[i];
                if (randomWeight <= 0)
                {
                    pieceId = finalRule[i - 1];
                    break;
                }
            }
        }

        if (!pieceConfigSO.allRegisteredPieces.ContainsKey(pieceId))
        {
            pieceId = 1;
            Debug.LogWarning("Try to spawn an invalid piece");
        }
        return pieceId;
    }


    public void Revive(uint newMoves, params int[] newPowerupIds)
    {
        // Reset some settings
        LevelProgress = LevelProgress.Playing;
        hintEntity.HintOn = UserDataManager.HintOn;
        ReceivePlayerInputOnGameBoard();

        // add moves
        Moves += (int)newMoves;
        OnMoveConsumed?.Invoke(Moves);

        // 分发携带的道具
        if (newPowerupIds != null && newPowerupIds.Length > 0)
        {
            // 重置复活道具分发状态
            AssignedRevivePowerups = false;
            RevivePowerupIds = newPowerupIds.ToList();

            AssignedBoostAndRetryPowerups = true;
            AssignedStreakPowerups = true;
            AssignedRevivePowerups = RevivePowerupIds.Count <= 0;
            AssignPowerups();
        }
    }


    private int GetRandomWeightInt(int maxNum) => UnityEngine.Random.Range(0, maxNum + 1);


    public async void SwitchSceneBack()
    {
        ButtonBase.Lock();
        UserDataManager.BuildSceneLoadComplete = false;
        UserDataManager.HomeSceneSwitchFlag = false;
        eventSystem.enabled = false;
        audioListener.enabled = false;
        DOTween.Clear();
        var op = SceneManager.LoadSceneAsync("HomeScene", LoadSceneMode.Additive);
        //op.allowSceneActivation = false;
        await UniTask.WaitUntil(() => UserDataManager.BuildSceneLoadComplete);
        await SceneManager.UnloadSceneAsync("GameScene");
        UserDataManager.HomeSceneSwitchFlag = true;
    }

    #region Debug Test Method
    public void ForceCompleteLevel()
    {
        // 屏蔽玩家继续输入
        MutePlayerInputOnGameBoard();

        // 目标完成, 游戏胜利
        hintEntity.HintOn = false;
        TokenSource = new();
        MainGameUIManager.OnLevelSuccess(OnCongratulateComplete, OnCongratulateSkip, TokenSource.Token);
    }

    public void ForceFailLevel()
    {
        OnAllMovesConsumed();
    }
    #endregion
}


public enum LevelProgress
{
    Initializing,
    Playing,
    Rewarding,
    Finished
}

public enum UsingProp
{
    None,
    Hammer,
    Gun,
    Cannon,
    Dice
}