using System;
using Random = System.Random;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class QuitPopupDisplayer : PopupPage
{
    [SerializeField] GameObject phase1Root;
    [SerializeField] GameObject phase2Root;
    [SerializeField] GameObject p2Part1;
    [SerializeField] GameObject p2Part2;
    [SerializeField] Image streakImage;
    [SerializeField] Sprite[] streakSprite;

    private void UpdateDisplay()
    {
        phase1Root.SetActive(true);
        phase2Root.SetActive(false);
        p2Part1.transform.DOComplete();
        p2Part1.transform.localScale = Vector3.zero;
        p2Part2.transform.DOComplete();
        p2Part2.transform.localScale = Vector3.zero;
    }

    public async void CloseCheck()
    {
        if ((0 < UserDataManager.WinStreak) && (!phase2Root.activeSelf))
        {
            ButtonBase.Lock();
            phase1Root.SetActive(false);
            phase2Root.SetActive(true);
            streakImage.sprite = streakSprite[UserDataManager.WinStreak - 1];
            await p2Part1.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
            await p2Part2.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
            ButtonBase.Unlock();
        }
        else
        {
            await PopupManager.CloseCurrentPageAsync();
            MainGameUIManager.OnLevelFailed();
        }
    }

    public override void ShowMe()
    {
        UpdateDisplay();
        base.ShowMe();
    }
}
