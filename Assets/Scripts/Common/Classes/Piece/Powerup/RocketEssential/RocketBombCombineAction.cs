using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketBombCombineAction : MonoBehaviour, IGameBoardAction
{
    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;

    [Header("Spine Animations")]
    [SerializeField] private AnimationReferenceAsset hRocketCombineBombHorizontalAnimation;
    [SerializeField] private AnimationReferenceAsset vRocketCombineBombHorizontalAnimation;
    [SerializeField] private AnimationReferenceAsset hRocketCombineBombVerticalAnimation;
    [SerializeField] private AnimationReferenceAsset vRocketCombineBombVerticalAnimation;

    private bool completed;
    private Action callback;
    private readonly string combineEventName = "complete";

    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    public void PlayCombineAnimation(bool verticalSwap, bool rocketVertical, Action onCombineCompleteCallback)
    {
        _skeletonAnimation ??= GetComponent<SkeletonAnimation>();

        callback = onCombineCompleteCallback;
        SkeletonAnimation.AnimationState.SetAnimation(0, verticalSwap ? 
                                                         rocketVertical ? vRocketCombineBombVerticalAnimation : hRocketCombineBombVerticalAnimation :
                                                         rocketVertical ? vRocketCombineBombHorizontalAnimation : hRocketCombineBombHorizontalAnimation,
                                                         false);
        SkeletonAnimation.AnimationState.Event += HandleCombineComplete;
        SkeletonAnimation.AnimationState.Complete += delegate { completed = true; };
    }

    private void HandleCombineComplete(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == combineEventName)
        {
            callback?.Invoke();
        }
    }

    public bool Tick()
    {
        if (completed)
        {
            Destroy(gameObject);
            return false;
        }

        return true;
    }
}
