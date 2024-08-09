using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombAction : MonoBehaviour, IGameBoardAction
{
    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;

    [SerializeField] private AnimationReferenceAsset explodeAnimation;
    [SerializeField] private AnimationReferenceAsset explodeGreatAnimation;
    private readonly string explodeEventName = "boom";


    public Action DestroyCallback { get; protected set; }
    public GridPosition Pivot { get; private set; }
    public bool IsGreatBomb { get; private set; }
    public int ExplodeDiameter { get; private set; }
    public List<GridPosition> DamagePositions { get; private set; } = new();

    // State
    private bool completed;


    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    public void Initialize(GridPosition centerPosition, bool greatBomb, int explodeDiameter, Action destroyCallback, ref HashSet<GridPosition> preClearedPositions)
    {
        Pivot = centerPosition;
        IsGreatBomb = greatBomb;
        ExplodeDiameter = explodeDiameter;
        DestroyCallback = destroyCallback;

        if (preClearedPositions != null)
        {
            var slotGrid = GameBoardManager.instance.slotGrid;
            // 计算本次爆炸将要消除的位置, 并加入
            for (int x = centerPosition.X - explodeDiameter / 2; x <= centerPosition.X + explodeDiameter / 2; x++)
            {
                for (int y = centerPosition.Y - explodeDiameter / 2; y <= centerPosition.Y + explodeDiameter / 2; y++)
                {
                    var curPosition = new GridPosition(x, y);
                    if (GridMath.IsPositionOnGrid(slotGrid, curPosition) &&
                        preClearedPositions.Add(curPosition))
                    {
                        DamagePositions.Add(curPosition);
                    }
                }
            }
        }

        completed = false;

        SkeletonAnimation.AnimationState.SetAnimation(0, IsGreatBomb ? explodeGreatAnimation : explodeAnimation, false);
        SkeletonAnimation.AnimationState.Event += HandleBombEvent;
        SkeletonAnimation.AnimationState.Complete += SetCompleteState;
    }


    private void SetCompleteState(TrackEntry trackEntry) => completed = true;

    private void HandleBombEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == explodeEventName)
        {
            DestroyCallback?.Invoke();       // 开始爆炸前先销毁炸弹自身

            Damage sourceDamage = new();
            DamagePositions.ForEach(pos => sourceDamage.AddToDamagePositions(pos));

            if (DamagePositions.Count > 0)
            {
                DamagePositions.ForEach(pos => GameBoardManager.instance.DamageSlot(sourceDamage, pos));
            }
            else GameBoardManager.instance.BombExplode(Pivot, ExplodeDiameter);
        }
    }

    public bool Tick()
    {
        if (completed)
        {
            Destroy(gameObject);
            return false;
        }
        else return true;
    }
}
