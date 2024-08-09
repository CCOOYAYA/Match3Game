using System;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

/// <summary>
/// 包含了全部的游戏上下文设置
/// </summary>
[Serializable]
public class GameContextSettings
{
    public PieceAnimationSetting pieceAnimationSetting;
    public AssignPowerupSetting assignPowerupSetting;
    public PowerupSetting powerupSetting;
    public DynamicDifficultySetting dynamicDifficultySetting;
    public AudioSetting audioSetting;
}


[Serializable]
public class PieceAnimationSetting
{
    [Header("General")]
    public float addToEmptyPositionsDelay = 0.15f;
    public float emptyPositionsCheckTimeout = 0.25f;
    public float activatePowerupInterval = 0.05f;

    [Header("Move")]
    public float pieceMoveDelay = 0f;
    public float pieceMoveInterval = 0.05f;
    public float pieceMoveSpeed = 24f;
    public AnimationCurve pieceMoveSpeedCurve;
    public AnimationCurve pieceBouncePositionCurve;
    public AnimationCurve pieceBounceSquishXCurve;
    public AnimationCurve pieceBounceSquishYCurve;

    [Header("Swap")]
    public float pieceSwapDuration = 0.2f;
    public Ease pieceSwapEase = Ease.Flash;

    [Header("Merge")]
    public float pieceMergeDelayDuration = 0f;
    public float pieceMergeMoveDuration = 0.325f;
    public float pieceMergeMoveCompleteElapsedTime = 0.167f;
    public float pieceMergeCollectableDelayInterval = 0.05f;
    public Ease pieceMergeMoveEase = Ease.OutCubic;

    [Header("Rearrange")]
    public float pieceArrangeDelayDuration = 0f;
    public float pieceArrangeMoveDuration = 1.125f;
    public Ease pieceArrangeMoveEase = Ease.InOutBounce;
}


[Serializable]
public class AssignPowerupSetting
{
    public float assignBoostInterval = 0.15f;
    public float assignReviveInterval = 0.2f;
}


[Serializable]
public class PowerupSetting
{
    public Powerup[] registeredPowerups;
}

[Serializable]
public class DynamicDifficultySetting
{
    [Header("Basic Stragegy")]
    public int basicActivateSinceLevel;

    [Header("In-game Strategy")]
    public int inGameActivateSinceLevel;

    [Header("Close-end Strategy")]
    public int closeEndActivateSinceLevel;
    public int closeEndLastFiveActivateSinceLevel;
    public int closeEndLastThreeActivateSinceLevel;
}

[Serializable]
public class AudioSetting
{
    public AudioMixer mixer;

    public AudioSource sfxSourcePrefab;
    public AudioSource musicSourcePrefab;
}