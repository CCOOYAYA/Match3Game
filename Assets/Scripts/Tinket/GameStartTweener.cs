using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStartTweener : SlideTweenerGroup
{
    [SerializeField] SimpleSlideTweener tweener1;
    [SerializeField] SimpleSlideTweener tweener2;

    public override async UniTask TweenIn()
    {
        tweener1.SetTweenTime(UserDataManager.IsRetryMode ? 0f : 0.3f);
        tweener2.SetTweenTime(UserDataManager.IsRetryMode ? 0f : 0.3f);
        await base.TweenIn();
        tweener2.SetTweenTime(0.2f);
    }
}
