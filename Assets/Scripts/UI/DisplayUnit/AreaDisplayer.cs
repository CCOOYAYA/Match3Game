using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AreaDisplayer : MonoBehaviour
{
    [SerializeField] AreaInfoListSO areaInfoSO;
    [SerializeField] Image areaImage;
    [SerializeField] Image bgImage;
    [SerializeField] Image bannerL;
    [SerializeField] Image bannerR;
    [SerializeField] TextMeshProUGUI areaName;
    [SerializeField] TextMeshProUGUI areaNo;
    [SerializeField] GameObject ActiveArea;
    [SerializeField] GameObject CompleteArea;
    [SerializeField] GameObject LockIcon;
    [SerializeField] RectTransform fullprogress;
    [SerializeField] RectTransform progress;
    [SerializeField] TextMeshProUGUI progressText;

    private int areaID;

    public void UpdateDisplay(int areaID)
    {
        if (areaInfoSO.areaInfos.Count < areaID)
            return;
        this.areaID = areaID;
        AreaInfoListSO.AreaInfo areaInfo = areaInfoSO.areaInfos[areaID - 1];
        areaName.text = areaInfo.name.GetLocalizedString();
        areaNo.text = "Area " + areaID;
        if (areaID - (UserDataManager.TotalBuildStage == 15 ? 1 : 0) < UserDataManager.CurrentSceneID)
        {
            //Complete
            areaImage.sprite = areaInfo.completeSprite;
            bgImage.sprite = areaInfoSO.unlockedbg;
            bannerL.sprite = areaInfoSO.unlockedbanner;
            bannerR.sprite = areaInfoSO.unlockedbanner;
            areaName.color = Color.white;
            areaName.enableVertexGradient = true;
            areaName.fontMaterial = areaInfoSO.unlockedFontMat;
            areaNo.color = areaInfoSO.unlockedNoColor;
            CompleteArea.SetActive(true);
            ActiveArea.SetActive(false);
            LockIcon.SetActive(false);
        }
        else if (areaID == UserDataManager.CurrentSceneID)
        {
            //Active
            areaImage.sprite = areaInfo.emptySprite;
            bgImage.sprite = areaInfoSO.unlockedbg;
            bannerL.sprite = areaInfoSO.unlockedbanner;
            bannerR.sprite = areaInfoSO.unlockedbanner;
            areaName.color = Color.white;
            areaName.enableVertexGradient = true;
            areaName.fontMaterial = areaInfoSO.unlockedFontMat;
            areaNo.color = areaInfoSO.unlockedNoColor;
            progress.sizeDelta = Vector2.right * fullprogress.sizeDelta.x * (UserDataManager.TotalBuildStage / 15f) + Vector2.up * fullprogress.sizeDelta.y;
            progressText.text = UserDataManager.TotalBuildStage + "/15";
            CompleteArea.SetActive(false);
            ActiveArea.SetActive(true);
            LockIcon.SetActive(false);
        }
        else
        {
            //Lock
            areaImage.sprite = areaInfo.lockSprite;
            bgImage.sprite = areaInfoSO.lockedbg;
            bannerL.sprite = areaInfoSO.lockedbanner;
            bannerR.sprite = areaInfoSO.lockedbanner;
            areaName.color = areaInfoSO.lockedNameColor;
            areaName.enableVertexGradient = false;
            areaName.fontMaterial = areaInfoSO.lockedFontMat;
            areaNo.color = areaInfoSO.lockedNoColor;
            CompleteArea.SetActive(false);
            ActiveArea.SetActive(false);
            LockIcon.SetActive(true);
        }
    }

    public void Review()
    {
        HomeSceneUIManager.EnterReviewView(areaID);
    }
}
