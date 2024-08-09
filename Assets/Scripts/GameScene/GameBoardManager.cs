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
    public Grid slotGrid;                               // ��ǰ��������
    private int xMax;
    private int yMax;
    private bool boardInitialized;                      // ���̳�ʼ����ʶ��
    private Vector3 gridOriginalPosition;               // ��������ԭ��(0,0)λ��

    private PieceAnimationSetting pieceAnimationSetting;
    private PowerupSetting powerupSetting;


    // Pre-defined
    public readonly SelectRandom selectRandomStrategy = new();                      // ���ѡ��Ŀ�����ӷ���(FlyBomb)
    public readonly SelectMost selectMostStrategy = new();                          // ѡ������Ŀ�����ӷ���(FlyBomb)
    public readonly List<int> matchCheckOrder = new() { 4, 1, 2, 3, 0, -1 };        // ���Powerup�����ȼ�����, �±��ӦPowerupSetting.registeredPowerups�е�λ��
    public readonly List<int> possibleMatchCheckOrder = new() { 4, 1, 2, 0 };       // �����ܺϳ�Powerup�����ȼ�����, �±��ӦPowerupSetting.registeredPowerups�е�λ��, ����������Ҫ�������
    private readonly List<List<GridPosition>> basicMatchPositions = new()           // ����������ȫ�����λ�ÿ�����
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


    public HashSet<int> RainbowSelectBasicPieceIds { get; private set; } = new();   // ��Rainbowѡ��Ļ�������Id
    public HashSet<int> AllowedBasicPieceIds { get; private set; } = new();         // �޳���Rainbowѡ��Ļ�������Id������Match�Ļ�������Id


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


    private List<IGameBoardAction> ActivatedActions { get; set; } = new();  // �����е�powerup��prop�¼�


    private Queue<GridPosition> QueuedClickPositions { get; set; } = new();                                 // ����ĵ��λ��
    private Queue<(GridPosition, GridPosition)> QueuedSwapPositions { get; set; } = new();                  // ����Ľ���λ��
    private Dictionary<(GridPosition, GridPosition), SwapStage> SwappingPositions { get; set; } = new();    // ���ڽ�����λ��


    private List<GridPosition> TickingPositions { get; set; } = new();      // �ƶ�����λ��
    private List<GridPosition> NewTickingPositions { get; set; } = new();   // �������ƶ�����λ��
    private List<GridPosition> EmptyPositions { get; set; } = new();        // �����ӵĿ���λ��
    private HashSet<GridPosition> NewEmptyPositions { get; set; } = new();  // �����Ŀ���λ��
    private HashSet<Piece> CompleteMovePieces { get; set; } = new();        // �˶���Ԥ��λ�õ�����


    private HashSet<GridPosition> PositionsToCheckMatch { get; set; } = new();                  // ���м�������λ��
    private Dictionary<GridPosition, float> DelayedPostionsToCheckMatch { get; set; } = new();  // ��Ϊ��λ�õ��µı��Ƴٵ�λ��
    private HashSet<Match> MatchesToExecute { get; set; } = new();                              // ������Match
    private List<UniTask> ExecutingMatchAndDamageTasks { get; set; } = new();

    private PriorityQueue<PossibleSwap, int> PossibleSwaps { get; set; } = new();


    // Callbacks
    public Dictionary<GridPosition, Action<Damage>> PositionBottomDamagedCallbacks { get; private set; } = new();           // Damage�м��������ӻص�
    public Dictionary<GridPosition, Action<Damage>> PositionAdjacentDamagedCallbacks { get; private set; } = new();         // Damage���ڻ������ӻص�
    public Dictionary<GridPosition, Action<Damage>> PositionEnterCollectCallbacks { get; private set; } = new();            // ����λ���ռ����ӻص�

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

        // ��ʼ�������
        piecePool.InitializePool(GameManager.LevelSpawnerRule, onReleasePieceRecordCallback);
        damageVFXPool.InitializePool();
        explodeVFXPool.InitializePool();

        // ����SubUpdate
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
    /// �������������Ŀ����������
    /// </summary>
    public void SetGridAndTargetGrid(GameLevel gameLevel)
    {
        if (gameLevel == null || gameLevel.slotInfo == null || gameLevel.pieceInfo == null)
        {
            throw new System.ArgumentNullException("Game level config is invalid");
        }

        // �½�����, ����ʼ��
        xMax = gameLevel.xMax;
        yMax = gameLevel.yMax;
        slotOriginalInterval = floorConfigSO.spritePixels / 100f;
        gridOriginalPosition = new Vector3(slotOriginalInterval * (xMax - 1) / -2, slotOriginalInterval * (yMax - 1) / 2, 0) + transform.position; 

        slotGrid = new Grid();
        slotGrid.SetGrid(new Slot[yMax, xMax]);

        // ��ʼ�����̲�λ
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

                // ���ɵذ�
                var floor = Instantiate(floorConfigSO.floorPrefab, insWorldPosition, Quaternion.identity, slot.transform);
                floor.InitializeFloor(floorSprite, 1f, insPosition);
                floor.SpriteMask.sprite = slotDropPort ? floorSprite : null; // ֻ�����ɿڲ���Ҫ��������
                slot.floor = floor;


                // ���ɵײ�����
                int bottomPieceId = bottomInfo[2];
                if (bottomPieceId != 0)
                {
                    int bottomPieceClearNum = bottomInfo[3];
                    var bottomPieceRootGridPosition = new GridPosition(bottomInfo[4], bottomInfo[5]);
                    var bottomPiece = PlacePieceAt(insPosition, bottomPieceRootGridPosition, bottomPieceId, bottomPieceClearNum, PieceColors.Colorless);
                    slot.bottomPiece = bottomPiece;
                }


                // �����м�����
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


                // ���ɶ�������
                int upperPieceId = upperInfo[2];
                if (upperPieceId != 0)
                {
                    int upperPieceClearNum = upperInfo[3];
                    var upperPieceRootGridPosition = new GridPosition(upperInfo[4], upperInfo[5]);
                }
            }

            slotGrid[insPosition] = slot;
        }

        // �����߿�
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

        FindAllSlotsVerticalSpawner();      // ��ʼ��ÿ����λ�Ķ�Ӧ�Ĵ�ֱ�����
        UpdateAllSlotsFillType();           // ����ÿ����λ��Ӧ���������

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
            // ������Ϸ�������߼�ѭ��
            // ����Ҫ�����powerup�¼����и���
            if (ActivatedActions.Count > 0)
            {
                DoTickActions();

                GameBoardInactivity = false;
            }

            // �����û�����
            if (QueuedClickPositions.Count > 0 || QueuedSwapPositions.Count > 0)
            {
                DoHandleInput();

                GameBoardInactivity = false;
            }

            // ���²���
            UpdateAllSlotsFillType();       // �������ӵ�FillType
            UpdateAllowedBasicPieceIds();   // ��������Match�Ļ�������Id

            // ����Ҫ����λ���ƶ���Ӧ������
            if (TickingPositions.Count > 0)
            {
                DoMovePieces();

                GameBoardInactivity = false;
            }

            // �Կ�λ�ó��Խ������
            if (EmptyPositions.Count > 0)
            {
                DoEmptyCheck();

                GameBoardInactivity = false;
            }

            // ���ƶ������û�к����ƶ������ӽ���ֹͣ
            if (CompleteMovePieces.Count > 0)
            {
                DoStopPieces();

                GameBoardInactivity = false;
            }

            // ���ƶ��������λ�ü���Ƿ����Match
            if (PositionsToCheckMatch.Count > 0)
            {
                DoMatchCheck();

                GameBoardInactivity = false;
            }

            // �Լ�⵽��Match���д���
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

            // ����֡���������ƶ�������λ�ü����б�, ��һ֡�ƶ�
            if (NewTickingPositions.Count > 0)
            {
                TickingPositions.AddRange(NewTickingPositions);
                NewTickingPositions.Clear();

                GameBoardInactivity = false;
            }

            // ������Ծ����һ��ʱ��������ʾ(��������)
            if (GameBoardInactivity)
            {
                InactivityDuration += Time.deltaTime;

                if (gameBoardHintEntity.HintOn &&
                    InactivityDuration >= gameBoardHintEntity.InactivityBeforeHint)
                {
                    // ���ȫ�����ܵĺϳ�(���δ���������)
                    if (PossibleSwaps.Count <= 0)
                    {
                        FindAllPossibleMatch();
                    }


                    if (PossibleSwaps.Count <= 0 && !IsRearranging)
                    {
                        // TODO: fix this when there's no match
                        // δ�ҵ��κ������ƥ��
                        HandleAutoShuffle();
                    }
                    else if (PossibleSwaps.Count > 0 && HintCoolDown <= 0)
                    {
                        // Hint��ȴ��������Hint
                        DoHint();
                    }

                    // ��ʼHint��ȴ(��ǰ����Hint��)
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
            // ��Ϸ�������߼�ѭ��
            InactivityDuration = 0f;
            HintCoolDown = 0f;
            gameBoardHintEntity.HintOn = false;

            // ����Ҫ�����powerup�¼����и���
            if (ActivatedActions.Count > 0)
            {
                DoTickActions();

                GameBoardInactivity = false;
            }

            // ���²���
            UpdateAllSlotsFillType();       // �������ӵ�FillType
            UpdateAllowedBasicPieceIds();   // ��������Match�Ļ�������Id

            // ����Ҫ����λ���ƶ���Ӧ������
            if (TickingPositions.Count > 0)
            {
                DoMovePieces();

                GameBoardInactivity = false;
            }

            // �Կ�λ�ó��Խ������
            if (EmptyPositions.Count > 0)
            {
                DoEmptyCheck(true);

                GameBoardInactivity = false;
            }

            // ���ƶ������û�к����ƶ������ӽ���ֹͣ
            if (CompleteMovePieces.Count > 0)
            {
                DoStopPieces();

                GameBoardInactivity = false;
            }

            // ���ƶ��������λ�ü���Ƿ����Match
            if (PositionsToCheckMatch.Count > 0)
            {
                DoMatchCheck();

                GameBoardInactivity = false;
            }

            // �Լ�⵽��Match���д���(�ڱ�ѭ�������ɵ�Powerup�ڳ�ʼ����ɺ����������)
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

            // ����֡���������ƶ�������λ�ü����б�, ��һ֡�ƶ�
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
            // �Ƴ���Actions����ɵ�Actions
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
    /// ���²�λ���������(б�����: 1 or �������: -1 or ��ֱ���: 0)
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

                    // �ҵ�����Ĵ�ֱ�����ϵĻ�Ծ�Ĳ�λ
                    var upPosition = position + GridPosition.Up;
                    var upRightPosition = position + GridPosition.UpRight;
                    var upLeftPosition = position + GridPosition.UpLeft;
                    if (GridMath.IsPositionOnGrid(slotGrid, upPosition) && 
                        slotGrid[upPosition].IsActive == false)
                    {
                        // ���Ϸ�һ��λ���ϵĲ�λ���ڵ��ǲ���Ծ, ��ô��Ҫ�ҵ�����Ĵ�ֱ�����ϵĻ�Ծ��λ
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
    /// ��������Match������Id
    /// </summary>
    private void UpdateAllowedBasicPieceIds()
    {
        AllowedBasicPieceIds.Clear();
        AllowedBasicPieceIds.UnionWith(Constants.BasicPieceIds.Where(x => RainbowSelectBasicPieceIds.Contains(x) == false));
    }


    /// <summary>
    /// ����TickingPositions�е�λ���ƶ���Ӧ������, �������Ҳ����������Bounce�Ķ���
    /// </summary>
    private void DoMovePieces()
    {
        // �����µ����Ͻ�������
        TickingPositions = TickingPositions.Distinct().ToList();    // TODO: ����TickingPositions�����ظ���
        TickingPositions.Sort((a, b) =>
        {
            int yCmp = b.Y.CompareTo(a.Y);
            if (yCmp == 0)
            {
                return a.X.CompareTo(b.X);
            }
            return yCmp;
        });


        // ��ȫ����λ�ý��е���
        var deltaTime = Time.deltaTime;
        for (int i = 0; i < TickingPositions.Count; i++)
        {
            var currentPosition = TickingPositions[i];
            var currentSlot = slotGrid[currentPosition];
            var currentPostitionWorldPosition = GetGridPositionWorldPosition(currentPosition);

            // ���һ��λ���Ѿ�������, ���������������ƶ�, �׳�����
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

                // ��������λ��
                piece.TickMove(deltaTime);
                var maxDistance = pieceAnimationSetting.pieceMoveSpeedCurve.Evaluate(piece.MoveTime) * Time.deltaTime * pieceAnimationSetting.pieceMoveSpeed;
                piece.Transform.position = Vector3.MoveTowards(piece.GetWorldPosition(), currentPostitionWorldPosition, maxDistance);

                var distance = Vector3.Distance(piece.GetWorldPosition(), currentPostitionWorldPosition);
                if (distance <= approximateDistance)
                {
                    // ����С�ڽ�����ֵ����Ϊ���ӵ��ﱾ���ƶ��յ�
                    // ����������ɵ�����, ����Ҫ�ر�����
                    if (currentSlot.IsSpawner && piece.EnteredBoard == false)
                    {
                        piece.EnteredBoard = true;
                        piece.SkeletonAnimation.maskInteraction = SpriteMaskInteraction.None;
                    }

                    piece.ReachGridPosition(currentPosition);   // ����: ������һ������λ��
                    currentSlot.incomingPiece = null;
                    currentSlot.piece = piece;
                    CompleteMovePieces.Add(piece);
                }
                else if (distance <= slotOriginalInterval / 2 &&
                         piece.GridPosition.Equals(currentPosition) == false)
                {
                    // ����С�ڸ��ӵİ뾶�������ӵ�ǰλ�ú͸���λ�ò���
                    // ��ô��Ϊ���ӵ������������
                    piece.ReachGridPosition(currentPosition);
                }
            }
            else if (currentSlot.piece?.CurrentState == State.Bouncing)
            {
                // ����˶������ӽ��е�����������
                var piece = currentSlot.piece;
                piece.TickBounce(deltaTime);

                float maxTime = pieceAnimationSetting.pieceBouncePositionCurve
                    .keys[pieceAnimationSetting.pieceBouncePositionCurve.length - 1].time;

                if (piece.MoveTime >= maxTime)
                {
                    // ��ɵ�������
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
                    // ���е�������
                    piece.Transform.position = currentPostitionWorldPosition + Vector3.up * pieceAnimationSetting.pieceBouncePositionCurve.Evaluate(piece.MoveTime);
                    piece.Transform.localScale = new Vector3(pieceAnimationSetting.pieceBounceSquishXCurve.Evaluate(piece.MoveTime), pieceAnimationSetting.pieceBounceSquishYCurve.Evaluate(piece.MoveTime), 1);
                }
            }
        }
    }


    private void DoMatchCheck()
    {
        // �Ƴ�λ����û������ || �����˶��е�����
        PositionsToCheckMatch.RemoveWhere(x => slotGrid[x].piece == null || slotGrid[x].piece.MovingToSlot != null);

        var readyToCheckPositions = new List<GridPosition>();   // ��Χû�и�����ͬ������incoming��λ��
        foreach (var gridPosition in PositionsToCheckMatch)
        {
            // û�и������������뵱ǰλ�õ�����id��ͬ��ʱ���ٽ��м��
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

                // ��ǰλ�����ڵİ�����ͬid���ӵĲ�λ������ͬid��incoming����id�Ĳ�λ����
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

        // �Է��ϵ�λ�ý��м������
        if (readyToCheckPositions.Count > 0)
        {
            foreach (var index in matchCheckOrder)
            {
                var removeList = new List<GridPosition>();
                readyToCheckPositions.ForEach(pos =>
                {
                    // ��ÿ��λ�ü���Ƿ���Ժϳɵ�ǰindexָ����powerup
                    if (CheckMatchAt(pos, index, true))
                    {
                        // ���ڳɹ����õ���λ��, ����Ҫ�ٽ��е����ȼ��ļ��, ��˼����Ƴ��б�, �ڱ�����ɺ��Ƴ�
                        removeList.Add(pos);
                    }
                });

                // �Ƴ��ɹ�ƥ�䵽��״��λ��
                readyToCheckPositions = readyToCheckPositions.Except(removeList).ToList();
            }

            // �Ƴ����й�����λ��
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
    /// ����EmptyPositions�е�λ��, Ѱ�Һ��ʵ����ӽ������;
    /// �������ֻ���𷢳�����, �����ƶ���DoMovePieces()����
    /// </summary>
    private void DoEmptyCheck(bool ignoreSpawnerRule = false)
    {
        // �Կ�λ�ý�������, б�����(FillType == 1)��λ�÷����������(FillType == 0)֮����г������
        EmptyPositions = EmptyPositions
            .Where(x => slotGrid[x].FillType != -1)
            .OrderBy(x => slotGrid[x].FillType)       
            .ToList();

        for (int i = 0; i < EmptyPositions.Count; i++)
        {
            var emptyPosition = EmptyPositions[i];
            var emptySlot = slotGrid[emptyPosition];

            // �Ƴ������ǿյ�λ��, �Ƴ�û������������λ��
            if (!emptySlot.IsEmpty)
            {
                EmptyPositions.RemoveAt(i);
                i--;
                continue;
            }

            // �������������ӽ����λ��
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


            if (findDroppableActiveUpSlot)                   // �ɹ��ҵ���Ծ��λ�ڴ�ֱ�Ϸ����ܹ��ṩ���ӵĲ�λ
            {
                if (upSlot.piece == null)           // �Ϸ���λ������
                {
                    if (emptySlot.FillType == 1)    // �ÿ�λ���Ƿ񱻱��Ϊб��
                    {
                        if (GridMath.IsPositionOnBoard(slotGrid, upRightPosition, out var upRightSlot) &&
                            upRightSlot.FillType != -1) // ���Ϸ�λ���л�Ծ��λ && ���Ϸ�λ�ò�λ�ܱ��������
                        {
                            if (upRightSlot.CanLeave == false || upRightSlot.piece == null || isRightSlotEmptyAndFillable)
                            {
                                // ���Ϸ���λ�����������뿪 || ���Ϸ���λ������ || �ҷ���λ���Ա�����ҵ�ǰΪ��
                                // �ȴ�
                                continue;
                            }

                            if (ExceedMoveInterval(upRightSlot.piece, emptyPosition) == false)
                            {
                                // ���Ϸ���λ���Ӳ����㷢��
                                continue;
                            }

                            var fillPiece = upRightSlot.piece;
                            upRightSlot.piece = null;
                            emptySlot.incomingPiece = fillPiece;

                            fillPiece.StartMove(emptySlot);                         // ��������, Ŀ��Ϊ���ղ�λ
                            upRightSlot.LastFireTime = Time.time;                   // ��¼��λ��󷢳�ʱ��

                            if (!NewTickingPositions.Contains(emptyPosition))
                                NewTickingPositions.Add(emptyPosition);             // �������ƶ����Ŀ�λ���뵽��Ҫ�ƶ�����λ���б���
                            CompleteMovePieces.Remove(fillPiece);                   // ���ƶ��������Ƴ�����ƶ���ϣ��

                            if (!EmptyPositions.Contains(upRightPosition))
                                EmptyPositions.Add(upRightPosition);                // ���������ӵ�λ�ñ��Ϊ��
                            if (EmptyPositions.Contains(emptyPosition))
                                EmptyPositions.Remove(emptyPosition);               // �ÿ�λ�ò���Ϊ��, �Ƴ���λ���б�

                            i--;

                            UpdateAllSlotsFillType();                               // �ƶ����Ӻ���Ҫ�������
                        }
                        else if (GridMath.IsPositionOnBoard(slotGrid, upLeftPosition, out var upLeftSlot) &&
                            upLeftSlot.FillType != -1)  // ���Ϸ�λ���л�Ծ��λ && ���Ϸ�λ�ò�λ�ܱ��������
                        {
                            if (upLeftSlot.CanLeave == false || upLeftSlot.piece == null || isLeftSlotEmptyAndFillable)
                            {
                                // ���Ϸ���λ�����������뿪 || ���Ϸ���λ������ || ��λ�ÿ��Ա�����ҵ�ǰΪ��
                                // �ȴ�
                                continue;
                            }

                            if (ExceedMoveInterval(upLeftSlot.piece, emptyPosition) == false)
                            {
                                // ���Ϸ���λ���Ӳ����㷢��
                                continue;
                            }

                            var fillPiece = upLeftSlot.piece;
                            upLeftSlot.piece = null;
                            emptySlot.incomingPiece = fillPiece;

                            fillPiece.StartMove(emptySlot);                         // ��������, Ŀ��Ϊ���ղ�λ
                            upLeftSlot.LastFireTime = Time.time;                    // ��¼��λ��󷢳�ʱ��

                            if (!NewTickingPositions.Contains(emptyPosition))
                                NewTickingPositions.Add(emptyPosition);             // �������ƶ����Ŀ�λ���뵽��Ҫ�ƶ�����λ���б���
                            CompleteMovePieces.Remove(fillPiece);                   // ���ƶ��������Ƴ�����ƶ���ϣ��

                            if (!EmptyPositions.Contains(upLeftPosition))
                                EmptyPositions.Add(upLeftPosition);                 // ���������ӵ�λ�ñ��Ϊ��
                            if (EmptyPositions.Contains(emptyPosition))
                                EmptyPositions.Remove(emptyPosition);               // �ÿ�λ�ò���Ϊ��, �Ƴ���λ���б�
                            
                            i--;

                            UpdateAllSlotsFillType();                               // �ƶ����Ӻ���Ҫ�������
                        }
                        else Debug.LogWarning($"Unhandled situation");
                    }
                    else continue;  // �ȴ�(��ֱ��������ӵ�λ)
                }
                else
                {
                    // �Ϸ���λ��������
                    if (upSlot.CanLeave == false)
                    {
                        continue;
                    }

                    if (ExceedMoveInterval(upSlot.piece, emptyPosition) == false)
                    {
                        // �Ϸ���λ���Ӳ����㷢��
                        continue;
                    }

                    var fillPiece = upSlot.piece;
                    upSlot.piece = null;
                    emptySlot.incomingPiece = fillPiece;

                    fillPiece.StartMove(emptySlot);                         // ��������, Ŀ��Ϊ���ղ�λ
                    upSlot.LastFireTime = Time.time;                        // ��¼��λ��󷢳�ʱ��

                    if (!NewTickingPositions.Contains(emptyPosition))
                        NewTickingPositions.Add(emptyPosition);             // �������ƶ����Ŀ�λ���뵽��Ҫ�ƶ�����λ���б���
                    CompleteMovePieces.Remove(fillPiece);                   // ���ƶ��������Ƴ�����ƶ���ϣ��

                    if (!EmptyPositions.Contains(upPosition))
                        EmptyPositions.Add(upPosition);                     // ���������ӵ�λ�ñ��Ϊ��
                    if (EmptyPositions.Contains(emptyPosition))
                        EmptyPositions.Remove(emptyPosition);               // �ÿ�λ�ò���Ϊ��, �Ƴ���λ���б�
                    
                    i--;

                    UpdateAllSlotsFillType();                               // �ƶ����Ӻ���Ҫ�������
                }
            }
            else 
            {
                // δ���ҵ���Ծ��λ���Ϸ��Ĳ�λ
                if (emptySlot.IsSpawner)
                {
                    if (ExceedMoveInterval(null, emptyPosition) == false)
                    {
                        // ��������㷢���������ӳټ�������
                        continue;
                    }

                    var fillPiece = ActivateSpawnerAt(emptyPosition, ignoreSpawnerRule);    // ��������
                    emptySlot.incomingPiece = fillPiece;

                    fillPiece.StartMove(emptySlot);                         // ��������, Ŀ��Ϊ�������
                    emptySlot.LastSpawnTime = Time.time;                    // ��¼��λ�������ʱ��

                    if (!NewTickingPositions.Contains(emptyPosition))
                        NewTickingPositions.Add(emptyPosition);             // �������ƶ����Ŀ�λ���뵽��Ҫ�ƶ�����λ���б���

                    if (EmptyPositions.Contains(emptyPosition))
                        EmptyPositions.Remove(emptyPosition);               // �ÿ�λ�ò���Ϊ��, �Ƴ���λ���б�
                    
                    i--;

                    UpdateAllSlotsFillType();                               // �ƶ����Ӻ���Ҫ�������
                }
                else if (emptySlot.FillType == 1)
                {
                    if (GridMath.IsPositionOnBoard(slotGrid, upRightPosition, out var upRightSlot) &&
                        upRightSlot.FillType != -1) // ���Ϸ�λ���л�Ծ��λ && ���Ϸ�λ�ò�λ�ܱ��������
                    {
                        if (upRightSlot.CanLeave == false || upRightSlot.piece == null || isRightSlotEmptyAndFillable)
                        {
                            // ���Ϸ���λ�����������뿪 || ���Ϸ���λ������ || �ҷ���λ�ɱ�����ҵ�ǰΪ��
                            // �ȴ�
                            continue;
                        }

                        if (ExceedMoveInterval(upRightSlot.piece, emptyPosition) == false)
                        {
                            // ���Ϸ���λ���Ӳ����㷢��
                            continue;
                        }

                        var fillPiece = upRightSlot.piece;
                        upRightSlot.piece = null;
                        emptySlot.incomingPiece = fillPiece;

                        fillPiece.StartMove(emptySlot);                         // ��������, Ŀ��Ϊ���ղ�λ
                        upRightSlot.LastFireTime = Time.time;                   // ��¼��λ��󷢳�ʱ��


                        if (!NewTickingPositions.Contains(emptyPosition))
                            NewTickingPositions.Add(emptyPosition);             // �������ƶ����Ŀ�λ���뵽��Ҫ�ƶ�����λ���б���
                        CompleteMovePieces.Remove(fillPiece);                   // ���ƶ��������Ƴ�����ƶ���ϣ��

                        if (!EmptyPositions.Contains(upRightPosition))
                            EmptyPositions.Add(upRightPosition);                // ���������ӵ�λ�ñ��Ϊ��
                        if (EmptyPositions.Contains(emptyPosition))
                            EmptyPositions.Remove(emptyPosition);               // �ÿ�λ�ò���Ϊ��, �Ƴ���λ���б�
                        i--;
                        
                        UpdateAllSlotsFillType();                               // �ƶ����Ӻ���Ҫ�������
                    }
                    else if (GridMath.IsPositionOnBoard(slotGrid, upLeftPosition, out var upLeftSlot) &&
                             upLeftSlot.FillType != -1)     // ���Ϸ�λ���л�Ծ��λ && ���Ϸ�λ�ò�λ�ܱ��������
                    {
                        if (upLeftSlot.CanLeave == false || upLeftSlot.piece == null || isLeftSlotEmptyAndFillable)
                        {
                            // ���Ϸ���λ�����������뿪 || ���Ϸ���λ������ || �󷽲�λ�ɱ�����ҵ�ǰΪ��
                            // �ȴ�
                            continue;
                        }

                        if (ExceedMoveInterval(upLeftSlot.piece, emptyPosition) == false)
                        {
                            // ���Ϸ���λ���Ӳ����㷢��
                            continue;
                        }

                        var fillPiece = upLeftSlot.piece;
                        upLeftSlot.piece = null;
                        emptySlot.incomingPiece = fillPiece;

                        fillPiece.StartMove(emptySlot);                         // ��������, Ŀ��Ϊ���ղ�λ
                        upLeftSlot.LastFireTime = Time.time;                    // ��¼��λ��󷢳�ʱ��

                        if (!NewTickingPositions.Contains(emptyPosition))
                            NewTickingPositions.Add(emptyPosition);             // �������ƶ����Ŀ�λ���뵽��Ҫ�ƶ�����λ���б���
                        CompleteMovePieces.Remove(fillPiece);                   // ���ƶ��������Ƴ�����ƶ���ϣ��

                        if (!EmptyPositions.Contains(upLeftPosition))
                            EmptyPositions.Add(upLeftPosition);                 // ���������ӵ�λ�ñ��Ϊ��
                        if (EmptyPositions.Contains(emptyPosition))
                            EmptyPositions.Remove(emptyPosition);               // �ÿ�λ�ò���Ϊ��, �Ƴ���λ���б�
                        i--;

                        UpdateAllSlotsFillType();                               // �ƶ����Ӻ���Ҫ�������
                    }
                }
            }
        }
    }


    /// <summary>
    /// ��鵱ǰʱ�������Ƿ񳬳���
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

        // �������ƶ������б�
        CompleteMovePieces.Clear();
    }


    /// <summary>
    /// ����ʼλ�ÿ�ʼ���ƥ��, �����������ȫ��������ʼλ�����ӵ�����, ��������״ѡ������
    /// </summary>
    /// <param name="startPosition">������ʼλ��</param>
    /// <param name="checkPowerupIndex">����powerup���±�, �� == -1 �����������</param>
    /// <param name="fromCascading">true: ���Լ������, false: ������ҽ���</param>
    /// <param name="createMatch">true: ��Ⲣ����Match����ִ��, false: ������������Match����ִ��</param>
    private bool CheckMatchAt(GridPosition startPosition, int checkPowerupIndex, bool fromCascading = false, bool createMatch = true)
    {
        // ������λ��
        if (!GridMath.IsPositionOnBoard(slotGrid, startPosition, out var centerSlot) ||
            centerSlot.piece == null)
        {
            return false;
        }

        // �����Ѿ�����������match || ���Ƿ�Rainbowѡ��Ļ������ӵ�����
        var centerPiece = centerSlot.piece;
        if (centerPiece.CanUse ||
            AllowedBasicPieceIds.Contains(centerPiece.Id) == false ||
            centerPiece.CurrentMatch != null)
        {
            return false;
        }

        // ʹ��BFS���ҵ�����������ʮ�����ӵİ�����ͬ���ӵĲ�λ
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


        // �����������������ӵ���ͬ���ӵ�λ���ж��ܹ��ϳɸ�powerup
        List<GridPosition> matchedShapePositions = new();
        List<GridPosition> lineList = new();

        MatchShape matchedShape = null;
        Powerup matchedPowerup = null;
        GridPosition matchedCenterPosition = startPosition;

        if (checkPowerupIndex != -1)
        {
            // ���ϳ�
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
            // ������3��
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


        // δ���ҵ��κ�ƥ���powerup��3��������ɵ�һ��/��
        if (matchedShapePositions.Count <= 0 && lineList.Count <= 0)
        {
            return false;
        }


        // ����Match
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

        // ������λ��
        if (!GridMath.IsPositionOnBoard(checkSlotGrid, startPosition, out var centerSlot) ||
            centerSlot.piece == null)
        {
            hintAnimation = null;
            animationWorldPosition = Vector3.zero;
            animationRotation = 0;
            matchedShapePositions.Clear();
            return false;
        }

        // �����Ѿ�����������match || ���Ƿ�Rainbowѡ��Ļ������ӵ�����
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

        // ʹ��BFS���ҵ�����������ʮ�����ӵİ�����ͬ���ӵĲ�λ
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

        // �����������������ӵ���ͬ���ӵ�λ���ж��ܹ��ϳɸ�powerup
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
            // ������3��
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

        // δ���ҵ��κ�ƥ���powerup��3��������ɵ�һ��/��
        if (matchedShapePositions.Count <= 0 && lineList.Count <= 0)
        {
            hintAnimation = null;
            animationWorldPosition = Vector3.zero;
            animationRotation = 0;
            matchedShapePositions.Clear();
            return false;
        }

        // ����Hint���ɲ���
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
            // ��ͨ3������Ҫ����
            matchedShapePositions = lineList;
            hintAnimation = null;
        }

        // ���㶯������λ��(��������)
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
    /// ������һ��Match, �ϳ�powerup���������
    /// </summary>
    private async UniTask ExecuteMatch(Match match, bool isReward = false)
    {
        // �쳣���
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

        // �ڿ�ʼִ������ǰ, ֹͣ���ӵ���
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
        // �����ص�
        Damage sourceDamage = new();
        match.MatchingPositions.ForEach(position => sourceDamage.AddToDamagePositions(position));

        match.MatchingPositions.ForEach(pos =>
        {
            // ��������Damage�ص�
            if (PositionAdjacentDamagedCallbacks.TryGetValue(pos, out var adjacentDamageCallback))
            {
                adjacentDamageCallback?.Invoke(sourceDamage);
                turnScore += 10;
            }

            // Ҫ����BottomDamage�ص�
            if (PositionBottomDamagedCallbacks.TryGetValue(pos, out var centerPieceDamageCallback))
            {
                centerPieceDamageCallback?.Invoke(sourceDamage);
                turnScore += 10;
            }
        });
        // �÷�
        GameManager.instance.AddScore(turnScore, true);

        // ��ʼ����Match�Ķ���
        if (match.SpawnedPowerup == null)
        {
            // Match: Damage
            // ������λ�ϵĻ�������, �����Ŷ�Ч, �����λ����

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
    /// Ѱ�����п��ܵĺϳ����
    /// </summary>
    private void FindAllPossibleMatch()
    {
        // ��ʼ��һ���ȼ�
        // ��һ���ȼ�: ���ȼ����ϵ��ߵ����
        foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            // ��������Swap��λ��
            if (IsSlotSwappable(gridPosition) == false)
                continue;

            // ������Powerup��λ��
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

            // ��һ���ȼ�: ��齻�������ɵ��ߵ����
            foreach (var gridPosition in (IEnumerable<GridPosition>)copiedSlotGrid)
            {
                // ��������Swap��λ��
                if (IsSlotSwappable(gridPosition) == false)
                    continue;

                var slot = copiedSlotGrid[gridPosition];
                var upPosition = gridPosition + GridPosition.Up;
                var rightPosition = gridPosition + GridPosition.Right;

                if (GridMath.IsPositionOnBoard(copiedSlotGrid, upPosition, out var upSlot) &&
                    IsSlotSwappable(upPosition))
                {
                    // ��ʱ����˫��λ��, ���ڼ��
                    (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);

                    foreach (var index in possibleMatchCheckOrder)
                    {
                        // �ϳɵ�Powerup��Id
                        var synthesizePieceId = index switch
                        {
                            0 => Constants.PieceFlyBombId,
                            1 => Constants.PieceBombId,
                            2 => Constants.PieceHRocketId,
                            4 => Constants.PieceRainbowId,
                            _ => 0
                        };

                        // ����
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

                        // ����
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

                    // ��ԭ����
                    (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
                }


                if (GridMath.IsPositionOnBoard(copiedSlotGrid, rightPosition, out var rightSlot) &&
                    IsSlotSwappable(rightPosition))
                {
                    // ��ʱ����˫��λ��, ���ڼ��
                    (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);

                    foreach (var index in possibleMatchCheckOrder)
                    {
                        // �ϳɵ�Powerup��Id
                        var synthesizePieceId = index switch
                        {
                            0 => Constants.PieceFlyBombId,
                            1 => Constants.PieceBombId,
                            2 => Constants.PieceHRocketId,
                            4 => Constants.PieceRainbowId,
                            _ => 0
                        };

                        // ����
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

                        // ����
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

                    // ��ԭ����
                    (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
                }
            }

            // ��ʱTop Priority���Ѽ�����
            if (PossibleSwaps.Count > 0)
                return;

            // ��ʼ�ڶ����ȼ�
            // �ڶ����ȼ�: ������3��
            int basicMatchPriority = -3;
            foreach (var gridPosition in (IEnumerable<GridPosition>)copiedSlotGrid)
            {
                // ��������Swap��λ��
                if (IsSlotSwappable(gridPosition) == false)
                    continue;

                var slot = copiedSlotGrid[gridPosition];
                var upPosition = gridPosition + GridPosition.Up;
                var rightPosition = gridPosition + GridPosition.Right;

                if (GridMath.IsPositionOnBoard(copiedSlotGrid, upPosition, out var upSlot) &&
                    IsSlotSwappable(upPosition))
                {
                    // ��ʱ����˫��λ��, ���ڼ��
                    (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);


                    // ����
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

                    // ����
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


                    // ��ԭ����
                    (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
                }


                if (GridMath.IsPositionOnBoard(copiedSlotGrid, rightPosition, out var rightSlot) &&
                    IsSlotSwappable(rightPosition))
                {
                    // ��ʱ����˫��λ��, ���ڼ��
                    (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);

                    // ����
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

                    // ����
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

                    // ��ԭ����
                    (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
                }
            }

            // ��ʱ�ڶ����ȼ��Ѽ�����
            if (PossibleSwaps.Count > 0)
            {
                return;
            }
        }

        // ��ʼ�������ȼ�
        // �������ȼ�: Rainbow + ��������
        foreach (var gridPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            // ��������Swap��λ��
            if (IsSlotSwappable(gridPosition) == false)
                continue;

            // ������Rainbow��λ��
            var slot = slotGrid[gridPosition];
            if (slot.piece == null || slot.piece.Id != Constants.PieceRainbowId)
                continue;

            var upPosition = gridPosition + GridPosition.Up;
            var rightPosition = gridPosition + GridPosition.Right;
            var downPosition = gridPosition + GridPosition.Down;
            var leftPosition = gridPosition + GridPosition.Left;

            // ����
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

            // ����
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

            // ����
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

            // ����
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
    /// ����ڸ���SlotGrid������Ƿ���ڿ��ܵ�Match���
    /// </summary>
    public bool CanFindPossibleMatch(Grid newSlotGrid)
    {
        foreach (var gridPosition in (IEnumerable<GridPosition>)newSlotGrid)
        {
            // ��������Swap��λ��
            if (IsSlotSwappable(gridPosition) == false)
                continue;

            var slot = newSlotGrid[gridPosition];
            var upPosition = gridPosition + GridPosition.Up;
            var rightPosition = gridPosition + GridPosition.Right;

            if (GridMath.IsPositionOnBoard(newSlotGrid, upPosition, out var upSlot) &&
                IsSlotSwappable(upPosition))
            {
                // ��ʱ����˫��λ��, ���ڼ��
                (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);

                foreach (var index in matchCheckOrder)
                {
                    // ����
                    {
                        if (CheckMatchAt(newSlotGrid, upPosition, index,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
                            return true;
                        }
                    }

                    // ����
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

                // ��ԭ����
                (slot.piece, upSlot.piece) = (upSlot.piece, slot.piece);
            }


            if (GridMath.IsPositionOnBoard(newSlotGrid, rightPosition, out var rightSlot) &&
                IsSlotSwappable(rightPosition))
            {
                // ��ʱ����˫��λ��, ���ڼ��
                (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);

                foreach (var index in matchCheckOrder)
                {
                    // ����
                    {
                        if (CheckMatchAt(newSlotGrid, rightPosition, index,
                                         out var hintAnimation, out var animationWorldPosition, out var animationRotation,
                                         out var matchedShapePositions))
                        {
                            (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
                            return true;
                        }
                    }

                    // ����
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

                // ��ԭ����
                (slot.piece, rightSlot.piece) = (rightSlot.piece, slot.piece);
            }
        }

        return false;
    }


    /// <summary>
    /// ������ʾ
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
        FindAllSlotsVerticalSpawner();  // ���µ����
        UpdateAllSlotsFillType();       // ���µ�������
        foreach (var slot in slotGrid)  // �����п�λ�ü����б�, �������
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
    /// ���Action
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
    /// ��λ���������λ���б���, ��һ֡������м����ƶ�
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
    /// �ݴ�Useλ��
    /// </summary>
    public void QueueClick(GridPosition usePosition)
    {
        QueuedClickPositions.Enqueue(usePosition);
    }


    /// <summary>
    /// �ݴ�Swapλ��
    /// </summary>
    public void QueueSwap(GridPosition fromPosition, GridPosition toPosition)
    {
        QueuedSwapPositions.Enqueue((fromPosition, toPosition));
    }


    /// <summary>
    /// �������ڵ�λ�ý���һ���˺�
    /// </summary>
    /// <param name="bound">����Χ</param>
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

        // ʵ�ʼ��ʱ��Ҫ+1�ĵײ���Χ�Ϳ��, ��Ҫ���Ǳ߽����(e.g. �ײ��߽�λ���ϵ������Ѿ���ʼ�˶�, ��ʱͨ�����λ���ϵĲ�λ�޷�׷�ٵ�, ��Ҫ�ڵײ�λ��������һ�е�incomingPiece׷�ٵ�)
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

                // ����BottomDamage�ص�
                if ((pieceType == 1 || pieceType == 2) &&
                    curPiece != null &&
                    Constants.BasicPieceIds.Contains(curPiece.Id) &&
                    PositionBottomDamagedCallbacks.TryGetValue(curPosition, out var callback))
                {
                    callback?.Invoke(sourceDamage);
                    turnScore += 10;
                }

                // �������Ϸ�������, ֱ�ӷ���
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

        // �÷�
        GameManager.instance.AddScore(turnScore, false);
    }





    /// <summary>
    /// Prop��Powerup��λ�ý���һ���˺�(�����������ϲ������1��ClearNum), ��������λ�õ��˺��ص�
    /// </summary>
    /// <param name="gridPosition">�˺�λ��</param>
    public void DamageSlot(Damage sourceDamage, GridPosition gridPosition)
    {
        // �����ܳ������̷�Χ��damage
        if (!GridMath.IsPositionOnGrid(slotGrid, gridPosition, out var damageSlot) ||
            IsSlotSwapping(gridPosition)) 
        { 
            return; 
        }

        if (sourceDamage.IgnorePositions.Contains(gridPosition))
        {
            return;
        }

        // Ѱ����Ҫ����������
        (Piece damagePiece, int pieceType) = damageSlot switch
        {
            var x when x.upperPiece != null => (x.upperPiece, 0),
            var x when x.piece != null => (x.piece, 1),
            var x when x.incomingPiece != null => (x.incomingPiece, 2),
            var x when x.bottomPiece != null => (x.bottomPiece, 3),
            _ => (null, -1)
        };

        // ���ε÷�
        int turnScore = 0;

        // ֻ���м���л������ӲŴ���BottomDamage�ص�
        if (damagePiece != null &&
            (pieceType == 1 || pieceType == 2) &&
            Constants.BasicPieceIds.Contains(damagePiece.Id) &&
            PositionBottomDamagedCallbacks.TryGetValue(gridPosition, out var callback))
        {
            callback?.Invoke(sourceDamage);
            turnScore += 10;
        }

        // �������Ϸ�������, ֱ�ӷ���
        if (damagePiece == null || pieceType == -1 ||           // δ���ҵ�����
            (damagePiece.MovingToSlot != null && !damagePiece.GridPosition.Equals(gridPosition)) ||   // �˶��е����ӻ�δ���뱾λ����
            damagePiece.CurrentMatch != null ||                 // ���Ӱ�����Match��
            damagePiece.CurrentState == State.Disposed ||       // �����Ѿ����ͷ�
            damagePiece.SelectedToReplace == true ||            // ���ӱ�Rainbowѡ��
            damagePiece.EnteredBoard == false)                  // ���ӻ�δ�������
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
    /// ����RainbowAciton������, �ͻ������ӽ�������������
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


        // ����AdjacentDamage�ص�
        if (PositionAdjacentDamagedCallbacks.TryGetValue(position, out var adjacentCallback))
        {
            adjacentCallback?.Invoke(sourceDamage);
            turnScore += 10;
        }

        // ����BottomDamage�ص�
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
    /// ֱ����������, ���Ქ��������Ч����Ч, Ҳ��Ҫ�����ռ�Ŀ������
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
    /// ֱ���滻��λ�ϵ�����, Ҳ��Ҫ�����ռ�Ŀ������(����ǵĻ�)
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
    /// ����δ��ʼ��ʱ��������
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
    /// ֱ��������������һ������
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
    /// ��������, ����һ�����Ӳ��ƶ��������, ʹ������������ɵ����ӻᰴ�����õĵ�������������
    /// </summary>
    private Piece ActivateSpawnerAt(GridPosition spawnerPosition, bool ignoreSpawnerRule)
    {
        var insGridPosition = spawnerPosition + GridPosition.Up;

        // �������õ����ɹ��������ɿڳ��������Id
        int pieceId = ignoreSpawnerRule ? GetPieceIdToAvoidMatch(spawnerPosition) : GameManager.instance.GetPieceIdFromSpawner(spawnerPosition);
        var piece = piecePool.NewPieceAt(GetGridPositionWorldPosition(insGridPosition), pieceId, false, insGridPosition);

        // ��������������������(��powerupʱ), ��Ҫ��鵱ǰ���ϵ���������
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

            // ����������Ϊ���ص�Ŀ������ && �����б������������� >= ʣ�����������ʱ
            // ��Ҫ��������ʹ�ñ����Ե����ɿ�
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
    /// TODO: ���ɵ�������Ҫ�����ܱ���Match
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
    /// �����ӱ���ȫ�ݻ�ʱ���Ը���LevelTarget
    /// </summary>
    /// <param name="pieceId">���ӵ�Id</param>
    /// <param name="collectId">�����ռ���Id</param>
    /// <param name="collectPrefab">�ռ���Prefab</param>
    /// <returns>�����ܷ��ռ��ҵ�ǰ����Ŀ��Ϊ�����</returns>
    private bool TryUpdateLevelTargetPiece(int pieceId, out int collectId, out GameObject collectPrefab)
    {
        if (pieceConfigSO.allRegisteredPieces.TryGetValue(pieceId, out var registeredPiece) == false)
            throw new InvalidOperationException("Unknown piece");

        collectId = registeredPiece.pieceTargetReference.collectId;
        if (collectId != 0 &&
            GameManager.LevelTarget.TargetDic.TryGetValue(collectId, out var leftCount) &&
            leftCount > 0)
        {
            // �ɱ��ռ� && ��������Ŀ�� && ��ǰĿ��δ���
            // ����Ŀ������ʣ������, �����յ�ǰ����Ŀ���������ж����/ʧ����Ϸ
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
    /// �����ӱ���ȫ����ʱ�����ռ�
    /// </summary>
    private async void TryCollectLevelTargetPiece(int collectId, GameObject collectPrefab, Vector3 startPosition, float delayTime = 0f)
    {
        if (delayTime > 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delayTime));
        }

        if (collectPrefab != null)
        {
            // ����Ҫ���е�����ʵ��, �ڻص��и���
            var insCollectableObj = Instantiate(collectPrefab, startPosition, Quaternion.identity);
            var insCollectable = insCollectableObj.GetComponent<Collectable>();
            insCollectable.StartBezierMoveToLevelTargetDisplay();
        }
        else
        {
            // ֱ�Ӹ���
            MainGameUIManager.Instance.UpdateLevelTargetDisplay(collectId);
        }

    }


    /// <summary>
    /// ���ڴ�������λ���ӻ�ʹ�ô���λ���ϵ�powerup
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

                        // ���Powerup���Ĳ���
                        GameManager.instance.ConsumeMove();
                    }
                }
                else piece.PlayClickAnimation();
            }
        }
    }


    /// <summary>
    /// ���ڴ�����ʹ��Prop������µ�������ϵ�����
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
                // ��ť����
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
    /// ���ڴ�����������λ����
    /// </summary>
    private async void SwapPiece(GridPosition fromPosition, GridPosition toPosition)
    {
        Piece pieceFrom = slotGrid[fromPosition].piece;
        Piece pieceTo = slotGrid[toPosition].piece;

        // �ٴ�У��˫������ͬʱΪ��
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

        // ��¼����
        if (SwappingPositions.TryAdd((fromPosition, toPosition), SwapStage.Forward) ||
            SwappingPositions.TryAdd((toPosition, fromPosition), SwapStage.Forward))
        {
            var rotation = fromPosition.X == toPosition.X ? Quaternion.Euler(0, 0, 90f) : Quaternion.identity;
            explodeVFXPool.PlayExplodeVFXAt((GetGridPositionWorldPosition(fromPosition) + GetGridPositionWorldPosition(toPosition)) / 2, rotation, swapVFXAnimation, 10);
        }

        // �жϽ������Ƿ���Ҫ���Ĳ���
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

        // ���н���
        if (pieceFrom != null && pieceTo != null)
        {
            // ����˫������Ϊ��
            Debug.Log($"<color=orange>Swap</color> {fromPosition} and {toPosition}");

            slotGrid[fromPosition].incomingPiece = pieceTo;
            slotGrid[fromPosition].piece = null;
            slotGrid[toPosition].incomingPiece = pieceFrom;
            slotGrid[toPosition].piece = null;

            // �жϽ���˫���������
            bool isPieceFromPowerup = pieceFrom.CanUse, isPieceToPowerup = pieceTo.CanUse;
            int powerupCount = isPieceFromPowerup ? isPieceToPowerup ? 2 : 1 : isPieceToPowerup ? 1 : 0;
            pieceFrom.SortingGroup.sortingOrder = pieceFrom.SortingGroup.sortingOrder <= pieceTo.SortingGroup.sortingOrder ? pieceTo.SortingGroup.sortingOrder + 1 : pieceFrom.SortingGroup.sortingOrder;

            if (powerupCount == 0 || powerupCount == 1)
            {
                // ����˫����ȫ��powerup
                // ִ�н�������
                await DOTween.Sequence()
                    .Join(pieceTo.Transform.DOMove(pieceFrom.GetWorldPosition(), pieceAnimationSetting.pieceSwapDuration))
                    .Join(pieceFrom.Transform.DOMove(pieceTo.GetWorldPosition(), pieceAnimationSetting.pieceSwapDuration))
                    .SetEase(pieceAnimationSetting.pieceSwapEase);

                // ��ɺ󽻻�˫��λ��
                slotGrid[fromPosition].incomingPiece = null;
                slotGrid[fromPosition].piece = pieceTo;
                pieceTo.GridPosition = fromPosition;
                slotGrid[toPosition].incomingPiece = null;
                slotGrid[toPosition].piece = pieceFrom;
                pieceFrom.GridPosition = toPosition;

                // ��⽻��˫�������λ���Ƿ������ƥ��/ʹ��
                if (!isPieceFromPowerup && !isPieceToPowerup)
                {
                    // pieceTo ��ʱ�� fromPositionλ����
                    // ֻ�л������Ӳ���Ҫ���ϳ�
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

                    // pieceFrom ��ʱ�� toPositionλ����
                    // ֻ�л������Ӳ���Ҫ���ϳ�
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
                    // pieceTo ��ʱ�� fromPositionλ����
                    // ֻ�л������Ӳ���Ҫ���ϳ�
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

                    // Powerup����
                    HandlePowerupSwapActivate(pieceFrom, pieceTo, toPosition);
                }
                else if (!isPieceFromPowerup && isPieceToPowerup)
                {
                    // pieceFrom ��ʱ�� toPositionλ����
                    // ֻ�л������Ӳ���Ҫ���ϳ�
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

                    // Powerup����
                    HandlePowerupSwapActivate(pieceTo, pieceFrom, toPosition);
                }

                // ��������򽻻�
                if (SwappingPositions[(fromPosition, toPosition)] == SwapStage.Forward) 
                {
                    if (consumeMove)   
                    {
                        // ������ƥ��, ��ɽ���
                        SwappingPositions.Remove((fromPosition, toPosition));
                        SwappingPositions.Remove((toPosition, fromPosition));
                    }
                    else
                    {
                        // δ�ܲ����κ�ƥ��, �ٽ���һ�ν���
                        // ��ʼ���򽻻�
                        SwappingPositions[(fromPosition, toPosition)] = SwapStage.Revert;
                        SwappingPositions[(toPosition, fromPosition)] = SwapStage.Revert;
                        SwapPiece(toPosition, fromPosition);
                    }
                }
                else
                {
                    // ��ɷ��򽻻���ɾȥ���ν����ļ�¼
                    SwappingPositions.Remove((fromPosition, toPosition));
                    SwappingPositions.Remove((toPosition, fromPosition));
                }
            }
            else
            {
                // ����˫��ȫ�ǵ���
                // From Powerup�ƶ���To Powerup
                await pieceFrom.Transform
                    .DOMove(pieceTo.GetWorldPosition(), pieceAnimationSetting.pieceSwapDuration)
                    .SetEase(pieceAnimationSetting.pieceSwapEase);

                // ������ɺ��λ��
                slotGrid[fromPosition].piece = pieceFrom;
                slotGrid[fromPosition].incomingPiece = null;
                slotGrid[toPosition].piece = pieceTo;
                slotGrid[toPosition].incomingPiece = null;

                HandlePowerupSwapActivate(pieceFrom, pieceTo, toPosition);

                // powerup+powerup����������ɾȥ���ν���
                SwappingPositions.Remove((fromPosition, toPosition));
                SwappingPositions.Remove((toPosition, fromPosition));
            }
        }
        else if (pieceTo != null && pieceFrom == null)
        {
            // ����˫����һ��Ϊ��(pieceFromΪ��)
            Debug.Log($"<color=orange>Swap</color> {fromPosition} and {toPosition}, {fromPosition} is null");

            slotGrid[fromPosition].IncreaseEnterLock(1);
            slotGrid[fromPosition].IncreaseLeaveLock(1);
            slotGrid[fromPosition].incomingPiece = pieceTo;
            slotGrid[fromPosition].piece = null;

            slotGrid[toPosition].IncreaseEnterLock(1);
            slotGrid[toPosition].IncreaseLeaveLock(1);
            slotGrid[toPosition].incomingPiece = null;
            slotGrid[toPosition].piece = null;

            // ��PieceTo�ƶ���fromPosition
            await pieceTo.Transform
                .DOMove(GetGridPositionWorldPosition(fromPosition), pieceAnimationSetting.pieceSwapDuration)
                .SetEase(pieceAnimationSetting.pieceSwapEase);

            // ������ɺ��λ��
            slotGrid[fromPosition].DecreaseEnterLock(1);
            slotGrid[fromPosition].DecreaseLeaveLock(1);
            slotGrid[fromPosition].piece = pieceTo;
            slotGrid[fromPosition].incomingPiece = null;
            pieceTo.GridPosition = fromPosition;

            slotGrid[toPosition].DecreaseEnterLock(1);
            slotGrid[toPosition].DecreaseLeaveLock(1);

            // ���Match��Activate���
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
                    // �����λ��
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

                    // ������ƥ��, ��ɽ���
                    SwappingPositions.Remove((fromPosition, toPosition));
                    SwappingPositions.Remove((toPosition, fromPosition));
                }
                else
                {
                    // δ�ܲ����κ�ƥ��, �ٽ���һ�ν���
                    // ��ʼ���򽻻�
                    SwappingPositions[(fromPosition, toPosition)] = SwapStage.Revert;
                    SwappingPositions[(toPosition, fromPosition)] = SwapStage.Revert;
                    SwapPiece(toPosition, fromPosition);
                }
            }
            else
            {
                // ��ɷ��򽻻���ɾȥ���ν����ļ�¼
                // �����λ��
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
            // ����˫����һ��Ϊ��(pieceToΪ��)
            Debug.Log($"<color=orange>Swap</color> {fromPosition} and {toPosition}, {toPosition} is null");

            slotGrid[fromPosition].IncreaseEnterLock(1);
            slotGrid[fromPosition].IncreaseLeaveLock(1);
            slotGrid[fromPosition].incomingPiece = null;
            slotGrid[fromPosition].piece = null;

            slotGrid[toPosition].IncreaseEnterLock(1);
            slotGrid[toPosition].IncreaseLeaveLock(1);
            slotGrid[toPosition].incomingPiece = pieceFrom;
            slotGrid[toPosition].piece = null;

            // ��PieceFrom�ƶ���toPosition
            await pieceFrom.Transform
                .DOMove(GetGridPositionWorldPosition(toPosition), pieceAnimationSetting.pieceSwapDuration)
                .SetEase(pieceAnimationSetting.pieceSwapEase);

            // ������ɺ��λ��
            slotGrid[fromPosition].DecreaseEnterLock(1);
            slotGrid[fromPosition].DecreaseLeaveLock(1);

            slotGrid[toPosition].DecreaseEnterLock(1);
            slotGrid[toPosition].DecreaseLeaveLock(1);
            slotGrid[toPosition].piece = pieceFrom;
            slotGrid[toPosition].incomingPiece = null;
            pieceFrom.GridPosition = toPosition;

            // ���Match��Activate���
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
                    // �����λ��
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

                    // ������ƥ��, ��ɽ���
                    SwappingPositions.Remove((fromPosition, toPosition));
                    SwappingPositions.Remove((toPosition, fromPosition));
                }
                else
                {
                    // δ�ܲ����κ�ƥ��, �ٽ���һ�ν���
                    // ��ʼ���򽻻�
                    SwappingPositions[(fromPosition, toPosition)] = SwapStage.Revert;
                    SwappingPositions[(toPosition, fromPosition)] = SwapStage.Revert;
                    SwapPiece(toPosition, fromPosition);
                }
            }
            else
            {
                // ��ɷ��򽻻���ɾȥ���ν����ļ�¼
                // �����λ��
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
    /// ����������: no match exsits on gameboard, but at least 1 more possible swap match
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
                    // ��ӻ�������λ��
                    basicPiecePositions.Add(slot.GridPosition);
                    if (!basicPieceColors.TryAdd(piece.Id, 1))
                    {
                        basicPieceColors[piece.Id]++;
                    }
                }
                else if (Constants.PowerupPieceIds.Contains(piece.Id))
                {
                    // ���Powerupλ��
                    powerupPositions.Add(slot.GridPosition);
                }
            }
        }

        bool canRearrange = basicPieceColors.Values.Any(count => count >= 3);
        var random = new System.Random();
        Dictionary<GridPosition, GridPosition> dic = new(); // (startPosition, endPosition)

        // �����ǰ����������������ź�Ҳ�����ܲ�������һ��Match, ����
        if (basicPiecePositions.Count <= 2 || canRearrange == false)
        {
            Debug.LogError("Fail to generate gameboard: not enough piece to create a possible match");
            return;
        }

        // ��ǰ��������������������ź��������һ����һ��Swap�����ɵ�Match, ����ȫ���Ļ�������
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
    /// ����Dice����
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
                    // ��ӻ�������λ��
                    basicPiecePositions.Add(slot.GridPosition);
                    if (!basicPieceColors.TryAdd(piece.Id, 1))
                    {
                        basicPieceColors[piece.Id]++;
                    }
                }
                else if (Constants.PowerupPieceIds.Contains(piece.Id))
                {
                    // ���Powerupλ��
                    powerupPositions.Add(slot.GridPosition);
                }
            }
        }

        bool canRearrange = basicPieceColors.Values.Any(count => count >= 3);
        var random = new System.Random();
        Dictionary<GridPosition, GridPosition> dic = new(); // (startPosition, endPosition)

        if (basicPiecePositions.Count <= 2 || canRearrange == false)
        {
            // �����ǰ����������������ź�Ҳ�����ܲ�������һ��Match, ��ô����ȫ���Ļ������Ӻ�Powerup
            basicPiecePositions.AddRange(powerupPositions);
            basicPiecePositions = basicPiecePositions.OrderBy(x => random.Next()).ToList();
            int left = 0, right = basicPiecePositions.Count - 1;

            // ��β����
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

        // ��ǰ��������������������ź��������һ����һ��Swap�����ɵ�Match, ����ȫ���Ļ�������
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
            // ��������Դ���, �������
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
    /// �����Զ�����
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
                        // ��ӻ�������λ��
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
    /// ˳�򴥷����������е�Powerup
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
        // ���������������
        if (!powerup.CanUse || powerup.Used || powerup.SelectedToReplace) { return; }

        // ��������Ӳ�����Action�����б���
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
    /// �������ӽ���
    /// </summary>
    /// <param name="main">��Powerup, ��������ʼ������</param>
    /// <param name="minor">��Piece, ����������������</param>
    /// <param name="swapCompletePosition">����������λ��</param>
    /// <returns>��Powerup�ܷ񴥷�</returns>
    public bool HandlePowerupSwapActivate(Piece main, Piece minor, GridPosition swapCompletePosition)
    {
        // ��Powerup, ����Ϊ���ұ�����Powerup
        if (main == null || !main.CanUse) { return false; }

        // ��PieceΪ�ջ���Powerup
        if (minor == null || !minor.CanUse)
        {
            if (main.Id == Constants.PieceRainbowId)
            {
                // ֻ��rainbow + basic �Żᴥ��, �������������
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

        // ��Piece��Powerup
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
            // ���Rainbow���ڽ�����ɵ�λ����(���ƶ�������), ��ô��Ҫ�����������ӵ�λ��
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
    /// ը����ը, ��pivotΪ���ĵ�
    /// </summary>
    public void BombExplode(GridPosition pivot, int diameter) => DamageArea(new RectInt(pivot.X - diameter / 2, pivot.Y - diameter / 2, diameter, diameter));


    /// <summary>
    /// ���ȫ��
    /// </summary>
    public void DoubleRainbowExplode() => DamageArea(new RectInt(0, 0, xMax, yMax));


    public Piece FindFlyBombTargetPiece(Vector3 flyBombActionWorldPosition, int carryPowerupId)
    {
        Piece targetPiece;
        if (carryPowerupId == Constants.PieceBombId ||
            carryPowerupId == Constants.PieceHRocketId ||
            carryPowerupId == Constants.PieceVRocketId)
        {
            // Я��bomb
            // Я��HRocket
            // Я��VRocket
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
    /// �ɵ����ʮ�ֱ�ը
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
    /// flyBomb��ز�����ը�򴥷�Я����powerupЧ��
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
    /// flybomb��ش�����ը����ըЧ��
    /// </summary>
    private void CreateBombExplodeAt(GridPosition explodePosition, AnimationReferenceAsset bombExplodeAnimation)
    {
        // �Ӷ�����л�ȡһ���������󲢲���
        explodeVFXPool.PlayExplodeVFXAt(explodePosition, bombExplodeAnimation);

        // ������ը
        DamageArea(new RectInt(explodePosition.X - 2, explodePosition.Y - 2, 5, 5));
    }


    /// <summary>
    /// flybomb��ش����Ļ������Ч��
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
    /// flybomb��ش����ı�ըЧ��
    /// </summary>
    private void CreateFlyBombLandExplodeAt(GridPosition landPosition, AnimationReferenceAsset landExplodeAnimation)
    {
        // �Ӷ�����л�ȡһ���������󲢲���
        explodeVFXPool.PlayExplodeVFXAt(landPosition, landExplodeAnimation);

        Damage sourceDamage = new Damage();
        sourceDamage.AddToDamagePositions(landPosition);
        DamageSlot(sourceDamage, landPosition);
    }


    /// <summary>
    /// ��ȡ���������Ŀ��еĻ������ӵ�id, �������ֵΪ0���ʾδ���ҵ��κλ�������
    /// </summary>
    public int GetMostFreeBasicPieceId()
    {
        int res = 0;
        Dictionary<int, int> dic = new();

        foreach (var slot in slotGrid)
        {
            // ����Inactive������
            if (slot.IsActive == false)
                continue;

            // ���Զ��㱻���ǵ�����
            if (slot.upperPiece != null)
                continue;

            // �����������Ĳ�λ
            if (slot.HasMoveConstrain)
                continue;

            // Ѱ������
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            // �ҵ��������Ƿ�Rainbowѡ�е���ͬid�Ļ�������
            if (piece != null && AllowedBasicPieceIds.Contains(piece.Id))
            {
                if (!dic.TryAdd(piece.Id, 1))
                    dic[piece.Id]++;

                // �ҵ���������
                if (res == 0)
                    res = piece.Id;
                else if (dic[piece.Id] > dic[res])
                    res = piece.Id;
            }
        }
        return res;
    }


    /// <summary>
    /// ��ȡ���������Ŀ��еĻ������ӵ�����
    /// </summary>
    public int GetMostFreeBasicPieceCount()
    {
        Dictionary<int, int> dic = new();

        foreach (var slot in slotGrid)
        {
            // ����Inactive������
            if (slot.IsActive == false)
                continue;

            // ���Զ��㱻���ǵ�����
            if (slot.upperPiece != null)
                continue;

            // �����������Ĳ�λ
            if (slot.HasMoveConstrain)
                continue;

            // Ѱ������
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CurrentMatch == null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            // �ҵ��������Ƿ�Rainbowѡ�е���ͬid�Ļ�������
            if (piece != null && AllowedBasicPieceIds.Contains(piece.Id))
            {
                if (!dic.TryAdd(piece.Id, 1))
                    dic[piece.Id]++;
            }
        }

        return dic.Count > 0 ? dic.Values.Max() : 0;
    }


    /// <summary>
    /// ��ȡ������ȫ���������ӵ���������
    /// </summary>
    public int GetGameBoardBasicPieceTypecCount()
    {
        HashSet<int> set = new();
        foreach (var slot in slotGrid)
        {
            // ����Inactive������
            if (slot.IsActive == false)
                continue;

            // ���Զ��㱻���ǵ�����
            if (slot.upperPiece != null)
                continue;

            // Ѱ������
            var piece = slot switch
            {
                var x when x.piece != null => x.piece,
                var x when x.incomingPiece != null => x.incomingPiece,
                _ => null
            };

            // �ҵ��������ǻ�������
            if (piece != null && Constants.BasicPieceIds.Contains(piece.Id))
            {
                set.Add(piece.Id);
            }
        }

        return set.Count;
    }


    /// <summary>
    /// ��ȡ�����ϸ�pieceId�Ŀ�����������
    /// </summary>
    public int GetGameBoardFreePieceCountByPieceId(int pieceId)
    {
        var count = 0;
        foreach (var slot in slotGrid)
        {
            // ����Inactive������
            if (slot.IsActive == false)
                continue;

            // ���Զ��㱻���ǵ�����
            if (slot.upperPiece != null)
                continue;

            // �����������Ĳ�λ
            if (slot.HasMoveConstrain)
                continue;

            // Ѱ������
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
    /// ��ȡ������Powerup������
    /// </summary>
    public Dictionary<int, int> GetGameBoardPowerupCountDic(bool ignoreUsed = true)
    {
        var res = new Dictionary<int, int>();
        foreach (var slot in slotGrid)
        {
            // ����Inactive������
            if (slot.IsActive == false)
                continue;

            // ���Զ��㱻���ǵ�����
            if (slot.upperPiece != null)
                continue;

            // Ѱ������
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CanUse => x.piece,
                var x when x.incomingPiece != null && x.incomingPiece.CanUse => x.incomingPiece,
                _ => null
            };

            // �ҵ������� && (�ں���ʹ�ù���Powerup������ && δ��ʹ�ù�)
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
            // ����Inactive������
            if (slot.IsActive == false)
                continue;

            // ���Զ��㱻���ǵ�����
            if (slot.upperPiece != null)
                continue;

            // Ѱ������
            var piece = slot switch
            {
                var x when x.piece != null && x.piece.CanUse => x.piece,
                var x when x.incomingPiece != null && x.incomingPiece.CanUse => x.incomingPiece,
                _ => null
            };

            // �ҵ������� && (�ں���ʹ�ù���Powerup������ && δ��ʹ�ù�)
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
            // ����Inactive������
            if (slot.IsActive == false)
                continue;

            // ���Զ��㱻���ǵ�����
            if (slot.upperPiece != null)
                continue;

            // Ѱ������
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
    /// ��ǰ�����ϵ�Ŀ�������ܷ��κ�һ��Prop���μ�����������ȫ���
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
    /// ��ѡ�������Ļ�������ʱ����Rainbowѡ���б�
    /// </summary>
    public void OnSelectMostBasicPieceId(int selectPieceId)
    {
        // ��ѡ���������Ч, ֱ�ӷ���
        if (selectPieceId == 0) { return; }

        RainbowSelectBasicPieceIds.Add(selectPieceId);
    }


    /// <summary>
    /// ��Rainbow��ɺ��ͷű���Rainbowѡ��Ļ�������
    /// </summary>
    public void ReleaseSelectMostBasicPieceId(int selectPieceId)
    {
        if (selectPieceId == 0) { return; }

        RainbowSelectBasicPieceIds.Remove(selectPieceId);
    }


    private const float straightDistance = 1.000f;      // ����λֱ�߾���
    private const float diagonalDistance = 1.414f;      // ����λ�Խ��߾���
    /// <summary>
    /// ����ȫ����λ�Ĵ�ֱ�����ϵĵ����
    /// </summary>
    private void FindAllSlotsVerticalSpawner()
    {
        // ����ֻ��鴹ֱ����
        var failList = new List<Slot>();      // δ���ҵ���ֱ����Ĳ�λ�б�
        foreach (var checkPosition in (IEnumerable<GridPosition>)slotGrid)
        {
            slotGrid[checkPosition].Spawner = null;     // �����κβ�λ���Ƚ�����ղ���
            bool findDropPort = false;
            var curPosition = checkPosition;
            while (GridMath.IsPositionOnGrid(slotGrid, curPosition, out var curSlot))
            {
                // ��λ���������ƶ�������, ʧ��
                if (curSlot.HasUnMovablePiece)
                {
                    break;
                }

                // �ҵ������, �ɹ�
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
    /// ����������ת��Ϊ��������
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
    /// ��λ�Ƿ���Ӧ�������
    /// </summary>
    public bool IsSlotSelectable(GridPosition gridPosition) => slotGrid[gridPosition].CanReceiveSelect;


    /// <summary>
    /// ��λ�Ƿ���Ӧ��������
    /// </summary>
    public bool IsSlotSwappable(GridPosition gridPosition) => slotGrid[gridPosition].CanReceiveSwap && 
                                                              !IsSlotSwapping(gridPosition) && 
                                                              GameManager.CurrentProp == UsingProp.None;


    /// <summary>
    /// ��λ�Ƿ����ڽ���������
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
    /// ��ȡȫ�����ڵ�Active��λ, �������ԽǵĲ�λ
    /// ���matchId == 0: ������id�Ƿ���ȷ���ȫ��, ����ֻ�᷵��id��ȵĲ�λ
    /// ���checkPiece == true, ������slot��piece, ��matchId == 0 ������²�����
    /// ���checkIncoming == true, ������slot��incomingPiece, �� matchId == 0 ������²�����
    /// ���ĳһλ�ñ�upperPiece����, ��ô��λ��Ҳ�ᱻ����
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
                    // ������UpperPiece���ǵ�����
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