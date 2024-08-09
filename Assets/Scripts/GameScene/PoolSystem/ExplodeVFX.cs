using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ExplodeVFX : MonoBehaviour
{
    public SortingGroup SortingGroup { get; set; }
    public SkeletonAnimation SkeletonAnimation { get; set; }

    public void PlayExplode(AnimationReferenceAsset explodeAnimation, int order = 2)
    {
        SortingGroup.sortingOrder = order;

        SkeletonAnimation.skeletonDataAsset = explodeAnimation.SkeletonDataAsset;
        SkeletonAnimation.Initialize(true);
        SkeletonAnimation.AnimationState.SetAnimation(0, explodeAnimation.Animation, false);
    }
}
