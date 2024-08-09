using Spine.Unity;
using System;
using Animation = Spine.Animation;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Crate : Piece
{
    [Header("Animations")]
    [SerializeField] private AnimationReferenceAsset idleAnimation_1;
    [SerializeField] private AnimationReferenceAsset idleAnimation_2;
    [SerializeField] private AnimationReferenceAsset idleAnimation_3;
    [SerializeField] private AnimationReferenceAsset idleAnimation_4;

    [SerializeField] private AnimationReferenceAsset clickAnimation_1;
    [SerializeField] private AnimationReferenceAsset clickAnimation_2;
    [SerializeField] private AnimationReferenceAsset clickAnimation_3;
    [SerializeField] private AnimationReferenceAsset clickAnimation_4;

    [SerializeField] private AnimationReferenceAsset damageAnimation_1;
    [SerializeField] private AnimationReferenceAsset damageAnimation_2;
    [SerializeField] private AnimationReferenceAsset damageAnimation_3;
    [SerializeField] private AnimationReferenceAsset damageAnimation_4;

    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_1;
    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_2;
    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_3;
    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_4;
    

    public override void InitializePiece(bool withinBoard, GridPosition gridPosition,
                                        bool overridePieceClearNum, int overrideClearNum,
                                        bool overridePieceColor, PieceColors overrideColors,
                                        SpawnTypeEnum spawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        base.InitializePiece(withinBoard, gridPosition,
                             overridePieceClearNum, overrideClearNum,
                             overridePieceColor, overrideColors,
                             spawnTypeEnum);

        var idleAnimation = GetIdleAnimation().Animation;
        SkeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);

        // 注册相邻合成回调
        GameBoardManager.instance.RegisterAdjacentDamagedCallback(GridPosition + GridPosition.Up, AdjacentDamage);
        GameBoardManager.instance.RegisterAdjacentDamagedCallback(GridPosition + GridPosition.Right, AdjacentDamage);
        GameBoardManager.instance.RegisterAdjacentDamagedCallback(GridPosition + GridPosition.Down, AdjacentDamage);
        GameBoardManager.instance.RegisterAdjacentDamagedCallback(GridPosition + GridPosition.Left, AdjacentDamage);
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
            3 => clickAnimation_3,
            4 => clickAnimation_4,
            _ => null
        };
        if (playClickAnimation != null)
        {
            SkeletonAnimation.AnimationState.SetAnimation(0, playClickAnimation, false);
        }
    }



    private AnimationReferenceAsset GetIdleAnimation() => ClearNum switch
    {
        1 => idleAnimation_1,
        2 => idleAnimation_2,
        3 => idleAnimation_3,
        4 => idleAnimation_4,
        _ => null
    };


    // Callback for adjacent damage
    private void AdjacentDamage(Damage sourceDamage)
    {
        GameBoardManager.instance.DamagePiece(sourceDamage, this, GridPosition);
    }


    public override void Damage(Damage sourceDamage,
                                Action<Vector3, AnimationReferenceAsset> onDamagePlayVFX, 
                                Action<int, Vector3> onDamageCollectTarget, 
                                Action<Piece> onDamageControlPosition)
    {
        (AnimationReferenceAsset damageAnimation, AnimationReferenceAsset damageDebrisAnimation) = ClearNum switch
        {
            1 => (damageAnimation_1, damageDebrisAnimation_1),
            2 => (damageAnimation_2, damageDebrisAnimation_2),
            3 => (damageAnimation_3, damageDebrisAnimation_3),
            4 => (damageAnimation_4, damageDebrisAnimation_4),
            _ => (null, null)
        };
        ClearNum--;

        idleAnimation = GetIdleAnimation()?.Animation;
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
            onDamageControlPosition?.Invoke(this);
        }
        else
        {
            // 删去回调
            GameBoardManager.instance.UnRegisterAdjacentDamagedCallback(GridPosition + GridPosition.Up, AdjacentDamage);
            GameBoardManager.instance.UnRegisterAdjacentDamagedCallback(GridPosition + GridPosition.Right, AdjacentDamage);
            GameBoardManager.instance.UnRegisterAdjacentDamagedCallback(GridPosition + GridPosition.Down, AdjacentDamage);
            GameBoardManager.instance.UnRegisterAdjacentDamagedCallback(GridPosition + GridPosition.Left, AdjacentDamage);

            GetOccupiedSlot().FirstOrDefault().IncreaseEnterAndLeaveLock();
            onDamageCollectTarget?.Invoke(Id, GetWorldPosition());
            onDamageControlPosition?.Invoke(this);
            GameBoardManager.instance.OnFullyDamageUnMovablePieceUpdateGameBoard();
        }
    }
}
