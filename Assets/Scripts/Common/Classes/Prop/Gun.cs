using Spine;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour, IGameBoardAction
{
    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    private readonly string arrowName = "rocket_0";
    private Bone arrowBone;

    private Damage sourceDamage;
    private List<GridPosition> lockedPositions;
    private HashSet<GridPosition> damagedPositions;
    private float sinceAllDamaged;
    private bool completed;
    private bool locked;

    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();

        arrowBone = SkeletonAnimation.Skeleton.FindBone(arrowName);
    }

    public void PropActivate(Grid slotGrid, GridPosition clickPosition)
    {
        SkeletonAnimation.AnimationState.Complete += delegate { completed = true; };
        GameManager.instance.MutePlayerInputOnGameBoard();

        lockedPositions = new();
        damagedPositions = new();
        for (int x = 0; x < slotGrid.XMax; x++)
        {
            var lockPosition = new GridPosition(x, clickPosition.Y);
            if (GridMath.IsPositionOnBoard(slotGrid, lockPosition, out var lockSlot))
            {
                lockSlot.IncreaseEnterLock(1);
                lockedPositions.Add(lockPosition);
            }
        }

        sourceDamage = new();
        lockedPositions.ForEach(pos => sourceDamage.AddToDamagePositions(pos));

        locked = true;
    }

    public bool Tick()
    {
        if (locked &&
            damagedPositions.Count == lockedPositions.Count)
        {
            sinceAllDamaged += Time.deltaTime;
            if (sinceAllDamaged >= 0.167f)
            {
                lockedPositions.ForEach(pos => GameBoardManager.instance.slotGrid[pos].DecreaseEnterLock(1));
                locked = false;
            }
        }

        if (completed)
        {
            // 接受用户输入
            GameManager.instance.ReceivePlayerInputOnGameBoard();

            // 解锁位置
            lockedPositions.ForEach(pos =>
            {
                if (!damagedPositions.Contains(pos))
                {
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

        if (locked)
        {
            Vector3 arrowPosition = arrowBone.GetWorldPosition(SkeletonAnimation.transform);
            var gridPosition = GameBoardManager.instance.GetGridPositionByWorldPosition(arrowPosition);

            var positionsBehindArrow = new List<GridPosition>();
            int x = gridPosition.X, y = gridPosition.Y;
            while (x >= 0)
            {
                positionsBehindArrow.Add(new GridPosition(x, y));

                x--;
            }

            positionsBehindArrow.ForEach(damagePos =>
            {
                if (lockedPositions.Contains(damagePos) &&
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
