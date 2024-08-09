using Spine;
using Spine.Unity;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
using UnityEngine;

public class RainbowAction : MonoBehaviour, IGameBoardAction
{
    [Header("Animations")]
    [SerializeField] private AnimationReferenceAsset singleSpinAnimation;
    [SerializeField] private AnimationReferenceAsset doubleSpinAnimation;
    [SerializeField] private AnimationReferenceAsset explodeAnimaiton;
    [SerializeField] private float failFindDelay;

    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    private readonly string doubleExplodeEventName = "boom";


    [Header("Line")]
    [SerializeField] private RainbowLine rainbowLineVFX;


    [Header("Actions")]
    [SerializeField] private FlybombAction flybombVFX;
    [SerializeField] private BombAction bombVFX;
    [SerializeField] private RocketAction rocketVFX;
    [Range(0, 1f)]
    [SerializeField] private double rocketWeight;
    [SerializeField] private int selectIntervalFrame;
    [SerializeField] private int activateIntervalFrame;

    private RainbowActionState State { get; set; }
    public Action DestroyCallback { get; private set; }
    public Rainbow Rainbow { get; private set; }
    public int SwappedPieceId { get; private set; }

    public int SelectPieceId { get; private set; }
    private readonly Random random = new();
    private HashSet<Piece> allPieces = new();           // ȫ����������
    private HashSet<Piece> selectedPieces = new();      // �ѱ�ѡ�еĻ������ӵ�λ��
    private List<Piece> unselectedPieces = new();       // δ��ѡ�еĻ������ӵ�λ��
    private int replacedCount;
    private int spawnedCount;
    //private float waitForSpawnCompleteTime;
    //private readonly float waitForSpawnCompleteTimeout = 5f;

    private List<RainbowLine> allLines = new();                 // ȫ�������Ĺ���
    private PriorityQueue<Piece, int> queuedPieces = new();     // �����߻��к������(�������������)
    private HashSet<Slot> lockedSlots = new();                  // �������뿪�Ĳ�λ, �����������������


    private int lastSelectFrame;
    private int lastActivateFrame;


    // State
    private bool playedExplodeAnimation;
    private bool replacedOrDestroyed;

    private enum RainbowActionState
    {
        Selecting,
        Waiting,
        Activating,
        Completed
    }


    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }


    public void Initialize(Rainbow rainbow, int swappedPieceId, Action destroyCallback)
    {
        Rainbow = rainbow;
        SwappedPieceId = swappedPieceId;
        DestroyCallback = destroyCallback;

        if (SwappedPieceId == Constants.PieceRainbowId)
        {
            // ����һ��rainbow���, ������ը
            SkeletonAnimation.AnimationState.SetAnimation(0, doubleSpinAnimation, false);
            SkeletonAnimation.AnimationState.Event += HandleDoubleRainbowExplode;
            SkeletonAnimation.AnimationState.Complete += SetCompleteState;
        }
        else
        {
            // ��������ӽ�����ѡ�񽻻��Ļ�������, ����ѡ�����������Ļ�������
            // ������Rainbow�Ĳ�λ
            LockSlot(GameBoardManager.instance.slotGrid[Rainbow.GridPosition]);
            // ��ѡ������
            SelectPieceId = GameBoardManager.instance.AllowedBasicPieceIds.Contains(SwappedPieceId) ? SwappedPieceId : GameBoardManager.instance.GetMostFreeBasicPieceId();
            GameBoardManager.instance.OnSelectMostBasicPieceId(SelectPieceId);

            if (SelectPieceId != 0)
            {
                // �л�������
                SkeletonAnimation.AnimationState.SetAnimation(0, singleSpinAnimation, true);
                State = RainbowActionState.Selecting;
                lastSelectFrame = selectIntervalFrame;
            }
            else
            {
                // �޻�������, ��תһ��ʱ����Ի�
                SkeletonAnimation.AnimationState.SetAnimation(0, singleSpinAnimation, false);
                SkeletonAnimation.AnimationState.AddAnimation(0, explodeAnimaiton, false, failFindDelay);
                if (!replacedOrDestroyed)
                {
                    DestroyCallback?.Invoke();
                    replacedOrDestroyed = true;
                }
                SkeletonAnimation.AnimationState.Complete += SetCompleteState;
            }
        }
    }


    /// <summary>
    /// ����rainbow��ϱ�ը
    /// </summary>
    private void HandleDoubleRainbowExplode(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == doubleExplodeEventName)
        {
            DestroyCallback?.Invoke();
            GameBoardManager.instance.DoubleRainbowExplode();
        }
    }


    /// <summary>
    /// ������λ���뿪
    /// </summary>
    private void LockSlot(Slot slot)
    {
        if (lockedSlots.Add(slot))
        {
            slot.IncreaseEnterAndLeaveLock();
        }
    }


    /// <summary>
    /// ������λ���뿪
    /// </summary>
    private void UnlockSlot(Slot slot)
    {
        if (lockedSlots.Remove(slot))
        {
            slot.DecreaseEnterAndLeaveLock();
        }
    }


    /// <summary>
    /// �������״̬�ص�
    /// </summary>
    private void SetCompleteState(TrackEntry trackEntry)
    {
        State = RainbowActionState.Completed;
    }


    /// <summary>
    /// ������ը��ɻص�
    /// </summary>
    private void OnExplodeComplete(TrackEntry trackEntry)
    {
        if (SkeletonAnimation.Skeleton.A != 0f)
        {
            SkeletonAnimation.Skeleton.A = 0f;
        }
    }

    /// <summary>
    /// ѡ�������δ��ѡ�����idһ�µĻ�������
    /// </summary>
    private Piece SelectRandomBasicPiece()
    {
        Grid slotGrid = GameBoardManager.instance.slotGrid;

        // ����ȫ���Ļ�������
        allPieces.Clear();
        foreach (var slot in slotGrid)
        {
            // Ѱ�Ҹ�λ���϶�Ӧ������
            var piece = slot switch
            {
                var x when x.upperPiece != null => null,
                var x when x.piece != null => x.piece,
                var x when x.incomingPiece != null && x.incomingPiece.EnteredBoard => x.incomingPiece,
                _ => null
            };

            // ������ || id���� || ��������Match
            if (piece == null ||
                piece.Id != SelectPieceId ||
                piece.CurrentMatch != null ||
                piece.CurrentState == global::State.Disposed)
                continue;

            allPieces.Add(piece);
        }

        // ����ͳ��ȫ��δ��ѡ��Ļ�������
        unselectedPieces.Clear();
        foreach (var pos in allPieces)
        {
            if (!selectedPieces.Contains(pos))
                unselectedPieces.Add(pos);      // ���δ��ѡ�й���λ��
        }

        // ���������һ��δ��ѡ���������
        if (unselectedPieces.Count > 0)
        {
            unselectedPieces = unselectedPieces.OrderBy(x => random.Next()).ToList();
            var randomIndex = random.Next(unselectedPieces.Count);
            var resPiece = unselectedPieces[randomIndex];
            selectedPieces.Add(resPiece);
            unselectedPieces.RemoveAt(randomIndex);
            return resPiece;
        }
        else return null;
    }


    private bool IsReadyForWaiting()
    {
        Grid slotGrid = GameBoardManager.instance.slotGrid;

        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive)
                continue;

            if (slot.IsSpawner && 
                slot.piece == null && 
                !slot.HasMoveConstrain)
                return false;
        }

        return true;
    }


    /// <summary>
    /// �������ȼ�, �������ȶ���, �±�ԽС��actionӵ��Խ�ߵ����ȼ�
    /// </summary>
    private int GetPriority(GridPosition gridPosition) => gridPosition.X + gridPosition.Y * 11;


    /// <summary>
    /// ��ȡһ��[0,1)֮��������
    /// </summary>
    private double GetRandomNumber() => random.NextDouble();


    /// <summary>
    /// ���ߵִ�Ŀ������(����Ŀ������)��Ļص�
    /// </summary>
    private void OnLineReachEndPosition(Piece reachedPiece)
    {
        // �����Ҫ�滻����, ���滻�ٲ��Ŷ���
        int replacePieceId = SwappedPieceId switch
        {
            var x when x == Constants.PieceFlyBombId || x == Constants.PieceBombId      => SwappedPieceId,
            var x when x == Constants.PieceHRocketId || x == Constants.PieceVRocketId   => GetRandomNumber() <= rocketWeight ? Constants.PieceHRocketId : Constants.PieceVRocketId,
            _ => 0
        };

        if (replacePieceId == 0)
        {
            reachedPiece.OnRainbowSelect();
            queuedPieces.Enqueue(reachedPiece, GetPriority(reachedPiece.MovingToSlot == null ? reachedPiece.GridPosition : reachedPiece.MovingToSlot.GridPosition));
        }
        else
        {
            var replacePosition = reachedPiece.MovingToSlot == null ? reachedPiece.GridPosition : reachedPiece.MovingToSlot.GridPosition;
            Replace(replacePosition, replacePieceId);
        }
    }


    private void OnLineDestroy(RainbowLine line) => allLines.Remove(line);


    private void Replace(GridPosition replaceGridPosition, int replacePieceId)
    {
        var replacedPiece = GameBoardManager.instance.ReplacePiece(replaceGridPosition, replacePieceId, SpawnTypeEnum.RainbowSpawn);

        if (replacedPiece.Id == Constants.PieceFlyBombId || 
            replacedPiece.Id == Constants.PieceBombId || 
            replacedPiece.Id == Constants.PieceHRocketId || 
            replacedPiece.Id == Constants.PieceVRocketId)
        {
            replacedCount++;
            replacedPiece.OnSpawnCallback = () => { spawnedCount++; };
        }

        queuedPieces.Enqueue(replacedPiece, GetPriority(replaceGridPosition));
    }


    public bool Tick()
    {
        if (SwappedPieceId == Constants.PieceRainbowId &&
            State != RainbowActionState.Completed)
        {
            return true;
        }

        if (State == RainbowActionState.Completed)
        {
            GameManager.instance.ReceivePlayerInputOnGameBoard();                   // ��ɺ�ָ��������
            // ���ջ���: �������������λ��, ��ô��Ҫ����
            if (lockedSlots.Count > 0)
            {
                for (int i = 0; i < lockedSlots.Count; i++)
                {
                    UnlockSlot(lockedSlots.ElementAt(i));
                    i--;
                }
            }
            Destroy(gameObject);
            return false;
        }
        else if (State == RainbowActionState.Selecting)
        {
            // ѡ�����ӹ���
            if (--lastSelectFrame > 0)
                return true;
            else lastSelectFrame = selectIntervalFrame;

            // ѡ������Ļ�������
            var piece = SelectRandomBasicPiece();
            if (piece != null)
            {
                // ������������, ������λ
                var slotGrid = GameBoardManager.instance.slotGrid;
                var lockSlot = piece.MovingToSlot ?? slotGrid[piece.GridPosition];
                LockSlot(lockSlot);

                // ����ѡ��, ��������Ϊ��Rainbowѡ��
                piece.OnRainbowSelect();

                // ��������, ����Ŀ������
                var insLine = Instantiate(rainbowLineVFX, transform.position, Quaternion.identity);
                insLine.Initialize(transform.position, piece, OnLineReachEndPosition, OnLineDestroy);
                allLines.Add(insLine);
            }
            else if (IsReadyForWaiting())
            {
                State = RainbowActionState.Waiting;
            }
        }
        else if (State == RainbowActionState.Waiting)
        {
            if (!playedExplodeAnimation)
            {
                SkeletonAnimation.AnimationState.SetAnimation(0, explodeAnimaiton, false);
                SkeletonAnimation.AnimationState.Complete += OnExplodeComplete;
                playedExplodeAnimation = true;
            }

            if (!replacedOrDestroyed)
            {
                if (SwappedPieceId == Constants.PieceFlyBombId ||
                    SwappedPieceId == Constants.PieceBombId ||
                    SwappedPieceId == Constants.PieceHRocketId ||
                    SwappedPieceId == Constants.PieceVRocketId)
                {
                    Replace(Rainbow.GridPosition, SwappedPieceId);
                }
                else Replace(Rainbow.GridPosition, SelectPieceId);

                replacedOrDestroyed = true;
                GameBoardManager.instance.ReleaseSelectMostBasicPieceId(SelectPieceId);
            }

            if (playedExplodeAnimation && replacedOrDestroyed &&
                GameBoardManager.instance.IsAllPiecesOnGameBoardStill &&
                allLines.Count <= 0 &&
                spawnedCount >= replacedCount)
            {
                State = RainbowActionState.Activating;
            }
        }
        else if (State == RainbowActionState.Activating)
        {
            // �������
            if (SwappedPieceId == Constants.PieceFlyBombId ||
                SwappedPieceId == Constants.PieceHRocketId ||
                SwappedPieceId == Constants.PieceVRocketId)
            {
                // �������μ���
                if (--lastActivateFrame > 0)
                    return true;
                else lastActivateFrame = activateIntervalFrame;

                if (queuedPieces.Count > 0)
                {
                    var powerup = queuedPieces.Dequeue();
                    var unlockPosition = powerup.GridPosition;
                    var unlockSlot = GameBoardManager.instance.slotGrid[unlockPosition];

                    // ����Powerup
                    if (SwappedPieceId == Constants.PieceFlyBombId)
                    {
                        var flybomb = powerup as FlyBomb;
                        GameBoardManager.instance.AddAction(flybomb.RainbowActionActivate());
                    }
                    else if (SwappedPieceId == Constants.PieceHRocketId || SwappedPieceId == Constants.PieceVRocketId)
                    {
                        var rocket = powerup as Rocket;
                        GameBoardManager.instance.AddAction(rocket.RainbowActionActivate());
                    }

                    // �������λ�õ�����
                    UnlockSlot(unlockSlot);
                }
                else
                {
                    // �������
                    State = RainbowActionState.Completed;
                }
            }
            else
            {
                // ͬʱ��������е�����
                if (SwappedPieceId == Constants.PieceBombId)
                {
                    // bomb���
                    var allBombs = new List<Bomb>();
                    while(queuedPieces.Count > 0)
                    {
                        allBombs.Add(queuedPieces.Dequeue() as Bomb);
                    }

                    var preClearedPositions = new HashSet<GridPosition>();
                    allBombs.ForEach(bomb => GameBoardManager.instance.AddAction(bomb.RainbowActionActivate(ref preClearedPositions)));

                    // ����ȫ���ļ���λ�õ�����
                    for (int i = 0; i < lockedSlots.Count; i++)
                    {
                        UnlockSlot(lockedSlots.ElementAt(i));
                        i--;
                    }
                }
                else
                {
                    // ��ͨ����ȫ�����
                    Damage sourceDamage = new();
                    foreach (var slot in lockedSlots)
                    {
                        sourceDamage.AddToDamagePositions(slot.GridPosition);
                    }

                    while (queuedPieces.Count > 0)
                    {
                        var piece = queuedPieces.Dequeue();
                        GameBoardManager.instance.RainbowActionDamage(sourceDamage, piece);
                    }

                    for (int i = 0; i < lockedSlots.Count; i++)
                    {
                        UnlockSlot(lockedSlots.ElementAt(i));
                        i--;
                    }
                }

                // ��ɺ�ָ���Id���ӵ�Match, ����������Rainbowѡ��
                GameBoardManager.instance.ReleaseSelectMostBasicPieceId(SelectPieceId);

                // �������
                State = RainbowActionState.Completed;
            }
        }

        return true;
    }
}
