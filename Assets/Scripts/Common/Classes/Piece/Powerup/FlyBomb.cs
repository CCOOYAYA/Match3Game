using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlyBomb : Powerup
{
    [SerializeField] private int standaloneActivateScore;
    [SerializeField] private FlybombAction flybombVFX;
    [Tooltip("Instantiate count when combines with another flybomb")]
    [SerializeField] private int multiFlyBombCount = 3;

    [Header("Animation")]
    [SerializeField] private AnimationReferenceAsset rainbowSpawnAnimation;
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
        SkeletonAnimation.AnimationState.SetAnimation(0, SpawnType == SpawnTypeEnum.RainbowSpawn ? rainbowSpawnAnimation : spawnAnimation, false);     // �������ɶ���
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
    /// ��������
    /// </summary>
    public FlybombAction StandaloneActivate()
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(StandAloneActivateScore, false);  // �÷�
        MarkAsUsed();                                                   // ����״̬������

        var insAction = Instantiate(flybombVFX, GetWorldPosition(), Quaternion.identity);
        insAction.Initialize(GridPosition, Constants.PieceNoneId, DestroySelf);
        return insAction;
    }


    /// <summary>
    /// ��FlyBomb��������
    /// </summary>
    public IEnumerable<FlybombAction> FlyBombActivate(FlyBomb minorFlyBomb, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(4 * StandAloneActivateScore, false);
        MarkAsUsed();
        minorFlyBomb.MarkAsUsed();

        var takeOffWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);
        var insActions = new List<FlybombAction>();

        for (int i = 0; i < multiFlyBombCount; i++)
        {
            Action destroyCallback = null;
            if (i == multiFlyBombCount - 1)
            {
                destroyCallback += DestroySelf;
                destroyCallback += minorFlyBomb.DestroySelf;
            }

            var insAction = Instantiate(flybombVFX, takeOffWorldPosition, Quaternion.identity);
            insAction.Initialize(swapCompletePosition, Constants.PieceNoneId, destroyCallback, overwritePointDegree: true, forceEndPointDegree: i * 360f / multiFlyBombCount);
            insActions.Add(insAction);
        }
        return insActions;
    }


    /// <summary>
    /// ��Bomb��������
    /// </summary>
    public FlybombAction BombActivate(Bomb bomb, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(2 * (StandAloneActivateScore + bomb.StandAloneActivateScore), false);
        MarkAsUsed();
        bomb.MarkAsUsed();

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);

        Action destroyCallback = DestroySelf;
        destroyCallback += bomb.DestroySelf;
        var insAction = Instantiate(flybombVFX, insWorldPosition, Quaternion.identity);
        insAction.Initialize(swapCompletePosition, Constants.PieceBombId, destroyCallback);
        return insAction;
    }


    /// <summary>
    /// ��Rocket��������
    /// </summary>
    public FlybombAction RocketActivate(Rocket rocket, GridPosition swapCompletePosition)
    {
        if (Used || SelectedToReplace) { return null; }

        GameManager.instance.AddScore(2 * (StandAloneActivateScore + rocket.StandAloneActivateScore), false);
        MarkAsUsed();
        rocket.MarkAsUsed();

        var insWorldPosition = GameBoardManager.instance.GetGridPositionWorldPosition(swapCompletePosition);

        Action destroyCallback = DestroySelf;
        destroyCallback += rocket.DestroySelf;
        var insAction = Instantiate(flybombVFX, insWorldPosition, Quaternion.identity);
        insAction.Initialize(swapCompletePosition, rocket.Id, destroyCallback);
        return insAction;
    }


    /// <summary>
    /// ��Rainbow��������
    /// </summary>
    public RainbowAction RainbowActivate(Rainbow rainbow, GridPosition swapCompletePosition)
    {
        return rainbow.FlyBombActivate(this, swapCompletePosition);
    }


    /// <summary>
    /// ��RainbowAction����
    /// </summary>
    public FlybombAction RainbowActionActivate()
    {
        if (Used) { return null; }

        GameManager.instance.AddScore(StandAloneActivateScore, false);
        MarkAsUsed();

        var insAction = Instantiate(flybombVFX, GetWorldPosition(), Quaternion.identity);
        insAction.Initialize(GridPosition, Constants.PieceNoneId, DestroySelf, crossExplodeClear: false);
        return insAction;
    }
}
