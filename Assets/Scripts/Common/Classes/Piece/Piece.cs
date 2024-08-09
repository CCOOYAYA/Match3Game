using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class Piece : MonoBehaviour, IDisposable
{
    // 实例化参数
    [SerializeField] private int id;
    [SerializeField] private string pieceDefaultSortingLayerName;
    [SerializeField] public int pieceDefaultSortingOrder;
    [SerializeField] private PieceConfigSO pieceConfigSO;

    // 组件属性
    public Transform Transform => transform;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    public SortingGroup SortingGroup => _sortingGroup;

    // 基础属性
    public int Id => id;
    public Vector2Int Size { get; protected set; }
    public PieceLayer PieceLayer { get; protected set; }
    public PieceTags PieceTags { get; protected set; }
    public PieceColors PieceColors { get; protected set; }
    public int ClearNum { get; protected set; }
    public bool CanMove { get; protected set; }
    public bool CanUse { get; protected set; }
    public bool Used { get; protected set; }            // 是否被使用
    public bool ClaimedReward { get; protected set; }   // 是否发放了奖励(powerup in reward stage or coin piece)

    // 状态属性
    public GridPosition GridPosition { get; set; }      // 棋盘中的位置
    public Slot MovingToSlot { get; set; }              // 正在运动前往的槽位
    public State CurrentState { get; set; }             // 当前运动状态
    public bool EnteredBoard { get; set; }              // 是否进入棋盘
    public float MoveTime { get; protected set; }       // 已处于移动中的时间


    public Match CurrentMatch { get; set; }                 // 包含的match
    public int SelectedByFlyBomb { get; protected set; }    // 被飞弹选为target的次数
    public bool SelectedToReplace { get; protected set; }   // 被rainbow或moves选中的状态(用于替换生成Powerup)


    // Spine
    private SkeletonAnimation _skeletonAnimation;
    private SortingGroup _sortingGroup;

    protected string spawnAnimationName = "ani_spawn";
    protected string idleAnimationName = "ani_idle";
    protected string clickAnimationName = "ani_click";
    protected Spine.Animation spawnAnimation;
    protected Spine.Animation idleAnimation;
    protected Spine.Animation clickAnimation;

    protected string spawnCompleteEventName = "spawn_complete";

    // 回调
    public Action OnSpawnCallback { get; set; }
    public Action OnDamageCallback { get; set; }

    /// <summary>
    /// 初始化SkeletonAnimation组件
    /// </summary>
    protected virtual void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
        _sortingGroup = GetComponent<SortingGroup>();

        spawnAnimation = SkeletonAnimation.Skeleton.Data.FindAnimation(spawnAnimationName);
        idleAnimation = SkeletonAnimation.Skeleton.Data.FindAnimation(idleAnimationName);
        clickAnimation = SkeletonAnimation.Skeleton.Data.FindAnimation(clickAnimationName);
    }


    /// <summary>
    /// 初始化棋子并设置棋子相关属性, 在实例化之后会立即调用
    /// </summary>
    /// <param name="withinBoard">棋子是否生成在棋盘内</param>
    /// <param name="gridPosition">棋子的位置</param>
    /// <param name="overridePieceColor">是否重载配置表中的棋子颜色</param>
    /// <param name="overrideColors">棋子的颜色, 会覆盖配置表中的默认颜色</param>
    public virtual void InitializePiece(bool withinBoard, GridPosition gridPosition,
                                        bool overridePieceClearNum, int overrideClearNum,
                                        bool overridePieceColor, PieceColors overrideColors,
                                        SpawnTypeEnum spawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        pieceConfigSO.allRegisteredPieces.TryGetValue(Id, out var registeredPiece);
        if (registeredPiece == null)
        {
            throw new NullReferenceException("Unregistered Piece while initializing");
        }

        Transform.rotation = Quaternion.Euler(registeredPiece.pieceInsArgs.pieceRotation);
        SkeletonAnimation.maskInteraction = withinBoard ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask; // 按照是否生成在棋盘内开启遮罩

        Size = registeredPiece.pieceInsArgs.pieceSize;
        PieceLayer = registeredPiece.pieceInsArgs.pieceLayer;
        PieceTags = registeredPiece.pieceInsArgs.pieceTags;
        PieceColors = overridePieceColor == false ? registeredPiece.pieceInsArgs.pieceColors : overrideColors;
        
        ClearNum = overridePieceClearNum == false ? registeredPiece.pieceInsArgs.pieceClearNum : overrideClearNum;
        CanMove = registeredPiece.pieceInsArgs.pieceCanMove;
        CanUse = registeredPiece.pieceInsArgs.pieceCanUse;
        Used = false;
        ClaimedReward = false;

        GridPosition = gridPosition;
        MovingToSlot = null;
        CurrentState = State.Still;
        EnteredBoard = withinBoard;
        MoveTime = 0f;

        CurrentMatch = null;
        SelectedByFlyBomb = 0;
        SelectedToReplace = false;
    }


    protected virtual void OnSpawnCompleteCallback(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == spawnCompleteEventName)
        {
            OnSpawnCallback?.Invoke();
            SkeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);

            SkeletonAnimation.AnimationState.Event -= OnSpawnCompleteCallback;
        }
    }


    /// <summary>
    /// 重置状态, 在被释放到对象池前调用
    /// </summary>
    public virtual void Dispose()
    {
        SetAlpha(1f);                                   // 重置透明度
        Transform.position = new Vector3(20f, 0f, 0f);  // 放置到摄像机外, 防止被点击

        // 重设状态
        MovingToSlot = null;
        CurrentState = State.Disposed;
        EnteredBoard = false;
        MoveTime = 0f;

        CurrentMatch = null;
        SelectedByFlyBomb = 0;
        SelectedToReplace = false;

        OnSpawnCallback = null;
        OnDamageCallback = null;
    }


    public virtual void ClaimReward() => ClaimedReward = true;


    /// <summary>
    /// 播放点击反馈
    /// </summary>
    public virtual void PlayClickAnimation()
    {
        // 不处于静止状态 或者 动画未完成 或者 是powerup
        // 不能够再点击
        if (CurrentState != State.Still ||
            !SkeletonAnimation.AnimationState.GetCurrent(0).IsComplete ||
            CanUse)
        {
            return;
        }

        SkeletonAnimation.AnimationState.SetAnimation(0, clickAnimation, false);
    }


    /// <summary>
    /// 当被RainbowAction选中(此时射线还未射出)
    /// </summary>
    public virtual void OnRainbowSelect() => SelectedToReplace = true;


    /// <summary>
    /// 当被Reward射线粒子选中
    /// </summary>
    public virtual void OnRewardSelect() => SelectedToReplace = true;


    /// <summary>
    /// 播放Hint时的高亮动画
    /// </summary>
    public virtual void PlayHintAnimation(float duration) { }


    /// <summary>
    /// 播放待机动画
    /// </summary>
    public virtual void PlayIdleAnimation() { }


    public virtual IEnumerable<Slot> GetOccupiedSlot() => new List<Slot> { MovingToSlot ?? GameBoardManager.instance.slotGrid[GridPosition] };


    /// <summary>
    /// 消除棋子层数, 并按消除前的剩余层数播放消除动效
    /// </summary>
    public abstract void Damage(Damage sourceDamage,
                                Action<Vector3, AnimationReferenceAsset> onDamagePlayVFX, 
                                Action<int, Vector3> onDamageCollectTarget, 
                                Action<Piece> onDamageControlPosition);


    /// <summary>
    /// 到达位置
    /// </summary>
    public void ReachGridPosition(GridPosition newGridPositoin)
    {
        GridPosition = newGridPositoin;
    }
    public void StartMove(Slot moveToSlot)
    {
        if (CurrentState != State.Moving)
        {
            MoveTime = 0f;
        }
        MovingToSlot = moveToSlot;
        CurrentState = State.Moving;
    }
    public void TickMove(float deltaTime)
    {
        MoveTime += deltaTime;
    }
    public void StopMove()
    {
        MoveTime = 0f;
        MovingToSlot = null;
        CurrentState = State.Bouncing;
    }
    public void TickBounce(float deltaTime)
    {
        MoveTime += deltaTime;
    }
    public void StopBouncing()
    {
        MoveTime = 0f;
        CurrentState = State.Still;
    }
    public void StartDestroy()
    {
        CurrentState = State.Disappering;
    }


    public Vector3 GetWorldPosition() => Transform.position;
    public int GetCollectId() => pieceConfigSO.allRegisteredPieces[Id].pieceTargetReference.collectId;

    public void IncreaseSelectByFlyBomb() => SelectedByFlyBomb++;
    public void DecreaseSelectByFlyBomb() => SelectedByFlyBomb = SelectedByFlyBomb <= 1 ? 0 : SelectedByFlyBomb - 1;


    
    /// <summary>
    /// 设置棋子的透明度
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (SkeletonAnimation.Skeleton.A != alpha)
        {
            SkeletonAnimation.Skeleton.A = alpha;
        }
    }
}


/// <summary>
/// 棋子状态
/// </summary>
public enum State
{
    Still,          // 静止
    Moving,         // 移动动画中
    Bouncing,       // 弹跳动画中 
    Disappering,    // 消失(消除)动画中
    Disposed        // 被释放入对象池种
}


public enum SpawnTypeEnum
{
    NormalSpawn,
    RainbowSpawn,
    BoostSpawn,
    StreakSpwan
}