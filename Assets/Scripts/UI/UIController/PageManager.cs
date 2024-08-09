using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PageManager : ScrollRect
{
    [SerializeField] RectTransform pageL;
    [SerializeField] RectTransform pageM;
    [SerializeField] RectTransform pageR;
    [SerializeField] Transform pageSelector;
    [SerializeField] BottomBannerDisplayer displayer;

    private int currentPage = 1;
    private Vector2 pageCenter = Vector2.zero;
    private bool quickFlag;

    public static float ScreenWidth { get; private set; }
    public static float ScreenHeight { get; private set; }

    private void Update()
    {
        //Resize();
    }

    public void Resize()
    {
        Vector2 viewSize = viewport.rect.size;
        ScreenWidth = viewSize.x;
        ScreenHeight = viewSize.y;
        pageL.sizeDelta = viewSize;
        pageL.localPosition = new Vector3(viewSize.x * -1f, 0f);
        pageM.sizeDelta = viewSize;
        pageM.localPosition = Vector3.zero;
        pageR.sizeDelta = viewSize;
        pageR.localPosition = new Vector3(viewSize.x, 0f);
        content.sizeDelta = Vector2.right * viewSize.x * 3f;
        //UserDataManager.ShortScreen = viewSize.y / viewSize.x < 16f / 9f + 0.1f;
        Rect safeArea = Screen.safeArea;
        UserDataManager.TopMargin = safeArea.yMax < ScreenHeight - 10;
        UserDataManager.BottomMargin = 10 < safeArea.yMin;

        displayer.SetStyle();
    }

    private void PageCheck()
    {
        float pageWidth = viewport.rect.size.x;
        float magicDistance = pageWidth * 0.2f;
        quickFlag = false;
        if ((magicDistance < content.localPosition.x - pageCenter.x) && (0 < currentPage))
        {
            currentPage--;
            quickFlag = false;
        }
        else if ((magicDistance < pageCenter.x - content.localPosition.x) && (currentPage < 2))
        {
            currentPage++;
            quickFlag = false;
        }
    }

    public void PageTween(int page)
    {
        float pageWidth = viewport.rect.size.x;
        currentPage = page;
        pageCenter = Vector2.left * pageWidth * (page - 1);
        float tweenTime = quickFlag ? 0.2f : 0.8f;
        content.DOLocalMove(pageCenter, tweenTime).SetEase(Ease.OutQuart);
    }

    public void PageSwitch(int page)
    {
        content.DOComplete();
        DisableMe(); //需要手动恢复
        float pageWidth = viewport.rect.size.x;
        pageCenter = Vector2.left * pageWidth * (page - 1);
        content.localPosition = pageCenter;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        if (!enabled)
            return;
        content.DOComplete();
        pageSelector.DOComplete();
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        if (!enabled)
            return;
        pageSelector.localPosition += (Vector3.left * eventData.delta.x / 3f);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        if (!enabled)
            return;
        PageCheck();
        displayer.SetPage(currentPage, quickFlag);
    }

    public void EnableMe()
    {
        enabled = true;
    }

    public void DisableMe()
    {
        enabled = false;
    }
}
