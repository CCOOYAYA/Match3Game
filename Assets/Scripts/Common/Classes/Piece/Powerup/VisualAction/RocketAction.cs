using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RocketAction : MonoBehaviour, IGameBoardAction
{
    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    private readonly string rocket1Name = "rocket_0";
    private readonly string rocket2Name = "rocket_1";
    private Bone rocket1Bone;
    private Bone rocket2Bone;

    private float sinceAllDamaged;

    [Header("Trail")]
    [SerializeField]    private GameObject trail_0;
    [SerializeField]    private GameObject trail_1;

    [Header("Spine Animations")]
    [SerializeField]    private AnimationReferenceAsset executeAnimation;


    public GridPosition Pivot { get; private set; }
    public List<GridPosition> TargetPositions { get; private set; } = new();
    public bool Vertical { get; private set; }
    public Action DestroyCallback { get; private set; }


    private Damage sourceDamage;
    private HashSet<GridPosition> damagedPositions = new();     // ����������λ��
    private bool completed;                                     // ��ɷ���
    private bool locked = true;                                 // λ�ý�������


    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();

        rocket1Bone = SkeletonAnimation.Skeleton.FindBone(rocket1Name);
        rocket2Bone = SkeletonAnimation.Skeleton.FindBone(rocket2Name);
    }


    public void Initialize(GridPosition centerPosition, List<GridPosition> targetPositions, bool vertical, Action destroyCallbacks)
    {
        Pivot = centerPosition;
        TargetPositions = targetPositions;
        Vertical = vertical;
        DestroyCallback = destroyCallbacks;

        DestroyCallback?.Invoke();

        SkeletonAnimation.AnimationState.SetAnimation(0, executeAnimation, false);
        SkeletonAnimation.AnimationState.Complete += SetCompleteState;
        trail_0.SetActive(true);
        trail_1.SetActive(true);

        sourceDamage = new();
        TargetPositions.ForEach(pos => sourceDamage.AddToDamagePositions(pos));
    }


    private void SetCompleteState(TrackEntry trackEntry) => completed = true;


    public bool Tick()
    {
        Vector3 rocket1Pos = rocket1Bone.GetWorldPosition(SkeletonAnimation.transform);
        Vector3 rocket2Pos = rocket2Bone.GetWorldPosition(SkeletonAnimation.transform);
        trail_0.transform.position = rocket1Pos;
        trail_1.transform.position = rocket2Pos;

        if (locked &&
            damagedPositions.Count == TargetPositions.Count)
        {
            sinceAllDamaged += Time.deltaTime;

            if (sinceAllDamaged >= 0.167f)
            {
                TargetPositions.ForEach(pos => GameBoardManager.instance.slotGrid[pos].DecreaseEnterLock(1));
                locked = false;
            }
        }

        // ��ɺ�����vfx
        if (completed)
        {
            // ����λ��
            TargetPositions.ForEach(pos =>
            {
                if (damagedPositions.Contains(pos) == false)
                {
                    // �����δ������λ��, ��Ҫ����
                    GameBoardManager.instance.DamageSlot(sourceDamage, pos);
                }

                if (locked)
                {
                    GameBoardManager.instance.slotGrid[pos].DecreaseEnterLock(1);
                }
            });
            Destroy(gameObject);
            return false;
        }

        // δ��������λ��ʱÿ֡�����ͷλ�ò���������
        if (locked)
        {
            var gridPosition1 = GameBoardManager.instance.GetGridPositionByWorldPosition(rocket1Pos);
            var gridPosition2 = GameBoardManager.instance.GetGridPositionByWorldPosition(rocket2Pos);

            var positionsBetweenTwoRockets = new List<GridPosition>();
            if (Vertical)
            {
                int x = gridPosition1.X;
                int up = Math.Max(gridPosition1.Y, gridPosition2.Y),
                    down = Math.Min(gridPosition1.Y, gridPosition1.Y);

                while (up >= down)
                {
                    positionsBetweenTwoRockets.Add(new GridPosition(x, up));
                    positionsBetweenTwoRockets.Add(new GridPosition(x, down));

                    up--;
                    down++;
                }
            }
            else
            {
                int y = gridPosition1.Y;
                int left = Math.Min(gridPosition1.X, gridPosition2.X),
                    right = Math.Max(gridPosition1.X, gridPosition2.X);

                while (left <= right)
                {
                    positionsBetweenTwoRockets.Add(new GridPosition(left, y));
                    positionsBetweenTwoRockets.Add(new GridPosition(right, y));

                    left++;
                    right--;
                }
            }

            positionsBetweenTwoRockets.ForEach(damagePos =>
            {
                if (TargetPositions.Contains(damagePos) &&
                    damagedPositions.Add(damagePos) &&
                    GridMath.IsPositionOnBoard(GameBoardManager.instance.slotGrid, damagePos))
                {
                    GameBoardManager.instance.DamageSlot(sourceDamage, damagePos);
                }
            });
        }
        return true;
    }
}
