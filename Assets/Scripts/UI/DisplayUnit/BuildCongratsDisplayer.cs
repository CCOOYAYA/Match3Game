using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine;
using Spine.Unity;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildCongratsDisplayer : MonoBehaviour
{
    [SerializeField] Image shadow;
    [SerializeField] GameObject banner;
    [SerializeField] GameObject character;

    public async UniTask ShowMe()
    {
        banner.transform.localScale = Vector3.up;
        gameObject.SetActive(true);
        _ = banner.transform.DOScale(Vector3.one, 0.8f).SetEase(Ease.OutBack);
        _ = character.transform.DOShakeScale(0.8f, 0.3f, 7, 90, true, ShakeRandomnessMode.Harmonic);
        await UniTask.Delay(3000);
        HideMe();
    }

    public void Test()
    {
        ShowMe().Forget();
    }

    public void HideMe()
    {
        gameObject.SetActive(false);
    }
}
