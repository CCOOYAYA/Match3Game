using DG.Tweening;
using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildButton : SimpleButton
{
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] Image areaLight1;
    [SerializeField] Image areaLight2;
    [SerializeField] Image areaLight3;
    [SerializeField] GameObject costVFX;
    [SerializeField] Transform starPos;

    private Tween light1Tween;
    private Tween light2Tween;
    private Tween light3Tween;

    private Color hidecolor = new Color(1f, 1f, 1f, 0f);
    private Color showcolor = Color.white;

    private int buttonStage = 0;
    private Tween lightTween = null;
    private bool TweenLock = false;

    public Vector3 StarPos => starPos.position;

    // Start is called before the first frame update
    void Start()
    {
        light1Tween = areaLight1.DOFade(1, 0.15f).SetLoops(3, LoopType.Yoyo).SetAutoKill(false).Pause();
        light2Tween = areaLight2.DOFade(1, 0.15f).SetLoops(3, LoopType.Yoyo).SetAutoKill(false).Pause();
    }

    protected void SetStage0()
    {
        areaLight1.color = hidecolor;
        areaLight2.color = hidecolor;
        areaLight3.color = hidecolor;
    }

    public void SetStage(int stage,int cost, bool force = false)
    {
        buttonStage = stage;
        costText.text = cost.ToString();
        //costVFX.SetActive(cost <= UserDataManager.Stars);
        if (force)
        {
            switch (stage)
            {
                case 1:
                    areaLight1.color = showcolor;
                    areaLight2.color = hidecolor;
                    areaLight3.color = hidecolor;
                    break;
                case 2:
                    areaLight1.color = showcolor;
                    areaLight2.color = showcolor;
                    areaLight3.color = hidecolor;
                    break;
                case 3:
                    areaLight1.color = showcolor;
                    areaLight2.color = showcolor;
                    areaLight3.color = showcolor;
                    break;
                default:
                    SetStage0();
                    return;
            }
        }
        else
        {
            switch (stage)
            {
                case 1:
                    lightTween = light1Tween;
                    break;
                case 2:
                    lightTween = light2Tween;
                    break;
                default:
                    lightTween = null;
                    SetStage0();
                    return;
            }
        }
    }

    private void TweenLight()
    {
        if (lightTween == null)
            return;
        lightTween.Restart();
        lightTween = null;
    }

    public void ShowMe()
    {
        if (buttonStage == 3)
        { 
            gameObject.SetActive(false);
            return;
        }
        TweenLock = false;
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).OnComplete(TweenLight);
    }

    public void HideMe()
    {
        if (buttonStage == 3)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        TweenLock = true;
        transform.DOComplete();
        transform.localScale = Vector3.one;
        transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
    }

    protected override void ClearTween()
    {
        if (TweenLock)
            return;
        base.ClearTween();
    }
}
