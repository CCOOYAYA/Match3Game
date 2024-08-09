using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottomBannerDisplayer : MonoBehaviour
{
    [SerializeField] private PageManager pageManager;
    [SerializeField] private RectTransform parent;
    [SerializeField] private RectTransform buttonParent;

    [SerializeField] private RectTransform[] areas;
    [SerializeField] private RectTransform highLight;
    [SerializeField] private RectTransform selectLine;
    [SerializeField] private RectTransform[] icons;
    [SerializeField] private GameObject[] texts;

    [SerializeField] private float highpos;
    [SerializeField] private float lowpos;

    private Tween highlightTween = null;

    public void SetStyle()
    {
        if (!UserDataManager.BottomMargin)
        {
            parent.sizeDelta = Vector2.up * 211f;
            buttonParent.sizeDelta = Vector2.up * 211f;
        }
        else
        {
            parent.sizeDelta = Vector2.up * 249f;
            buttonParent.sizeDelta = Vector2.up * 249f;
        }
    }

    public void SetPage(int pageID, bool quickFlag)
    {
        //if ((highlightTween != null) && (highlightTween.active))
        //  return;
        highLight.DOComplete();
        selectLine.DOComplete();
        float tweenTime = quickFlag ? 0.2f : 0.8f;
        highlightTween = highLight.DOLocalMoveX(areas[pageID].localPosition.x, tweenTime).SetEase(Ease.OutQuart);
        var newSize = highLight.sizeDelta;
        newSize.x = areas[pageID].rect.width;
        highLight.DOSizeDelta(newSize, tweenTime).SetEase(Ease.OutQuart);
        selectLine.DOLocalMoveX((pageID - 1) * 360f, tweenTime).SetEase(Ease.OutQuart);
        for (int i = 0; i < 3; i++)
        {
            icons[i].DOComplete();
            if (pageID == i)
            {
                icons[i].DOLocalMoveY(highpos, 0.2f).SetEase(Ease.OutBack);
                texts[i].gameObject.SetActive(true);
            }
            else
            {
                icons[i].DOLocalMoveY(lowpos, 0.2f);
                texts[i].gameObject.SetActive(false);
            }
        }
        pageManager.PageTween(pageID);
    }

    public void SetPageButton(int pageID)
    {
        SetPage(pageID, false);
    }
}
