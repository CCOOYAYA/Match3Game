using Spine;
using Spine.Unity;
using System;
using UnityEngine;

public class DamageVFX : MonoBehaviour
{
    public SkeletonAnimation SkeletonAnimation { get; set; }

    public void Play(AnimationReferenceAsset damageAnimation)
    {
        SkeletonAnimation.skeletonDataAsset = damageAnimation.SkeletonDataAsset;
        SkeletonAnimation.Initialize(true);
        SkeletonAnimation.AnimationState.SetAnimation(0, damageAnimation.Animation, false);
    }
}
