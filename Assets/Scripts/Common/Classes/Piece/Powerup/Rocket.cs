using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Rocket : Powerup
{
    [SerializeField]    private bool vertical;
    [SerializeField]    private int standaloneActivateScore;
    [SerializeField]    private RocketAction rocketVFX;

    [Header("Animation")]
    [SerializeField]    private AnimationReferenceAsset rainbowSpawnAnimation;
    [SerializeField]    private AnimationReferenceAsset boostSpawnAnimation;
    [SerializeField]    private AnimationReferenceAsset streakSpawnAnimation;
    [SerializeField]    private AnimationReferenceAsset rainbowIdleAnimation;
    [SerializeField]    private RocketBombCombineAction rocketBombCombineVFX;



    private readonly Quaternion verticalRotation = Quaternion.Euler(0, 0, 90f);
    private readonly Quaternion horizontalRotation = Quaternion.identity;

    public int StandAloneActivateScore => standaloneActivateScore;
    public SpawnTypeEnum SpawnType { get; private set; }

    public override void InitializePiece(bool withinBoard, GridPosition gridPosition,
                                     bool overridePieceClearNum, int overrideClearNum,
                                     bool overridePieceColor, PieceColors overrideColors,
                                     SpawnTypeEnum spawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        base.InitializePiece(withinBoard, gridPosition, overridePieceClearNum, overrideClearNum, overridePieceColor, overrideColors, spawnTypeEnum);

        SpawnType = spawnTypeEnum;
        var useSpawnAnimation = spawnTypeEnum switch
        {
            SpawnTypeEnum.RainbowSpawn => rainbowSpawnAnimation,
            SpawnTypeEnum.BoostSpawn => boostSpawnAnimation,
            SpawnTypeEnum.StreakSpwan => streakSpawnAnimation,
            _ => spawnAnimation
        };
        SkeletonAnimation.AnimationState.SetAnimation(0, useSpawnAnimation, false);     // 播放生成动画
        SkeletonAnimation.AnimationState.Event += OnSpawnCompleteCallback;
    }


    protected override void OnSpawnCompleteCallback(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == spawnCompleteEventName)
        {
            OnSpawnCallback?.Invoke();

            SkeletonAnimation.AnimationState.SetAnimation(0, SpawnType == SpawnTypeEnum.RainbowSpawn ? rainbowIdleAnimation : idleAnimation, true);
            SkeletonAnimation.AnimationState.Event -= OnSpawnCompleteCallback;
        }
    }


    /// <summary>
    /// 独立激活Rocket, 发射火箭动画
    /// </summary>
    public RocketAction StandaloneActivate()
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(StandAloneActivateScore, false);  // 得分
        MarkAsUsed();                                                   // 设置状态并隐藏

        var gridPosition = GameBoardManager.instance.GetGridPositionByWorldPosition(GetWorldPosition());
        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(gridPosition);
        var slotGrid = GameBoardManager.instance.slotGrid;

        var targetPositions = new List<GridPosition>();
        var addPosition = vertical ? new GridPosition(gridPosition.X, 0) : new GridPosition(0, gridPosition.Y);
        while (GridMath.IsPositionOnGrid(slotGrid, addPosition))
        {
            LockAndAddToTargetPositions(slotGrid, addPosition, ref targetPositions);
            addPosition += vertical ? GridPosition.Down : GridPosition.Right;
        }

        var insAction = Instantiate(rocketVFX, insWorldPosition, vertical ? verticalRotation : horizontalRotation);
        insAction.Initialize(GridPosition, targetPositions, vertical, DestroySelf);
        return insAction;
    }


    /// <summary>
    /// 和FlyBomb交换激活
    /// </summary>
    public FlybombAction FlyBombActivate(FlyBomb flyBomb, GridPosition swapCompletePosition)
    {
        return flyBomb.RocketActivate(this, swapCompletePosition);
    }



    /// <summary>
    /// 和Bomb交换激活
    /// </summary>
    public RocketBombCombineAction BombActivate(Bomb bomb, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(2 * (StandAloneActivateScore + bomb.StandAloneActivateScore), false);
        MarkAsUsed();
        bomb.MarkAsUsed();

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);
        var slotGrid = GameBoardManager.instance.slotGrid;
        var verticalSwap = GridPosition - bomb.GridPosition == GridPosition.Down || GridPosition - bomb.GridPosition == GridPosition.Up;

        var insCombineVFX = Instantiate(rocketBombCombineVFX, insWorldPosition, Quaternion.identity);
        insCombineVFX.PlayCombineAnimation(verticalSwap, vertical, () =>
        {
            var horizontalPositions = new List<GridPosition>() { swapCompletePosition, swapCompletePosition + GridPosition.Up, swapCompletePosition + GridPosition.Down };
            var verticalPositions = new List<GridPosition>() { swapCompletePosition, swapCompletePosition + GridPosition.Left, swapCompletePosition + GridPosition.Right };

            horizontalPositions.ForEach(pos =>
            {
                insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(pos);
                var targetPositions = new List<GridPosition>();
                var addPosition = new GridPosition(0, pos.Y);
                while (GridMath.IsPositionOnGrid(slotGrid, addPosition))
                {
                    LockAndAddToTargetPositions(slotGrid, addPosition, ref targetPositions);
                    addPosition += GridPosition.Right;
                }

                Action destroyCallbacks = DestroySelf;
                destroyCallbacks += bomb.DestroySelf;
                var insAction = Instantiate(rocketVFX, insWorldPosition, horizontalRotation);
                insAction.Initialize(pos, targetPositions, false, destroyCallbacks);
                GameBoardManager.instance.AddAction(insAction);
            });
            verticalPositions.ForEach(pos =>
            {
                insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(pos);
                var targetPositions = new List<GridPosition>();
                var addPosition = new GridPosition(pos.X, 0);
                while (GridMath.IsPositionOnGrid(slotGrid, addPosition))
                {
                    LockAndAddToTargetPositions(slotGrid, addPosition, ref targetPositions);
                    addPosition += GridPosition.Down;
                }

                Action destroyCallbacks = DestroySelf;
                destroyCallbacks += bomb.DestroySelf;
                var insAction = Instantiate(rocketVFX, insWorldPosition, verticalRotation);
                insAction.Initialize(pos, targetPositions, true, pos.Equals(verticalPositions.Last()) ? destroyCallbacks : null);  // 最后一个发出的Action再销毁此Powerup和Bomb
                GameBoardManager.instance.AddAction(insAction);
            });
        });
        return insCombineVFX;
    }


    /// <summary>
    /// 和Rocket交换激活
    /// </summary>
    public IEnumerable<RocketAction> RocketActivate(Rocket minorRocket, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(4 * StandAloneActivateScore, false);
        MarkAsUsed();
        minorRocket.MarkAsUsed();

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);
        var slotGrid = GameBoardManager.instance.slotGrid;

        var insActions = new List<RocketAction>();
        {
            // vertical
            var targetPositions = new List<GridPosition>();
            var addPosition = new GridPosition(swapCompletePosition.X, 0);
            while (GridMath.IsPositionOnGrid(slotGrid, addPosition))
            {
                LockAndAddToTargetPositions(slotGrid, addPosition, ref targetPositions);
                addPosition += GridPosition.Down;
            }

            var insAction = Instantiate(rocketVFX, insWorldPosition, verticalRotation);
            insAction.Initialize(swapCompletePosition, targetPositions, true, minorRocket.DestroySelf);
            insActions.Add(insAction);
        }

        {
            // horizontal
            var targetPositions = new List<GridPosition>();
            var addPosition = new GridPosition(0, swapCompletePosition.Y);
            while (GridMath.IsPositionOnGrid(slotGrid, addPosition))
            {
                LockAndAddToTargetPositions(slotGrid, addPosition, ref targetPositions);
                addPosition += GridPosition.Right;
            }

            var insAction = Instantiate(rocketVFX, insWorldPosition, horizontalRotation);
            insAction.Initialize(swapCompletePosition, targetPositions, false, DestroySelf);
            insActions.Add(insAction);
        }
        return insActions;
    }


    /// <summary>
    /// 和Rainbow交换激活
    /// </summary>
    public RainbowAction RainbowActivate(Rainbow rainbow, GridPosition swapCompletePosition)
    {
        return rainbow.RocketActivate(this, swapCompletePosition);
    }


    /// <summary>
    /// 由RainbowAction激活
    /// </summary>
    public RocketAction RainbowActionActivate()
    {
        if (Used) { return null; }

        GameManager.instance.AddScore(StandAloneActivateScore, false);
        MarkAsUsed();

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(GridPosition);
        var slotGrid = GameBoardManager.instance.slotGrid;

        var targetPositions = new List<GridPosition>();
        var addPosition = vertical ? new GridPosition(GridPosition.X, 0) : new GridPosition(0, GridPosition.Y);
        while (GridMath.IsPositionOnGrid(slotGrid, addPosition))
        {
            LockAndAddToTargetPositions(slotGrid, addPosition, ref targetPositions);
            addPosition += vertical ? GridPosition.Down : GridPosition.Right;
        }

        var insAction = Instantiate(rocketVFX, insWorldPosition, vertical ? verticalRotation : horizontalRotation);
        insAction.Initialize(GridPosition, targetPositions, vertical, DestroySelf);
        return insAction;
    }


    /// <summary>
    /// 锁定当前位置并将它加入到targetPositions列表中
    /// </summary>
    private void LockAndAddToTargetPositions(Grid slotGrid, GridPosition addPosition, ref List<GridPosition> targetPositions)
    {
        if (slotGrid[addPosition].IsActive)
        {
            slotGrid[addPosition].IncreaseEnterLock(1);
            targetPositions.Add(addPosition);
        }
    }
}
