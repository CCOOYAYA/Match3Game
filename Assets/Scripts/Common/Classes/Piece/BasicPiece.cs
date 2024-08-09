using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BasicPiece : Piece
{
    [Header("Audio")]
    [SerializeField]        private AudioSource damageAudio;

    [Header("Animaitons")]
    [SerializeField]        private AnimationReferenceAsset hintAnimation;
    [SerializeField]        private AnimationReferenceAsset rainbowIdleAnimation;
    [SerializeField]        private AnimationReferenceAsset damageAnimation;


    public override void InitializePiece(bool withinBoard, GridPosition gridPosition,
                                         bool overridePieceClearNum, int overrideClearNum,
                                         bool overridePieceColor, PieceColors overrideColors,
                                         SpawnTypeEnum spawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        base.InitializePiece(withinBoard, gridPosition, overridePieceClearNum, overrideClearNum, overridePieceColor, overrideColors, spawnTypeEnum);

        SkeletonAnimation.AnimationState.SetAnimation(0, spawnTypeEnum == SpawnTypeEnum.RainbowSpawn ? rainbowIdleAnimation : idleAnimation, true);
    }

    int invokeTime = 0;
    public override void OnRainbowSelect()
    {
        if (invokeTime == 0)
        {
            base.OnRainbowSelect();
            invokeTime++;
        }
        else SkeletonAnimation.AnimationState.SetAnimation(0, rainbowIdleAnimation, true);
    }


    public override void PlayHintAnimation(float duration)
    {
        if (SkeletonAnimation == null)
            return;

        SkeletonAnimation?.AnimationState.SetAnimation(0, hintAnimation, true);
        SkeletonAnimation?.AnimationState.AddAnimation(0, idleAnimation, true, duration);
    }


    public override void PlayIdleAnimation()
    {
        if (SkeletonAnimation == null)
            return;

        SkeletonAnimation?.AnimationState.SetAnimation(0, idleAnimation, true);
    }


    public override void Damage(Damage sourceDamage,
                                Action<Vector3, AnimationReferenceAsset> onDamagePlayVFX, 
                                Action<int, Vector3> onDamageCollectTarget, 
                                Action<Piece> onDamageControlPosition)
    {
        ClearNum = 0;

        GetOccupiedSlot().FirstOrDefault().IncreaseEnterAndLeaveLock();

        onDamagePlayVFX?.Invoke(GetWorldPosition(), damageAnimation);
        onDamageCollectTarget?.Invoke(Id, GetWorldPosition());
        onDamageControlPosition?.Invoke(this);
    }
}
