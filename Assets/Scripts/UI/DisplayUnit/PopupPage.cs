using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class PopupPage : MonoBehaviour
{
    [SerializeField] private AssetSwitcher switcher;
    [SerializeField] protected EnterType enterType = EnterType.None;
    [SerializeField] SlideTweenerGroup tweenIn;
    [SerializeField] SlideTweenerGroup tweenOut;

    protected enum EnterType { None, ZoomIn, Slide };

    public virtual void ShowMe()
    {
        switcher?.SetDifficultyLevel();
        TweenPage();
        gameObject.SetActive(true);
        if (tweenIn != null)
            tweenIn.TweenIn().Forget();
    }

    protected virtual void TweenPage()
    {
        if (gameObject.activeSelf)
            return;
        if (enterType == EnterType.ZoomIn)
            transform.DOScale(Vector3.one * 1.03f, 0.1f).SetLoops(2, LoopType.Yoyo);
    }

    public virtual async UniTask HideMe()
    {
        if (tweenOut != null)
            await tweenOut.TweenOut();
        gameObject.SetActive(false);
    }
}
