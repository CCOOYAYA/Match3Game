using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] TopBannerResizer topArea;
    [SerializeField] UserRankDisplayer protoRankDisplayer;
    [SerializeField] UserRankDisplayer playerRankDisplayer;
    [SerializeField] RectTransform viewPort;
    [SerializeField] RectTransform content;
    [SerializeField] GameObject topButton;
    [SerializeField] GameObject bottomButton;

    private float hideTime = 0;
    private Vector3 bottomPos;
    private List<UserRankDisplayer> userRanks = new();
    private UserRankDisplayer playerRankDisplayerInList;

    [SerializeField] string[] botNameList;
    private string RandomName => botNameList[UnityEngine.Random.Range(0, botNameList.Length)];

    private void Update()
    {
        if (hideTime < Time.time)
        {
            topButton.SetActive(false);
            bottomButton.SetActive(false);
            hideTime = Time.time + 36000f;
        }
        if (playerRankDisplayerInList == null)
            return;
        if (playerRankDisplayerInList.transform.position.y < playerRankDisplayer.transform.position.y)
        {
            playerRankDisplayerInList.HideMe();
            playerRankDisplayer.gameObject.SetActive(true);
        }
        else
        {
            playerRankDisplayerInList.ShowMe();
            playerRankDisplayer.gameObject.SetActive(false);
        }
    }

    public void InitMe()
    {
        topArea.Resize();
        TestBoardInit();
    }

    public void UpdateUserInfo()
    {
        playerRankDisplayer.UpdateUserInfo();
        playerRankDisplayerInList.UpdateUserInfo();
    }

    public void TestBoardInit()
    {
        int topLevel = Math.Min(UserDataManager.NextLevelID + 8, UserDataManager.MaxLevel - 1);
        for (int i = 0; i < 10; i++)
        {
            var newRankDisplayer = Instantiate<UserRankDisplayer>(protoRankDisplayer, content);
            int level = topLevel - i;
            if (UserDataManager.NextLevelID - 1 == level)
            {
                newRankDisplayer.UpdateDisplay(i + 1, UserDataManager.UserAvatarID, UserDataManager.UserAvatarFrameID, UserDataManager.UserName, level, true);
                playerRankDisplayer.UpdateDisplay(i + 1, UserDataManager.UserAvatarID, UserDataManager.UserAvatarFrameID, UserDataManager.UserName, level, true);
                playerRankDisplayerInList = newRankDisplayer;
            }
            else
                newRankDisplayer.UpdateDisplay(i + 1, UnityEngine.Random.Range(0, 6), UnityEngine.Random.Range(0, 6), RandomName, level);
        }
        for (int i = 0; i < 10; i++)
        {
            var newRankDisplayer = Instantiate<UserRankDisplayer>(protoRankDisplayer, content);
            newRankDisplayer.UpdateDisplay(i + 11, UnityEngine.Random.Range(0, 6), UnityEngine.Random.Range(0, 6), RandomName, 0);
        }
        topButton.SetActive(false);
        bottomButton.SetActive(false);
    }

    public void OnDrag()
    {
        bottomPos = Vector3.up * (content.rect.height - viewPort.rect.height);
        topButton.SetActive(5f < content.localPosition.y);
        bottomButton.SetActive(content.localPosition.y < bottomPos.y - 5f);
        hideTime = Time.time + 2f;
    }

    public async void GoTop()
    {
        ButtonBase.Lock();
        await content.DOLocalMove(Vector3.zero,0.2f);
        ButtonBase.Unlock();
        OnDrag();
    }

    public async void GoPlayerPos()
    {
        bottomPos = Vector3.up * (content.rect.height - viewPort.rect.height);
        Vector3 playerPos = Vector3.up * Math.Min(viewPort.rect.height * -0.5f - playerRankDisplayerInList.transform.localPosition.y, bottomPos.y);
        ButtonBase.Lock();
        await content.DOLocalMove(playerPos, 0.2f);
        ButtonBase.Unlock();
        bottomButton.SetActive(false);
        OnDrag();
    }

    public async void GoBottom()
    {
        bottomPos = Vector3.up * (content.rect.height - viewPort.rect.height);
        ButtonBase.Lock();
        await content.DOLocalMove(bottomPos, 0.2f);
        ButtonBase.Unlock();
        bottomButton.SetActive(false);
        OnDrag();
    }
}