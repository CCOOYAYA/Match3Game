using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideTweenerGroup : MonoBehaviour
{
    [SerializeField] public SimpleSlideTweener[] tweeners;
    [SerializeField] float stepDelay = -1f;

    private List<SimpleSlideTweener> tweenerList = new();

    public virtual async UniTask TweenIn()
    {
        if (tweenerList.Count != 0)
            tweeners = tweenerList.ToArray();
        List<UniTask> tasks = new();
        for (int i = 0; i < tweeners.Length; i++)
        {
            if (0f < stepDelay)
                tasks.Add(tweeners[i].TweenIn(stepDelay * i));
            else
                tasks.Add(tweeners[i].TweenIn());
        }
        await UniTask.WhenAll(tasks);
    }

    public async UniTask TweenOut()
    {
        if (tweenerList.Count != 0)
            tweeners = tweenerList.ToArray();
        List<UniTask> tasks = new();
        for (int i = 0; i < tweeners.Length; i++)
        {
            if (0f < stepDelay)
                tasks.Add(tweeners[i].TweenOut(stepDelay * i));
            else
                tasks.Add(tweeners[i].TweenOut());
        }
        await UniTask.WhenAll(tasks);
    }

    public void ClearTween()
    {
        for (int i = 0; i < tweeners.Length; i++)
            tweeners[i].ClearTween();
    }

    public void AddTweener(SimpleSlideTweener slideTweener)
    {
        tweenerList.Add(slideTweener);
    }

    public void ClearTweener()
    {
        tweenerList.Clear();
    }

    public void ShowMe()
    {
        for (int i = 0; i < tweeners.Length; i++)
            tweeners[i].ShowMe();
    }

    public void HideMe()
    {
        for (int i = 0; i < tweeners.Length; i++)
            tweeners[i].HideMe();
    }
}
