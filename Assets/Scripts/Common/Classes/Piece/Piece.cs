using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class Piece : MonoBehaviour, IDisposable
{
    // ʵ��������
    [SerializeField] private int id;
    [SerializeField] private string pieceDefaultSortingLayerName;
    [SerializeField] public int pieceDefaultSortingOrder;
    [SerializeField] private PieceConfigSO pieceConfigSO;

    // �������
    public Transform Transform => transform;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    public SortingGroup SortingGroup => _sortingGroup;

    // ��������
    public int Id => id;
    public Vector2Int Size { get; protected set; }
    public PieceLayer PieceLayer { get; protected set; }
    public PieceTags PieceTags { get; protected set; }
    public PieceColors PieceColors { get; protected set; }
    public int ClearNum { get; protected set; }
    public bool CanMove { get; protected set; }
    public bool CanUse { get; protected set; }
    public bool Used { get; protected set; }            // �Ƿ�ʹ��
    public bool ClaimedReward { get; protected set; }   // �Ƿ񷢷��˽���(powerup in reward stage or coin piece)

    // ״̬����
    public GridPosition GridPosition { get; set; }      // �����е�λ��
    public Slot MovingToSlot { get; set; }              // �����˶�ǰ���Ĳ�λ
    public State CurrentState { get; set; }             // ��ǰ�˶�״̬
    public bool EnteredBoard { get; set; }              // �Ƿ��������
    public float MoveTime { get; protected set; }       // �Ѵ����ƶ��е�ʱ��


    public Match CurrentMatch { get; set; }                 // ������match
    public int SelectedByFlyBomb { get; protected set; }    // ���ɵ�ѡΪtarget�Ĵ���
    public bool SelectedToReplace { get; protected set; }   // ��rainbow��movesѡ�е�״̬(�����滻����Powerup)


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

    // �ص�
    public Action OnSpawnCallback { get; set; }
    public Action OnDamageCallback { get; set; }

    /// <summary>
    /// ��ʼ��SkeletonAnimation���
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
    /// ��ʼ�����Ӳ����������������, ��ʵ����֮�����������
    /// </summary>
    /// <param name="withinBoard">�����Ƿ�������������</param>
    /// <param name="gridPosition">���ӵ�λ��</param>
    /// <param name="overridePieceColor">�Ƿ��������ñ��е�������ɫ</param>
    /// <param name="overrideColors">���ӵ���ɫ, �Ḳ�����ñ��е�Ĭ����ɫ</param>
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
        SkeletonAnimation.maskInteraction = withinBoard ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask; // �����Ƿ������������ڿ�������

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
    /// ����״̬, �ڱ��ͷŵ������ǰ����
    /// </summary>
    public virtual void Dispose()
    {
        SetAlpha(1f);                                   // ����͸����
        Transform.position = new Vector3(20f, 0f, 0f);  // ���õ��������, ��ֹ�����

        // ����״̬
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
    /// ���ŵ������
    /// </summary>
    public virtual void PlayClickAnimation()
    {
        // �����ھ�ֹ״̬ ���� ����δ��� ���� ��powerup
        // ���ܹ��ٵ��
        if (CurrentState != State.Still ||
            !SkeletonAnimation.AnimationState.GetCurrent(0).IsComplete ||
            CanUse)
        {
            return;
        }

        SkeletonAnimation.AnimationState.SetAnimation(0, clickAnimation, false);
    }


    /// <summary>
    /// ����RainbowActionѡ��(��ʱ���߻�δ���)
    /// </summary>
    public virtual void OnRainbowSelect() => SelectedToReplace = true;


    /// <summary>
    /// ����Reward��������ѡ��
    /// </summary>
    public virtual void OnRewardSelect() => SelectedToReplace = true;


    /// <summary>
    /// ����Hintʱ�ĸ�������
    /// </summary>
    public virtual void PlayHintAnimation(float duration) { }


    /// <summary>
    /// ���Ŵ�������
    /// </summary>
    public virtual void PlayIdleAnimation() { }


    public virtual IEnumerable<Slot> GetOccupiedSlot() => new List<Slot> { MovingToSlot ?? GameBoardManager.instance.slotGrid[GridPosition] };


    /// <summary>
    /// �������Ӳ���, ��������ǰ��ʣ���������������Ч
    /// </summary>
    public abstract void Damage(Damage sourceDamage,
                                Action<Vector3, AnimationReferenceAsset> onDamagePlayVFX, 
                                Action<int, Vector3> onDamageCollectTarget, 
                                Action<Piece> onDamageControlPosition);


    /// <summary>
    /// ����λ��
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
    /// �������ӵ�͸����
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
/// ����״̬
/// </summary>
public enum State
{
    Still,          // ��ֹ
    Moving,         // �ƶ�������
    Bouncing,       // ���������� 
    Disappering,    // ��ʧ(����)������
    Disposed        // ���ͷ���������
}


public enum SpawnTypeEnum
{
    NormalSpawn,
    RainbowSpawn,
    BoostSpawn,
    StreakSpwan
}