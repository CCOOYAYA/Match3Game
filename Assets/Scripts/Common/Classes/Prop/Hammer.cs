using DG.Tweening;
using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hammer : MonoBehaviour, IGameBoardAction
{
    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;

    [SerializeField] 
    private Ease moveEase = Ease.Flash;
    [SerializeField]
    [Tooltip("Duration for hammer to move: will not be longer than hit animation duration")]
    private float moveDuration = 0.3f;

    [SerializeField] private AnimationReferenceAsset hammerMoveAnimation;
    [SerializeField] private AnimationReferenceAsset hammerHitAnimation;

    private readonly string hammerHitEventName = "hit";
    private GridPosition hitPosition;
    private bool completed;

    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    public void PropActivate(GridPosition hitPosition)
    {
        this.hitPosition = hitPosition;

        SkeletonAnimation.AnimationState.SetAnimation(0, hammerMoveAnimation, false);
        SkeletonAnimation.AnimationState.AddAnimation(0, hammerHitAnimation, false, hammerMoveAnimation.Animation.Duration);
        SkeletonAnimation.AnimationState.Event += HandleHitEvent;
        SkeletonAnimation.AnimationState.Complete += delegate (TrackEntry trackEntry) 
        {
            if (trackEntry.Animation.Name == hammerHitAnimation.Animation.Name)
            {
                completed = true;
            }
        };
        LerpHammerMove();
        GameManager.instance.MutePlayerInputOnGameBoard();
    }

    private void LerpHammerMove()
    {
        var endPosition = GameBoardManager.instance.GetGridPositionWorldPosition(hitPosition);
        var duration = Mathf.Min(hammerHitAnimation.Animation.Duration, moveDuration);
        transform.DOMove(endPosition, duration).SetEase(moveEase);
    }

    private void HandleHitEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == hammerHitEventName)
        {
            // 处理Hammer击中事件
            Damage sourceDamage = new Damage();
            sourceDamage.AddToDamagePositions(hitPosition);

            GameBoardManager.instance.DamageSlot(sourceDamage, hitPosition);
        }
    }

    public bool Tick()
    {
        if (completed)
        {
            // 接受用户输入
            GameManager.instance.ReceivePlayerInputOnGameBoard();

            Destroy(gameObject);
            return false;
        }

        return true;
    }
}
