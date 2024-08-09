using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Refrigerator : Piece
{
    [Header("Animations")]
    [SerializeField] private AnimationReferenceAsset idleAnimation_door_closed;
    [SerializeField] private AnimationReferenceAsset idleAnimation_door_opened;
    [SerializeField] private AnimationReferenceAsset idleAnimation_popsicle;

    [SerializeField] private AnimationReferenceAsset damageAnimation_door_closed_body;
    [SerializeField] private AnimationReferenceAsset damageAnimation_door_opened_body;
    [SerializeField] private AnimationReferenceAsset damageAnimation_popsicle_other;

    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_door_closed;
    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_full_clear;
    [SerializeField] private AnimationReferenceAsset damageDebrisAnimation_popsicle;

    [SerializeField] private AnimationReferenceAsset clickAnimation_door_closed;
    [SerializeField] private AnimationReferenceAsset clickAnimation_door_opened;
    [SerializeField] private AnimationReferenceAsset clickAnimation_popsicle;

    [Header("Popsicles")]
    [SerializeField] private List<SkeletonAnimation> popsicleList;


    public List<GridPosition> GridPositions { get; private set; } = new();
    public List<GridPosition> AdjacentPositions { get; private set; } = new();
    public bool Undamaged { get; private set; }
    public HashSet<Guid> DamagedGuids { get; private set; } = new();
    


    public override void InitializePiece(bool withinBoard, GridPosition gridPosition,
                                         bool overridePieceClearNum, int overrideClearNum, 
                                         bool overridePieceColor, PieceColors overrideColors,
                                         SpawnTypeEnum spawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        base.InitializePiece(withinBoard, gridPosition,
                             overridePieceClearNum, overrideClearNum,
                             overridePieceColor, overrideColors,
                             spawnTypeEnum);

        GridPositions = new List<GridPosition>
        {
            gridPosition,
            gridPosition + GridPosition.Right,
            gridPosition + GridPosition.Down,
            gridPosition + GridPosition.DownRight
        };
        AdjacentPositions = new List<GridPosition>
        {
            gridPosition + new GridPosition(0, -1),
            gridPosition + new GridPosition(1, -1),
            gridPosition + new GridPosition(2, 0),
            gridPosition + new GridPosition(2, 1),
            gridPosition + new GridPosition(1, 2),
            gridPosition + new GridPosition(0, 2),
            gridPosition + new GridPosition(-1, 1),
            gridPosition + new GridPosition(-1, 0)
        };
        Undamaged = true;
        DamagedGuids.Clear();

        AdjacentPositions.ForEach(pos => GameBoardManager.instance.RegisterAdjacentDamagedCallback(pos, AdjacentDamage));

        SetIdleAnimation(true);
    }


    public override void PlayClickAnimation()
    {
        if (CurrentState != State.Still ||
            !SkeletonAnimation.AnimationState.GetCurrent(0).IsComplete ||
            CanUse)
        {
            return;
        }

        var bodyClickAnimation = Undamaged ? clickAnimation_door_closed : clickAnimation_door_opened;
        SkeletonAnimation.AnimationState.SetAnimation(0, bodyClickAnimation, false);

        foreach (var popsicle in popsicleList)
        {
            if (popsicle.gameObject.activeInHierarchy)
            {
                popsicle.AnimationState.SetAnimation(0, clickAnimation_popsicle, false);
            }
        }
    }


    private void AdjacentDamage(Damage sourceDamage)
    {
        GameBoardManager.instance.DamagePiece(sourceDamage, this, GridPosition);
    }


    private void SetIdleAnimation(bool apply)
    {
        idleAnimation = Undamaged ? idleAnimation_door_closed.Animation : idleAnimation_door_opened.Animation;
        if (apply)
        {
            SkeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
        }

        for (int i = 0; i < popsicleList.Count; i++)
        {
            if (Undamaged || (i + ClearNum) < popsicleList.Count)
            {
                popsicleList[i].gameObject.SetActive(false);
            }
            else
            {
                popsicleList[i].gameObject.SetActive(true);
                popsicleList[i].AnimationState.SetAnimation(0, idleAnimation_popsicle, true);
            }
        }
    }

    public override IEnumerable<Slot> GetOccupiedSlot() => 
        new List<Slot>
        {
            GameBoardManager.instance.slotGrid[GridPosition],
            GameBoardManager.instance.slotGrid[GridPosition + GridPosition.Right],
            GameBoardManager.instance.slotGrid[GridPosition + GridPosition.Down],
            GameBoardManager.instance.slotGrid[GridPosition + GridPosition.DownRight]
        };


    public override void Damage(Damage sourceDamage,
                                Action<Vector3, AnimationReferenceAsset> onDamagePlayVFX,
                                Action<int, Vector3> onDamageCollectTarget, 
                                Action<Piece> onDamageControlPosiiton)
    {
        if (!DamagedGuids.Add(sourceDamage.SourceGuid))
            return;

        if (Undamaged)
        {
            Undamaged = false;
            SetIdleAnimation(false);

            foreach (var popsicle in popsicleList)
            {
                if (popsicle.gameObject.activeInHierarchy)
                {
                    popsicle.AnimationState.SetAnimation(0, damageAnimation_popsicle_other, false);
                    popsicle.AnimationState.AddAnimation(0, idleAnimation_popsicle, true, damageAnimation_popsicle_other.Animation.Duration);
                }
            }
            SkeletonAnimation.AnimationState.SetAnimation(0, damageAnimation_door_closed_body, false);
            if (idleAnimation != null)
            {
                SkeletonAnimation.AnimationState.AddAnimation(0, idleAnimation, true, damageAnimation_door_closed_body.Animation.Duration);
            }

            onDamagePlayVFX?.Invoke(GetWorldPosition(), damageDebrisAnimation_door_closed);
            return;
        }

        int coverCount = 0, adjacentCount = 0;
        sourceDamage.DamagePositions.ForEach(pos =>
        {
            if (GridPositions.Contains(pos))
            {
                coverCount++;
            }
            else if (AdjacentPositions.Contains(pos))
            {
                adjacentCount++;
            }
        });

        int finalDamage = coverCount > 0 ? coverCount : adjacentCount;
        while (finalDamage-- > 0 && ClearNum > 0)
        {
            var popsicle = GetFirstUndamagedPopsicle();
            onDamagePlayVFX?.Invoke(popsicle.transform.position, damageDebrisAnimation_popsicle);
            onDamageCollectTarget?.Invoke(Id, GetWorldPosition());

            popsicle.gameObject.SetActive(false);
            ClearNum--;
        }

        if (ClearNum <= 0)
        {
            sourceDamage.AddToIgnorePositions(GridPositions);
            onDamagePlayVFX?.Invoke(GetWorldPosition(), damageDebrisAnimation_full_clear);

            GridPositions.ForEach(pos => GameBoardManager.instance.slotGrid[pos].IncreaseEnterAndLeaveLock());
            AdjacentPositions.ForEach(pos => GameBoardManager.instance.UnRegisterAdjacentDamagedCallback(pos, AdjacentDamage));

            onDamageControlPosiiton?.Invoke(this);
            GameBoardManager.instance.OnFullyDamageUnMovablePieceUpdateGameBoard();
        }
        else
        {
            foreach (var popsicle in popsicleList)
            {
                if (popsicle.gameObject.activeInHierarchy)
                {
                    popsicle.AnimationState.SetAnimation(0, damageAnimation_popsicle_other, false);
                    popsicle.AnimationState.AddAnimation(0, idleAnimation_popsicle, true, damageAnimation_popsicle_other.Animation.Duration);
                }
            }

            SkeletonAnimation.AnimationState.SetAnimation(0, damageAnimation_door_opened_body, false);
            SkeletonAnimation.AnimationState.AddAnimation(0, idleAnimation_door_opened, true, damageAnimation_door_opened_body.Animation.Duration);

            onDamageControlPosiiton?.Invoke(this);
        }
    }


    private SkeletonAnimation GetFirstUndamagedPopsicle() => popsicleList[popsicleList.Count - ClearNum];
}
