using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : Powerup
{
    [SerializeField] private int standaloneActivateScore;
    [SerializeField] private int bombExplodeDiameter = 5;       // 普通爆炸范围: 5x5
    [SerializeField] private int greatBombExplodeDiameter = 9;  // 合成爆炸范围: 9x9
    [SerializeField] private BombAction bombVFX;

    [Header("Animations")]
    [SerializeField] private AnimationReferenceAsset rainbowSpawnAnimation;
    [SerializeField] private AnimationReferenceAsset boostSpawnAnimation;
    [SerializeField] private AnimationReferenceAsset streakSpawnAnimation;
    [SerializeField] private AnimationReferenceAsset rainbowIdleAnimation;

    public int StandAloneActivateScore => standaloneActivateScore;
    public SpawnTypeEnum SpawnType { get; private set; }

    public override void InitializePiece(bool withinBoard, GridPosition gridPosition,
                                     bool overridePieceClearNum, int overrideClearNum,
                                     bool overridePieceColor, PieceColors overrideColors,
                                     SpawnTypeEnum spawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        base.InitializePiece(withinBoard, gridPosition, overridePieceClearNum, overrideClearNum, overridePieceColor, overrideColors, spawnTypeEnum);

        SpawnType = spawnTypeEnum;
        var useSpawnAnimation = SpawnType switch
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
    /// 独自激活
    /// </summary>
    public BombAction StandaloneActivate()
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(StandAloneActivateScore, false);  // 得分
        MarkAsUsed();                                                   // 设置状态并隐藏

        HashSet<GridPosition> nullRef = null;
        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(GridPosition);
        var bombAction = Instantiate(bombVFX, insWorldPosition, Quaternion.identity);
        bombAction.Initialize(GridPosition, false, bombExplodeDiameter, DestroySelf, ref nullRef);

        return bombAction;
    }


    /// <summary>
    /// 和FlyBomb交换激活
    /// </summary>
    public FlybombAction FlyBombActivate(FlyBomb flyBomb, GridPosition swapCompletePosition)
    {
        return flyBomb.BombActivate(this, swapCompletePosition);
    }


    /// <summary>
    /// 和Bomb交换激活
    /// </summary>
    public BombAction BombActivate(Bomb minorBomb, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(4 * StandAloneActivateScore, false);
        MarkAsUsed();                   // 设置状态并隐藏
        minorBomb.MarkAsUsed();         // 设置状态并隐藏

        Action destroyCallback = DestroySelf;
        destroyCallback += minorBomb.DestroySelf;

        HashSet<GridPosition> nullRef = null;

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);
        var bombAction = Instantiate(bombVFX, insWorldPosition, Quaternion.identity);
        bombAction.Initialize(swapCompletePosition, true, greatBombExplodeDiameter, destroyCallback, ref nullRef);

        return bombAction;
    }


    /// <summary>
    /// 和Rocket交换激活
    /// </summary>
    public RocketBombCombineAction RocketActivate(Rocket rocket, GridPosition swapCompletePosition)
    {
        return rocket.BombActivate(this, swapCompletePosition);
    }



    /// <summary>
    /// 和Rainbow交换激活
    /// </summary>
    public RainbowAction RainbowActivate(Rainbow rainbow, GridPosition swapCompletePosition)
    {
        return rainbow.BombActivate(this, swapCompletePosition);
    }


    /// <summary>
    /// 由RainbowAction激活
    /// </summary>
    public BombAction RainbowActionActivate(ref HashSet<GridPosition> preClearedPositions)
    {
        if (Used) { return null; }

        GameManager.instance.AddScore(StandAloneActivateScore, false);
        MarkAsUsed();   // 设置状态并隐藏

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(GridPosition);
        var bombAction = Instantiate(bombVFX, insWorldPosition, Quaternion.identity);
        bombAction.Initialize(GridPosition, false, bombExplodeDiameter, DestroySelf, ref preClearedPositions);

        return bombAction;
    }
}
