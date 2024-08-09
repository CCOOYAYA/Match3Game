using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : Powerup
{
    [SerializeField] private int standaloneActivateScore;
    [SerializeField] private int bombExplodeDiameter = 5;       // ��ͨ��ը��Χ: 5x5
    [SerializeField] private int greatBombExplodeDiameter = 9;  // �ϳɱ�ը��Χ: 9x9
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
        SkeletonAnimation.AnimationState.SetAnimation(0, useSpawnAnimation, false);     // �������ɶ���
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
    /// ���Լ���
    /// </summary>
    public BombAction StandaloneActivate()
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(StandAloneActivateScore, false);  // �÷�
        MarkAsUsed();                                                   // ����״̬������

        HashSet<GridPosition> nullRef = null;
        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(GridPosition);
        var bombAction = Instantiate(bombVFX, insWorldPosition, Quaternion.identity);
        bombAction.Initialize(GridPosition, false, bombExplodeDiameter, DestroySelf, ref nullRef);

        return bombAction;
    }


    /// <summary>
    /// ��FlyBomb��������
    /// </summary>
    public FlybombAction FlyBombActivate(FlyBomb flyBomb, GridPosition swapCompletePosition)
    {
        return flyBomb.BombActivate(this, swapCompletePosition);
    }


    /// <summary>
    /// ��Bomb��������
    /// </summary>
    public BombAction BombActivate(Bomb minorBomb, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(4 * StandAloneActivateScore, false);
        MarkAsUsed();                   // ����״̬������
        minorBomb.MarkAsUsed();         // ����״̬������

        Action destroyCallback = DestroySelf;
        destroyCallback += minorBomb.DestroySelf;

        HashSet<GridPosition> nullRef = null;

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);
        var bombAction = Instantiate(bombVFX, insWorldPosition, Quaternion.identity);
        bombAction.Initialize(swapCompletePosition, true, greatBombExplodeDiameter, destroyCallback, ref nullRef);

        return bombAction;
    }


    /// <summary>
    /// ��Rocket��������
    /// </summary>
    public RocketBombCombineAction RocketActivate(Rocket rocket, GridPosition swapCompletePosition)
    {
        return rocket.BombActivate(this, swapCompletePosition);
    }



    /// <summary>
    /// ��Rainbow��������
    /// </summary>
    public RainbowAction RainbowActivate(Rainbow rainbow, GridPosition swapCompletePosition)
    {
        return rainbow.BombActivate(this, swapCompletePosition);
    }


    /// <summary>
    /// ��RainbowAction����
    /// </summary>
    public BombAction RainbowActionActivate(ref HashSet<GridPosition> preClearedPositions)
    {
        if (Used) { return null; }

        GameManager.instance.AddScore(StandAloneActivateScore, false);
        MarkAsUsed();   // ����״̬������

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(GridPosition);
        var bombAction = Instantiate(bombVFX, insWorldPosition, Quaternion.identity);
        bombAction.Initialize(GridPosition, false, bombExplodeDiameter, DestroySelf, ref preClearedPositions);

        return bombAction;
    }
}
