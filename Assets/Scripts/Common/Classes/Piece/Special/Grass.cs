using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grass : Piece
{
    [Header("Animations")]
    [SerializeField] private AnimationReferenceAsset idleAnimation_1;
    [SerializeField] private AnimationReferenceAsset idleAnimation_2;

    [SerializeField] private AnimationReferenceAsset clickAnimation_1;
    [SerializeField] private AnimationReferenceAsset clickAnimation_2;

    [SerializeField] private AnimationReferenceAsset damageAnimation_1;
    [SerializeField] private AnimationReferenceAsset damageAnimation_2;

    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_1;
    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_2;

    public override void InitializePiece(bool withinBoard, GridPosition gridPosition, 
                                         bool overridePieceClearNum, int overrideClearNum, 
                                         bool overridePieceColor, PieceColors overrideColors,
                                         SpawnTypeEnum spawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        base.InitializePiece(withinBoard, gridPosition, overridePieceClearNum, overrideClearNum, overridePieceColor, overrideColors, spawnTypeEnum);

        SetIdleAnimation(true);

        GameBoardManager.instance.RegisterBottomDamagedCallback(GridPosition, DamageCenterPiece);
    }


    public override void PlayClickAnimation()
    {
        // 不处于静止状态 或者 动画未完成 或者 是powerup
        // 不能够再点击
        if (CurrentState != State.Still ||
            !SkeletonAnimation.AnimationState.GetCurrent(0).IsComplete ||
            CanUse)
        {
            return;
        }

        var playClickAnimation = ClearNum switch
        {
            1 => clickAnimation_1,
            2 => clickAnimation_2,
            _ => null
        };

        if (playClickAnimation != null)
        {
            SkeletonAnimation.AnimationState.SetAnimation(0, playClickAnimation, false);
        }
    }

    private void SetIdleAnimation(bool apply)
    {
        idleAnimation = ClearNum switch
        {
            1 => idleAnimation_1.Animation,
            2 => idleAnimation_2.Animation,
            _ => null
        };

        if (apply)
        {
            SkeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
        }
    }

    private void DamageCenterPiece(Damage sourceDamage)
    {
        GameBoardManager.instance.DamagePiece(sourceDamage, this, GridPosition);
    }

    public override void Damage(Damage sourceDamage,
                                Action<Vector3, AnimationReferenceAsset> onDamagePlayVFX, 
                                Action<int, Vector3> onDamageCollectTarget, 
                                Action<Piece> onDamagerControlPosition)
    {
        (AnimationReferenceAsset damageAnimation, AnimationReferenceAsset damageDebrisAnimation) = ClearNum switch
        {
            1 => (damageAnimation_1, damageDebrisAnimation_1),
            2 => (damageAnimation_2, damageDebrisAnimation_2),
            _ => (null, null)
        };
        ClearNum--;
        SetIdleAnimation(false);

        if (damageAnimation != null && damageDebrisAnimation != null)
        {
            SkeletonAnimation.AnimationState.SetAnimation(0, damageAnimation, false);
            if (idleAnimation != null)
            {
                SkeletonAnimation.AnimationState.AddAnimation(0, idleAnimation, true, damageAnimation.Animation.Duration);
            }
            onDamagePlayVFX?.Invoke(GetWorldPosition(), damageDebrisAnimation);
        }


        if (ClearNum > 0)
        {
            onDamagerControlPosition?.Invoke(this);
        }
        if (ClearNum <= 0)
        {
            // 删去回调
            GameBoardManager.instance.UnRegisterBottomDamagedCallback(GridPosition, DamageCenterPiece);

            GetOccupiedSlot().FirstOrDefault().IncreaseEnterAndLeaveLock();
            onDamageCollectTarget?.Invoke(Id, GetWorldPosition());
            onDamagerControlPosition?.Invoke(this);
        }
    }
}
