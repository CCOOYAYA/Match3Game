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
    private HashSet<Piece> allPieces = new();           // 全部基础棋子
    private HashSet<Piece> selectedPieces = new();      // 已被选中的基础棋子的位置
    private List<Piece> unselectedPieces = new();       // 未被选中的基础棋子的位置
    private int replacedCount;
    private int spawnedCount;
    //private float waitForSpawnCompleteTime;
    //private readonly float waitForSpawnCompleteTimeout = 5f;

    private List<RainbowLine> allLines = new();                 // 全部发出的光线
    private PriorityQueue<Piece, int> queuedPieces = new();     // 被射线击中后的棋子(将被激活或消除)
    private HashSet<Slot> lockedSlots = new();                  // 被锁定离开的槽位, 将用于在消除后解锁


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
            // 与另一个rainbow组合, 清屏爆炸
            SkeletonAnimation.AnimationState.SetAnimation(0, doubleSpinAnimation, false);
            SkeletonAnimation.AnimationState.Event += HandleDoubleRainbowExplode;
            SkeletonAnimation.AnimationState.Complete += SetCompleteState;
        }
        else
        {
            // 与基础棋子交换后选择交换的基础棋子, 否则选择棋盘上最多的基础棋子
            // 先锁定Rainbow的槽位
            LockSlot(GameBoardManager.instance.slotGrid[Rainbow.GridPosition]);
            // 再选择棋子
            SelectPieceId = GameBoardManager.instance.AllowedBasicPieceIds.Contains(SwappedPieceId) ? SwappedPieceId : GameBoardManager.instance.GetMostFreeBasicPieceId();
            GameBoardManager.instance.OnSelectMostBasicPieceId(SelectPieceId);

            if (SelectPieceId != 0)
            {
                // 有基础棋子
                SkeletonAnimation.AnimationState.SetAnimation(0, singleSpinAnimation, true);
                State = RainbowActionState.Selecting;
                lastSelectFrame = selectIntervalFrame;
            }
            else
            {
                // 无基础棋子, 旋转一段时间后自毁
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
    /// 两个rainbow组合爆炸
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
    /// 锁定槽位的离开
    /// </summary>
    private void LockSlot(Slot slot)
    {
        if (lockedSlots.Add(slot))
        {
            slot.IncreaseEnterAndLeaveLock();
        }
    }


    /// <summary>
    /// 解锁槽位的离开
    /// </summary>
    private void UnlockSlot(Slot slot)
    {
        if (lockedSlots.Remove(slot))
        {
            slot.DecreaseEnterAndLeaveLock();
        }
    }


    /// <summary>
    /// 设置完成状态回调
    /// </summary>
    private void SetCompleteState(TrackEntry trackEntry)
    {
        State = RainbowActionState.Completed;
    }


    /// <summary>
    /// 当自身爆炸完成回调
    /// </summary>
    private void OnExplodeComplete(TrackEntry trackEntry)
    {
        if (SkeletonAnimation.Skeleton.A != 0f)
        {
            SkeletonAnimation.Skeleton.A = 0f;
        }
    }

    /// <summary>
    /// 选择随机的未被选择过的id一致的基础棋子
    /// </summary>
    private Piece SelectRandomBasicPiece()
    {
        Grid slotGrid = GameBoardManager.instance.slotGrid;

        // 更新全部的基础棋子
        allPieces.Clear();
        foreach (var slot in slotGrid)
        {
            // 寻找该位置上对应的棋子
            var piece = slot switch
            {
                var x when x.upperPiece != null => null,
                var x when x.piece != null => x.piece,
                var x when x.incomingPiece != null && x.incomingPiece.EnteredBoard => x.incomingPiece,
                _ => null
            };

            // 无棋子 || id不对 || 棋子正在Match
            if (piece == null ||
                piece.Id != SelectPieceId ||
                piece.CurrentMatch != null ||
                piece.CurrentState == global::State.Disposed)
                continue;

            allPieces.Add(piece);
        }

        // 重新统计全部未被选择的基础棋子
        unselectedPieces.Clear();
        foreach (var pos in allPieces)
        {
            if (!selectedPieces.Contains(pos))
                unselectedPieces.Add(pos);      // 添加未被选中过的位置
        }

        // 返回随机的一个未被选择过的棋子
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
    /// 触发优先级, 用于优先队列, 下标越小的action拥有越高的优先级
    /// </summary>
    private int GetPriority(GridPosition gridPosition) => gridPosition.X + gridPosition.Y * 11;


    /// <summary>
    /// 获取一个[0,1)之间的随机数
    /// </summary>
    private double GetRandomNumber() => random.NextDouble();


    /// <summary>
    /// 射线抵达目标棋子(击中目标棋子)后的回调
    /// </summary>
    private void OnLineReachEndPosition(Piece reachedPiece)
    {
        // 如果需要替换棋子, 先替换再播放动画
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
            GameManager.instance.ReceivePlayerInputOnGameBoard();                   // 完成后恢复玩家输入
            // 保险机制: 如果仍有锁定的位置, 那么需要解锁
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
            // 选择棋子过程
            if (--lastSelectFrame > 0)
                return true;
            else lastSelectFrame = selectIntervalFrame;

            // 选中随机的基础棋子
            var piece = SelectRandomBasicPiece();
            if (piece != null)
            {
                // 发出锁定请求, 锁定槽位
                var slotGrid = GameBoardManager.instance.slotGrid;
                var lockSlot = piece.MovingToSlot ?? slotGrid[piece.GridPosition];
                LockSlot(lockSlot);

                // 发出选中, 将棋子设为被Rainbow选中
                piece.OnRainbowSelect();

                // 发出射线, 射向目标棋子
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
            // 激活过程
            if (SwappedPieceId == Constants.PieceFlyBombId ||
                SwappedPieceId == Constants.PieceHRocketId ||
                SwappedPieceId == Constants.PieceVRocketId)
            {
                // 道具依次激活
                if (--lastActivateFrame > 0)
                    return true;
                else lastActivateFrame = activateIntervalFrame;

                if (queuedPieces.Count > 0)
                {
                    var powerup = queuedPieces.Dequeue();
                    var unlockPosition = powerup.GridPosition;
                    var unlockSlot = GameBoardManager.instance.slotGrid[unlockPosition];

                    // 激活Powerup
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

                    // 解除激活位置的锁定
                    UnlockSlot(unlockSlot);
                }
                else
                {
                    // 激活完成
                    State = RainbowActionState.Completed;
                }
            }
            else
            {
                // 同时激活队列中的棋子
                if (SwappedPieceId == Constants.PieceBombId)
                {
                    // bomb清除
                    var allBombs = new List<Bomb>();
                    while(queuedPieces.Count > 0)
                    {
                        allBombs.Add(queuedPieces.Dequeue() as Bomb);
                    }

                    var preClearedPositions = new HashSet<GridPosition>();
                    allBombs.ForEach(bomb => GameBoardManager.instance.AddAction(bomb.RainbowActionActivate(ref preClearedPositions)));

                    // 解锁全部的激活位置的锁定
                    for (int i = 0; i < lockedSlots.Count; i++)
                    {
                        UnlockSlot(lockedSlots.ElementAt(i));
                        i--;
                    }
                }
                else
                {
                    // 普通棋子全部清除
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

                // 完成后恢复该Id棋子的Match, 并允许其他Rainbow选择
                GameBoardManager.instance.ReleaseSelectMostBasicPieceId(SelectPieceId);

                // 激活完成
                State = RainbowActionState.Completed;
            }
        }

        return true;
    }
}
