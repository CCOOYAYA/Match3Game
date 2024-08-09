using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Rainbow : Powerup
{
    [SerializeField] private int standaloneActivateScore;
    [SerializeField] private RainbowAction rainbowVFX;

    [Header("Animation")]
    [SerializeField] private AnimationReferenceAsset boostSpawnAnimation;
    [SerializeField] private AnimationReferenceAsset streakSpawnAnimation;

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
            SpawnTypeEnum.BoostSpawn => boostSpawnAnimation,
            SpawnTypeEnum.StreakSpwan => streakSpawnAnimation,
            _ => spawnAnimation
        };
        SkeletonAnimation.AnimationState.SetAnimation(0, useSpawnAnimation, false);     // 播放生成动画
        SkeletonAnimation.AnimationState.Event += OnSpawnCompleteCallback;
    }


    public RainbowAction StandaloneActivate()
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.MutePlayerInputOnGameBoard();              // 屏蔽玩家点击
        GameManager.instance.AddScore(StandAloneActivateScore, false);  // 得分
        MarkAsUsed();                                                   // 设置状态并隐藏

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(GridPosition);

        var insAction = Instantiate(rainbowVFX, insWorldPosition, Quaternion.identity);
        insAction.Initialize(this, Constants.PieceNoneId, DestroySelf);
        return insAction;
    }

    public RainbowAction BasicPieceActivate(Piece basicPiece)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.MutePlayerInputOnGameBoard();              // 屏蔽玩家点击
        GameManager.instance.AddScore(StandAloneActivateScore, false);  // 得分
        MarkAsUsed();                                                   // 设置状态并隐藏

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(GridPosition);

        var insAction = Instantiate(rainbowVFX, insWorldPosition, Quaternion.identity);
        insAction.Initialize(this, basicPiece.Id, DestroySelf);
        return insAction;
    }

    public RainbowAction FlyBombActivate(FlyBomb flyBomb, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.MutePlayerInputOnGameBoard();
        GameManager.instance.AddScore(2 * (StandAloneActivateScore + flyBomb.StandAloneActivateScore), false);
        MarkAsUsed();
        flyBomb.MarkAsUsed();

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);

        var insAction = Instantiate(rainbowVFX, insWorldPosition, Quaternion.identity);
        insAction.Initialize(this, Constants.PieceFlyBombId, DestroySelf);
        flyBomb.DestroySelf();
        return insAction;
    }

    public RainbowAction BombActivate(Bomb bomb, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.MutePlayerInputOnGameBoard();
        GameManager.instance.AddScore(2 * (StandAloneActivateScore + bomb.StandAloneActivateScore), false);
        MarkAsUsed();
        bomb.MarkAsUsed();

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);

        var insAction = Instantiate(rainbowVFX, insWorldPosition, Quaternion.identity);
        insAction.Initialize(this, Constants.PieceBombId, DestroySelf);
        bomb.DestroySelf();
        return insAction;
    }


    public RainbowAction RocketActivate(Rocket rocket, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.MutePlayerInputOnGameBoard();
        GameManager.instance.AddScore(2 * (StandAloneActivateScore + rocket.StandAloneActivateScore), false);
        MarkAsUsed();
        rocket.MarkAsUsed();

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);

        var insAction = Instantiate(rainbowVFX, insWorldPosition, Quaternion.identity);
        insAction.Initialize(this, rocket.Id, DestroySelf);
        rocket.DestroySelf();
        return insAction;
    }

    public RainbowAction RainbowActivate(Rainbow minorRainbow, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.MutePlayerInputOnGameBoard();
        GameManager.instance.AddScore(4 * StandAloneActivateScore, false);
        MarkAsUsed();
        minorRainbow.MarkAsUsed();

        Action destroyCallback = DestroySelf;
        destroyCallback += minorRainbow.DestroySelf;
        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);
        var insAction = Instantiate(rainbowVFX, insWorldPosition, Quaternion.identity);
        insAction.Initialize(this, Constants.PieceRainbowId, destroyCallback);
        return insAction;
    }
}
