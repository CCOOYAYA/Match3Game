using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json.Bson;
using Spine.Unity;

public class BuildUIManager : MonoBehaviour
{
    [SerializeField] private BuildButton protoButton;
    [SerializeField] private SlideTweenerGroup buttonArea;
    [SerializeField] protected SkeletonGraphic buildVFX;
    [SerializeField] InverseCameraTweener inverseCameraTweener;
    [SerializeField] GameObject starPos;
    [SerializeField] TempFlyingObject flyingStar;
    [SerializeField] TextMeshProUGUI starText;

    private List<BuildButton> buildButtons = new();

    public BuildButton InstantiateBuildButton(int buildID)
    {
        if (buildButtons.Count - 1 < buildID)
        {
            BuildButton button = Instantiate(protoButton, Vector3.zero, Quaternion.identity, buttonArea.transform);
            buildButtons.Add(button);
            return button;
        }
        else
            return buildButtons[buildID];
    }

    protected void UpdateStarCount()
    {
        starText.text = UserDataManager.Stars.ToString();
    }

    public async UniTask ShowMe()
    {
        UpdateStarCount();
        buildVFX.AnimationState.SetAnimation(0, "build_vfx", false).TrackTime = 10000f;
        buttonArea.gameObject.SetActive(true);
        foreach (var button in buildButtons)
            button.ShowMe();
        await buttonArea.TweenIn();
    }

    public async UniTask HideMe()
    {
        foreach (var button in buildButtons)
            button.HideMe();
        await buttonArea.TweenOut();
        buttonArea.gameObject.SetActive(false);
    }

    public async UniTask DoBuild(BuildArea area)
    {
        ButtonBase.Lock();
        UpdateStarCount();
        /*/
        var fStar = Instantiate<TempFlyingObject>(flyingStar, starPos.transform.position, Quaternion.identity, transform);
        fStar.SetTargetPosition(area.ButtonStarPos);
        fStar.StartBezierMoveToRewardDisplay();
        await UniTask.WaitUntil(() => fStar == null);
        */
        await HideMe();
        area.DoBuild();
        buildVFX.transform.position = area.CenterPos;
        buildVFX.AnimationState.SetAnimation(0, "build_vfx", false);
        inverseCameraTweener.CameraZoomIn(area.CenterPos, 0.3f, 2f);
        ButtonBase.Unlock();
    }
}
