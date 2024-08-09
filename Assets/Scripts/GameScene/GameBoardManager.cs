using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using DG.Tweening;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


[DefaultExecutionOrder(-9999)]
public class GameBoardManager : MonoBehaviour
{
    [Header("Shuffle")]
    //[SerializeField] private uint subUpdateIntervalFrame;
    [SerializeField] private int startShuffleMaxAttempt;
    [SerializeField] private int autoShuffleMaxAttempt;

    [Header("Instantiate Option")]
                     public float slotOriginalInterval;
    [SerializeField] private float approximateDistance = 0.025f;
    [SerializeField] private Transform pieceHolderTransform;
    [SerializeField] private Transform frameHolderTransform;
    [SerializeField] private Slot slotPrefab;
    [SerializeField] private FloorConfigSO floorConfigSO;
    [SerializeField] private PieceConfigSO pieceConfigSO;

    [Header("Object Pools")]
    [SerializeField] private PiecePool piecePool;
    [SerializeField] private DamageVFXPool damageVFXPool;
    [SerializeField] private ExplodeVFXPool explodeVFXPool;

    [Header("Powerup")]
    [SerializeField] private GameBoardHintEntity gameBoardHintEntity;
    [SerializeField] private List<AnimationReferenceAsset> hintAnimations;
    [SerializeField] private AnimationReferenceAsset swapVFXAnimation;
    [SerializeField] private RocketAction rocketVFXPrefab;

    [Header("Prop")]
    [SerializeField] private PropButton hammerPropButton;
    [SerializeField] private Hammer hammerPrefab;
    [SerializeField] private Gun gunPrefab;
    [SerializeField] private Cannon cannonPrefab;
    [SerializeField] private Dice dicePrefab;
 

    // Context & Setting
    public Grid slotGrid;                               // 当前棋盘网格
    private int xMax;
    private int yMax;
    private bool boardInitialized;                      // 棋盘初始化标识符
    private Vector3 gridOriginalPosition;               // 棋盘网格原点(0,0)位置

    private PieceAnimationSetting pieceAnimationSetting;
    private PowerupSetting powerupSetting;


    // Pre-defined
    public readonly SelectRandom selectRandomStrategy = new();                      // 随机选择目标棋子方案(FlyBomb)
    public readonly SelectMost selectMostStrategy = new();                          // 选择最多的目标棋子方案(FlyBomb)
    public readonly List<int> matchCheckOrder = new() { 4, 1, 2, 3, 0, -1 };        // 检测Powerup的优先级排序, 下标对应PowerupSetting.registeredPowerups中的位置
    public readonly List<int> possibleMatchCheckOrder = new() { 4, 1, 2, 0 };       // 检测可能合成Powerup的优先级排序, 下标对应PowerupSetting.registeredPowerups中的位置, 基础三消需要单独检测
    private readonly List<List<GridPosition>> basicMatchPositions = new()           // 基础三消的全部相对位置可能性
    {
        new() { GridPosition.Zero, GridPosition.Left, GridPosition.Right },
        new() { GridPosition.Zero, GridPosition.Up, GridPosition.Down },
        new() { GridPosition.Zero, GridPosition.Left, GridPosition.Left + GridPosition.Left },
        new() { GridPosition.Zero, GridPosition.Right, GridPosition.Right + GridPosition.Right },
        new() { GridPosition.Zero, GridPosition.Up, GridPosition.Up + GridPosition.Up },
        new() { GridPosition.Zero, GridPosition.Down, GridPosition.Down + GridPosition.Down }
    };
    private readonly GridPosition[] adjacentLookupDirections = new[]
    {
        GridPosition.Up, GridPosition.Right, GridPosition.Down, GridPosition.Left
    };


    public HashSet<int> RainbowSelectBasicPieceIds { get; private set; } = new();   // 被Rainbow选择的基础棋子Id
    public HashSet<int> AllowedBasicPieceIds { get; private set; } = new();         // 剔除被Rainbow选择的基础棋子Id后允许Match的基础棋子Id


    // Track gameboard state
    public bool GameBoardInactivity { get; private set; }
    public bool IsRearranging { get; private set; }
    public float InactivityDuration { get; private set; }
    public float HintCoolDown { get; private set; }
    public bool IsAllPiecesOnGameBoardStill
    {
        get
        {
            foreach (var slot in slotGrid)
            {
                if (slot.incomingPiece != null || (slot.piece != null && slot.piece.MovingToSlot != null))
                {
                    return false;
                }
            }
            return true;
        }
    }


    private List<IGameBoardAction> ActivatedActions { get; set; } = new();  // 进行中的powerup和prop事件


    private Queue<GridPosition> QueuedClickPositions { get; set; } = new();                                 // 缓存的点击位置
    private Queue<(GridPosition, GridPosition)> QueuedSwapPositions { get; set; } = new();                  // 缓存的交换位置
    private Dictionary<(GridPosition, GridPosition), SwapStage> SwappingPositions { get; set; } = new();    // 正在交换的位置


    private List<GridPosition> TickingPositions { get; set; } = new();      // 移动到的位置
    private List<GridPosition> NewTickingPositions { get; set; } = new();   // 新增的移动到的位置
    private List<GridPosition> EmptyPositions { get; set; } = new();        // 无棋子的空闲位置
    private HashSet<GridPosition> NewEmptyPositions { get; set; } = new();  // 新增的空闲位置
    private HashSet<Piece> CompleteMovePieces { get; set; } = new();        // 运动到预定位置的棋子


    private HashSet<GridPosition> PositionsToCheckMatch { get; set; } = new();                  // 进行级联检测的位置
    private Dictionary<GridPosition, float> DelayedPostionsToCheckMatch { get; set; } = new();  // 因为空位置导致的被推迟的位置
    private HashSet<Match> MatchesToExecute { get; set; } = new();                              // 产生的Match
    private List<UniTask> ExecutingMatchAndDamageTasks { get; set; } = new();

    private PriorityQueue<PossibleSwap, int> PossibleSwaps { get; set; } = new();


    // Callbacks
    public Dictionary<GridPosition, Action<Damage>> PositionBottomDamagedCallbacks { get; private set; } = new();           // Damage中间层基础棋子回调
    public Dictionary<GridPosition, Action<Damage>> PositionAdjacentDamagedCallbacks { get; private set; } = new();         // Damage相邻基础棋子回调
    public Dictionary<GridPosition, Action<Damage>> PositionEnterCollectCallbacks { get; private set; } = new();            // 进入位置收集棋子回调

    private Action SubUpdateCallbacks { get; set; }

    public static GameBoardManager instance;
    private void Awake()
    {
        if (instance != null)
            Destroy(instance);
        instance = this;
    }


    public void SetGameBoardEssentials(PieceAnimationSetting pieceAnimationSetting, PowerupSetting powerupSetting, 
                                       Action<int> onReleasePieceRecordCallback)
    {
        this.pieceAnimationSetting = pieceAnimationSetting;
        this.powerupSetting = powerupSetting;

        // 初始化对象池
        piecePool.InitializePool(GameManager.LevelSpawnerRule, onReleasePieceRecordCallback);
        damageVFXPool.InitializePool();
        explodeVFXPool.InitializePool();

        // 开启SubUpdate
        //RegisterSubUpdate(HandleAutoShuffle);
        //StartSubUpdate();
    }


    //private void RegisterSubUpdate(Action callback)
    //{
    //    SubUpdateCallbacks += callback;
    //}

    //private void UnRegisterSubUpdate(Action callback)
    //{
    //    if (SubUpdateCallbacks != null)
    //        SubUpdateCallbacks -= callback;
    //}

    //private void StartSubUpdate()
    //{
    //    SubUpdate().Forget();
    //}


    /// <summary>
    /// 设置棋盘网格和目标棋盘网格
    /// </summary>
    public void SetGridAndTargetGrid(GameLevel gameLevel)
    {
        if (gameLevel == null || gameLevel.slotInfo == null || gameLevel.pieceInfo == null)
        {
            throw new System.ArgumentNullException("Game level config is invalid");
        }

        // 新建棋盘, 并初始化
        xMax = gameLevel.xMax;
        yMax = gameLevel.yMax;
        slotOriginalInterval = floorConfigSO.spritePixels / 100f;
        gridOriginalPosition = new Vector3(slotOriginalInterval * (xMax - 1) / -2, slotOriginalInterval * (yMax - 1) / 2, 0) + transform.position; 

        slotGrid = new Grid();
        slotGrid.SetGrid(new Slot[yMax, xMax]);

        // 初始化棋盘槽位
        foreach (var array in gameLevel.slotInfo)
        {
            int x = array[0], y = array[1];
            var insPosition = new GridPosition(x, y);
            var insWorldPosition = GetGridPositionWorldPosition(insPosition);
            bool slotActive = array[2] == 1;
            bool slotDropPort = array[3] == 1;

            var slot = Instantiate(slotPrefab, insWorldPosition, Quaternion.identity, transform);
            slot.transform.localScale = Vector3.one;
            slot.InitializeSlot(insPosition, slotActive, slotDropPort);

            if (floorConfigSO.SpriteDictionary.TryGetValue(0, out var floorSprite) == false)
                throw new NullReferenceException("Cannot find sprite for instantiating");

            if (slotActive)
            {
                gameLevel.GetDataByGridPositon(insPosition, out var bottomInfo, out var pieceInfo, out var upperInfo);

                // 生成地板
                var floor = Instantiate(floorConfigSO.floorPrefab, insWorldPosition, Quaternion.identity, slot.transform);
                floor.InitializeFloor(floorSprite, 1f, insPosition);
                floor.SpriteMask.sprite = slotDropPort ? floorSprite : null; // 只有生成口才需要开启遮罩
                slot.floor = floor;


                // 生成底层棋子
                int bottomPieceId = bottomInfo[2];
                if (bottomPieceId != 0)
                {
                    int bottomPieceClearNum = bottomInfo[3];
                    var bottomPieceRootGridPosition = new GridPosition(bottomInfo[4], bottomInfo[5]);
                    var bottomPiece = PlacePieceAt(insPosition, bottomPieceRootGridPosition, bottomPieceId, bottomPieceClearNum, PieceColors.Colorless);
                    slot.bottomPiece = bottomPiece;
                }


                // 生成中间棋子
                int pieceId = pieceInfo[2];
                if (pieceId != 0)
                {
                    int pieceClearNum = pieceInfo[3];
                    var pieceRootGridPosition = new GridPosition(pieceInfo[4], pieceInfo[5]);
                    List<int> pieceColorIndex = new();
                    for (int i = 6; i < pieceInfo.Length; i++)
                        pieceColorIndex.Add(pieceInfo[i]);
                    PieceColors pieceColors = PieceColors.Colorless;
                    pieceColorIndex.ForEach(colorIndex =>
                    {
                        pieceColors |= (PieceColors)(1 << colorIndex);
                    });
                    var piece = PlacePieceAt(insPosition, pieceRootGridPosition, pieceId, pieceClearNum, pieceColors);
                    slot.piece = piece;
                }
                else if(pieceInfo.Length == 5)
                {
                    var pieceRootGridPosition = new GridPosition(pieceInfo[3], pieceInfo[4]);
                    if (!pieceRootGridPosition.Equals(new GridPosition(-1, -1)) &&
                        slotGrid[pieceRootGridPosition] != null)
                    {
                        slot.piece = slotGrid[pieceRootGridPosition].piece;
                    }
                }


                // 生成顶层棋子
                int upperPieceId = upperInfo[2];
                if (upperPieceId != 0)
                {
                    int upperPieceClearNum = upperInfo[3];
                    var upperPieceRootGridPosition = new GridPosition(upperInfo[4], upperInfo[5]);
                }
            }

            slotGrid[insPosition] = slot;
        }

        // 生成线框
        {
            var board = new bool[xMax + 4, yMax + 4];
            var intBoard = new int[xMax + 4, yMax + 4];

            for (int i = 0; i < xMax + 4; i++)
            {
                for (int j = 0; j < yMax + 4; j++)
                {
                    board[i, j] = false;
                    intBoard[i, j] = 0;
                }
            }

            foreach (var array in gameLevel.slotInfo)
            {
                int x = array[0] + 2, y = array[1] + 2;
                board[x, y] = array[2] == 1;
            }

            for (int i = 1; i < xMax + 3; i++)
            {
                for (int j = 1; j < yMax + 3; j++)
                {
                    if (!board[i, j])
                    {
                        if (board[i, j - 1])
                            intBoard[i, j] += 1;
                        if (board[i + 1, j])
                            intBoard[i, j] += 2;
                        if (board[i, j + 1])
                            intBoard[i, j] += 4;
                        if (board[i - 1, j])
                            intBoard[i, j] += 8;
                        if ((!board[i + 1, j]) && (!board[i, j - 1]) && (board[i + 1, j - 1]))
                            intBoard[i, j] += 16;
                        if ((!board[i + 1, j]) && (!board[i, j + 1]) && (board[i + 1, j + 1]))
                            intBoard[i, j] += 32;
                        if ((!board[i - 1, j]) && (!board[i, j + 1]) && (board[i - 1, j + 1]))
                            intBoard[i, j] += 64;
                        if ((!board[i - 1, j]) && (!board[i, j - 1]) && (board[i - 1, j - 1]))
                            intBoard[i, j] += 128;
                    }
                }
            }

            var topLeftPos = Vector3.left * (xMax + 1) * slotOriginalInterval * 0.5f + Vector3.up * (yMax + 1) * slotOriginalInterval * 0.5f;
            for (int i = 1; i < xMax + 3; i++)
            {
                for (int j = 1; j < yMax + 3; j++)
                {
                    if (0 < intBoard[i, j])
                    {
                        try
                        {
                            floorConfigSO.SpriteDictionary.TryGetValue(intBoard[i, j], out var frameSprite);

                            var frame = Instantiate(floorConfigSO.framePrefab, Vector3.zero, Quaternion.identity, frameHolderTransform);
                            frame.transform.localPosition = topLeftPos + Vector3.right * (i - 1) * slotOriginalInterval + Vector3.down * (j - 1) * slotOriginalInterval;
                            frame.sprite = frameSprite;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }
            }

        }

        FindAllSlotsVerticalSpawner();      // 初始化每个槽位的对应的垂直掉落口
        UpdateAllSlotsFillType();           // 更新每个槽位对应的填充类型

        // TODO: fill empty positions according to config
        bool hasExistMatch = false, hasPossibleMatch = false;
        Dictionary<int, int> fillPiecesDic = new ();
        if (gameLevel.fillType == 0)
        {
            // fill spawner slot reachable positions
            foreach (var slot in slotGrid)
            {
                if (slot.IsActive &&
                    slot.FillType != -1 &&
                    slot.IsEmpty)
                {
                    var position = slot.GridPosition;
                    var availableBasicPieces = GameManager.LevelSpawnerRule.AppearPieceIds.ToList();

                    int leftPieceId = Constants.PieceNoneId,
                        bottomPieceId = Constants.PieceNoneId,
                        rightPieceId = Constants.PieceNoneId,
                        topPieceId = Constants.PieceNoneId;

                    // check left
                    if (GridMath.IsPositionOnBoard(slotGrid, position + GridPosition.Left, out var leftSlot) &&
                        leftSlot.piece != null)
                    {
                        leftPieceId = leftSlot.piece.Id;

                        if (GridMath.IsPositionOnBoard(slotGrid, position + new GridPosition(-2, 0), out var leftLeftSlot) &&
                            leftLeftSlot.piece != null && leftPieceId == leftLeftSlot.piece.Id)
                        {
                            availableBasicPieces.Remove(leftPieceId);
                        }
                    }

                    // check bottom
                    if (GridMath.IsPositionOnBoard(slotGrid, position + GridPosition.Down, out var bottomSlot) &&
                        bottomSlot.piece != null)
                    {
                        bottomPieceId = bottomSlot.piece.Id;

                        if (GridMath.IsPositionOnBoard(slotGrid, position + new GridPosition(0, 2), out var bottomBottomSlot) &&
                            bottomBottomSlot.piece != null && bottomPieceId == bottomBottomSlot.piece.Id)
                        {
                            availableBasicPieces.Remove(bottomPieceId);
                        }

                        if (leftPieceId != Constants.PieceNoneId && leftPieceId == bottomPieceId &&
                            GridMath.IsPositionOnBoard(slotGrid, position + GridPosition.DownLeft, out var bottomLeftSlot) &&
                            bottomLeftSlot.piece != null && bottomPieceId == bottomLeftSlot.piece.Id)
                        {
                            availableBasicPieces.Remove(leftPieceId);
                        }
                    }

                    // check right
                    if (GridMath.IsPositionOnBoard(slotGrid, position + GridPosition.Right, out var rightSlot) &&
                        rightSlot.piece != null)
                    {
                        rightPieceId = rightSlot.piece.Id;

                        if (rightPieceId != Constants.PieceNoneId && leftPieceId == rightPieceId)
                        {
                            availableBasicPieces.Remove(rightPieceId);
                        }

                        if (GridMath.IsPositionOnBoard(slotGrid, position + new GridPosition(2, 0), out var rightRightSlot) &&
                            rightRightSlot.piece != null && rightPieceId == rightRightSlot.piece.Id)
                        {
                            availableBasicPieces.Remove(rightPieceId);
                        }

                        if (rightPieceId != Constants.PieceNoneId && rightPieceId == bottomPieceId &&
                            GridMath.IsPositionOnBoard(slotGrid, position + GridPosition.DownRight, out var downRightSlot) &&
                            downRightSlot.piece != null && rightPieceId == downRightSlot.piece.Id)
                        {
                            availableBasicPieces.Remove(rightPieceId);
                        }
                    }

                    // check up
                    if (GridMath.IsPositionOnBoard(slotGrid, position + GridPosition.Up, out var topSlot) &&
                        topSlot.piece != null)
                    {
                        topPieceId = topSlot.piece.Id;

                        if (topPieceId != Constants.PieceNoneId && topPieceId == bottomPieceId)
                        {
                            availableBasicPieces.Remove(topPieceId);
                        }

                        if (GridMath.IsPositionOnBoard(slotGrid, position + new GridPosition(0, -2), out var topTopSlot) &&
                            topTopSlot.piece != null && topPieceId == topTopSlot.piece.Id)
                        {
                            availableBasicPieces.Remove(topPieceId);
                        }

                        if (topPieceId != Constants.PieceNoneId && topPieceId == rightPieceId &&
                            GridMath.IsPositionOnBoard(slotGrid, position + GridPosition.UpRight, out var topRightSlot) &&
                            topRightSlot.piece != null && topPieceId == topRightSlot.piece.Id)
                        {
                            availableBasicPieces.Remove(topPieceId);
                        }

                        if (topPieceId != Constants.PieceNoneId && topPieceId == leftPieceId &&
                            GridMath.IsPositionOnBoard(slotGrid, position + GridPosition.UpLeft, out var topLeftSlot) &&
                            topLeftSlot.piece != null && topPieceId == topLeftSlot.piece.Id)
                        {
                            availableBasicPieces.Remove(topPieceId);
                        }
                    }

                    var choosePieceId = Constants.PieceRedId;
                    if (fillPiecesDic.Count <= 0 || fillPiecesDic.Any(kvp => kvp.Value >= 3))
                    {
                        choosePieceId = availableBasicPieces[Random.Range(0, availableBasicPieces.Count)];
                    }
                    else
                    {
                        choosePieceId = availableBasicPieces.FirstOrDefault(x => fillPiecesDic.ContainsKey(x) && fillPiecesDic[x] < 3);
                    }

                    if (!pieceConfigSO.allRegisteredPieces.TryGetValue(choosePieceId, out var registeredPiece))
                    {
                        Debug.LogWarning($"No Available Piece for {position}, pieceId = {choosePieceId}");
                        choosePieceId = Constants.PieceRedId;
                        //hasMatch = true;
                    }
                    slot.piece = PlacePieceAt(position, position, choosePieceId, registeredPiece.pieceInsArgs.pieceClearNum, registeredPiece.pieceInsArgs.pieceColors);
                    
                    if (!fillPiecesDic.TryAdd(choosePieceId, 1))
                    {
                        fillPiecesDic[choosePieceId]++;
                    }
                }
            }

            HandleStartShuffle();
        }
        else if (gameLevel.fillType == 1)
        {
            // fill all empty positions
            foreach (var slot in slotGrid)
            {
                
            }
        }

        // rearrange the gameboard if needed
        foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            foreach (var index in matchCheckOrder)
            {
                if (CheckMatchAt(gridPosition, index, createMatch: false))
                {
                    hasExistMatch = true;
                    HandleStartShuffle();
                    break;
                }
            }

            if (hasExistMatch)
                break;
        }

        boardInitialized = true;

        // check match if needed
        if (hasExistMatch)
        {
            var checkedPositions = new HashSet<GridPosition>();
            matchCheckOrder.ForEach(index =>
            {
                foreach (var pos in (IEnumerable<GridPosition>)slotGrid)
                {
                    if (checkedPositions.Contains(pos))
                        continue;

                    if (CheckMatchAt(pos, index))
                        checkedPositions.Add(pos);
                }
            });
        }
    }


    public void TryAssignBoostAndRetryPowerups(Action<Queue<(Slot assignReplaceSlot, int assignPowerupPieceId)>> onAssignBoostAndRetrySucceed,
                                               Action onAssignBoostAndRetryFail,
                                               Action onAssignBoostAndRetryComplete)
    {
        var availableSlots = new List<Slot>();
        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive)
                continue;

            if (slot.upperPiece != null)
                continue;

            if (slot.HasMoveConstrain)
                continue;

            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            if (piece != null &&
                Constants.BasicPieceIds.Contains(piece.Id) &&
                !GameManager.LevelTarget.IsThisPieceTarget(piece.Id, out var remainCount) &&
                AdjacentSlotsDonotIncludePowerups(slot.GridPosition))
            {
                availableSlots.Add(slot);
            }
        }


        bool assignSucceed = false;
        if (UserDataManager.BoostPowerups.Count > 0 ||
            UserDataManager.RetryPowerup != 0)
        {
            var assignPowerupIds = new List<int>();
            if (UserDataManager.RetryPowerup != 0)
            { 
                assignPowerupIds.Add(UserDataManager.RetryPowerup); 
            }
            UserDataManager.BoostPowerups.ForEach(id => assignPowerupIds.Add(id));

            if (availableSlots.Count > assignPowerupIds.Count)
            {
                var random = new System.Random();
                availableSlots = availableSlots.OrderBy(x => random.Next()).ToList();

                var argQueue = new Queue<(Slot, int)>();
                while (assignPowerupIds.Count > 0)
                {
                    var assignPowerupId = assignPowerupIds.FirstOrDefault();
                    var replaceSlot = availableSlots.FirstOrDefault();
                    if (replaceSlot == null)
                    {
                        break;
                    }

                    argQueue.Enqueue((replaceSlot, assignPowerupId));
                    assignPowerupIds.RemoveAt(0);

                    availableSlots.RemoveAt(0);
                    foreach (var adjacent in GetAdjacentSlots(slotGrid, replaceSlot.GridPosition, (0, false, false)))
                    {
                        if (availableSlots.Contains(adjacent))
                        {
                            availableSlots.Remove(adjacent);
                        }
                    }
                }

                if (assignPowerupIds.Count <= 0)
                {
                    assignSucceed = true;

                    UserDataManager.ClearBoostPowerups();
                    onAssignBoostAndRetrySucceed?.Invoke(argQueue);
                }
            }

            if (!assignSucceed)
            {
                onAssignBoostAndRetryFail?.Invoke();
                return;
            }
        }

        onAssignBoostAndRetryComplete?.Invoke();
    }


    public void TryAssignStreakPowerups(Action<Queue<(Slot assignReplaceSlot, int assignPowerupPieceId)>> onAssignStreakSucceed,
                                        Action onAssignStreakFail,
                                        Action onAssignStreakComplete)
    {
        var availableSlots = new List<Slot>();
        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive)
                continue;

            if (slot.upperPiece != null)
                continue;

            if (slot.HasMoveConstrain)
                continue;

            if (slot.transform.position.x >= Constants.StreakBoxBound.xMin &&
                slot.transform.position.x <= Constants.StreakBoxBound.xMax &&
                slot.transform.position.y >= Constants.StreakBoxBound.yMin &&
                slot.transform.position.y <= Constants.StreakBoxBound.yMax)
                continue;

            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            if (piece != null &&
                Constants.BasicPieceIds.Contains(piece.Id) &&
                !GameManager.LevelTarget.IsThisPieceTarget(piece.Id, out var remainCount) &&
                AdjacentSlotsDonotIncludePowerups(slot.GridPosition))
            {
                availableSlots.Add(slot);
            }
        }


        bool assignSucceed = false;
        if (UserDataManager.WinStreak != 0)
        {
            List<int> streakPowerupIds = UserDataManager.WinStreak switch
            {
                1 => new List<int> { Random.Range(0, 1f) <= 0.5f ? Constants.PieceHRocketId : Constants.PieceVRocketId },
                2 => new List<int> { Random.Range(0, 1f) <= 0.5f ? Constants.PieceHRocketId : Constants.PieceVRocketId, Constants.PieceBombId },
                3 => new List<int> { Random.Range(0, 1f) <= 0.5f ? Constants.PieceHRocketId : Constants.PieceVRocketId, Constants.PieceBombId, Constants.PieceRainbowId },
                _ => new List<int>()
            };

            if (availableSlots.Count >= streakPowerupIds.Count)
            {
                var random = new System.Random();
                availableSlots = availableSlots.OrderBy(x => random.Next()).ToList();

                var argQueue = new Queue<(Slot, int)>();
                while (streakPowerupIds.Count > 0)
                {
                    var assignPowerupId = streakPowerupIds.FirstOrDefault();
                    var replaceSlot = availableSlots.FirstOrDefault();
                    if (replaceSlot == null)
                    {
                        break;
                    }

                    argQueue.Enqueue((replaceSlot, assignPowerupId));
                    streakPowerupIds.RemoveAt(0);

                    availableSlots.RemoveAt(0);
                    foreach (var adjacent in GetAdjacentSlots(slotGrid, replaceSlot.GridPosition, (0, false, false)))
                    {
                        if (availableSlots.Contains(adjacent))
                        {
                            availableSlots.Remove(adjacent);
                        }
                    }
                }

                if (streakPowerupIds.Count <= 0)
                {
                    assignSucceed = true;

                    onAssignStreakSucceed?.Invoke(argQueue);
                }
            }

            if (!assignSucceed)
            {
                onAssignStreakFail?.Invoke();
                return;
            }
        }

        onAssignStreakComplete?.Invoke();
    }


    public void TryAssignRevivePowerups(Action<Queue<(Slot assignReplaceSlot, int assignPowerupPieceId)>> onAssignReviveSucceed,
                                        Action onAssignReviveFail,
                                        Action onAssignReviveComplete)
    {
        var availableSlots = new List<Slot>();
        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive)
                continue;

            if (slot.upperPiece != null)
                continue;

            if (slot.HasMoveConstrain)
                continue;

            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            if (piece != null &&
                Constants.BasicPieceIds.Contains(piece.Id) &&
                !GameManager.LevelTarget.IsThisPieceTarget(piece.Id, out var remainCount) &&
                AdjacentSlotsDonotIncludePowerups(slot.GridPosition))
            {
                availableSlots.Add(slot);
            }
        }


        bool assignSucceed = false;
        if (GameManager.instance.RevivePowerupIds.Count > 0)
        {
            var revivePowerupIds = new List<int>();
            GameManager.instance.RevivePowerupIds.ForEach(powerupId => revivePowerupIds.Add(powerupId));

            if (availableSlots.Count >= revivePowerupIds.Count)
            {
                var random = new System.Random();
                availableSlots = availableSlots.OrderBy(x => random.Next()).ToList();

                var argQueue = new Queue<(Slot, int)>();
                while (revivePowerupIds.Count > 0)
                {
                    var assignPowerupId = revivePowerupIds.FirstOrDefault();
                    var replaceSlot = availableSlots.FirstOrDefault();
                    if (replaceSlot == null)
                    {
                        break;
                    }

                    argQueue.Enqueue((replaceSlot, assignPowerupId));
                    revivePowerupIds.RemoveAt(0);

                    availableSlots.RemoveAt(0);
                    foreach (var adjacent in GetAdjacentSlots(slotGrid, replaceSlot.GridPosition, (0, false, false)))
                    {
                        if (availableSlots.Contains(adjacent))
                        {
                            availableSlots.Remove(adjacent);
                        }
                    }
                }
                
                if (revivePowerupIds.Count <= 0)
                {
                    assignSucceed = true;

                    GameManager.instance.RevivePowerupIds.Clear();
                    onAssignReviveSucceed?.Invoke(argQueue);
                }
            }

            if (!assignSucceed)
            {
                onAssignReviveFail?.Invoke();
                return;
            }
        }

        onAssignReviveComplete?.Invoke();
    }


    private void Update()
    {
        if (!boardInitialized) { return; }

        GameBoardInactivity = GameManager.CurrentProp == UsingProp.None && 
                              GameManager.instance.AssigningPowerupTasks.Count <= 0;

        if (GameManager.LevelProgress == LevelProgress.Playing)
        {
            // 正常游戏流程主逻辑循环
            // 对需要处理的powerup事件进行更新
            if (ActivatedActions.Count > 0)
            {
                DoTickActions();

                GameBoardInactivity = false;
            }

            // 处理用户输入
            if (QueuedClickPositions.Count > 0 || QueuedSwapPositions.Count > 0)
            {
                DoHandleInput();

                GameBoardInactivity = false;
            }

            // 更新参数
            UpdateAllSlotsFillType();       // 更新棋子的FillType
            UpdateAllowedBasicPieceIds();   // 更新允许Match的基础棋子Id

            // 对需要填充的位置移动对应的棋子
            if (TickingPositions.Count > 0)
            {
                DoMovePieces();

                GameBoardInactivity = false;
            }

            // 对空位置尝试进行填充
            if (EmptyPositions.Count > 0)
            {
                DoEmptyCheck();

                GameBoardInactivity = false;
            }

            // 对移动完成且没有后续移动的棋子进行停止
            if (CompleteMovePieces.Count > 0)
            {
                DoStopPieces();

                GameBoardInactivity = false;
            }

            // 对移动结束后的位置检测是否存在Match
            if (PositionsToCheckMatch.Count > 0)
            {
                DoMatchCheck();

                GameBoardInactivity = false;
            }

            // 对检测到的Match进行处理
            if (MatchesToExecute.Count > 0)
            {
                DoExecuteMatches(false);

                GameBoardInactivity = false;
            }

            if (ExecutingMatchAndDamageTasks.Count > 0)
            {
                DoClearCompletedTasks();

                GameBoardInactivity = false;
            }

            // 将本帧新增的有移动发生的位置加入列表, 下一帧移动
            if (NewTickingPositions.Count > 0)
            {
                TickingPositions.AddRange(NewTickingPositions);
                NewTickingPositions.Clear();

                GameBoardInactivity = false;
            }

            // 当不活跃超过一段时间后进行提示(如若开启)
            if (GameBoardInactivity)
            {
                InactivityDuration += Time.deltaTime;

                if (gameBoardHintEntity.HintOn &&
                    InactivityDuration >= gameBoardHintEntity.InactivityBeforeHint)
                {
                    // 检测全部可能的合成(如果未检测的情况下)
                    if (PossibleSwaps.Count <= 0)
                    {
                        FindAllPossibleMatch();
                    }


                    if (PossibleSwaps.Count <= 0 && !IsRearranging)
                    {
                        // TODO: fix this when there's no match
                        // 未找到任何满足的匹配
                        HandleAutoShuffle();
                    }
                    else if (PossibleSwaps.Count > 0 && HintCoolDown <= 0)
                    {
                        // Hint冷却完毕则进行Hint
                        DoHint();
                    }

                    // 开始Hint冷却(当前不在Hint中)
                    if (gameBoardHintEntity.IsHinting == false)
                    {
                        HintCoolDown = Mathf.Max(0f, HintCoolDown - Time.deltaTime);
                    }
                }
            }
            else
            {
                InactivityDuration = 0f;
                HintCoolDown = 0f;
                if (PossibleSwaps.Count > 0)
                {
                    PossibleSwaps.Clear();
                }
                gameBoardHintEntity.ShutDownHint();
            }
        }
        else if (GameManager.LevelProgress == LevelProgress.Rewarding)
        {
            // 游戏奖励主逻辑循环
            InactivityDuration = 0f;
            HintCoolDown = 0f;
            gameBoardHintEntity.HintOn = false;

            // 对需要处理的powerup事件进行更新
            if (ActivatedActions.Count > 0)
            {
                DoTickActions();

                GameBoardInactivity = false;
            }

            // 更新参数
            UpdateAllSlotsFillType();       // 更新棋子的FillType
            UpdateAllowedBasicPieceIds();   // 更新允许Match的基础棋子Id

            // 对需要填充的位置移动对应的棋子
            if (TickingPositions.Count > 0)
            {
                DoMovePieces();

                GameBoardInactivity = false;
            }

            // 对空位置尝试进行填充
            if (EmptyPositions.Count > 0)
            {
                DoEmptyCheck(true);

                GameBoardInactivity = false;
            }

            // 对移动完成且没有后续移动的棋子进行停止
            if (CompleteMovePieces.Count > 0)
            {
                DoStopPieces();

                GameBoardInactivity = false;
            }

            // 对移动结束后的位置检测是否存在Match
            if (PositionsToCheckMatch.Count > 0)
            {
                DoMatchCheck();

                GameBoardInactivity = false;
            }

            // 对检测到的Match进行处理(在本循环中生成的Powerup在初始化完成后会立即激活)
            if (MatchesToExecute.Count > 0)
            {
                DoExecuteMatches(true);

                GameBoardInactivity = false;
            }

            if (ExecutingMatchAndDamageTasks.Count > 0)
            {
                DoClearCompletedTasks();

                GameBoardInactivity = false;
            }

            // 将本帧新增的有移动发生的位置加入列表, 下一帧移动
            if (NewTickingPositions.Count > 0)
            {
                TickingPositions.AddRange(NewTickingPositions);
                NewTickingPositions.Clear();

                GameBoardInactivity = false;
            }
        }
    }


    //private async UniTaskVoid SubUpdate()
    //{
    //    while (true)
    //    {
    //        await UniTask.DelayFrame((int)subUpdateIntervalFrame);

    //        SubUpdateCallbacks?.Invoke();
    //    }
    //}


    private void DoTickActions()
    {
        for (int i = 0; i < ActivatedActions.Count; i++)
        {
            // 移除空Actions和完成的Actions
            if (ActivatedActions[i] == null || !ActivatedActions[i].Tick())
            {
                ActivatedActions.RemoveAt(i);
                i--;
            }
        }
    }


    private void DoHandleInput()
    {
        while (QueuedClickPositions.Count > 0)
        {
            if (GameManager.CurrentProp != UsingProp.None)
            {
                UsingPropClickPiece(QueuedClickPositions.Dequeue(), GameManager.CurrentProp);
            }
            else ClickPiece(QueuedClickPositions.Dequeue());
        }
        while (QueuedSwapPositions.Count > 0)
        {
            var t = QueuedSwapPositions.Dequeue();
            SwapPiece(t.Item1, t.Item2);
        }
    }


    /// <summary>
    /// 更新槽位的填充种类(斜向填充: 1 or 不可填充: -1 or 垂直填充: 0)
    /// </summary>
    private void UpdateAllSlotsFillType()
    {
        foreach (var slot in slotGrid)
        {
            slot.FillType = 0;
            if (slot.IsActive)
            {
                if (slot.HasUnMovablePiece)
                {
                    slot.FillType = -1;
                    continue;
                }

                if (slot.Spawner == null)
                {
                    var position = slot.GridPosition;

                    // 找到最近的垂直方向上的活跃的槽位
                    var upPosition = position + GridPosition.Up;
                    var upRightPosition = position + GridPosition.UpRight;
                    var upLeftPosition = position + GridPosition.UpLeft;
                    if (GridMath.IsPositionOnGrid(slotGrid, upPosition) && 
                        slotGrid[upPosition].IsActive == false)
                    {
                        // 当上方一格位置上的槽位存在但是不活跃, 那么需要找到最近的垂直方向上的活跃槽位
                        int X = upPosition.X, Y = upPosition.Y;
                        for (; Y >= 0; Y--)
                        {
                            var detectPositon = new GridPosition(X, Y);
                            if (GridMath.IsPositionOnGrid(slotGrid, detectPositon) == false)
                                break;

                            if (GridMath.IsPositionOnBoard(slotGrid, detectPositon))
                                break;
                        }
                        upPosition = new GridPosition(X, Y);
                    }

                    if (GridMath.IsPositionOnGrid(slotGrid, upPosition, out var upSlot) == false ||
                        upSlot.IsActive == false ||
                        upSlot.HasUnMovablePiece ||
                        upSlot.FillType == -1)
                    {
                        if (upSlot.FillType == -1 && 
                            !upSlot.HasUnMovablePiece && 
                            !upSlot.IsEmpty)
                        {
                            slot.FillType = 0;
                            continue;
                        }

                        slot.FillType = 1;
                        bool upRightSlotActive = GridMath.IsPositionOnBoard(slotGrid, upRightPosition, out var upRightSlot),
                             upLeftSlotActive = GridMath.IsPositionOnBoard(slotGrid, upLeftPosition, out var upLeftSlot);

                        if ((upRightSlot == null || upRightSlotActive == false || upRightSlot.FillType == -1) &&
                            (upLeftSlot == null || upLeftSlotActive == false || upLeftSlot.FillType == -1))
                        {
                            slot.FillType = -1;
                        }

                        //if ((upRightSlot == null || !upRightSlotActive) && upRightSlot?.FillType == -1 && (upRightSlot?.piece == null || upRightSlot?.HasUnMovablePiece == true) &&
                        //    (upLeftSlot == null || !upLeftSlotActive) && upLeftSlot?.FillType == -1 && (upLeftSlot?.piece == null || upLeftSlot?.HasUnMovablePiece == true))
                        //{
                        //    slot.FillType = -1;
                        //}
                    }
                }
            }
        }
    }


    /// <summary>
    /// 更新允许Match的棋子Id
    /// </summary>
    private void UpdateAllowedBasicPieceIds()
    {
        AllowedBasicPieceIds.Clear();
        AllowedBasicPieceIds.UnionWith(Constants.BasicPieceIds.Where(x => RainbowSelectBasicPieceIds.Contains(x) == false));
    }


    /// <summary>
    /// 按照TickingPositions中的位置移动对应的棋子, 这个方法也包含了棋子Bounce的动画
    /// </summary>
    private void DoMovePieces()
    {
        // 按左下到右上进行排序
        TickingPositions = TickingPositions.Distinct().ToList();    // TODO: 调查TickingPositions中有重复的
        TickingPositions.Sort((a, b) =>
        {
            int yCmp = b.Y.CompareTo(a.Y);
            if (yCmp == 0)
            {
                return a.X.CompareTo(b.X);
            }
            return yCmp;
        });


        // 对全部空位置进行迭代
        var deltaTime = Time.deltaTime;
        for (int i = 0; i < TickingPositions.Count; i++)
        {
            var currentPosition = TickingPositions[i];
            var currentSlot = slotGrid[currentPosition];
            var currentPostitionWorldPosition = GetGridPositionWorldPosition(currentPosition);

            // 如果一个位置已经有棋子, 且又有棋子向它移动, 抛出错误
            if (currentSlot.incomingPiece != null && currentSlot.piece != null)
            {
                Debug.LogError($"{currentPosition} has incoming piece {currentSlot.incomingPiece} and contains piece {currentSlot.piece}");
                continue;
            }

            if (currentSlot.incomingPiece?.CurrentState == State.Moving)
            {
                var piece = currentSlot.incomingPiece;
                var piecePosition = piece.GridPosition;
                var piecePositionWorldPosition = GetGridPositionWorldPosition(piecePosition);

                // 更新棋子位置
                piece.TickMove(deltaTime);
                var maxDistance = pieceAnimationSetting.pieceMoveSpeedCurve.Evaluate(piece.MoveTime) * Time.deltaTime * pieceAnimationSetting.pieceMoveSpeed;
                piece.Transform.position = Vector3.MoveTowards(piece.GetWorldPosition(), currentPostitionWorldPosition, maxDistance);

                var distance = Vector3.Distance(piece.GetWorldPosition(), currentPostitionWorldPosition);
                if (distance <= approximateDistance)
                {
                    // 距离小于近似阈值则认为棋子到达本次移动终点
                    // 如果是新生成的棋子, 则需要关闭遮罩
                    if (currentSlot.IsSpawner && piece.EnteredBoard == false)
                    {
                        piece.EnteredBoard = true;
                        piece.SkeletonAnimation.maskInteraction = SpriteMaskInteraction.None;
                    }

                    piece.ReachGridPosition(currentPosition);   // 保险: 再设置一下棋子位置
                    currentSlot.incomingPiece = null;
                    currentSlot.piece = piece;
                    CompleteMovePieces.Add(piece);
                }
                else if (distance <= slotOriginalInterval / 2 &&
                         piece.GridPosition.Equals(currentPosition) == false)
                {
                    // 距离小于格子的半径并且棋子当前位置和格子位置不等
                    // 那么认为棋子到达了这个格子
                    piece.ReachGridPosition(currentPosition);
                }
            }
            else if (currentSlot.piece?.CurrentState == State.Bouncing)
            {
                // 完成运动的棋子进行弹跳动画表现
                var piece = currentSlot.piece;
                piece.TickBounce(deltaTime);

                float maxTime = pieceAnimationSetting.pieceBouncePositionCurve
                    .keys[pieceAnimationSetting.pieceBouncePositionCurve.length - 1].time;

                if (piece.MoveTime >= maxTime)
                {
                    // 完成弹跳动画
                    piece.Transform.position = currentPostitionWorldPosition;
                    piece.Transform.localScale = Vector3.one;
                    CompleteMovePieces.Add(piece);

                    if (AllowedBasicPieceIds.Contains(piece.Id))
                    {
                        PositionsToCheckMatch.Add(currentPosition);
                    }
                }
                else
                {
                    // 进行弹跳动画
                    piece.Transform.position = currentPostitionWorldPosition + Vector3.up * pieceAnimationSetting.pieceBouncePositionCurve.Evaluate(piece.MoveTime);
                    piece.Transform.localScale = new Vector3(pieceAnimationSetting.pieceBounceSquishXCurve.Evaluate(piece.MoveTime), pieceAnimationSetting.pieceBounceSquishYCurve.Evaluate(piece.MoveTime), 1);
                }
            }
        }
    }


    private void DoMatchCheck()
    {
        // 移除位置上没有棋子 || 仍在运动中的棋子
        PositionsToCheckMatch.RemoveWhere(x => slotGrid[x].piece == null || slotGrid[x].piece.MovingToSlot != null);

        var readyToCheckPositions = new List<GridPosition>();   // 周围没有更多相同的棋子incoming的位置
        foreach (var gridPosition in PositionsToCheckMatch)
        {
            // 没有更多相邻棋子与当前位置的棋子id相同的时候再进行检测
            var id = slotGrid[gridPosition].piece.Id;
            bool moreSameIdPieceIncoming = false;
            bool emptyPositionsWaitForFill = false;

            var queue = new Queue<Slot>();
            var visited = new HashSet<Slot>();
            queue.Enqueue(slotGrid[gridPosition]);
            visited.Add(slotGrid[gridPosition]);
            while (queue.Count > 0)
            {
                var curSlot = queue.Dequeue();
                var curPosition = curSlot.GridPosition;

                if (curSlot.IsEmpty)
                {
                    if (curSlot.IsSpawner || EmptyPositions.Contains(curPosition))
                    {
                        emptyPositionsWaitForFill = true;
                        break;
                    }
                }
                else if (curSlot.incomingPiece != null)
                {
                    moreSameIdPieceIncoming = true;
                    break;
                }

                // 当前位置相邻的包含相同id棋子的槽位或者相同id的incoming棋子id的槽位加入
                foreach (var direction in adjacentLookupDirections)
                {
                    var lookupDirection = curPosition + direction;
                    if (GridMath.IsPositionOnBoard(slotGrid, lookupDirection, out var lookupSlot) &&
                        visited.Add(lookupSlot))
                    {
                        var containPiece = lookupSlot switch
                        {
                            var x when x.upperPiece != null     => null,
                            var x when x.piece != null          => x.piece,
                            var x when x.incomingPiece != null  => x.incomingPiece,
                            _                                   => null
                        };

                        if (containPiece != null)
                        {
                            if (containPiece.Id == id)
                            {
                                queue.Enqueue(lookupSlot);
                            }
                        }
                        else
                        {
                            if ((EmptyPositions.Contains(lookupDirection) && lookupSlot.FillType != -1) || 
                                lookupSlot.IsSpawner)
                            {
                                queue.Enqueue(lookupSlot);
                            }
                        }
                    }
                }
            }

            if (emptyPositionsWaitForFill)
            {
                if (DelayedPostionsToCheckMatch.TryAdd(gridPosition, pieceAnimationSetting.emptyPositionsCheckTimeout) == false)
                { 
                    DelayedPostionsToCheckMatch[gridPosition] -= Time.deltaTime;
                    if (DelayedPostionsToCheckMatch[gridPosition] <= 0)
                    {
                        DelayedPostionsToCheckMatch.Remove(gridPosition);
                        emptyPositionsWaitForFill = false;
                    }
                }

            }

            if (!moreSameIdPieceIncoming && !emptyPositionsWaitForFill)
            {
                readyToCheckPositions.Add(gridPosition);
            }
        }

        // 对符合的位置进行级联检测
        if (readyToCheckPositions.Count > 0)
        {
            foreach (var index in matchCheckOrder)
            {
                var removeList = new List<GridPosition>();
                readyToCheckPositions.ForEach(pos =>
                {
                    // 对每个位置检测是否可以合成当前index指代的powerup
                    if (CheckMatchAt(pos, index, true))
                    {
                        // 对于成功检测得到的位置, 不需要再进行低优先级的检测, 因此加入移除列表, 在本轮完成后移除
                        removeList.Add(pos);
                    }
                });

                // 移除成功匹配到形状的位置
                readyToCheckPositions = readyToCheckPositions.Except(removeList).ToList();
            }

            // 移除进行过检测的位置
            readyToCheckPositions.ForEach(pos => 
            {
                PositionsToCheckMatch.Remove(pos);
                if (DelayedPostionsToCheckMatch.ContainsKey(pos))
                {
                    DelayedPostionsToCheckMatch.Remove(pos);
                }
            });
        }
    }


    private void DoExecuteMatches(bool activateAfterInitialized)
    {
        foreach (var match in MatchesToExecute)
        {
            ExecutingMatchAndDamageTasks.Add(ExecuteMatch(match, activateAfterInitialized));
        }
        MatchesToExecute.Clear();
    }


    private void DoClearCompletedTasks()
    {
        for (int i = 0; i < ExecutingMatchAndDamageTasks.Count; i++)
        {
            if (ExecutingMatchAndDamageTasks[i].Status == UniTaskStatus.Succeeded)
            {
                ExecutingMatchAndDamageTasks.RemoveAt(i);
                i--;
            }
        }
    }


    /// <summary>
    /// 按照EmptyPositions中的位置, 寻找合适的棋子进行填充;
    /// 这个方法只负责发出棋子, 具体移动由DoMovePieces()负责
    /// </summary>
    private void DoEmptyCheck(bool ignoreSpawnerRule = false)
    {
        // 对空位置进行排序, 斜向掉落(FillType == 1)的位置放在正常填充(FillType == 0)之后进行尝试填充
        EmptyPositions = EmptyPositions
            .Where(x => slotGrid[x].FillType != -1)
            .OrderBy(x => slotGrid[x].FillType)       
            .ToList();

        for (int i = 0; i < EmptyPositions.Count; i++)
        {
            var emptyPosition = EmptyPositions[i];
            var emptySlot = slotGrid[emptyPosition];

            // 移除不再是空的位置, 移除没有棋子能填充的位置
            if (!emptySlot.IsEmpty)
            {
                EmptyPositions.RemoveAt(i);
                i--;
                continue;
            }

            // 跳过不允许棋子进入的位置
            if (emptySlot.CanEnter == false)
            {
                continue;
            }

            var leftPosition = emptyPosition + GridPosition.Left;
            var rightPosition = emptyPosition + GridPosition.Right;
            var upPosition = emptyPosition + GridPosition.Up;
            var upRightPosition = emptyPosition + GridPosition.UpRight;
            var upLeftPosition = emptyPosition + GridPosition.UpLeft;
            bool isLeftSlotEmptyAndFillable = GridMath.IsPositionOnBoard(slotGrid, leftPosition, out var leftSlot) && leftSlot.FillType != -1 && leftSlot.piece == null;
            bool isRightSlotEmptyAndFillable = GridMath.IsPositionOnBoard(slotGrid, rightPosition, out var rightSlot) && rightSlot.FillType != -1 && rightSlot.piece == null;

            if (GridMath.IsPositionOnGrid(slotGrid, upPosition) && slotGrid[upPosition].IsActive == false)
            {
                int X = upPosition.X, Y = upPosition.Y;
                for (; Y >= 0; Y--)
                {
                    var detectPosition = new GridPosition(X, Y);
                    if (GridMath.IsPositionOnGrid(slotGrid, detectPosition) == false)
                        break;

                    if (GridMath.IsPositionOnBoard(slotGrid, detectPosition))
                        break;
                }
                upPosition = new GridPosition(X, Y);
            }
            bool findDroppableActiveUpSlot = GridMath.IsPositionOnBoard(slotGrid, upPosition, out var upSlot) && 
                                    !upSlot.HasUnMovablePiece;


            if (findDroppableActiveUpSlot)                   // 成功找到活跃的位于垂直上方的能够提供棋子的槽位
            {
                if (upSlot.piece == null)           // 上方槽位无棋子
                {
                    if (emptySlot.FillType == 1)    // 该空位置是否被标记为斜向
                    {
                        if (GridMath.IsPositionOnBoard(slotGrid, upRightPosition, out var upRightSlot) &&
                            upRightSlot.FillType != -1) // 右上方位置有活跃槽位 && 右上方位置槽位能被棋子填充
                        {
                            if (upRightSlot.CanLeave == false || upRightSlot.piece == null || isRightSlotEmptyAndFillable)
                            {
                                // 右上方槽位不允许棋子离开 || 右上方槽位无棋子 || 右方槽位可以被填充且当前为空
                                // 等待
                                continue;
                            }

                            if (ExceedMoveInterval(upRightSlot.piece, emptyPosition) == false)
                            {
                                // 右上方槽位棋子不满足发出
                                continue;
                            }

                            var fillPiece = upRightSlot.piece;
                            upRightSlot.piece = null;
                            emptySlot.incomingPiece = fillPiece;

                            fillPiece.StartMove(emptySlot);                         // 发出棋子, 目标为本空槽位
                            upRightSlot.LastFireTime = Time.time;                   // 记录槽位最后发出时间

                            if (!NewTickingPositions.Contains(emptyPosition))
                                NewTickingPositions.Add(emptyPosition);             // 将本次移动到的空位加入到需要移动到的位置列表中
                            CompleteMovePieces.Remove(fillPiece);                   // 将移动的棋子移出完成移动哈希表

                            if (!EmptyPositions.Contains(upRightPosition))
                                EmptyPositions.Add(upRightPosition);                // 将发出棋子的位置标记为空
                            if (EmptyPositions.Contains(emptyPosition))
                                EmptyPositions.Remove(emptyPosition);               // 该空位置不再为空, 移出空位置列表

                            i--;

                            UpdateAllSlotsFillType();                               // 移动棋子后需要更新填充
                        }
                        else if (GridMath.IsPositionOnBoard(slotGrid, upLeftPosition, out var upLeftSlot) &&
                            upLeftSlot.FillType != -1)  // 左上方位置有活跃槽位 && 左上方位置槽位能被棋子填充
                        {
                            if (upLeftSlot.CanLeave == false || upLeftSlot.piece == null || isLeftSlotEmptyAndFillable)
                            {
                                // 左上方槽位不允许棋子离开 || 左上方槽位无棋子 || 左方位置可以被填充且当前为空
                                // 等待
                                continue;
                            }

                            if (ExceedMoveInterval(upLeftSlot.piece, emptyPosition) == false)
                            {
                                // 左上方槽位棋子不满足发出
                                continue;
                            }

                            var fillPiece = upLeftSlot.piece;
                            upLeftSlot.piece = null;
                            emptySlot.incomingPiece = fillPiece;

                            fillPiece.StartMove(emptySlot);                         // 发出棋子, 目标为本空槽位
                            upLeftSlot.LastFireTime = Time.time;                    // 记录槽位最后发出时间

                            if (!NewTickingPositions.Contains(emptyPosition))
                                NewTickingPositions.Add(emptyPosition);             // 将本次移动到的空位加入到需要移动到的位置列表中
                            CompleteMovePieces.Remove(fillPiece);                   // 将移动的棋子移出完成移动哈希表

                            if (!EmptyPositions.Contains(upLeftPosition))
                                EmptyPositions.Add(upLeftPosition);                 // 将发出棋子的位置标记为空
                            if (EmptyPositions.Contains(emptyPosition))
                                EmptyPositions.Remove(emptyPosition);               // 该空位置不再为空, 移出空位置列表
                            
                            i--;

                            UpdateAllSlotsFillType();                               // 移动棋子后需要更新填充
                        }
                        else Debug.LogWarning($"Unhandled situation");
                    }
                    else continue;  // 等待(垂直方向的棋子到位)
                }
                else
                {
                    // 上方槽位含有棋子
                    if (upSlot.CanLeave == false)
                    {
                        continue;
                    }

                    if (ExceedMoveInterval(upSlot.piece, emptyPosition) == false)
                    {
                        // 上方槽位棋子不满足发出
                        continue;
                    }

                    var fillPiece = upSlot.piece;
                    upSlot.piece = null;
                    emptySlot.incomingPiece = fillPiece;

                    fillPiece.StartMove(emptySlot);                         // 发出棋子, 目标为本空槽位
                    upSlot.LastFireTime = Time.time;                        // 记录槽位最后发出时间

                    if (!NewTickingPositions.Contains(emptyPosition))
                        NewTickingPositions.Add(emptyPosition);             // 将本次移动到的空位加入到需要移动到的位置列表中
                    CompleteMovePieces.Remove(fillPiece);                   // 将移动的棋子移出完成移动哈希表

                    if (!EmptyPositions.Contains(upPosition))
                        EmptyPositions.Add(upPosition);                     // 将发出棋子的位置标记为空
                    if (EmptyPositions.Contains(emptyPosition))
                        EmptyPositions.Remove(emptyPosition);               // 该空位置不再为空, 移出空位置列表
                    
                    i--;

                    UpdateAllSlotsFillType();                               // 移动棋子后需要更新填充
                }
            }
            else 
            {
                // 未能找到活跃的位于上方的槽位
                if (emptySlot.IsSpawner)
                {
                    if (ExceedMoveInterval(null, emptyPosition) == false)
                    {
                        // 如果不满足发出条件则延迟激活掉落口
                        continue;
                    }

                    var fillPiece = ActivateSpawnerAt(emptyPosition, ignoreSpawnerRule);    // 激活掉落口
                    emptySlot.incomingPiece = fillPiece;

                    fillPiece.StartMove(emptySlot);                         // 发出棋子, 目标为本掉落口
                    emptySlot.LastSpawnTime = Time.time;                    // 记录槽位最后生成时间

                    if (!NewTickingPositions.Contains(emptyPosition))
                        NewTickingPositions.Add(emptyPosition);             // 将本次移动到的空位加入到需要移动到的位置列表中

                    if (EmptyPositions.Contains(emptyPosition))
                        EmptyPositions.Remove(emptyPosition);               // 该空位置不再为空, 移出空位置列表
                    
                    i--;

                    UpdateAllSlotsFillType();                               // 移动棋子后需要更新填充
                }
                else if (emptySlot.FillType == 1)
                {
                    if (GridMath.IsPositionOnBoard(slotGrid, upRightPosition, out var upRightSlot) &&
                        upRightSlot.FillType != -1) // 右上方位置有活跃槽位 && 右上方位置槽位能被棋子填充
                    {
                        if (upRightSlot.CanLeave == false || upRightSlot.piece == null || isRightSlotEmptyAndFillable)
                        {
                            // 右上方槽位不允许棋子离开 || 右上方槽位无棋子 || 右方槽位可被填充且当前为空
                            // 等待
                            continue;
                        }

                        if (ExceedMoveInterval(upRightSlot.piece, emptyPosition) == false)
                        {
                            // 右上方槽位棋子不满足发出
                            continue;
                        }

                        var fillPiece = upRightSlot.piece;
                        upRightSlot.piece = null;
                        emptySlot.incomingPiece = fillPiece;

                        fillPiece.StartMove(emptySlot);                         // 发出棋子, 目标为本空槽位
                        upRightSlot.LastFireTime = Time.time;                   // 记录槽位最后发出时间


                        if (!NewTickingPositions.Contains(emptyPosition))
                            NewTickingPositions.Add(emptyPosition);             // 将本次移动到的空位加入到需要移动到的位置列表中
                        CompleteMovePieces.Remove(fillPiece);                   // 将移动的棋子移出完成移动哈希表

                        if (!EmptyPositions.Contains(upRightPosition))
                            EmptyPositions.Add(upRightPosition);                // 将发出棋子的位置标记为空
                        if (EmptyPositions.Contains(emptyPosition))
                            EmptyPositions.Remove(emptyPosition);               // 该空位置不再为空, 移出空位置列表
                        i--;
                        
                        UpdateAllSlotsFillType();                               // 移动棋子后需要更新填充
                    }
                    else if (GridMath.IsPositionOnBoard(slotGrid, upLeftPosition, out var upLeftSlot) &&
                             upLeftSlot.FillType != -1)     // 左上方位置有活跃槽位 && 左上方位置槽位能被棋子填充
                    {
                        if (upLeftSlot.CanLeave == false || upLeftSlot.piece == null || isLeftSlotEmptyAndFillable)
                        {
                            // 左上方槽位不允许棋子离开 || 左上方槽位无棋子 || 左方槽位可被填充且当前为空
                            // 等待
                            continue;
                        }

                        if (ExceedMoveInterval(upLeftSlot.piece, emptyPosition) == false)
                        {
                            // 左上方槽位棋子不满足发出
                            continue;
                        }

                        var fillPiece = upLeftSlot.piece;
                        upLeftSlot.piece = null;
                        emptySlot.incomingPiece = fillPiece;

                        fillPiece.StartMove(emptySlot);                         // 发出棋子, 目标为本空槽位
                        upLeftSlot.LastFireTime = Time.time;                    // 记录槽位最后发出时间

                        if (!NewTickingPositions.Contains(emptyPosition))
                            NewTickingPositions.Add(emptyPosition);             // 将本次移动到的空位加入到需要移动到的位置列表中
                        CompleteMovePieces.Remove(fillPiece);                   // 将移动的棋子移出完成移动哈希表

                        if (!EmptyPositions.Contains(upLeftPosition))
                            EmptyPositions.Add(upLeftPosition);                 // 将发出棋子的位置标记为空
                        if (EmptyPositions.Contains(emptyPosition))
                            EmptyPositions.Remove(emptyPosition);               // 该空位置不再为空, 移出空位置列表
                        i--;

                        UpdateAllSlotsFillType();                               // 移动棋子后需要更新填充
                    }
                }
            }
        }
    }


    /// <summary>
    /// 检查当前时间棋子是否超出了
    /// </summary>
    private bool ExceedMoveInterval(Piece piece, GridPosition moveToPosition)
    {
        var currentTime = Time.time;

        if (piece == null)
        {
            return currentTime - slotGrid[moveToPosition].LastSpawnTime >= pieceAnimationSetting.pieceMoveInterval &&
                currentTime - slotGrid[moveToPosition].LastFireTime >= pieceAnimationSetting.pieceMoveInterval;
        }

        if (piece.MovingToSlot == null)
        {
            return currentTime - slotGrid[moveToPosition].LastFireTime >= pieceAnimationSetting.pieceMoveInterval;
        }
        else return true;
    }


    private void DoStopPieces()
    {
        foreach (var piece in CompleteMovePieces)
        {
            var piecePosition = piece.GridPosition;
            var worldPosition = GetGridPositionWorldPosition(piecePosition);
            if (piece.CurrentState == State.Moving)
            {
                piece.StopMove();
                //piece.Transform.position = worldPosition;

                TickingPositions.Remove(piecePosition);

                if (!NewTickingPositions.Contains(piecePosition))
                    NewTickingPositions.Add(piecePosition);
            }
            else if (piece.CurrentState == State.Bouncing)
            {
                piece.StopBouncing();
                //piece.Transform.position = worldPosition;

                TickingPositions.Remove(piecePosition);
            }
        }

        // 清除完成移动棋子列表
        CompleteMovePieces.Clear();
    }


    /// <summary>
    /// 从起始位置开始检测匹配, 这个方法会检测全部的与起始位置连接的棋子, 并按照形状选择棋子
    /// </summary>
    /// <param name="startPosition">检测的起始位置</param>
    /// <param name="checkPowerupIndex">检测的powerup的下标, 若 == -1 则检测基础三消</param>
    /// <param name="fromCascading">true: 来自级联检测, false: 来自玩家交换</param>
    /// <param name="createMatch">true: 检测并生成Match用于执行, false: 仅检测而不生成Match用于执行</param>
    private bool CheckMatchAt(GridPosition startPosition, int checkPowerupIndex, bool fromCascading = false, bool createMatch = true)
    {
        // 跳过空位置
        if (!GridMath.IsPositionOnBoard(slotGrid, startPosition, out var centerSlot) ||
            centerSlot.piece == null)
        {
            return false;
        }

        // 跳过已经包含在其他match || 不是非Rainbow选择的基础棋子的棋子
        var centerPiece = centerSlot.piece;
        if (centerPiece.CanUse ||
            AllowedBasicPieceIds.Contains(centerPiece.Id) == false ||
            centerPiece.CurrentMatch != null)
        {
            return false;
        }

        // 使用BFS来找到所有与棋子十字连接的包含相同棋子的槽位
        int id = centerPiece.Id;
        List<GridPosition> positions = new();

        HashSet<GridPosition> checkedSet = new();
        Queue<GridPosition> toCheck = new();
        toCheck.Enqueue(startPosition);
        while (toCheck.Count > 0)
        {
            var curPosition = toCheck.Dequeue();

            positions.Add(curPosition);
            checkedSet.Add(curPosition);

            foreach (var nextSlot in GetAdjacentSlots(slotGrid, curPosition, (id, true, false)))
            {
                var nextPosition = nextSlot.GridPosition;
                if (checkedSet.Contains(nextPosition))
                {
                    continue;
                }

                var nextSlotPiece = nextSlot.piece;
                if (nextSlotPiece.MovingToSlot == null &&
                    (nextSlotPiece.CurrentState == State.Bouncing || nextSlotPiece.CurrentState == State.Still) &&
                    nextSlotPiece.CurrentMatch == null)
                {
                    toCheck.Enqueue(nextPosition);
                }
            }
        }


        // 按照所有与棋子连接的相同棋子的位置判断能够合成该powerup
        List<GridPosition> matchedShapePositions = new();
        List<GridPosition> lineList = new();

        MatchShape matchedShape = null;
        Powerup matchedPowerup = null;
        GridPosition matchedCenterPosition = startPosition;

        if (checkPowerupIndex != -1)
        {
            // 检测合成
            Powerup powerup = powerupSetting.registeredPowerups[checkPowerupIndex];
            foreach (var shape in powerup.shapes)
            {
                if (shape.FitIn(positions, ref matchedShapePositions))
                {
                    matchedShape = shape;
                    matchedPowerup = powerup;
                    matchedCenterPosition = fromCascading ? powerup.GetMatchCenterPosition(matchedShapePositions) : startPosition;
                    break;
                }
            }
        }
        else
        {
            // 检测基础3消
            foreach (var posList in basicMatchPositions)
            {
                bool findLine = true;
                foreach (var pos in posList)
                {
                    var curPos = startPosition + pos;
                    if (!positions.Contains(curPos))
                    {
                        findLine = false;
                        break;
                    }
                    else lineList.Add(curPos);
                }

                if (findLine)
                {
                    break;
                }
                else lineList.Clear();
            }
        }


        // 未能找到任何匹配的powerup和3个棋子组成的一行/列
        if (matchedShapePositions.Count <= 0 && lineList.Count <= 0)
        {
            return false;
        }


        // 生成Match
        if (createMatch)
        {
            var finalMatch = CreateMatch(matchedCenterPosition);
            if (matchedShapePositions.Count > 0)
            {
                finalMatch.SpawnedPowerup = matchedPowerup;
                matchedShapePositions.ForEach(pos => finalMatch.AddPiece(slotGrid[pos].piece));
            }
            else
            {
                finalMatch.SpawnedPowerup = null;
                lineList.ForEach(pos => finalMatch.AddPiece(slotGrid[pos].piece));
            }

            Debug.Log($"Checks match at {startPosition}, matches powerup = {finalMatch.SpawnedPowerup?.name},\ncenter = {matchedCenterPosition}");
        }
        return true;
    }


    private bool CheckMatchAt(Grid checkSlotGrid, GridPosition startPosition, int checkPowerupIndex, 
                              out AnimationReferenceAsset hintAnimation, out Vector3 animationWorldPosition, out int animationRotation,
                              out List<GridPosition> matchedShapePositions)
    {
        animationRotation = 0;
        matchedShapePositions = new();

        // 跳过空位置
        if (!GridMath.IsPositionOnBoard(checkSlotGrid, startPosition, out var centerSlot) ||
            centerSlot.piece == null)
        {
            hintAnimation = null;
            animationWorldPosition = Vector3.zero;
            animationRotation = 0;
            matchedShapePositions.Clear();
            return false;
        }

        // 跳过已经包含在其他match || 不是非Rainbow选择的基础棋子的棋子
        var centerPiece = centerSlot.piece;
        if (centerPiece.CanUse ||
            AllowedBasicPieceIds.Contains(centerPiece.Id) == false ||
            centerPiece.CurrentMatch != null)
        {
            hintAnimation = null;
            animationWorldPosition = Vector3.zero;
            animationRotation = 0;
            matchedShapePositions.Clear();
            return false;
        }

        // 使用BFS来找到所有与棋子十字连接的包含相同棋子的槽位
        int id = centerPiece.Id;
        List<GridPosition> positions = new();

        HashSet<GridPosition> checkedSet = new();
        Queue<GridPosition> toCheck = new();
        toCheck.Enqueue(startPosition);
        while (toCheck.Count > 0)
        {
            var curPosition = toCheck.Dequeue();

            positions.Add(curPosition);
            checkedSet.Add(curPosition);

            foreach (var nextSlot in GetAdjacentSlots(checkSlotGrid, curPosition, (id, true, false)))
            {
                var nextPosition = nextSlot.GridPosition;
                if (checkedSet.Contains(nextPosition))
                {
                    continue;
                }

                var nextSlotPiece = nextSlot.piece;
                if (nextSlotPiece.MovingToSlot == null &&
                    (nextSlotPiece.CurrentState == State.Bouncing || nextSlotPiece.CurrentState == State.Still) &&
                    nextSlotPiece.CurrentMatch == null)
                {
                    toCheck.Enqueue(nextPosition);
                }
            }
        }

        // 按照所有与棋子连接的相同棋子的位置判断能够合成该powerup
        List<GridPosition> lineList = new();
        bool isFirstCoreShape = true;
        if (checkPowerupIndex != -1)
        {
            Powerup powerup = powerupSetting.registeredPowerups[checkPowerupIndex];
            for (int i = 0; i < powerup.coreShapes.Count; i++)
            {
                var matchShape = powerup.coreShapes[i];
                if (matchShape.FitInCore(positions, ref matchedShapePositions, out animationRotation))
                {
                    isFirstCoreShape = i == 0;
                    break;
                }
            }
        }
        else
        {
            // 检测基础3消
            foreach (var posList in basicMatchPositions)
            {
                bool findLine = true;
                foreach (var pos in posList)
                {
                    var curPos = startPosition + pos;
                    if (!positions.Contains(curPos))
                    {
                        findLine = false;
                        break;
                    }
                    else lineList.Add(curPos);
                }

                if (findLine)
                {
                    break;
                }
                else lineList.Clear();
            }
        }

        // 未能找到任何匹配的powerup和3个棋子组成的一行/列
        if (matchedShapePositions.Count <= 0 && lineList.Count <= 0)
        {
            hintAnimation = null;
            animationWorldPosition = Vector3.zero;
            animationRotation = 0;
            matchedShapePositions.Clear();
            return false;
        }

        // 计算Hint生成参数
        if (matchedShapePositions.Count > 0)
        {
            // Powerup Match
            hintAnimation = checkPowerupIndex switch
            {
                4 => hintAnimations[4],
                var x when x == 1 && isFirstCoreShape => hintAnimations[2],
                var x when x == 1 && !isFirstCoreShape => hintAnimations[5],
                2 or 3 => hintAnimations[3],
                0 => hintAnimations[6],
                _ => null
            };
        }
        else 
        {
            // 普通3消不需要动画
            matchedShapePositions = lineList;
            hintAnimation = null;
        }

        // 计算动画中心位置(世界坐标)
        int boundXMin = matchedShapePositions.Min(pos => pos.X);
        int boundYMin = matchedShapePositions.Min(pos => pos.Y);
        int boundXMax = matchedShapePositions.Max(pos => pos.X);
        int boundYMax = matchedShapePositions.Max(pos => pos.Y);

        var minPosition = GetGridPositionWorldPosition(new GridPosition(boundXMin, boundYMin));
        var maxPosition = GetGridPositionWorldPosition(new GridPosition(boundXMax, boundYMax));
        animationWorldPosition = (minPosition + maxPosition) / 2;
        return true;
    }


    public Match CreateMatch(GridPosition centerPosition)
    {
        Match match = new()
        {
            MatchingPositions = new(),
            CenterPosition = centerPosition,
            SpawnedPowerup = null,
        };
        MatchesToExecute.Add(match);
        return match;
    }



    /// <summary>
    /// 处理这一次Match, 合成powerup或进行消除
    /// </summary>
    private async UniTask ExecuteMatch(Match match, bool isReward = false)
    {
        // 异常检查
        bool errorReturn = false;
        foreach (var position in match.MatchingPositions)
        {
            if (!GridMath.IsPositionOnGrid(slotGrid, position, out var slot))
            {
                errorReturn = true;
                break;
            }

            if (slot.piece == null)
            {
                errorReturn = true;
                break;
            }

            var piece = slot.piece;
            if (piece.CurrentMatch != match || Constants.BasicPieceIds.Contains(piece.Id) == false)
            { 
                errorReturn = true;
                break;
            }
        }
        if (errorReturn)
        {
           throw new InvalidOperationException("Error when executing match: Invalid Match");
        }

        // 在开始执行消除前, 停止棋子弹跳
        match.MatchingPositions.ForEach(position =>
        {
            var piece = slotGrid[position].piece;
            if (piece.CurrentState == State.Bouncing)
            {
                if (TickingPositions.Contains(position))
                {
                    TickingPositions.Remove(position);
                }
                if (NewTickingPositions.Contains(position))
                {
                    NewTickingPositions.Remove(position);
                }

                piece.Transform.position = GetGridPositionWorldPosition(position);
                piece.Transform.localScale = Vector3.one;
                piece.StopBouncing();
            }
        });

        int turnScore = match.MatchingPositions.Count * 10;
        // 触发回调
        Damage sourceDamage = new();
        match.MatchingPositions.ForEach(position => sourceDamage.AddToDamagePositions(position));

        match.MatchingPositions.ForEach(pos =>
        {
            // 触发相邻Damage回调
            if (PositionAdjacentDamagedCallbacks.TryGetValue(pos, out var adjacentDamageCallback))
            {
                adjacentDamageCallback?.Invoke(sourceDamage);
                turnScore += 10;
            }

            // 要触发BottomDamage回调
            if (PositionBottomDamagedCallbacks.TryGetValue(pos, out var centerPieceDamageCallback))
            {
                centerPieceDamageCallback?.Invoke(sourceDamage);
                turnScore += 10;
            }
        });
        // 得分
        GameManager.instance.AddScore(turnScore, true);

        // 开始处理Match的动画
        if (match.SpawnedPowerup == null)
        {
            // Match: Damage
            // 消除槽位上的基础棋子, 并播放动效, 加入空位置中

            match.MatchingPositions.ForEach(position =>
            {
                var slot = slotGrid[position];
                var piece = slot.piece;

                if (piece != null && Constants.BasicPieceIds.Any(x => x == piece.Id))
                {
                    piece.Damage(
                        sourceDamage,
                        OnDamagePlayVFX,
                        OnDamageCollectTarget,
                        OnDamageControlPosition);
                }
                else throw new InvalidOperationException("Try to handle a match that contains null or non-basic pieces");
            });
        }
        else
        {
            // Match: Merge & Spawn
            var mergeSequence = DOTween.Sequence();
            var centerWorldPosition = GetGridPositionWorldPosition(match.CenterPosition);
            var elapsedTime = 0f;
            var invokedMergeComplete = false;

            foreach (var position in match.MatchingPositions)
            {
                var slot = slotGrid[position];
                slot.IncreaseEnterAndLeaveLock();

                var piece = slot.piece;
                _ = mergeSequence
                    .Join(piece.Transform
                        .DOMove(centerWorldPosition, pieceAnimationSetting.pieceMergeMoveDuration)
                        .SetEase(pieceAnimationSetting.pieceMergeMoveEase));
            }

            await mergeSequence
                .SetDelay(pieceAnimationSetting.pieceMergeDelayDuration)
                .OnUpdate(() =>
                {
                    elapsedTime += Time.deltaTime;

                    if (elapsedTime >= pieceAnimationSetting.pieceMergeMoveCompleteElapsedTime &&
                        !invokedMergeComplete)
                    {
                        invokedMergeComplete = true;

                        Vector3 centerPositionWorldPosition = GetGridPositionWorldPosition(match.CenterPosition);
                        int collected = 0;
                        match.MatchingPositions.ForEach(position =>
                        {
                            var piece = slotGrid[position].piece;
                            var pieceId = piece.Id;
                            if (piece != null && Constants.BasicPieceIds.Any(x => x == pieceId))
                            {
                                if (TryUpdateLevelTargetPiece(pieceId, out var collectId, out var collectPrefab))
                                {
                                    TryCollectLevelTargetPiece(collectId, collectPrefab, centerPositionWorldPosition, (float)(pieceAnimationSetting.pieceMergeCollectableDelayInterval * collected));
                                }

                                piecePool.ReleasePiece(piece);
                                slotGrid[position].piece = null;
                            }
                            else throw new InvalidOperationException("Try to handle a match that contains null or non-basic pieces");

                            collected++;
                        });

                        var powerupId = pieceConfigSO.allRegisteredPieces
                            .Where(pair => powerupSetting.registeredPowerups.Any(x => x.Equals(match.SpawnedPowerup)) && pair.Value.piecePrefab.Equals(match.SpawnedPowerup))
                            .Select(pair => pair.Key)
                            .FirstOrDefault();
                        var powerup = ReplacePiece(match.CenterPosition, powerupId);

                        if (isReward)
                        {
                            var rewardCoin = pieceConfigSO.allRegisteredPieces[powerupId].pieceRewardReference.rewardClearCoin;
                            UserDataManager.Instance.EarnCoin(rewardCoin);
                            powerup.ClaimReward();

                            powerup.OnSpawnCallback = powerupId switch
                            {
                                var x when x == Constants.PieceFlyBombId => () => AddAction((powerup as FlyBomb).StandaloneActivate()),
                                var x when x == Constants.PieceBombId => () => AddAction((powerup as Bomb).StandaloneActivate()),
                                var x when x == Constants.PieceHRocketId || x == Constants.PieceVRocketId => () => AddAction((powerup as Rocket).StandaloneActivate()),
                                var x when x == Constants.PieceRainbowId => () => AddAction((powerup as Rainbow).StandaloneActivate()),
                                _ => null
                            };

                            powerup.OnDamageCallback = () =>
                            {
                                while (rewardCoin-- > 0)
                                {
                                    var coinPrefab = pieceConfigSO.allRegisteredPieces[9999].pieceTargetReference.pieceCollectPrefab;
                                    var randomPoisiton = powerup.GetWorldPosition() + new Vector3(Random.Range(-slotOriginalInterval, slotOriginalInterval), 0.325f * Random.Range(-slotOriginalInterval, slotOriginalInterval), 0);
                                    var insCollectable = Instantiate(coinPrefab, randomPoisiton, Quaternion.identity).GetComponent<Collectable>();
                                    insCollectable.StartBezierMoveToRewardDisplay();
                                }
                            };
                        }

                        match.MatchingPositions.ForEach(pos =>
                        {
                            var slot = slotGrid[pos];
                            slot.DecreaseEnterAndLeaveLock();

                            if (slot.IsEmpty &&
                                !pos.Equals(match.CenterPosition) &&
                                !EmptyPositions.Contains(pos))
                            {
                                EmptyPositions.Add(pos);
                            }
                        });

                        // kill this sequence after complete merge
                        mergeSequence.Kill();
                    }
                });
        }
    }


    /// <summary>
    /// 寻找所有可能的合成情况
    /// </summary>
    private void FindAllPossibleMatch()
    {
        // 开始第一优先级
        // 第一优先级: 首先检查组合道具的情况
        foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            // 跳过不能Swap的位置
            if (IsSlotSwappable(gridPosition) == false)
                continue;

            // 跳过非Powerup的位置
            var slot = slotGrid[gridPosition];
            if (slot.piece == null || slot.piece.CanUse == false)
                continue;

            var upPosition = gridPosition + GridPosition.Up;
            var rightPosition = gridPosition + GridPosition.Right;

            if (GridMath.IsPositionOnBoard(slotGrid, upPosition, out var upSlot) &&
                IsSlotSwappable(upPosition) && 
                upSlot.piece != null && upSlot.piece.CanUse)
            {
                PossibleSwaps.Enqueue(new PossibleSwap()
                {
                    IsSynthesize = true,
                    StartPosition = gridPosition,
                    SwapDirection = GridPosition.Up,
                    ContainPosition = new List<GridPosition> { gridPosition, upPosition },

                    HintAnimation = hintAnimations[1],
                    AnimationWorldPosition = (GetGridPositionWorldPosition(gridPosition) + GetGridPositionWorldPosition(upPosition)) / 2,
                    AnimationRotation = 90
                }, -1 * gameBoardHintEntity.CalculatePriority(slot.piece.Id, upSlot.piece.Id));
            }

            if (GridMath.IsPositionOnBoard(slotGrid, rightPosition, out var rightSlot) &&
                IsSlotSwappable(rightPosition) &&
                rightSlot.piece != null && rightSlot.piece.CanUse)
            {
                PossibleSwaps.Enqueue(new PossibleSwap()
                {
                    IsSynthesize = true,
                    StartPosition = gridPosition,
                    SwapDirection = GridPosition.Right,
                    ContainPosition = new List<GridPosition> { gridPosition, rightPosition },

                    HintAnimation = hintAnimations[1],
                    AnimationWorldPosition = (GetGridPositionWorldPosition(gridPosition) + GetGridPositionWorldPosition(rightPosition)) / 2,
                    AnimationRotation = 0
                }, -1 * gameBoardHintEntity.CalculatePriority(slot.piece.Id, rightSlot.piece.Id));
            }
        }


        using (Grid copiedSlotGrid = new ())
        {
            copiedSlotGrid.SetGrid(new Slot[yMax, xMax]);
            foreach (var slot in slotGrid)
            {
                copiedSlotGrid[slot.GridPosition] = slot;
            }

            // 第一优先级: 检查交换后生成道具的情况
            foreach (var gridPosition in (IEnumerable<GridPosition>)copiedSlotGrid)
            {
                // 跳过不能Swap的位置
                if (IsSlotSwappable(gridPosition) == false)
                    continue;

                var slot = copiedSlotGrid[gridPosition];
                var upPosition = gridPosition + GridPosition.Up;
                var rightPosition = gridPosition + GridPosition.Right;

                if (GridMath.IsPositionOnBoard(copiedSlotGrid, upPosition, out var upSlot) &&
                    IsSlotSwappable(upPosition))
                {
                    // 暂时交换双方位置, 用于检测
                    (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);

                    foreach (var index in possibleMatchCheckOrder)
                    {
                        // 合成的Powerup的Id
                        var synthesizePieceId = index switch
                        {
                            0 => Constants.PieceFlyBombId,
                            1 => Constants.PieceBombId,
                            2 => Constants.PieceHRocketId,
                            4 => Constants.PieceRainbowId,
                            _ => 0
                        };

                        // 向上
                        {
                            if (CheckMatchAt(copiedSlotGrid, upPosition, index,
                                             out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                             out var matchedShapePositions))
                            {
                                PossibleSwaps.Enqueue(new PossibleSwap()
                                {
                                    IsSynthesize = false,
                                    StartPosition = gridPosition,
                                    SwapDirection = GridPosition.Up,
                                    ContainPosition = matchedShapePositions,

                                    HintAnimation = hintAnimation,
                                    AnimationWorldPosition = animationWorldPosition,
                                    AnimationRotation = animationRotation
                                }, -1 * gameBoardHintEntity.CalculatePriority(synthesizePieceId));
                            }
                        }

                        // 向下
                        {
                            if (CheckMatchAt(copiedSlotGrid, gridPosition, index,
                                             out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                             out var matchedShapePositions))
                            {
                                PossibleSwaps.Enqueue(new PossibleSwap()
                                {
                                    IsSynthesize = false,
                                    StartPosition = upPosition,
                                    SwapDirection = GridPosition.Down,
                                    ContainPosition = matchedShapePositions,

                                    HintAnimation = hintAnimation,
                                    AnimationWorldPosition = animationWorldPosition,
                                    AnimationRotation = animationRotation
                                }, -1 * gameBoardHintEntity.CalculatePriority(synthesizePieceId));
                            }
                        }
                    }

                    // 复原交换
                    (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
                }


                if (GridMath.IsPositionOnBoard(copiedSlotGrid, rightPosition, out var rightSlot) &&
                    IsSlotSwappable(rightPosition))
                {
                    // 暂时交换双方位置, 用于检测
                    (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);

                    foreach (var index in possibleMatchCheckOrder)
                    {
                        // 合成的Powerup的Id
                        var synthesizePieceId = index switch
                        {
                            0 => Constants.PieceFlyBombId,
                            1 => Constants.PieceBombId,
                            2 => Constants.PieceHRocketId,
                            4 => Constants.PieceRainbowId,
                            _ => 0
                        };

                        // 向右
                        {
                            if (CheckMatchAt(copiedSlotGrid, rightPosition, index,
                                             out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                             out var matchedShapePositions))
                            {
                                PossibleSwaps.Enqueue(new PossibleSwap()
                                {
                                    IsSynthesize = false,
                                    StartPosition = gridPosition,
                                    SwapDirection = GridPosition.Right,
                                    ContainPosition = matchedShapePositions,

                                    HintAnimation = hintAnimation,
                                    AnimationWorldPosition = animationWorldPosition,
                                    AnimationRotation = animationRotation
                                }, -1 * gameBoardHintEntity.CalculatePriority(synthesizePieceId));
                            }
                        }

                        // 向左
                        {
                            if (CheckMatchAt(copiedSlotGrid, gridPosition, index,
                                             out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                             out var matchedShapePositions))
                            {
                                PossibleSwaps.Enqueue(new PossibleSwap()
                                {
                                    IsSynthesize = false,
                                    StartPosition = rightPosition,
                                    SwapDirection = GridPosition.Left,
                                    ContainPosition = matchedShapePositions,

                                    HintAnimation = hintAnimation,
                                    AnimationWorldPosition = animationWorldPosition,
                                    AnimationRotation = animationRotation
                                }, -1 * gameBoardHintEntity.CalculatePriority(synthesizePieceId));
                            }
                        }
                    }

                    // 复原交换
                    (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
                }
            }

            // 此时Top Priority的已检查完毕
            if (PossibleSwaps.Count > 0)
                return;

            // 开始第二优先级
            // 第二优先级: 检查基础3消
            int basicMatchPriority = -3;
            foreach (var gridPosition in (IEnumerable<GridPosition>)copiedSlotGrid)
            {
                // 跳过不能Swap的位置
                if (IsSlotSwappable(gridPosition) == false)
                    continue;

                var slot = copiedSlotGrid[gridPosition];
                var upPosition = gridPosition + GridPosition.Up;
                var rightPosition = gridPosition + GridPosition.Right;

                if (GridMath.IsPositionOnBoard(copiedSlotGrid, upPosition, out var upSlot) &&
                    IsSlotSwappable(upPosition))
                {
                    // 暂时交换双方位置, 用于检测
                    (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);


                    // 向上
                    {
                        if (CheckMatchAt(copiedSlotGrid, upPosition, -1,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            PossibleSwaps.Enqueue(new PossibleSwap()
                            {
                                IsSynthesize = false,
                                StartPosition = gridPosition,
                                SwapDirection = GridPosition.Up,
                                ContainPosition = matchedShapePositions,

                                HintAnimation = null,
                                AnimationWorldPosition = animationWorldPosition,
                                AnimationRotation = animationRotation
                            }, basicMatchPriority);
                        }
                    }

                    // 向下
                    {
                        if (CheckMatchAt(copiedSlotGrid, gridPosition, -1,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            PossibleSwaps.Enqueue(new PossibleSwap()
                            {
                                IsSynthesize = false,
                                StartPosition = upPosition,
                                SwapDirection = GridPosition.Down,
                                ContainPosition = matchedShapePositions,

                                HintAnimation = null,
                                AnimationWorldPosition = animationWorldPosition,
                                AnimationRotation = animationRotation
                            }, basicMatchPriority);
                        }
                    }


                    // 复原交换
                    (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
                }


                if (GridMath.IsPositionOnBoard(copiedSlotGrid, rightPosition, out var rightSlot) &&
                    IsSlotSwappable(rightPosition))
                {
                    // 暂时交换双方位置, 用于检测
                    (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);

                    // 向右
                    {
                        if (CheckMatchAt(copiedSlotGrid, rightPosition, -1,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            PossibleSwaps.Enqueue(new PossibleSwap()
                            {
                                IsSynthesize = false,
                                StartPosition = gridPosition,
                                SwapDirection = GridPosition.Right,
                                ContainPosition = matchedShapePositions,

                                HintAnimation = null,
                                AnimationWorldPosition = animationWorldPosition,
                                AnimationRotation = animationRotation
                            }, basicMatchPriority);
                        }
                    }

                    // 向左
                    {
                        if (CheckMatchAt(copiedSlotGrid, gridPosition, -1,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            PossibleSwaps.Enqueue(new PossibleSwap()
                            {
                                IsSynthesize = false,
                                StartPosition = rightPosition,
                                SwapDirection = GridPosition.Left,
                                ContainPosition = matchedShapePositions,

                                HintAnimation = null,
                                AnimationWorldPosition = animationWorldPosition,
                                AnimationRotation = animationRotation
                            }, basicMatchPriority);
                        }
                    }

                    // 复原交换
                    (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
                }
            }

            // 此时第二优先级已检查完毕
            if (PossibleSwaps.Count > 0)
            {
                return;
            }
        }

        // 开始第三优先级
        // 第三优先级: Rainbow + 基础棋子
        foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            // 跳过不能Swap的位置
            if (IsSlotSwappable(gridPosition) == false)
                continue;

            // 跳过非Rainbow的位置
            var slot = slotGrid[gridPosition];
            if (slot.piece == null || slot.piece.Id != Constants.PieceRainbowId)
                continue;

            var upPosition = gridPosition + GridPosition.Up;
            var rightPosition = gridPosition + GridPosition.Right;
            var downPosition = gridPosition + GridPosition.Down;
            var leftPosition = gridPosition + GridPosition.Left;

            // 向上
            if (GridMath.IsPositionOnBoard(slotGrid, upPosition, out var upSlot) &&
                IsSlotSwappable(upPosition) &&
                upSlot.piece != null && Constants.BasicPieceIds.Contains(upSlot.piece.Id))
            {
                PossibleSwaps.Enqueue(new PossibleSwap()
                {
                    IsSynthesize = false,
                    StartPosition = gridPosition,
                    SwapDirection = GridPosition.Up,
                    ContainPosition = new List<GridPosition> { gridPosition, upPosition },

                    HintAnimation = hintAnimations[1],
                    AnimationWorldPosition = (GetGridPositionWorldPosition(gridPosition) + GetGridPositionWorldPosition(upPosition)) / 2,
                    AnimationRotation = 90
                }, -1 * GetGameBoardFreePieceCountByPieceId(upSlot.piece.Id));
            }

            // 向右
            if (GridMath.IsPositionOnBoard(slotGrid, rightPosition, out var rightSlot) &&
                IsSlotSwappable(rightPosition) &&
                rightSlot.piece != null && Constants.BasicPieceIds.Contains(rightSlot.piece.Id))
            {
                PossibleSwaps.Enqueue(new PossibleSwap()
                {
                    IsSynthesize = false,
                    StartPosition = gridPosition,
                    SwapDirection = GridPosition.Right,
                    ContainPosition = new List<GridPosition> { gridPosition, rightPosition },

                    HintAnimation = hintAnimations[1],
                    AnimationWorldPosition = (GetGridPositionWorldPosition(gridPosition) + GetGridPositionWorldPosition(rightPosition)) / 2,
                    AnimationRotation = 0
                }, -1 * GetGameBoardFreePieceCountByPieceId(rightSlot.piece.Id));
            }

            // 向下
            if (GridMath.IsPositionOnBoard(slotGrid, downPosition, out var downSlot) &&
                IsSlotSwappable(downPosition) &&
                downSlot.piece != null && Constants.BasicPieceIds.Contains(downSlot.piece.Id))
            {
                PossibleSwaps.Enqueue(new PossibleSwap()
                {
                    IsSynthesize = false,
                    StartPosition = gridPosition,
                    SwapDirection = GridPosition.Down,
                    ContainPosition = new List<GridPosition> { gridPosition, downPosition },

                    HintAnimation = hintAnimations[1],
                    AnimationWorldPosition = (GetGridPositionWorldPosition(gridPosition) + GetGridPositionWorldPosition(downPosition)) / 2,
                    AnimationRotation = 90
                }, -1 * GetGameBoardFreePieceCountByPieceId(downSlot.piece.Id));
            }

            // 向左
            if (GridMath.IsPositionOnBoard(slotGrid, leftPosition, out var leftSlot) &&
                IsSlotSwappable(leftPosition) &&
                leftSlot.piece != null && Constants.BasicPieceIds.Contains(leftSlot.piece.Id))
            {
                PossibleSwaps.Enqueue(new PossibleSwap()
                {
                    IsSynthesize = false,
                    StartPosition = gridPosition,
                    SwapDirection = GridPosition.Left,
                    ContainPosition = new List<GridPosition> { gridPosition, leftPosition },

                    HintAnimation = hintAnimations[1],
                    AnimationWorldPosition = (GetGridPositionWorldPosition(gridPosition) + GetGridPositionWorldPosition(leftPosition)) / 2,
                    AnimationRotation = 0
                }, -1 * GetGameBoardFreePieceCountByPieceId(leftSlot.piece.Id));
            }
        }
    }


    /// <summary>
    /// 检查在给定SlotGrid情况下是否存在可能的Match情况
    /// </summary>
    public bool CanFindPossibleMatch(Grid newSlotGrid)
    {
        foreach (var gridPosition in (IEnumerable<GridPosition>)newSlotGrid)
        {
            // 跳过不能Swap的位置
            if (IsSlotSwappable(gridPosition) == false)
                continue;

            var slot = newSlotGrid[gridPosition];
            var upPosition = gridPosition + GridPosition.Up;
            var rightPosition = gridPosition + GridPosition.Right;

            if (GridMath.IsPositionOnBoard(newSlotGrid, upPosition, out var upSlot) &&
                IsSlotSwappable(upPosition))
            {
                // 暂时交换双方位置, 用于检测
                (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);

                foreach (var index in matchCheckOrder)
                {
                    // 向上
                    {
                        if (CheckMatchAt(newSlotGrid, upPosition, index,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
                            return true;
                        }
                    }

                    // 向下
                    {
                        if (CheckMatchAt(newSlotGrid, gridPosition, index,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
                            return true;
                        }
                    }
                }

                // 复原交换
                (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
            }


            if (GridMath.IsPositionOnBoard(newSlotGrid, rightPosition, out var rightSlot) &&
                IsSlotSwappable(rightPosition))
            {
                // 暂时交换双方位置, 用于检测
                (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);

                foreach (var index in matchCheckOrder)
                {
                    // 向右
                    {
                        if (CheckMatchAt(newSlotGrid, rightPosition, index,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
                            return true;
                        }
                    }

                    // 向左
                    {
                        if (CheckMatchAt(newSlotGrid, gridPosition, index,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
                            return true;
                        }
                    }
                }

                // 复原交换
                (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
            }
        }

        return false;
    }


    /// <summary>
    /// 处理提示
    /// </summary>
    private void DoHint()
    {
        var possibleSwap = PossibleSwaps.Dequeue();
        gameBoardHintEntity.DisplayHint(possibleSwap);
        HintCoolDown = gameBoardHintEntity.InactivityBeforeHint;
    }



    public void RegisterBottomDamagedCallback(GridPosition damagedPosition, Action<Damage> callback)
    {
        if (!PositionBottomDamagedCallbacks.TryAdd(damagedPosition, callback))
            PositionBottomDamagedCallbacks[damagedPosition] += callback;
    }


    public void UnRegisterBottomDamagedCallback(GridPosition damagedPosition, Action<Damage> callback)
    {
        if (!PositionBottomDamagedCallbacks.ContainsKey(damagedPosition))
            return;

        PositionBottomDamagedCallbacks[damagedPosition] -= callback;
        if (PositionBottomDamagedCallbacks[damagedPosition] == null)
        {
            PositionBottomDamagedCallbacks.Remove(damagedPosition);
        }
    }


    public void RegisterAdjacentDamagedCallback(GridPosition damagedPosition, Action<Damage> callback)
    {
        if (!PositionAdjacentDamagedCallbacks.TryAdd(damagedPosition, callback))
            PositionAdjacentDamagedCallbacks[damagedPosition] += callback;
    }


    public void UnRegisterAdjacentDamagedCallback(GridPosition damagedPosition, Action<Damage> callback)
    {
        if (!PositionAdjacentDamagedCallbacks.ContainsKey(damagedPosition))
            return;

        PositionAdjacentDamagedCallbacks[damagedPosition] -= callback;
        if (PositionAdjacentDamagedCallbacks[damagedPosition] == null)
        {
            PositionAdjacentDamagedCallbacks.Remove(damagedPosition);
        }
    }


    public void RegisterEnterCollectCallback(GridPosition enterPosition, Action<Damage> callback)
    {
        if (!PositionEnterCollectCallbacks.TryAdd(enterPosition, callback))
            PositionEnterCollectCallbacks[enterPosition] += callback;
    }


    public void UnRegisterEnterCollectCallback(GridPosition enterPosition, Action<Damage> callback)
    {
        if (!PositionEnterCollectCallbacks.ContainsKey(enterPosition))
            return;

        PositionEnterCollectCallbacks[enterPosition] -= callback;
        if (PositionEnterCollectCallbacks[enterPosition] == null)
        {
            PositionEnterCollectCallbacks.Remove(enterPosition);
        }
    }


    public void OnFullyDamageUnMovablePieceUpdateGameBoard()
    {
        FindAllSlotsVerticalSpawner();  // 更新掉落口
        UpdateAllSlotsFillType();       // 更新掉落类型
        foreach (var slot in slotGrid)  // 将所有空位置加入列表, 尝试填充
        {
            if (slot.IsActive &&
                slot.FillType != -1 &&
                slot.IsEmpty)
            {
                AddToEmptyPositionsWithDelay(slot.GridPosition, pieceAnimationSetting.addToEmptyPositionsDelay);
            }
        }
    }


    private async void AddToEmptyPositionsWithDelay(GridPosition gridPosition, float delay)
    {
        if (!EmptyPositions.Contains(gridPosition))
        {
            NewEmptyPositions.Add(gridPosition);
            await UniTask.Delay(TimeSpan.FromSeconds(delay));

            if (!EmptyPositions.Contains(gridPosition))
                EmptyPositions.Add(gridPosition);

            NewEmptyPositions.Remove(gridPosition);
        }
    }


    /// <summary>
    /// 添加Action
    /// </summary>
    public void AddAction(IEnumerable<IGameBoardAction> gameBoardActions)
    {
        if (gameBoardActions != null)
        {
            ActivatedActions.AddRange(gameBoardActions);
        }
    }
    public void AddAction(IGameBoardAction gameBoardAction)
    {
        if (gameBoardAction != null)
        {
            ActivatedActions.Add(gameBoardAction);
        }
    }



    /// <summary>
    /// 将位置添加至空位置列表种, 下一帧将会进行检测和移动
    /// </summary>
    public void AddToEmptyPositions(IEnumerable<GridPosition> gridPositions)
    {
        EmptyPositions.AddRange(gridPositions);
    }
    public void AddToEmptyPositions(GridPosition gridPosition)
    {
        EmptyPositions.Add(gridPosition);
    }


    /// <summary>
    /// 暂存Use位置
    /// </summary>
    public void QueueClick(GridPosition usePosition)
    {
        QueuedClickPositions.Enqueue(usePosition);
    }


    /// <summary>
    /// 暂存Swap位置
    /// </summary>
    public void QueueSwap(GridPosition fromPosition, GridPosition toPosition)
    {
        QueuedSwapPositions.Enqueue((fromPosition, toPosition));
    }


    /// <summary>
    /// 对区域内的位置进行一次伤害
    /// </summary>
    /// <param name="bound">区域范围</param>
    public void DamageArea(RectInt bound)
    {
        Damage sourceDamage = new();
        for (int x = bound.xMin; x < bound.xMax; x++)
        {
            for (int y = bound.yMin; y < bound.yMax; y++)
            {
                var curPosition = new GridPosition(x, y);
                if (!GridMath.IsPositionOnGrid(slotGrid, curPosition) ||
                    !GridMath.IsPositionWithinArea(bound, curPosition) ||
                    IsSlotSwapping(curPosition))
                {
                    continue;
                }

                sourceDamage.AddToDamagePositions(curPosition);
            }
        }

        int turnScore = 0;

        // 实际检测时需要+1的底部范围和宽度, 需要考虑边界情况(e.g. 底部边界位置上的棋子已经开始运动, 此时通过这个位置上的槽位无法追踪到, 需要在底部位置再向下一行的incomingPiece追踪到)
        for (int x = bound.xMin; x <= bound.xMax + 1; x++)
        {
            for (int y = bound.yMin - 1; y <= bound.yMax + 1; y++)
            {
                var curPosition = new GridPosition(x, y);
                if (!GridMath.IsPositionOnGrid(slotGrid, curPosition, out var curSlot) ||
                    !GridMath.IsPositionWithinArea(bound, curPosition) ||
                    IsSlotSwapping(curPosition))
                {
                    continue;
                }

                if (sourceDamage.IgnorePositions.Contains(curPosition))
                {
                    continue;
                }

                (Piece curPiece, int pieceType) = curSlot switch
                {
                    var p when p.upperPiece != null => (p.upperPiece, 0),
                    var p when p.piece != null => (p.piece, 1),
                    var p when p.incomingPiece != null => (p.incomingPiece, 2),
                    var p when p.bottomPiece != null => (p.bottomPiece, 3),
                    _ => (null, -1)
                };

                // 触发BottomDamage回调
                if ((pieceType == 1 || pieceType == 2) &&
                    curPiece != null &&
                    Constants.BasicPieceIds.Contains(curPiece.Id) &&
                    PositionBottomDamagedCallbacks.TryGetValue(curPosition, out var callback))
                {
                    callback?.Invoke(sourceDamage);
                    turnScore += 10;
                }

                // 遇到不合法的棋子, 直接返回
                if (curPiece == null || pieceType == -1 ||
                    !curPiece.GridPosition.Equals(curPosition) ||
                    curPiece.CurrentMatch != null ||
                    curPiece.CurrentState == State.Disposed ||
                    curPiece.SelectedToReplace == true ||
                    curPiece.EnteredBoard == false)
                {
                    continue;
                }
                else
                {
                    turnScore += 10;
                }

                if (curPiece.CanUse)
                {
                    if (!curPiece.Used)
                    {
                        HandlePowerupStandaloneActivate(curPiece);
                    }
                    else
                    {
                        (curPiece as Powerup).DestroySelf();
                    }
                }

                curPiece.Damage(
                    sourceDamage,
                    OnDamagePlayVFX,
                    OnDamageCollectTarget,
                    OnDamageControlPosition);
            }
        }

        // 得分
        GameManager.instance.AddScore(turnScore, false);
    }





    /// <summary>
    /// Prop或Powerup对位置进行一次伤害(消除处于最上层的棋子1层ClearNum), 并触发该位置的伤害回调
    /// </summary>
    /// <param name="gridPosition">伤害位置</param>
    public void DamageSlot(Damage sourceDamage, GridPosition gridPosition)
    {
        // 不接受超出棋盘范围的damage
        if (!GridMath.IsPositionOnGrid(slotGrid, gridPosition, out var damageSlot) ||
            IsSlotSwapping(gridPosition)) 
        { 
            return; 
        }

        if (sourceDamage.IgnorePositions.Contains(gridPosition))
        {
            return;
        }

        // 寻找需要消除的棋子
        (Piece damagePiece, int pieceType) = damageSlot switch
        {
            var x when x.upperPiece != null => (x.upperPiece, 0),
            var x when x.piece != null => (x.piece, 1),
            var x when x.incomingPiece != null => (x.incomingPiece, 2),
            var x when x.bottomPiece != null => (x.bottomPiece, 3),
            _ => (null, -1)
        };

        // 本次得分
        int turnScore = 0;

        // 只有中间层有基础棋子才触发BottomDamage回调
        if (damagePiece != null &&
            (pieceType == 1 || pieceType == 2) &&
            Constants.BasicPieceIds.Contains(damagePiece.Id) &&
            PositionBottomDamagedCallbacks.TryGetValue(gridPosition, out var callback))
        {
            callback?.Invoke(sourceDamage);
            turnScore += 10;
        }

        // 遇到不合法的棋子, 直接返回
        if (damagePiece == null || pieceType == -1 ||           // 未能找到棋子
            (damagePiece.MovingToSlot != null && !damagePiece.GridPosition.Equals(gridPosition)) ||   // 运动中的棋子还未进入本位置中
            damagePiece.CurrentMatch != null ||                 // 棋子包含在Match中
            damagePiece.CurrentState == State.Disposed ||       // 棋子已经被释放
            damagePiece.SelectedToReplace == true ||            // 棋子被Rainbow选中
            damagePiece.EnteredBoard == false)                  // 棋子还未生成完毕
        {
            return;
        }
        else
        {
            turnScore += 10;
            GameManager.instance.AddScore(turnScore, false);
        }

        if (damagePiece.CanUse)
        {
            if (!damagePiece.Used)
            {
                HandlePowerupStandaloneActivate(damagePiece);
            }
            //else
            //{
            //    (damagePiece as Powerup).DestroySelf();
            //}
        }

        damagePiece.Damage(
            sourceDamage,
            OnDamagePlayVFX,
            OnDamageCollectTarget,
            OnDamageControlPosition);
    }


    // note that gridPosition param has to be its MovingToSlot's GridPosition
    public void DamagePiece(Damage sourceDamage, Piece damagePiece, GridPosition gridPosition)
    {
        if (IsSlotSwapping(gridPosition))
            return;

        damagePiece.Damage(
            sourceDamage,
            OnDamagePlayVFX,
            OnDamageCollectTarget,
            OnDamageControlPosition);

        GameManager.instance.AddScore(10, false);
    }


    /// <summary>
    /// 仅由RainbowAciton触发的, 和基础棋子交换后消除棋子
    /// </summary>
    public void RainbowActionDamage(Damage sourceDamage, Piece damagePiece)
    {
        if (damagePiece == null ||
            damagePiece.CanUse ||
            damagePiece.CurrentMatch != null ||
            damagePiece.CurrentState == State.Disposed || 
            damagePiece.EnteredBoard == false ||
            !Constants.BasicPieceIds.Contains(damagePiece.Id))
        {
            return;
        }

        int turnScore = 0;
        var position = damagePiece.GridPosition;

        if (sourceDamage.IgnorePositions.Contains(position))
        {
            return;
        }


        // 触发AdjacentDamage回调
        if (PositionAdjacentDamagedCallbacks.TryGetValue(position, out var adjacentCallback))
        {
            adjacentCallback?.Invoke(sourceDamage);
            turnScore += 10;
        }

        // 触发BottomDamage回调
        if (PositionBottomDamagedCallbacks.TryGetValue(position, out var callback))
        {
            callback?.Invoke(sourceDamage);
            turnScore += 10;
        }

        damagePiece.Damage(
            sourceDamage,
            OnDamagePlayVFX, 
            OnDamageCollectTarget, 
            OnDamageControlPosition);

        turnScore += 10;
        GameManager.instance.AddScore(turnScore, false);
    }


    private void OnDamagePlayVFX(Vector3 worldPosition, AnimationReferenceAsset animationReferenceAsset)
    {
        damageVFXPool.PlayDamageVFXAt(worldPosition, animationReferenceAsset);
    }


    private void OnDamageCollectTarget(int pieceId, Vector3 worldPosition)
    {
        if (TryUpdateLevelTargetPiece(pieceId, out var collectId, out var collectPrefab))
        {
            TryCollectLevelTargetPiece(collectId, collectPrefab, worldPosition);
        }
    }


    private void OnDamageControlPosition(Piece piece)
    {
        if (piece.ClearNum <= 0)
        {
            ExecutingMatchAndDamageTasks.Add(FreePositionWhenFullyDamagedPiece(piece));
        }
        else
        {
            ExecutingMatchAndDamageTasks.Add(UniTask.Delay(TimeSpan.FromSeconds(pieceAnimationSetting.addToEmptyPositionsDelay)));
        }
    }


    private async UniTask FreePositionWhenFullyDamagedPiece(Piece piece)
    {
        if (Constants.LargePieceIds.Contains(piece.Id))
        {
            var pieceOccupiedSlots = piece.GetOccupiedSlot();

            piecePool.ReleasePiece(piece);
            foreach (var slot in pieceOccupiedSlots)
            {
                slot.piece = null;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(pieceAnimationSetting.addToEmptyPositionsDelay));
            foreach (var slot in pieceOccupiedSlots)
            {
                slot.DecreaseEnterAndLeaveLock();
                if (slot.IsEmpty &&
                    !EmptyPositions.Contains(slot.GridPosition))
                {
                    EmptyPositions.Add(slot.GridPosition);
                }
            }

            return;
        }

        var pieceOccupiedSlot = piece.GetOccupiedSlot().FirstOrDefault();
        var isStill = piece.MovingToSlot == null;
        var pieceLayer = piece.PieceLayer;

        piecePool.ReleasePiece(piece);
        if (pieceLayer == PieceLayer.Upper)
        {
            pieceOccupiedSlot.upperPiece = null;

            await UniTask.Delay(TimeSpan.FromSeconds(pieceAnimationSetting.addToEmptyPositionsDelay));
            pieceOccupiedSlot.DecreaseEnterAndLeaveLock();
        }
        else if (pieceLayer == PieceLayer.Piece)
        {
            if (isStill)
            {
                pieceOccupiedSlot.piece = null;
            }
            else
            {
                pieceOccupiedSlot.incomingPiece = null;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(pieceAnimationSetting.addToEmptyPositionsDelay));
            pieceOccupiedSlot.DecreaseEnterAndLeaveLock();
            if (pieceOccupiedSlot.IsEmpty &&
                !EmptyPositions.Contains(pieceOccupiedSlot.GridPosition))
            {
                EmptyPositions.Add(pieceOccupiedSlot.GridPosition);
            }
        }
        else if (pieceLayer == PieceLayer.Bottom)
        {
            pieceOccupiedSlot.bottomPiece = null;

            await UniTask.Delay(TimeSpan.FromSeconds(pieceAnimationSetting.addToEmptyPositionsDelay));
            pieceOccupiedSlot.DecreaseEnterAndLeaveLock();
        }
        else
        {
            throw new InvalidOperationException("Unknown exception");
        }
    }


    /// <summary>
    /// 直接销毁棋子, 不会播放消除动效和音效, 也需要触发收集目标棋子
    /// </summary>
    public async void DestroyPowerup(Powerup powerup)
    {
        if (TryUpdateLevelTargetPiece(powerup.Id, out var collectId, out var collectPrefab))
        {
            TryCollectLevelTargetPiece(collectId, collectPrefab, powerup.GetWorldPosition());
        }

        GridPosition freePosition;
        if (powerup.MovingToSlot != null)
        {
            freePosition = powerup.MovingToSlot.GridPosition;
            slotGrid[freePosition].incomingPiece = null;
        }
        else
        {
            freePosition = powerup.GridPosition;
            slotGrid[freePosition].piece = null;
        }
        
        slotGrid[freePosition].IncreaseEnterAndLeaveLock();
        piecePool.ReleasePiece(powerup);

        await UniTask.Delay(TimeSpan.FromSeconds(pieceAnimationSetting.addToEmptyPositionsDelay));
        slotGrid[freePosition].DecreaseEnterAndLeaveLock();

        if (slotGrid[freePosition].IsEmpty &&
            !EmptyPositions.Contains(freePosition))
        {
            EmptyPositions.Add(freePosition);
        }
    }


    //public Piece ReplacePiece(Piece originalPiece, int replacePieceId, SpawnTypeEnum replacePieceSpawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    //{
    //    var worldPosition = originalPiece.GetWorldPosition();
    //    bool entered = originalPiece.EnteredBoard;
    //    var gridPosition = originalPiece.GridPosition;

    //    if (TryUpdateLevelTargetPiece(originalPiece.Id, out var collectId, out var collectPrefab))
    //    {
    //        TryCollectLevelTargetPiece(collectId, collectPrefab, originalPiece.GetWorldPosition());
    //    }

    //    GameManager.instance.AddScore(10, false);

    //    Piece replacePiece;
    //    if (originalPiece.MovingToSlot != null)
    //    {
    //        var movingToSlot = originalPiece.MovingToSlot;
    //        piecePool.ReleasePiece(originalPiece);
    //        replacePiece = piecePool.NewPieceAt(worldPosition, replacePieceId, entered, gridPosition, spawnTypeEnum: replacePieceSpawnTypeEnum);
    //        movingToSlot.incomingPiece = replacePiece;
    //    }
    //    else
    //    {
    //        var slot = slotGrid[gridPosition];
    //        piecePool.ReleasePiece(originalPiece);
    //        replacePiece = piecePool.NewPieceAt(worldPosition, replacePieceId, entered, gridPosition, spawnTypeEnum: replacePieceSpawnTypeEnum);
    //        slot.piece = replacePiece;
    //    }
    //    return replacePiece;
    //}


    /// <summary>
    /// 直接替换槽位上的棋子, 也需要触发收集目标棋子(如果是的话)
    /// </summary>
    public Piece ReplacePiece(GridPosition replacePosition, int replacePieceId, SpawnTypeEnum replacePieceSpawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        Piece originalPiece = slotGrid[replacePosition] switch
        {
            var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
            var x when x.incomingPiece != null => x.incomingPiece,
            _ => null
        };

        Vector3 originalPieceWorldPosition = originalPiece != null ? originalPiece.GetWorldPosition() : GetGridPositionWorldPosition(replacePosition);
        bool entered = originalPiece != null ? originalPiece.EnteredBoard : true;

        Piece replacedPiece;
        if (originalPiece != null)
        {
            if (TryUpdateLevelTargetPiece(originalPiece.Id, out var collectId, out var collectPrefab))
            {
                TryCollectLevelTargetPiece(collectId, collectPrefab, originalPiece.GetWorldPosition());
            }

            GameManager.instance.AddScore(10, false);

            if (originalPiece.MovingToSlot != null)
            {
                var movingToSlot = originalPiece.MovingToSlot;
                piecePool.ReleasePiece(originalPiece);
                replacedPiece = piecePool.NewPieceAt(originalPieceWorldPosition, replacePieceId, entered, replacePosition, spawnTypeEnum: replacePieceSpawnTypeEnum);
                movingToSlot.incomingPiece = replacedPiece;
            }
            else
            {
                var slot = slotGrid[replacePosition];
                piecePool.ReleasePiece(originalPiece);
                replacedPiece = piecePool.NewPieceAt(originalPieceWorldPosition, replacePieceId, entered, replacePosition, spawnTypeEnum: replacePieceSpawnTypeEnum);
                slot.piece = replacedPiece;
            }
        }
        else replacedPiece = NewPieceAt(replacePosition, replacePieceId, replacePieceSpawnTypeEnum);

        return replacedPiece;
    }


    /// <summary>
    /// 棋盘未初始化时生成棋子
    /// </summary>
    private Piece PlacePieceAt(GridPosition gridPosition, GridPosition rootGridPosition, int id, int clearNum, PieceColors pieceColors)
    {
        Vector3 worldPosition;
        pieceConfigSO.allRegisteredPieces.TryGetValue(id, out var registeredPiece);
        if (registeredPiece.pieceInsArgs.pieceSize != new Vector2Int(1, 1))
        {
            var bound = new RectInt(rootGridPosition.X, rootGridPosition.Y, registeredPiece.pieceInsArgs.pieceSize.x - 1, registeredPiece.pieceInsArgs.pieceSize.y - 1);
            worldPosition = (GetGridPositionWorldPosition(rootGridPosition) + 
                             GetGridPositionWorldPosition(new GridPosition(rootGridPosition.X + bound.width, rootGridPosition.Y + bound.height))) / 2;
        }
        else worldPosition = GetGridPositionWorldPosition(gridPosition);

        return piecePool.NewPieceAt(worldPosition, id, true, gridPosition, 
                                    true, clearNum,
                                    true, pieceColors);
    }


    /// <summary>
    /// 直接在棋盘上生成一个棋子
    /// </summary>
    private Piece NewPieceAt(GridPosition gridPosition, int id, SpawnTypeEnum pieceSpawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        if (boardInitialized)
        {
            if (GridMath.IsPositionOnBoard(slotGrid, gridPosition))
            {
                var newPiece = piecePool.NewPieceAt(GetGridPositionWorldPosition(gridPosition), id, true, gridPosition, spawnTypeEnum: pieceSpawnTypeEnum);
                slotGrid[gridPosition].piece = newPiece;

                if (EmptyPositions.Contains(gridPosition))
                {
                    EmptyPositions.Remove(gridPosition);
                }
                return newPiece;
            }
        }
        else throw new InvalidOperationException("Not allowed to new a piece when game board is not initialized, use PlacePieceAt function instead!");
        return null;
    }


    /// <summary>
    /// 激活掉落口, 生成一个棋子并移动到掉落口, 使用这个方法生成的棋子会按照配置的掉落规则进行生成
    /// </summary>
    private Piece ActivateSpawnerAt(GridPosition spawnerPosition, bool ignoreSpawnerRule)
    {
        var insGridPosition = spawnerPosition + GridPosition.Up;

        // 按照配置的生成规则在生成口出获得棋子Id
        int pieceId = ignoreSpawnerRule ? GetPieceIdToAvoidMatch(spawnerPosition) : GameManager.instance.GetPieceIdFromSpawner(spawnerPosition);
        var piece = piecePool.NewPieceAt(GetGridPositionWorldPosition(insGridPosition), pieceId, false, insGridPosition);

        // 当生成棋子是特殊棋子(非powerup时), 需要检查当前场上的棋子数量
        if (!Constants.BasicPieceIds.Contains(pieceId) && !Constants.PowerupPieceIds.Contains(pieceId))
        {
            int existCount = 0;
            foreach (var slot in slotGrid)
            {
                Piece checkPiece = slot switch
                {
                    var x when x.upperPiece != null => x.upperPiece,
                    var x when x.piece != null => x.piece,
                    var x when x.incomingPiece != null => x.incomingPiece,
                    var x when x.bottomPiece != null => x.bottomPiece,
                    _ => null
                };

                if (checkPiece != null && checkPiece.Id == pieceId)
                {
                    existCount++;
                }
            }

            // 本特殊棋子为本关的目标棋子 && 棋盘中本特殊棋子数量 >= 剩余的需求数量时
            // 需要更新所有使用本策略的生成口
            if (GameManager.LevelTarget.TargetDic.TryGetValue(pieceConfigSO.allRegisteredPieces[pieceId].pieceTargetReference.collectId, out var leftTargetCount) &&
                existCount >= leftTargetCount)
            {
                var discardSpawnerRuleIndex = GameManager.LevelSpawnerRule.SpawnerRuleDic[spawnerPosition].Peek().index;
                GameManager.LevelSpawnerRule.DiscardSpawnerRule(discardSpawnerRuleIndex);
            }
        }

        return piece;
    }


    /// <summary>
    /// TODO: 生成的棋子需要尽可能避免Match
    /// </summary>
    /// <param name="spawnerPosition"></param>
    /// <returns></returns>
    private int GetPieceIdToAvoidMatch(GridPosition spawnerPosition)
    {
        //int[,] pieceIdGrid = new int[yMax, xMax];
        //foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
        //{

        //}
        return GameManager.instance.GetPieceIdFromSpawner(spawnerPosition);
    }


    /// <summary>
    /// 当棋子被完全摧毁时尝试更新LevelTarget
    /// </summary>
    /// <param name="pieceId">棋子的Id</param>
    /// <param name="collectId">棋子收集物Id</param>
    /// <param name="collectPrefab">收集物Prefab</param>
    /// <returns>棋子能否被收集且当前棋子目标为被完成</returns>
    private bool TryUpdateLevelTargetPiece(int pieceId, out int collectId, out GameObject collectPrefab)
    {
        if (pieceConfigSO.allRegisteredPieces.TryGetValue(pieceId, out var registeredPiece) == false)
            throw new InvalidOperationException("Unknown piece");

        collectId = registeredPiece.pieceTargetReference.collectId;
        if (collectId != 0 &&
            GameManager.LevelTarget.TargetDic.TryGetValue(collectId, out var leftCount) &&
            leftCount > 0)
        {
            // 可被收集 && 本棋子是目标 && 当前目标未完成
            // 更新目标棋子剩余数量, 并按照当前其他目标完成情况判断完成/失败游戏
            if (GameManager.LevelTarget.UpdateTargetPieces(collectId))
            {
                GameManager.instance.OnAllLevelTargetsCompleted().Forget();
            }

            collectPrefab = registeredPiece.pieceTargetReference.pieceCollectPrefab;
            return true;
        }

        collectPrefab = null;
        return false;
    }


    /// <summary>
    /// 当棋子被完全销毁时进行收集
    /// </summary>
    private async void TryCollectLevelTargetPiece(int collectId, GameObject collectPrefab, Vector3 startPosition, float delayTime = 0f)
    {
        if (delayTime > 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delayTime));
        }

        if (collectPrefab != null)
        {
            // 对需要飞行的生成实例, 在回调中更新
            var insCollectableObj = Instantiate(collectPrefab, startPosition, Quaternion.identity);
            var insCollectable = insCollectableObj.GetComponent<Collectable>();
            insCollectable.StartBezierMoveToLevelTargetDisplay();
        }
        else
        {
            // 直接更新
            MainGameUIManager.Instance.UpdateLevelTargetDisplay(collectId);
        }

    }


    /// <summary>
    /// 用于处理点击槽位棋子或使用触发位置上的powerup
    /// </summary>
    private void ClickPiece(GridPosition clickPosition)
    {
        if (GridMath.IsPositionOnGrid(slotGrid, clickPosition, out var clickSlot))
        {
            var piece = clickSlot switch
            {
                var x when x.upperPiece != null => clickSlot.upperPiece,
                var x when x.piece != null => clickSlot.piece,
                var x when x.incomingPiece != null => clickSlot.incomingPiece,
                var x when x.bottomPiece != null => clickSlot.bottomPiece,
                _ => null
            };

            if (piece == null)
            {
                return;
            }
            else
            {
                Debug.Log($"<color=orange>Click</color> {clickPosition}");

                if (piece.CanUse)
                {
                    if (!piece.Used && !piece.SelectedToReplace)
                    {
                        HandlePowerupStandaloneActivate(piece);

                        // 点击Powerup消耗步数
                        GameManager.instance.ConsumeMove();
                    }
                }
                else piece.PlayClickAnimation();
            }
        }
    }


    /// <summary>
    /// 用于处理在使用Prop的情况下点击棋盘上的棋子
    /// </summary>
    private void UsingPropClickPiece(GridPosition clickPosition, UsingProp usingProp)
    {
        if (GridMath.IsPositionOnBoard(slotGrid, clickPosition))
        {
            Debug.Log($"<color=orange>Click</color> {clickPosition} using prop = {usingProp}");
            
            if (usingProp == UsingProp.Hammer)
            {
                var insHammer = Instantiate(hammerPrefab, hammerPropButton.transform.position, Quaternion.identity);
                insHammer.PropActivate(clickPosition);
                AddAction(insHammer);
            }
            else if (usingProp == UsingProp.Gun)
            {
                var playWorldPosition = new Vector3(GameManager.instance.ScreenWidth - 0.3f, GetGridPositionWorldPosition(clickPosition).y, 0);
                var insGun = Instantiate(gunPrefab, playWorldPosition, Quaternion.identity);
                insGun.PropActivate(slotGrid, clickPosition);
                AddAction(insGun);
            }
            else if (usingProp == UsingProp.Cannon)
            {
                // 按钮渐隐
                MainGameUIManager.HideBottomButtons();

                var playWorldPosition = GetGridPositionWorldPosition(new GridPosition(clickPosition.X, 11));
                var insCannon = Instantiate(cannonPrefab, playWorldPosition, Quaternion.identity);
                insCannon.PropActivate(slotGrid, clickPosition);
                AddAction(insCannon);
            }


            if (usingProp != UsingProp.None) 
            {
                UserDataManager.ConsumeProp();
                MainGameUIManager.PropUsingOff();
            }
        }
    }


    public void UsingPropDice()
    {
        Debug.Log("<color=orange>Click</color> using prop = Dice");

        var usingProp = GameManager.CurrentProp;
        var playWorldPosition = transform.position;
        var insDice = Instantiate(dicePrefab, playWorldPosition, Quaternion.identity);
        insDice.PropActivate();
        AddAction(insDice);

        if (usingProp != UsingProp.None)
        {
            UserDataManager.ConsumeProp();
            MainGameUIManager.PropUsingOff();
        }
    }


    /// <summary>
    /// 用于处理交换两个槽位棋子
    /// </summary>
    private async void SwapPiece(GridPosition fromPosition, GridPosition toPosition)
    {
        Piece pieceFrom = slotGrid[fromPosition].piece;
        Piece pieceTo = slotGrid[toPosition].piece;

        // 再次校验双方不能同时为空
        if (pieceFrom == null && pieceTo == null) 
        {
            SwappingPositions.Remove((fromPosition, toPosition));
            SwappingPositions.Remove((toPosition, fromPosition));
            return; 
        }

        if ((pieceFrom != null && pieceFrom.CurrentMatch != null) ||
            (pieceTo != null && pieceTo.CurrentMatch != null))
        {
            SwappingPositions.Remove((fromPosition, toPosition));
            SwappingPositions.Remove((toPosition, fromPosition));
            return; 
        }

        // 记录交换
        if (SwappingPositions.TryAdd((fromPosition, toPosition), SwapStage.Forward) ||
            SwappingPositions.TryAdd((toPosition, fromPosition), SwapStage.Forward))
        {
            var rotation = fromPosition.X == toPosition.X ? Quaternion.Euler(0, 0, 90f) : Quaternion.identity;
            explodeVFXPool.PlayExplodeVFXAt((GetGridPositionWorldPosition(fromPosition) + GetGridPositionWorldPosition(toPosition)) / 2, rotation, swapVFXAnimation, 10);
        }

        // 判断交换后是否需要消耗步数
        bool consumeMove = false;
        consumeMove |= CheckCanRainbowActivate(pieceTo, pieceFrom);
        consumeMove |= CheckCanRainbowActivate(pieceFrom, pieceTo);
        if (!consumeMove)
        {
            consumeMove = (pieceFrom != null && pieceFrom.CanUse) || (pieceTo != null && pieceTo.CanUse);
            if (!consumeMove)
            {
                slotGrid[fromPosition].piece = pieceTo;
                slotGrid[toPosition].piece = pieceFrom;

                int i = 0;
                while (!consumeMove && i < matchCheckOrder.Count)
                {
                    var index = matchCheckOrder[i];

                    if (pieceFrom != null &&
                        AllowedBasicPieceIds.Contains(pieceFrom.Id))
                    {
                        consumeMove |= CheckMatchAt(toPosition, index, createMatch: false);
                    }

                    if (!consumeMove &&
                        pieceTo != null &&
                        AllowedBasicPieceIds.Contains(pieceTo.Id))
                    {
                        consumeMove |= CheckMatchAt(fromPosition, index, createMatch: false);
                    }

                    i++;
                }

                slotGrid[fromPosition].piece = pieceFrom;
                slotGrid[toPosition].piece = pieceTo;
            }
        }
        if (consumeMove)
        {
            GameManager.instance.ConsumeMove();
        }

        slotGrid[fromPosition].IncreaseEnterAndLeaveLock();
        slotGrid[toPosition].IncreaseEnterAndLeaveLock();

        // 进行交换
        if (pieceFrom != null && pieceTo != null)
        {
            // 交换双方都不为空
            Debug.Log($"<color=orange>Swap</color> {fromPosition} and {toPosition}");

            slotGrid[fromPosition].incomingPiece = pieceTo;
            slotGrid[fromPosition].piece = null;
            slotGrid[toPosition].incomingPiece = pieceFrom;
            slotGrid[toPosition].piece = null;

            // 判断交换双方道具情况
            bool isPieceFromPowerup = pieceFrom.CanUse, isPieceToPowerup = pieceTo.CanUse;
            int powerupCount = isPieceFromPowerup ? isPieceToPowerup ? 2 : 1 : isPieceToPowerup ? 1 : 0;
            pieceFrom.SortingGroup.sortingOrder = pieceFrom.SortingGroup.sortingOrder <= pieceTo.SortingGroup.sortingOrder ? pieceTo.SortingGroup.sortingOrder + 1 : pieceFrom.SortingGroup.sortingOrder;

            if (powerupCount == 0 || powerupCount == 1)
            {
                // 交换双方不全是powerup
                // 执行交换动画
                await DOTween.Sequence()
                    .Join(pieceTo.Transform.DOMove(pieceFrom.GetWorldPosition(), pieceAnimationSetting.pieceSwapDuration))
                    .Join(pieceFrom.Transform.DOMove(pieceTo.GetWorldPosition(), pieceAnimationSetting.pieceSwapDuration))
                    .SetEase(pieceAnimationSetting.pieceSwapEase);

                // 完成后交换双方位置
                slotGrid[fromPosition].incomingPiece = null;
                slotGrid[fromPosition].piece = pieceTo;
                pieceTo.GridPosition = fromPosition;
                slotGrid[toPosition].incomingPiece = null;
                slotGrid[toPosition].piece = pieceFrom;
                pieceFrom.GridPosition = toPosition;

                // 检测交换双方到达的位置是否产生了匹配/使用
                if (!isPieceFromPowerup && !isPieceToPowerup)
                {
                    // pieceTo 此时在 fromPosition位置上
                    // 只有基础棋子才需要检查合成
                    if (AllowedBasicPieceIds.Contains(pieceTo.Id))
                    {
                        foreach (var index in matchCheckOrder)
                        {
                            if (CheckMatchAt(fromPosition, index))
                            {
                                break;
                            }
                        }
                    }

                    // pieceFrom 此时在 toPosition位置上
                    // 只有基础棋子才需要检查合成
                    if (AllowedBasicPieceIds.Contains(pieceFrom.Id))
                    {
                        foreach (var index in matchCheckOrder)
                        {
                            if (CheckMatchAt(toPosition, index))
                            {
                                break;
                            }
                        }
                    }
                }
                else if (isPieceFromPowerup && !isPieceToPowerup)
                {
                    // pieceTo 此时在 fromPosition位置上
                    // 只有基础棋子才需要检查合成
                    if (AllowedBasicPieceIds.Contains(pieceTo.Id))
                    {
                        foreach (var index in matchCheckOrder)
                        {
                            if (CheckMatchAt(fromPosition, index))
                            {
                                break;
                            }
                        }
                    }

                    // Powerup激活
                    HandlePowerupSwapActivate(pieceFrom, pieceTo, toPosition);
                }
                else if (!isPieceFromPowerup && isPieceToPowerup)
                {
                    // pieceFrom 此时在 toPosition位置上
                    // 只有基础棋子才需要检查合成
                    if (AllowedBasicPieceIds.Contains(pieceFrom.Id))
                    {
                        foreach (var index in matchCheckOrder)
                        {
                            if (CheckMatchAt(toPosition, index))
                            {
                                break;
                            }
                        }
                    }

                    // Powerup激活
                    HandlePowerupSwapActivate(pieceTo, pieceFrom, toPosition);
                }

                // 完成了正向交换
                if (SwappingPositions[(fromPosition, toPosition)] == SwapStage.Forward) 
                {
                    if (consumeMove)   
                    {
                        // 产生了匹配, 完成交换
                        SwappingPositions.Remove((fromPosition, toPosition));
                        SwappingPositions.Remove((toPosition, fromPosition));
                    }
                    else
                    {
                        // 未能产生任何匹配, 再进行一次交换
                        // 开始反向交换
                        SwappingPositions[(fromPosition, toPosition)] = SwapStage.Revert;
                        SwappingPositions[(toPosition, fromPosition)] = SwapStage.Revert;
                        SwapPiece(toPosition, fromPosition);
                    }
                }
                else
                {
                    // 完成反向交换后删去本次交换的记录
                    SwappingPositions.Remove((fromPosition, toPosition));
                    SwappingPositions.Remove((toPosition, fromPosition));
                }
            }
            else
            {
                // 交换双方全是道具
                // From Powerup移动到To Powerup
                await pieceFrom.Transform
                    .DOMove(pieceTo.GetWorldPosition(), pieceAnimationSetting.pieceSwapDuration)
                    .SetEase(pieceAnimationSetting.pieceSwapEase);

                // 设置完成后的位置
                slotGrid[fromPosition].piece = pieceFrom;
                slotGrid[fromPosition].incomingPiece = null;
                slotGrid[toPosition].piece = pieceTo;
                slotGrid[toPosition].incomingPiece = null;

                HandlePowerupSwapActivate(pieceFrom, pieceTo, toPosition);

                // powerup+powerup交换后总是删去本次交换
                SwappingPositions.Remove((fromPosition, toPosition));
                SwappingPositions.Remove((toPosition, fromPosition));
            }
        }
        else if (pieceTo != null && pieceFrom == null)
        {
            // 交换双方有一方为空(pieceFrom为空)
            Debug.Log($"<color=orange>Swap</color> {fromPosition} and {toPosition}, {fromPosition} is null");

            slotGrid[fromPosition].IncreaseEnterLock(1);
            slotGrid[fromPosition].IncreaseLeaveLock(1);
            slotGrid[fromPosition].incomingPiece = pieceTo;
            slotGrid[fromPosition].piece = null;

            slotGrid[toPosition].IncreaseEnterLock(1);
            slotGrid[toPosition].IncreaseLeaveLock(1);
            slotGrid[toPosition].incomingPiece = null;
            slotGrid[toPosition].piece = null;

            // 将PieceTo移动到fromPosition
            await pieceTo.Transform
                .DOMove(GetGridPositionWorldPosition(fromPosition), pieceAnimationSetting.pieceSwapDuration)
                .SetEase(pieceAnimationSetting.pieceSwapEase);

            // 设置完成后的位置
            slotGrid[fromPosition].DecreaseEnterLock(1);
            slotGrid[fromPosition].DecreaseLeaveLock(1);
            slotGrid[fromPosition].piece = pieceTo;
            slotGrid[fromPosition].incomingPiece = null;
            pieceTo.GridPosition = fromPosition;

            slotGrid[toPosition].DecreaseEnterLock(1);
            slotGrid[toPosition].DecreaseLeaveLock(1);

            // 检查Match和Activate情况
            if (pieceTo.CanUse)
            {
                HandlePowerupSwapActivate(pieceTo, null, fromPosition);
            }
            else if (AllowedBasicPieceIds.Contains(pieceTo.Id))
            {
                foreach (var index in matchCheckOrder)
                {
                    if (CheckMatchAt(fromPosition, index))
                    {
                        break;
                    }
                }
            }


            if (SwappingPositions[(fromPosition, toPosition)] == SwapStage.Forward)
            {
                if (consumeMove)
                {
                    // 加入空位置
                    if (slotGrid[fromPosition].IsEmpty &&
                        !EmptyPositions.Contains(fromPosition))
                    {
                        EmptyPositions.Add(fromPosition);
                    }
                    if (slotGrid[toPosition].IsEmpty &&
                        !EmptyPositions.Contains(toPosition))
                    {
                        EmptyPositions.Add(toPosition);
                    }

                    // 产生了匹配, 完成交换
                    SwappingPositions.Remove((fromPosition, toPosition));
                    SwappingPositions.Remove((toPosition, fromPosition));
                }
                else
                {
                    // 未能产生任何匹配, 再进行一次交换
                    // 开始反向交换
                    SwappingPositions[(fromPosition, toPosition)] = SwapStage.Revert;
                    SwappingPositions[(toPosition, fromPosition)] = SwapStage.Revert;
                    SwapPiece(toPosition, fromPosition);
                }
            }
            else
            {
                // 完成反向交换后删去本次交换的记录
                // 加入空位置
                if (slotGrid[fromPosition].IsEmpty &&
                    !EmptyPositions.Contains(fromPosition))
                {
                    EmptyPositions.Add(fromPosition);
                }
                if (slotGrid[toPosition].IsEmpty &&
                    !EmptyPositions.Contains(toPosition))
                {
                    EmptyPositions.Add(toPosition);
                }

                SwappingPositions.Remove((fromPosition, toPosition));
                SwappingPositions.Remove((toPosition, fromPosition));
            }
        }
        else if (pieceFrom != null && pieceTo == null)
        {
            // 交换双方有一方为空(pieceTo为空)
            Debug.Log($"<color=orange>Swap</color> {fromPosition} and {toPosition}, {toPosition} is null");

            slotGrid[fromPosition].IncreaseEnterLock(1);
            slotGrid[fromPosition].IncreaseLeaveLock(1);
            slotGrid[fromPosition].incomingPiece = null;
            slotGrid[fromPosition].piece = null;

            slotGrid[toPosition].IncreaseEnterLock(1);
            slotGrid[toPosition].IncreaseLeaveLock(1);
            slotGrid[toPosition].incomingPiece = pieceFrom;
            slotGrid[toPosition].piece = null;

            // 将PieceFrom移动到toPosition
            await pieceFrom.Transform
                .DOMove(GetGridPositionWorldPosition(toPosition), pieceAnimationSetting.pieceSwapDuration)
                .SetEase(pieceAnimationSetting.pieceSwapEase);

            // 设置完成后的位置
            slotGrid[fromPosition].DecreaseEnterLock(1);
            slotGrid[fromPosition].DecreaseLeaveLock(1);

            slotGrid[toPosition].DecreaseEnterLock(1);
            slotGrid[toPosition].DecreaseLeaveLock(1);
            slotGrid[toPosition].piece = pieceFrom;
            slotGrid[toPosition].incomingPiece = null;
            pieceFrom.GridPosition = toPosition;

            // 检查Match和Activate情况
            if (pieceFrom.CanUse)
            {
                HandlePowerupSwapActivate(pieceFrom, null, toPosition);
            }
            else if (AllowedBasicPieceIds.Contains(pieceFrom.Id))
            {
                foreach (var index in matchCheckOrder)
                {
                    if (CheckMatchAt(toPosition, index))
                    {
                        break;
                    }
                }
            }

            if (SwappingPositions[(fromPosition, toPosition)] == SwapStage.Forward)
            {
                if (consumeMove)
                {
                    // 加入空位置
                    if (slotGrid[fromPosition].IsEmpty &&
                        !EmptyPositions.Contains(fromPosition))
                    {
                        EmptyPositions.Add(fromPosition);
                    }
                    if (slotGrid[toPosition].IsEmpty &&
                        !EmptyPositions.Contains(toPosition))
                    {
                        EmptyPositions.Add(toPosition);
                    }

                    // 产生了匹配, 完成交换
                    SwappingPositions.Remove((fromPosition, toPosition));
                    SwappingPositions.Remove((toPosition, fromPosition));
                }
                else
                {
                    // 未能产生任何匹配, 再进行一次交换
                    // 开始反向交换
                    SwappingPositions[(fromPosition, toPosition)] = SwapStage.Revert;
                    SwappingPositions[(toPosition, fromPosition)] = SwapStage.Revert;
                    SwapPiece(toPosition, fromPosition);
                }
            }
            else
            {
                // 完成反向交换后删去本次交换的记录
                // 加入空位置
                if (slotGrid[fromPosition].IsEmpty &&
                    !EmptyPositions.Contains(fromPosition)) 
                {
                    EmptyPositions.Add(fromPosition);
                }
                if (slotGrid[toPosition].IsEmpty &&
                    !EmptyPositions.Contains(toPosition))
                {
                    EmptyPositions.Add(toPosition);
                }

                SwappingPositions.Remove((fromPosition, toPosition));
                SwappingPositions.Remove((toPosition, fromPosition));
            }
        }

        slotGrid[fromPosition].DecreaseEnterAndLeaveLock();
        slotGrid[toPosition].DecreaseEnterAndLeaveLock();
    }


    private bool CheckCanRainbowActivate(Piece main, Piece minor)
    {
        if (main == null || main.Id != Constants.PieceRainbowId) { return false; }

        if (minor == null || !minor.CanUse)
        {
            if (minor != null &&
                AllowedBasicPieceIds.Contains(minor.Id))
            {
                return true;
            }
            return false;
        }
        return true;
    }


    /// <summary>
    /// 处理开局重排: no match exsits on gameboard, but at least 1 more possible swap match
    /// </summary>
    public void HandleStartShuffle()
    {
        var basicPiecePositions = new List<GridPosition>();
        var powerupPositions = new List<GridPosition>();
        var basicPieceColors = new Dictionary<int, int>();

        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive ||
                slot.HasMoveConstrain ||
                slot.HasUnMovablePiece ||
                slot.upperPiece != null)
            {
                continue;
            }

            var piece = slot.piece;
            if (piece != null &&
                piece.CurrentMatch == null &&
                piece.CurrentState == State.Still &&
                piece.SelectedByFlyBomb <= 0 &&
                piece.SelectedToReplace == false)
            {
                if (Constants.BasicPieceIds.Contains(piece.Id))
                {
                    // 添加基础棋子位置
                    basicPiecePositions.Add(slot.GridPosition);
                    if (!basicPieceColors.TryAdd(piece.Id, 1))
                    {
                        basicPieceColors[piece.Id]++;
                    }
                }
                else if (Constants.PowerupPieceIds.Contains(piece.Id))
                {
                    // 添加Powerup位置
                    powerupPositions.Add(slot.GridPosition);
                }
            }
        }

        bool canRearrange = basicPieceColors.Values.Any(count => count >= 3);
        var random = new System.Random();
        Dictionary<GridPosition, GridPosition> dic = new(); // (startPosition, endPosition)

        // 如果当前基础棋子情况在重排后也不可能产生至少一个Match, 返回
        if (basicPiecePositions.Count <= 2 || canRearrange == false)
        {
            Debug.LogError("Fail to generate gameboard: not enough piece to create a possible match");
            return;
        }

        // 当前基础棋子情况满足在重排后产生至少一个在一步Swap后生成的Match, 重排全部的基础棋子
        List<GridPosition> originalPositions = basicPiecePositions.Concat(powerupPositions).ToList();
        List<GridPosition> rearrangePositions = basicPiecePositions.Concat(powerupPositions).ToList();
        Dictionary<GridPosition, Piece> originalPieceMap = new();
        foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            originalPieceMap.TryAdd(gridPosition, slotGrid[gridPosition].piece);
        }

        int attempt = 0;
        while (attempt++ <= startShuffleMaxAttempt)
        {
            // Fisher-Yates algorithm
            int n = rearrangePositions.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (rearrangePositions[n], rearrangePositions[k]) = (rearrangePositions[k], rearrangePositions[n]);
            }

            using (var afterSlotGrid = new Grid())
            {
                afterSlotGrid.SetGrid(new Slot[yMax, xMax]);
                foreach (var slot in slotGrid)
                {
                    afterSlotGrid[slot.GridPosition] = slot;
                }

                for (int i = 0; i < rearrangePositions.Count; i++)
                {
                    GridPosition startPosition = originalPositions[i], endPosition = rearrangePositions[i];
                    if (!startPosition.Equals(endPosition))
                    {
                        afterSlotGrid[endPosition].piece = afterSlotGrid[startPosition].piece;
                        dic.TryAdd(startPosition, endPosition);
                    }
                }

                // Make sure there's no exist match after shuffle
                bool noExistMatch = true;
                foreach (var gridPosition in (IEnumerable<GridPosition>)afterSlotGrid)
                {
                    foreach (var index in matchCheckOrder)
                    {
                        if (CheckMatchAt(afterSlotGrid, gridPosition, index, out _, out _, out _, out _))
                        {
                            noExistMatch = false;
                            break;
                        }
                    }

                    if (!noExistMatch)
                        break;
                }

                // Make sure we have a match after swap
                if (noExistMatch &&
                    CanFindPossibleMatch(afterSlotGrid))
                {
                    // reset current slotGrid
                    foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
                    {
                        slotGrid[gridPosition].piece = originalPieceMap[gridPosition];
                    }
                    
                    // place all
                    foreach (var kvp in dic)
                    {
                        var startPosition = kvp.Key;
                        var endPosition = kvp.Value;

                        slotGrid[endPosition].piece = slotGrid[startPosition].piece;
                        slotGrid[endPosition].piece.GridPosition = endPosition;
                    }
                    return;
                }

                // reset current slotGrid, and be ready for next iteration
                foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
                {
                    slotGrid[gridPosition].piece = originalPieceMap[gridPosition];
                }
            }
        }

        // exceed max attempt limit, ignore this
        return;
    }


    /// <summary>
    /// 处理Dice重排
    /// </summary>
    public void HandleDiceShuffle(int maxAttempts)
    {
        var basicPiecePositions = new List<GridPosition>();
        var powerupPositions = new List<GridPosition>();
        var basicPieceColors = new Dictionary<int, int>();

        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive ||
                slot.HasMoveConstrain ||
                slot.HasUnMovablePiece ||
                slot.upperPiece != null)
            { 
                continue;
            }

            var piece = slot.piece;
            if (piece != null &&
                piece.CurrentMatch == null &&
                piece.CurrentState == State.Still &&
                piece.SelectedByFlyBomb <= 0 &&
                piece.SelectedToReplace == false)
            {
                if (Constants.BasicPieceIds.Contains(piece.Id))
                {
                    // 添加基础棋子位置
                    basicPiecePositions.Add(slot.GridPosition);
                    if (!basicPieceColors.TryAdd(piece.Id, 1))
                    {
                        basicPieceColors[piece.Id]++;
                    }
                }
                else if (Constants.PowerupPieceIds.Contains(piece.Id))
                {
                    // 添加Powerup位置
                    powerupPositions.Add(slot.GridPosition);
                }
            }
        }

        bool canRearrange = basicPieceColors.Values.Any(count => count >= 3);
        var random = new System.Random();
        Dictionary<GridPosition, GridPosition> dic = new(); // (startPosition, endPosition)

        if (basicPiecePositions.Count <= 2 || canRearrange == false)
        {
            // 如果当前基础棋子情况在重排后也不可能产生至少一个Match, 那么重排全部的基础棋子和Powerup
            basicPiecePositions.AddRange(powerupPositions);
            basicPiecePositions = basicPiecePositions.OrderBy(x => random.Next()).ToList();
            int left = 0, right = basicPiecePositions.Count - 1;

            // 首尾互换
            while (left < right)
            {
                dic.TryAdd(basicPiecePositions[left], basicPiecePositions[right]);
                dic.TryAdd(basicPiecePositions[right], basicPiecePositions[left]);
                left++;
                right--;
            }

            if (dic.Count > 0)
            {
                DoRearrange(dic);
            }
            return;
        }

        // 当前基础棋子情况满足在重排后产生至少一个在一步Swap后生成的Match, 重排全部的基础棋子
        List<GridPosition> originalPositions = basicPiecePositions.Concat(powerupPositions).ToList();
        List<GridPosition> rearrangePositions = basicPiecePositions.Concat(powerupPositions).ToList();
        Dictionary<GridPosition, Piece> originalPieceMap = new ();
        foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            originalPieceMap.TryAdd(gridPosition, slotGrid[gridPosition].piece);
        }

        int attempt = 0;
        while (attempt <= maxAttempts)
        {
            // Fisher-Yates algorithm
            int n = rearrangePositions.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (rearrangePositions[n], rearrangePositions[k]) = (rearrangePositions[k], rearrangePositions[n]);
            }

            using (var afterSlotGrid = new Grid())
            {
                afterSlotGrid.SetGrid(new Slot[yMax, xMax]);
                foreach (var slot in slotGrid)
                {
                    afterSlotGrid[slot.GridPosition] = slot;
                }

                for (int i = 0; i < rearrangePositions.Count; i++)
                {
                    GridPosition startPosition = originalPositions[i], endPosition = rearrangePositions[i];
                    if (!startPosition.Equals(endPosition))
                    {
                        afterSlotGrid[endPosition].piece = afterSlotGrid[startPosition].piece;
                        dic.TryAdd(startPosition, endPosition);
                    }
                }

                if (CanFindPossibleMatch(afterSlotGrid))
                {
                    foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
                    {
                        slotGrid[gridPosition].piece = originalPieceMap[gridPosition];
                    }
                    break;
                }
            }

            foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
            {
                slotGrid[gridPosition].piece = originalPieceMap[gridPosition];
            }
            dic.Clear();
            attempt++;
        }

        if (attempt > maxAttempts)
        {
            // 超出最大尝试次数, 随机排序
            basicPiecePositions.AddRange(powerupPositions);
            basicPiecePositions = basicPiecePositions.OrderBy(x => random.Next()).ToList();
            int left = 0, right = basicPiecePositions.Count - 1;

            while (left < right)
            {
                dic.TryAdd(basicPiecePositions[left], basicPiecePositions[right]);
                left++;
                right--;
            }
        }

        if (dic.Count > 0)
            DoRearrange(dic);
    }


    /// <summary>
    /// 处理自动重排
    /// </summary>
    private void HandleAutoShuffle()
    {
        if (boardInitialized &&
            GameManager.LevelProgress == LevelProgress.Playing &&
            GetGameBoardPowerupCountDic().Count <= 0)
        {
            Debug.Log("---Auto Shuffle---");

            var basicPiecePositions = new List<GridPosition>();
            var basicPieceColors = new Dictionary<int, int>();

            foreach (var slot in slotGrid)
            {
                if (!slot.IsActive ||
                    slot.HasMoveConstrain ||
                    slot.HasUnMovablePiece ||
                    slot.upperPiece != null)
                {
                    continue;
                }

                var piece = slot.piece;
                if (piece != null &&
                    piece.CurrentMatch == null &&
                    piece.CurrentState == State.Still &&
                    piece.SelectedByFlyBomb <= 0 &&
                    piece.SelectedToReplace == false)
                {
                    if (Constants.BasicPieceIds.Contains(piece.Id))
                    {
                        // 添加基础棋子位置
                        basicPiecePositions.Add(slot.GridPosition);
                        if (!basicPieceColors.TryAdd(piece.Id, 1))
                        {
                            basicPieceColors[piece.Id]++;
                        }
                    }
                }
            }

            bool canRearrange = basicPieceColors.Values.Any(count => count >= 3);
            var random = new System.Random();
            Dictionary<GridPosition, GridPosition> dic = new();

            if (basicPiecePositions.Count <= 2 || canRearrange == false)
            {
                // ignore this situation
                return;
            }

            List<GridPosition> rearrangePositions = new();
            basicPiecePositions.ForEach(pos => rearrangePositions.Add(pos));
            Dictionary<GridPosition, Piece> originalPieceMap = new();
            foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
            {
                originalPieceMap.TryAdd(gridPosition, slotGrid[gridPosition].piece);
            }

            int attempt = 0;
            while (attempt <= autoShuffleMaxAttempt)
            {
                // Fisher-Yates algorithm
                int n = rearrangePositions.Count;
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    (rearrangePositions[n], rearrangePositions[k]) = (rearrangePositions[k], rearrangePositions[n]);
                }

                using (var afterSlotGrid = new Grid())
                {
                    afterSlotGrid.SetGrid(new Slot[yMax, xMax]);
                    foreach (var slot in slotGrid)
                    {
                        afterSlotGrid[slot.GridPosition] = slot;
                    }

                    for (int i = 0; i < rearrangePositions.Count; i++)
                    {
                        GridPosition startPosition = basicPiecePositions[i], endPosition = rearrangePositions[i];
                        if (!startPosition.Equals(endPosition))
                        {
                            afterSlotGrid[endPosition].piece = afterSlotGrid[startPosition].piece;
                            dic.TryAdd(startPosition, endPosition);
                        }
                    }

                    if (CanFindPossibleMatch(afterSlotGrid))
                    {
                        foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
                        {
                            slotGrid[gridPosition].piece = originalPieceMap[gridPosition];
                        }
                        break;
                    }
                }

                foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
                {
                    slotGrid[gridPosition].piece = originalPieceMap[gridPosition];
                }
                dic.Clear();
                attempt++;
            }

            if (attempt > autoShuffleMaxAttempt)
            {
                // exceed max attempt limit, ignore this
                return;
            }

            if (dic.Count > 0)
                DoRearrange(dic);
        }
    }


    private readonly string defaultLevelSortingLayerName = "CenterPiece";
    private readonly string highLevelSortingLayerName = "VFX";
    private async void DoRearrange(Dictionary<GridPosition, GridPosition> dic)
    {
        var allMovedPositions = new List<GridPosition>();
        var sequence = DOTween.Sequence();

        foreach (var kvp in dic)
        {
            GridPosition startPosition = kvp.Key;
            GridPosition endPosition = kvp.Value;
            var destinationWorldPosition = GetGridPositionWorldPosition(endPosition);
            if (!GridMath.IsPositionOnBoard(slotGrid, startPosition, out var startSlot) || 
                !GridMath.IsPositionOnBoard(slotGrid, endPosition, out var endSlot))
            {
                Debug.LogWarning("Rearrange encounters an error: OOB");
                continue;
            }

            if (!allMovedPositions.Contains(kvp.Key))
                allMovedPositions.Add(kvp.Key);

            if (!allMovedPositions.Contains(kvp.Value))
                allMovedPositions.Add(kvp.Value);

            var pieceFromStartPosition = startSlot.piece;
            if (pieceFromStartPosition == null)
            {
                Debug.LogWarning("Rearrange encounters an error: Null Reference");
            }
            
            //Debug.Log($"{startPosition} -> {endPosition}");

            var moveTween = pieceFromStartPosition.Transform
                .DOMove(destinationWorldPosition, pieceAnimationSetting.pieceArrangeMoveDuration)
                .OnStart(() =>
                {
                    pieceFromStartPosition.SortingGroup.sortingLayerName = highLevelSortingLayerName;
                })
                .OnComplete(() =>
                {
                    endSlot.piece = pieceFromStartPosition;
                    pieceFromStartPosition.GridPosition = endPosition;
                    pieceFromStartPosition.SortingGroup.sortingLayerName = defaultLevelSortingLayerName;
                });

            _ = sequence.Join(moveTween);
        }

        await sequence
            .SetDelay(pieceAnimationSetting.pieceArrangeDelayDuration)
            .SetEase(pieceAnimationSetting.pieceArrangeMoveEase)
            .OnStart(() =>
            {
                IsRearranging = true;
                GameManager.instance.MutePlayerInputOnGameBoard();
            })
            .OnUpdate(() => GameBoardInactivity = true)
            .OnComplete(() =>
            {
                IsRearranging = false;
                GameManager.instance.ReceivePlayerInputOnGameBoard();
            });

        allMovedPositions.ForEach(pos =>
        {
            foreach (var index in matchCheckOrder)
            {
                if (CheckMatchAt(pos, index))
                {
                    break;
                }
            }
        });
    }
    

    /// <summary>
    /// 顺序触发棋盘上所有的Powerup
    /// </summary>
    public async UniTask HandleAllPowerupsStandanloneActive()
    {
        var allPowerups = new List<Piece>();
        foreach (var slot in slotGrid)
        {
            if (slot.IsActive == false)
                continue;

            var piece = slot.piece;
            if (piece != null && piece.CanUse && piece.Used == false)
            {
                piece.OnDamageCallback = () =>
                {
                    var rewardCoin = pieceConfigSO.allRegisteredPieces[piece.Id].pieceRewardReference.rewardClearCoin;
                    while (rewardCoin-- > 0)
                    {
                        var coinPrefab = pieceConfigSO.allRegisteredPieces[9999].pieceTargetReference.pieceCollectPrefab;
                        var randomPoisiton = piece.GetWorldPosition() + new Vector3(Random.Range(-slotOriginalInterval, slotOriginalInterval), 0.325f * Random.Range(-slotOriginalInterval, slotOriginalInterval), 0);
                        var insCollectable = Instantiate(coinPrefab, randomPoisiton, Quaternion.identity).GetComponent<Collectable>();
                        insCollectable.StartBezierMoveToRewardDisplay();
                    }
                };

                UserDataManager.Instance.EarnCoin(pieceConfigSO.allRegisteredPieces[piece.Id].pieceRewardReference.rewardClearCoin);
                piece.ClaimReward();

                allPowerups.Add(piece);
            }
        }

        foreach (var powerup in allPowerups)
        {
            HandlePowerupStandaloneActivate(powerup);
            await UniTask.Delay(TimeSpan.FromSeconds(pieceAnimationSetting.activatePowerupInterval));
        }
    }


    public void HandlePowerupStandaloneActivate(Piece powerup)
    {
        // 跳过被处理的棋子
        if (!powerup.CanUse || powerup.Used || powerup.SelectedToReplace) { return; }

        // 激活的棋子产生的Action加入列表中
        AddAction(powerup.Id switch
        {
            var x when x == Constants.PieceFlyBombId                                    => (powerup as FlyBomb).StandaloneActivate(),
            var x when x == Constants.PieceBombId                                       => (powerup as Bomb).StandaloneActivate(),
            var x when x == Constants.PieceHRocketId || x == Constants.PieceVRocketId   => (powerup as Rocket).StandaloneActivate(),
            var x when x == Constants.PieceRainbowId                                    => (powerup as Rainbow).StandaloneActivate(),
            _                                                                           => null
        });
    }


    /// <summary>
    /// 处理棋子交换
    /// </summary>
    /// <param name="main">主Powerup, 即交换开始的棋子</param>
    /// <param name="minor">次Piece, 即交换结束的棋子</param>
    /// <param name="swapCompletePosition">交换结束的位置</param>
    /// <returns>主Powerup能否触发</returns>
    public bool HandlePowerupSwapActivate(Piece main, Piece minor, GridPosition swapCompletePosition)
    {
        // 主Powerup, 不得为空且必须是Powerup
        if (main == null || !main.CanUse) { return false; }

        // 次Piece为空或不是Powerup
        if (minor == null || !minor.CanUse)
        {
            if (main.Id == Constants.PieceRainbowId)
            {
                // 只有rainbow + basic 才会触发, 其他情况不触发
                if (minor != null && AllowedBasicPieceIds.Any(x => x == minor.Id))
                {
                    AddAction((main as Rainbow).BasicPieceActivate(minor));
                    return true;
                }
                else return false;
            }
            else
            {
                HandlePowerupStandaloneActivate(main);
                return true;
            }
        }

        // 次Piece是Powerup
        if (main.Id == Constants.PieceFlyBombId)
        {
            var flyBomb = main as FlyBomb;
            if (minor.Id == Constants.PieceFlyBombId)
            {
                AddAction(flyBomb.FlyBombActivate(minor as FlyBomb, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceBombId)
            {
                AddAction(flyBomb.BombActivate(minor as Bomb, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceHRocketId || minor.Id == Constants.PieceVRocketId)
            {
                AddAction(flyBomb.RocketActivate(minor as Rocket, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceRainbowId)
            {
                AddAction(flyBomb.RainbowActivate(minor as Rainbow, swapCompletePosition));
            }
            else return false;
        }
        else if (main.Id == Constants.PieceBombId)
        {
            var bomb = main as Bomb;
            if (minor.Id == Constants.PieceFlyBombId)
            {
                AddAction(bomb.FlyBombActivate(minor as FlyBomb, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceBombId)
            {
                AddAction(bomb.BombActivate(minor as Bomb, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceHRocketId || minor.Id == Constants.PieceVRocketId)
            {
                AddAction(bomb.RocketActivate(minor as Rocket, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceRainbowId)
            {
                AddAction(bomb.RainbowActivate(minor as Rainbow, swapCompletePosition));
            }
            else return false;
        }
        else if (main.Id == Constants.PieceHRocketId || main.Id == Constants.PieceVRocketId)
        {
            var rocket = main as Rocket;
            if (minor.Id == Constants.PieceFlyBombId)
            {
                AddAction(rocket.FlyBombActivate(minor as FlyBomb, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceBombId)
            {
                AddAction(rocket.BombActivate(minor as Bomb, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceHRocketId || minor.Id == Constants.PieceVRocketId)
            {
                AddAction(rocket.RocketActivate(minor as Rocket, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceRainbowId)
            {
                AddAction(rocket.RainbowActivate(minor as Rainbow, swapCompletePosition));
            }
            else return false;
        }
        else if (main.Id == Constants.PieceRainbowId)
        {
            // 如果Rainbow不在交换完成的位置上(被移动的棋子), 那么需要交换两个棋子的位置
            if (!main.GridPosition.Equals(swapCompletePosition))
            {
                GridPosition mainPos = main.GridPosition, minorPos = minor.GridPosition;
                main.GridPosition = minorPos;
                slotGrid[minorPos].piece = main;
                minor.GridPosition = mainPos;
                slotGrid[mainPos].piece = minor;
            }

            var rainbow = main as Rainbow;
            if (minor.Id == Constants.PieceFlyBombId)
            {
                AddAction(rainbow.FlyBombActivate(minor as FlyBomb, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceBombId)
            {
                AddAction(rainbow.BombActivate(minor as Bomb, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceHRocketId || minor.Id == Constants.PieceVRocketId)
            {
                AddAction(rainbow.RocketActivate(minor as Rocket, swapCompletePosition));
            }
            else if (minor.Id == Constants.PieceRainbowId)
            {
                AddAction(rainbow.RainbowActivate(minor as Rainbow, swapCompletePosition));
            }
            else return false;
        }
        return true;
    }



    /// <summary>
    /// 炸弹爆炸, 以pivot为中心点
    /// </summary>
    public void BombExplode(GridPosition pivot, int diameter) => DamageArea(new RectInt(pivot.X - diameter / 2, pivot.Y - diameter / 2, diameter, diameter));


    /// <summary>
    /// 清除全屏
    /// </summary>
    public void DoubleRainbowExplode() => DamageArea(new RectInt(0, 0, xMax, yMax));


    public Piece FindFlyBombTargetPiece(Vector3 flyBombActionWorldPosition, int carryPowerupId)
    {
        Piece targetPiece;
        if (carryPowerupId == Constants.PieceBombId ||
            carryPowerupId == Constants.PieceHRocketId ||
            carryPowerupId == Constants.PieceVRocketId)
        {
            // 携带bomb
            // 携带HRocket
            // 携带VRocket
            selectMostStrategy.SetSelectType(carryPowerupId switch 
            { 
                var x when x == Constants.PieceBombId => SelectMost.SelectType.Square,
                var x when x == Constants.PieceHRocketId => SelectMost.SelectType.Row,
                var x when x == Constants.PieceVRocketId => SelectMost.SelectType.Column,
                _ => SelectMost.SelectType.Undefined
            });
            targetPiece = selectMostStrategy.SelectTarget(flyBombActionWorldPosition);
        }
        else targetPiece = selectRandomStrategy.SelectTarget(flyBombActionWorldPosition);

        return targetPiece;
    }


    public void PlayFlyBombLaunchAt(GridPosition explodePosition, AnimationReferenceAsset launchAnimation)
    {
        explodeVFXPool.PlayExplodeVFXAt(explodePosition, launchAnimation);
    }


    /// <summary>
    /// 飞弹起飞十字爆炸
    /// </summary>
    public void FlyBombLaunchDamageAt(GridPosition explodePosition, bool damageAdjacentSlot)
    {
        var damagePositions = new List<GridPosition> { explodePosition };
        if (damageAdjacentSlot)
        {
            damagePositions.Add(explodePosition + GridPosition.Up);
            damagePositions.Add(explodePosition + GridPosition.Right);
            damagePositions.Add(explodePosition + GridPosition.Down);
            damagePositions.Add(explodePosition + GridPosition.Left);
        }

        Damage sourceDamage = new();
        damagePositions.ForEach(pos => sourceDamage.AddToDamagePositions(pos));
        damagePositions.ForEach(pos => DamageSlot(sourceDamage, pos));
    }


    /// <summary>
    /// flyBomb落地产生爆炸或触发携带的powerup效果
    /// </summary>
    public void FlyBombLandExplode(Vector3 landWorldPosition, int carryPowerupId, AnimationReferenceAsset explodeAnimation)
    {
        var landPosition = GetGridPositionByWorldPosition(landWorldPosition);

        if (carryPowerupId == Constants.PieceBombId)
        {
            CreateBombExplodeAt(landPosition, explodeAnimation);
        }
        else if (carryPowerupId == Constants.PieceHRocketId)
        {
            CreateRocketLaunchAt(landPosition, false);
        }
        else if (carryPowerupId == Constants.PieceVRocketId)
        {
            CreateRocketLaunchAt(landPosition, true);
        }
        else
        {
            CreateFlyBombLandExplodeAt(landPosition, explodeAnimation);
        }
    }


    /// <summary>
    /// flybomb落地触发的炸弹爆炸效果
    /// </summary>
    private void CreateBombExplodeAt(GridPosition explodePosition, AnimationReferenceAsset bombExplodeAnimation)
    {
        // 从对象池中获取一个动画对象并播放
        explodeVFXPool.PlayExplodeVFXAt(explodePosition, bombExplodeAnimation);

        // 立即爆炸
        DamageArea(new RectInt(explodePosition.X - 2, explodePosition.Y - 2, 5, 5));
    }


    /// <summary>
    /// flybomb落地触发的火箭发射效果
    /// </summary>
    private void CreateRocketLaunchAt(GridPosition launchPosition, bool vertical)
    {
        var targetPosition = new List<GridPosition>();
        var addPosition = vertical ? new GridPosition(launchPosition.X, 0) : new GridPosition(0, launchPosition.Y);
        while (GridMath.IsPositionOnGrid(slotGrid, addPosition, out var addSlot))
        {
            if (addSlot.IsActive)
            {
                slotGrid[addPosition].IncreaseEnterLock(1);
                targetPosition.Add(addPosition);
            }
            addPosition += vertical ? GridPosition.Down : GridPosition.Right;
        }

        var insAction = Instantiate(rocketVFXPrefab, GetGridPositionWorldPosition(launchPosition), vertical ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity);
        insAction.Initialize(launchPosition, targetPosition, vertical, null);
        AddAction(insAction);
    }


    /// <summary>
    /// flybomb落地触发的爆炸效果
    /// </summary>
    private void CreateFlyBombLandExplodeAt(GridPosition landPosition, AnimationReferenceAsset landExplodeAnimation)
    {
        // 从对象池中获取一个动画对象并播放
        explodeVFXPool.PlayExplodeVFXAt(landPosition, landExplodeAnimation);

        Damage sourceDamage = new Damage();
        sourceDamage.AddToDamagePositions(landPosition);
        DamageSlot(sourceDamage, landPosition);
    }


    /// <summary>
    /// 获取棋盘上最多的空闲的基础棋子的id, 如果返回值为0则表示未能找到任何基础棋子
    /// </summary>
    public int GetMostFreeBasicPieceId()
    {
        int res = 0;
        Dictionary<int, int> dic = new();

        foreach (var slot in slotGrid)
        {
            // 忽略Inactive的棋子
            if (slot.IsActive == false)
                continue;

            // 忽略顶层被覆盖的棋子
            if (slot.upperPiece != null)
                continue;

            // 忽略有锁定的槽位
            if (slot.HasMoveConstrain)
                continue;

            // 寻找棋子
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            // 找到棋子且是非Rainbow选中的相同id的基础棋子
            if (piece != null && AllowedBasicPieceIds.Contains(piece.Id))
            {
                if (!dic.TryAdd(piece.Id, 1))
                    dic[piece.Id]++;

                // 找到最多的棋子
                if (res == 0)
                    res = piece.Id;
                else if (dic[piece.Id] > dic[res])
                    res = piece.Id;
            }
        }
        return res;
    }


    /// <summary>
    /// 获取棋盘上最多的空闲的基础棋子的数量
    /// </summary>
    public int GetMostFreeBasicPieceCount()
    {
        Dictionary<int, int> dic = new();

        foreach (var slot in slotGrid)
        {
            // 忽略Inactive的棋子
            if (slot.IsActive == false)
                continue;

            // 忽略顶层被覆盖的棋子
            if (slot.upperPiece != null)
                continue;

            // 忽略有锁定的槽位
            if (slot.HasMoveConstrain)
                continue;

            // 寻找棋子
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            // 找到棋子且是非Rainbow选中的相同id的基础棋子
            if (piece != null && AllowedBasicPieceIds.Contains(piece.Id))
            {
                if (!dic.TryAdd(piece.Id, 1))
                    dic[piece.Id]++;
            }
        }

        return dic.Count > 0 ? dic.Values.Max() : 0;
    }


    /// <summary>
    /// 获取棋盘上全部基础棋子的种类数量
    /// </summary>
    public int GetGameBoardBasicPieceTypecCount()
    {
        HashSet<int> set = new();
        foreach (var slot in slotGrid)
        {
            // 忽略Inactive的棋子
            if (slot.IsActive == false)
                continue;

            // 忽略顶层被覆盖的棋子
            if (slot.upperPiece != null)
                continue;

            // 寻找棋子
            var piece = slot switch
            {
                var x when x.piece != null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            // 找到棋子且是基础棋子
            if (piece != null && Constants.BasicPieceIds.Contains(piece.Id))
            {
                set.Add(piece.Id);
            }
        }

        return set.Count;
    }


    /// <summary>
    /// 获取棋盘上该pieceId的空闲棋子数量
    /// </summary>
    public int GetGameBoardFreePieceCountByPieceId(int pieceId)
    {
        var count = 0;
        foreach (var slot in slotGrid)
        {
            // 忽略Inactive的棋子
            if (slot.IsActive == false)
                continue;

            // 忽略顶层被覆盖的棋子
            if (slot.upperPiece != null)
                continue;

            // 忽略有锁定的槽位
            if (slot.HasMoveConstrain)
                continue;

            // 寻找棋子
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            if (piece != null && piece.Id == pieceId)
                count++;
        }

        return count;
    }


    public Dictionary<int, int> GetGameBoardBasicPieceCountDic()
    {
        var res = new Dictionary<int, int>();
        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive)
                continue;

            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CanUse => x.piece,
                var x when x.incomingPiece != null && x.incomingPiece.CanUse => x.incomingPiece,
                _ => null
            };

            if (piece != null &&
                Constants.BasicPieceIds.Contains(piece.Id))
            {
                if (!res.TryAdd(piece.Id, 1))
                    res[piece.Id]++;
            }
        }

        return res;
    }


    /// <summary>
    /// 获取棋盘上Powerup的数量
    /// </summary>
    public Dictionary<int, int> GetGameBoardPowerupCountDic(bool ignoreUsed = true)
    {
        var res = new Dictionary<int, int>();
        foreach (var slot in slotGrid)
        {
            // 忽略Inactive的棋子
            if (slot.IsActive == false)
                continue;

            // 忽略顶层被覆盖的棋子
            if (slot.upperPiece != null)
                continue;

            // 寻找棋子
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CanUse => x.piece,
                var x when x.incomingPiece != null && x.incomingPiece.CanUse => x.incomingPiece,
                _ => null
            };

            // 找到了棋子 && (在忽略使用过的Powerup过滤下 && 未被使用过)
            if (piece != null &&
                ignoreUsed && piece.Used == false)
            {
                if (!res.TryAdd(piece.Id, 1))
                    res[piece.Id]++;
            }
        }
        return res;
    }


    public Dictionary<int, int> GetGameBoardUnclaimedPowerupCountDic()
    {
        var res = new Dictionary<int, int>();
        foreach (var slot in slotGrid)
        {
            // 忽略Inactive的棋子
            if (slot.IsActive == false)
                continue;

            // 忽略顶层被覆盖的棋子
            if (slot.upperPiece != null)
                continue;

            // 寻找棋子
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CanUse => x.piece,
                var x when x.incomingPiece != null && x.incomingPiece.CanUse => x.incomingPiece,
                _ => null
            };

            // 找到了棋子 && (在忽略使用过的Powerup过滤下 && 未被使用过)
            if (piece != null &&
                !piece.ClaimedReward && piece.Used == false)
            {
                if (!res.TryAdd(piece.Id, 1))
                    res[piece.Id]++;
            }
        }
        return res;
    }


    public Slot GetRandomSlotToReplace()
    {
        var allSlots = new List<Slot>();
        foreach (var slot in slotGrid)
        {
            // 忽略Inactive的棋子
            if (slot.IsActive == false)
                continue;

            // 忽略顶层被覆盖的棋子
            if (slot.upperPiece != null)
                continue;

            // 寻找棋子
            var piece = slot switch
            {
                var x when x.piece != null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            if (piece != null &&
                AllowedBasicPieceIds.Contains(piece.Id) &&
                piece.SelectedToReplace == false)
            {
                allSlots.Add(slot);
            }
        }

        if (allSlots.Count <= 0)
            return null;

        var randomIndex = Random.Range(0, allSlots.Count);
        allSlots[randomIndex].piece.OnRewardSelect();
        return allSlots[randomIndex];
    }


    /// <summary>
    /// 当前棋盘上的目标棋子能否被任何一种Prop单次激活的情况下完全清除
    /// </summary>
    /// <returns></returns>
    public bool CanTargetBeCompletedWithOneProp()
    {
        var targetList = GameManager.LevelTarget.GetRemainTargetPiecesId();
        List<GridPosition> levelTargets = new();

        foreach (var slot in slotGrid)
        {
            var topPiece = slot.GetTopPiece();
            if (topPiece != null &&
                targetList.Contains(topPiece.Id))
            {
                if (topPiece.ClearNum <= 1)
                {
                    levelTargets.Add(slot.GridPosition);
                }
                else return false;
            }
        }

        return IsPositionsVertical(levelTargets) || IsPositionsHorizontal(levelTargets);
    }


    private bool IsPositionsVertical(List<GridPosition> positions)
    {
        if (positions.Count <= 1)
            return true;

        int x = positions.FirstOrDefault().X;
        foreach (var position in positions)
        {
            if (x != position.X)
                return false;
        }
        return true;
    }


    private bool IsPositionsHorizontal(List<GridPosition> positions)
    {
        if (positions.Count <= 1)
            return true;

        int y = positions.FirstOrDefault().Y;
        foreach (var position in positions)
        {
            if (y != position.Y)
                return false;
        }
        return true;
    }


    /// <summary>
    /// 当选择场上最多的基础棋子时加入Rainbow选择列表
    /// </summary>
    public void OnSelectMostBasicPieceId(int selectPieceId)
    {
        // 当选择的棋子无效, 直接返回
        if (selectPieceId == 0) { return; }

        RainbowSelectBasicPieceIds.Add(selectPieceId);
    }


    /// <summary>
    /// 当Rainbow完成后释放被本Rainbow选择的基础棋子
    /// </summary>
    public void ReleaseSelectMostBasicPieceId(int selectPieceId)
    {
        if (selectPieceId == 0) { return; }

        RainbowSelectBasicPieceIds.Remove(selectPieceId);
    }


    private const float straightDistance = 1.000f;      // 两槽位直线距离
    private const float diagonalDistance = 1.414f;      // 两槽位对角线距离
    /// <summary>
    /// 更新全部槽位的垂直方向上的掉落口
    /// </summary>
    private void FindAllSlotsVerticalSpawner()
    {
        // 首先只检查垂直方向
        var failList = new List<Slot>();      // 未能找到垂直方向的槽位列表
        foreach (var checkPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            slotGrid[checkPosition].Spawner = null;     // 对于任何槽位都先进行清空操作
            bool findDropPort = false;
            var curPosition = checkPosition;
            while (GridMath.IsPositionOnGrid(slotGrid, curPosition, out var curSlot))
            {
                // 槽位包含不可移动的棋子, 失败
                if (curSlot.HasUnMovablePiece)
                {
                    break;
                }

                // 找到掉落口, 成功
                if (curSlot.IsSpawner)
                {
                    findDropPort = true;
                    slotGrid[checkPosition].Spawner = curSlot;
                    break;
                }

                curPosition += GridPosition.Up;
            }

            if (!findDropPort)
            {
                failList.Add(slotGrid[checkPosition]);
            }
        }
    }


    /// <summary>
    /// Get vertical path(worldposition) to destionation slot
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private Vector3[] ConvertPathToWorldPosition(GridPosition[] path)
    {
        var res = new Vector3[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            res[i] = GetGridPositionWorldPosition(path[i]);
        }
        return res;
    }





    #region Utility Functions
    /// <summary>
    /// Is the point down position on the gameboard and the slot is active?
    /// </summary>
    /// <param name="worldPointerPosition">point down world position</param>
    /// <param name="gridPosition">point down grid position</param>
    public bool IsPointerOnBoard(Vector3 worldPointerPosition, out GridPosition gridPosition)
    {
        gridPosition = GetGridPositionByWorldPosition(worldPointerPosition);
        return GridMath.IsPositionOnGrid(gridPosition, xMax, yMax) && slotGrid[gridPosition].IsActive;
    }


    /// <summary>
    /// 将世界坐标转化为棋盘坐标
    /// </summary>
    public GridPosition GetGridPositionByWorldPosition(Vector3 worldPointerPosition)
    {
        var x = (worldPointerPosition - gridOriginalPosition).x / slotOriginalInterval;
        var y = (worldPointerPosition - gridOriginalPosition).y / slotOriginalInterval;

        return new GridPosition(Convert.ToInt32(x), Convert.ToInt32(-y));
    }


    public Vector3 GetGridPositionWorldPosition(GridPosition gridPosition) => GetGridPositionWorldPosition(gridPosition.X, gridPosition.Y);


    private Vector3 GetGridPositionWorldPosition(int x, int y) =>
        new (gridOriginalPosition.x + x * slotOriginalInterval, gridOriginalPosition.y - y * slotOriginalInterval);



    /// <summary>
    /// 槽位是否响应点击交互
    /// </summary>
    public bool IsSlotSelectable(GridPosition gridPosition) => slotGrid[gridPosition].CanReceiveSelect;


    /// <summary>
    /// 槽位是否响应交换交互
    /// </summary>
    public bool IsSlotSwappable(GridPosition gridPosition) => slotGrid[gridPosition].CanReceiveSwap && 
                                                              !IsSlotSwapping(gridPosition) && 
                                                              GameManager.CurrentProp == UsingProp.None;


    /// <summary>
    /// 槽位是否正在交换棋子中
    /// </summary>
    public bool IsSlotSwapping(GridPosition gridPosition)
    {
        foreach (var kvp in SwappingPositions)
        {
            if (kvp.Key.Item1.Equals(gridPosition) || kvp.Key.Item2.Equals(gridPosition))
            {
                return true;
            }
        }
        return false;
    }


    public bool IsSlotSame(GridPosition gridPosition_1, GridPosition gridPosition_2) => gridPosition_1.Equals(gridPosition_2);


    public bool IsSlotAdjacent(GridPosition gridPosition_1, GridPosition gridPosition_2)
    {
        var isAdjacentPosition = gridPosition_2.Equals(gridPosition_1 + GridPosition.Up) ||
                                 gridPosition_2.Equals(gridPosition_1 + GridPosition.Right) ||
                                 gridPosition_2.Equals(gridPosition_1 + GridPosition.Down) ||
                                 gridPosition_2.Equals(gridPosition_1 + GridPosition.Left);
        return isAdjacentPosition;
    }


    public bool IsSlotDiagonal(GridPosition gridPosition_1, GridPosition gridPosition_2)
    {
        var isSidePosition = gridPosition_2.Equals(gridPosition_1 + GridPosition.Up) ||
                             gridPosition_2.Equals(gridPosition_1 + GridPosition.Right) ||
                             gridPosition_2.Equals(gridPosition_1 + GridPosition.Down) ||
                             gridPosition_2.Equals(gridPosition_1 + GridPosition.Left);
        return isSidePosition == false;
    }


    public bool AdjacentSlotsDonotIncludePowerups(GridPosition gridPosition)
    {
        foreach (var adjacent in GetAdjacentSlots(slotGrid, gridPosition, (0, false, false)))
        {
            if (adjacent.upperPiece != null ||
                adjacent.IsEmpty)
                continue;

            var topPiece = adjacent.GetTopPiece();
            if (topPiece.CanUse)
                return false;
        }
        return true;
    }


    public IEnumerable<GridPosition> GetVerticalAdjacentGridPositions(GridPosition currentPosition, GridPosition direction)
    {
        if (!direction.Equals(GridPosition.Up) && !direction.Equals(GridPosition.Down))
        {
            throw new ArgumentException("Cannot accept non-vertical directions");
        }

        bool up = direction.Equals(GridPosition.Up);
        for (int i = 0; i < 3; i++)
        {
            var lookupDirection = i switch
            {
                0 => currentPosition + (up ? GridPosition.Up : GridPosition.Down),
                1 => currentPosition + (up ? GridPosition.UpRight : GridPosition.DownLeft),
                2 => currentPosition + (up ? GridPosition.UpLeft : GridPosition.DownRight),
                _ => currentPosition
            };

            if (GridMath.IsPositionOnGrid(slotGrid, lookupDirection))
            {
                yield return lookupDirection;
            }
        }
    }

    
    /// <summary>
    /// 获取全部相邻的Active槽位, 不包含对角的槽位
    /// 如果matchId == 0: 不考虑id是否相等返回全部, 否则只会返回id相等的槽位
    /// 如果checkPiece == true, 检测的是slot的piece, 在matchId == 0 的情况下不适用
    /// 如果checkIncoming == true, 检测的是slot的incomingPiece, 在 matchId == 0 的情况下不适用
    /// 如果某一位置被upperPiece覆盖, 那么该位置也会被跳过
    /// </summary>
    public IEnumerable<Slot> GetAdjacentSlots(Grid fromSlotGrid, GridPosition currentPosition, (int matchId, bool checkPiece, bool checkIncoming) checkArg)
    {
        foreach (var lookupDirection in adjacentLookupDirections)
        {
            var position = currentPosition + lookupDirection;
            if (GridMath.IsPositionOnBoard(fromSlotGrid, position, out var slot))
            {
                if (checkArg.matchId == 0)
                {
                    yield return slot;
                }
                else
                {
                    // 跳过被UpperPiece覆盖的棋子
                    if (slot.upperPiece != null)
                    {
                        continue;
                    }

                    if (checkArg.checkPiece && slot.piece != null && slot.piece.Id == checkArg.matchId)
                    {
                        yield return slot;
                    }
                    else if (checkArg.checkIncoming && slot.incomingPiece != null && slot.incomingPiece.Id == checkArg.matchId)
                    {
                        yield return slot;
                    }
                }
            }
        }
    }
    #endregion


    public enum SwapStage
    {
        Forward,
        Revert
    }
}