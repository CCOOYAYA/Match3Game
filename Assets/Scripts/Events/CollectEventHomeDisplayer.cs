using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CollectEventHomeDisplayer : MonoBehaviour
{
    [SerializeField] RectTransform progressParent;
    [SerializeField] RectTransform progressBar;
    [SerializeField] TextMeshProUGUI progressText;
    [SerializeField] Transform targetPos;
    [SerializeField] TempFlyingObject flyingObject;

    private bool fullFlag = false;
    private int totProgress;

    public void UpdateDisplay(int progress,int totProgress)
    {
        progressBar.sizeDelta = new Vector2(progressParent.sizeDelta.x * progress / totProgress, progressParent.sizeDelta.y);
        progressText.text = progress + "/" + totProgress;
    }

    public async UniTask CollectTween()
    {
        await HomeSceneUIManager.SimpleFlyingObject(flyingObject, targetPos);
    }

    public async UniTask ProgressTween(int progress,int totProgress)
    {
        this.totProgress = totProgress;
        if (fullFlag)
            progressBar.sizeDelta = Vector2.up * progressParent.sizeDelta.y;
        await progressBar.DOSizeDelta(new Vector2(progressParent.sizeDelta.x * progress / totProgress, progressParent.sizeDelta.y), 0.2f).OnUpdate(UpdateProgressText);
        progressText.text = progress + "/" + totProgress;
        fullFlag = (progress == totProgress);
    }

    private void UpdateProgressText()
    {
        int progress = Mathf.CeilToInt(totProgress * progressBar.sizeDelta.x / progressParent.sizeDelta.x);
        progressText.text = progress + "/" + totProgress;
    }
}
