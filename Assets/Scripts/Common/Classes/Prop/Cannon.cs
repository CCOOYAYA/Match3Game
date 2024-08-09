using Spine;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour, IGameBoardAction
{
    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    private readonly string roundName = "rocket_0";
    private Bone roundBone;

    private Damage sourceDamage;
    private List<GridPosition> lockedPositions;
    private HashSet<GridPosition> damagedPositions;
    private float sinceAllDamaged;
    private bool completed;
    private bool locked;

    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();

        roundBone = SkeletonAnimation.Skeleton.FindBone(roundName);
    }

    public void PropActivate(Grid slotGrid, GridPosition clickPosition)
    {
        SkeletonAnimation.AnimationState.Complete += delegate { completed = true; };
        GameManager.instance.MutePlayerInputOnGameBoard();

        lockedPositions = new();
        damagedPositions = new();
        for (int y = 0; y < slotGrid.YMax; y++)
        {
            var lockPosition = new GridPosition(clickPosition.X, y);
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
            // 按钮从屏幕外渐入
            MainGameUIManager.ShowBottomButtons();

            // 接受用户输入
            GameManager.instance.ReceivePlayerInputOnGameBoard();

            // 解锁位置
            lockedPositions.ForEach(pos =>
            {
                if (damagedPositions.Contains(pos) == false)
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
            Vector3 arrowPosition = roundBone.GetWorldPosition(SkeletonAnimation.transform);
            var gridPosition = GameBoardManager.instance.GetGridPositionByWorldPosition(arrowPosition);

            var positionsBetweenTwoRockets = new List<GridPosition>();
            int x = gridPosition.X, y = gridPosition.Y;
            while (y <= GameBoardManager.instance.slotGrid.YMax)
            {
                positionsBetweenTwoRockets.Add(new GridPosition(x, y));

                y++;
            }

            positionsBetweenTwoRockets.ForEach(damagePos =>
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
