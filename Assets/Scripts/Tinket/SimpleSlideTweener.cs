using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSlideTweener : MonoBehaviour
{
    [SerializeField] GameObject parentObject;
    [SerializeField] Transform fromPos;
    [SerializeField] Ease easeIn;
    [SerializeField] Transform toPos;
    [SerializeField] Ease easeOut;
    [SerializeField] float tweenTime;
    [SerializeField] float delayTime;

    public float DelayTime => delayTime;

    public void SetTweenTime(float time)
    {
        tweenTime = time;
    }

    public void SetDelayTime(float time)
    {
        delayTime = time;
    }

    public async UniTask TweenIn()
    {
        if (parentObject != null)
            parentObject?.SetActive(true);
        transform.position = fromPos.position;
        await transform.DOLocalMove(Vector3.zero, tweenTime).SetDelay(delayTime).SetEase(easeIn);
    }

    public async UniTask TweenIn(float delayTime)
    {
        if (parentObject != null)
            parentObject?.SetActive(true);
        transform.position = fromPos.position;
        await transform.DOLocalMove(Vector3.zero, tweenTime).SetDelay(delayTime).SetEase(easeIn);
    }

    public async UniTask TweenOut()
    {
        transform.localPosition = Vector3.zero;
        await transform.DOMove(toPos.position, tweenTime).SetDelay(delayTime).SetEase(easeOut);
        if (parentObject != null)
            parentObject?.SetActive(false);
    }

    public async UniTask TweenOut(float delayTime)
    {
        transform.localPosition = Vector3.zero;
        await transform.DOMove(toPos.position, tweenTime).SetDelay(delayTime).SetEase(easeOut);
        if (parentObject != null)
            parentObject?.SetActive(false);
    }

    public void ClearTween()
    {
        transform.DOComplete();
    }

    public void ShowMe()
    {
        if (parentObject != null)
            parentObject.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void HideMe()
    {
        if (parentObject != null)
            parentObject.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
