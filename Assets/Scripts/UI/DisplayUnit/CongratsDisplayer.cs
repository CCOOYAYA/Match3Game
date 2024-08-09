using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine;
using Spine.Unity;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CongratsDisplayer : ButtonBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private Image shadow;
    [SerializeField] private Canvas topBarCanvas;
    [SerializeField] private Image clickArea;
    [SerializeField] private float receiveSkipDelay;

    // Callbacks && CancellationToken
    private float displayTime;
    private Action onAnimationComplete;
    private Action onAnimationSkip;
    private CancellationToken token;

    /// <summary>
    /// When skip is not allowed when onAnimationSkipCallback is set to null
    /// </summary>
    public void DisplayCongratulateAnimation(Action onAnimationCompleteCallback, Action onAnimationSkipCallback, CancellationToken cancellationToken = default)
    {
        gameObject.SetActive(true);
        shadow.DOColor(Color.white, 0.3f);
        animator.gameObject.SetActive(true);
        animator.Play("logo_celebrate");
        topBarCanvas.overrideSorting = true;
        topBarCanvas.sortingLayerName = "PopupUICanvas";
        topBarCanvas.sortingOrder = 200;

        displayTime = Time.time;
        onAnimationComplete = onAnimationCompleteCallback;
        onAnimationSkip = onAnimationSkipCallback;
        token = cancellationToken;

        WaitAnimationComplete();
    }

    private async void WaitAnimationComplete()
    {
        await UniTask.WaitUntil(() => !token.IsCancellationRequested &&
                                      animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);

        await MainGameUIManager.Instance.ChangeLevelTargetDisplayToRewardDisplay();

        if (!token.IsCancellationRequested)
        {
            animator.gameObject.SetActive(false);
            shadow.gameObject.SetActive(false);
            topBarCanvas.overrideSorting = false;
            onAnimationComplete?.Invoke();
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        // can skip after some delay
        if (Time.time - displayTime >= receiveSkipDelay) 
        {
            animator.gameObject.SetActive(false);
            shadow.gameObject.SetActive(false);
            topBarCanvas.overrideSorting = false;
            onAnimationComplete = null;
            MainGameUIManager.Instance.ChangeLevelTargetDisplayToRewardDisplay(true).Forget();
            onAnimationSkip?.Invoke();
        }
    }
}
