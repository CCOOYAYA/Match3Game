using DG.Tweening;
using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

public class BuildArea : MonoBehaviour
{
    [SerializeField] protected SkeletonGraphic buildingTest;

    private int buildID;
    protected int areaStage = 0;
    protected int[] buildCost;
    protected BuildButton myButton;

    public string boneName;

    protected int CurrentCost => (areaStage < buildCost.Length) ? buildCost[areaStage] : 2;
    public bool CanBuild => ((areaStage < 3) && (CurrentCost <= UserDataManager.Stars));
    public Vector3 CenterPos => buildingTest.transform.position;
    public int AreaID => buildID;
    public BuildButton BuildButton => myButton;
    public Vector3 ButtonStarPos => myButton.StarPos;

    public void BuildCheck()
    {
        if (areaStage == 3)
            return;
        BuildManager.BuildCheck(this, CurrentCost);
    }

    public void DoBuild()
    {
        areaStage++;        
        myButton.SetStage(areaStage, CurrentCost);
        if (areaStage < 4)
        {
            buildingTest.AnimationState.SetAnimation(0, "ani_build_" + areaStage + "_appear", false);
            buildingTest.AnimationState.AddAnimation(0, "ani_build_" + areaStage + "_idle", true, 0f);
        }
    }

    public BuildingInfo SaveMe()
    {
        BuildingInfo result = new BuildingInfo();
        result.assetName = buildingTest.skeletonDataAsset.name;
        result.transformParent = transform.parent.name;
        result.boneName = boneName;
        result.VFXOffset = Vector3.zero;
        result.ButtonOffset = transform.InverseTransformPoint(myButton.transform.position);
        result.buildCost = buildCost;
        return result;
    }

    public void LoadMe(int buildID, AssetBundle assetBundle, BuildingInfo info, bool reviewMode = false)
    {
        this.buildID = buildID;
        buildingTest.skeletonDataAsset = assetBundle.LoadAsset<SkeletonDataAsset>(info.assetName);
        buildingTest.SetAllDirty();
        boneName = info.boneName;
        if (reviewMode)
        {
            buildingTest.AnimationState.SetAnimation(0, "ani_build_3_idle", true);
            return;
        }
        myButton = BuildManager.InstantiateBuildButton(buildID);
        myButton.clickEvent.RemoveAllListeners();
        myButton.clickEvent.AddListener(BuildCheck);
        myButton.transform.position = transform.TransformPoint(info.ButtonOffset);
        areaStage = Math.Min(UserDataManager.BuildStage[buildID], 3);
        buildCost = info.buildCost;
        if (buildCost == null)
        {
            buildCost = new int[3];
            buildCost[0] = 1;
            buildCost[1] = 2;
            buildCost[2] = 3;
        }
        if (0 < areaStage)
        {
            buildingTest.AnimationState.SetAnimation(0, "ani_build_" + areaStage + "_idle", true);
            myButton.SetStage(areaStage, CurrentCost, true);
        }
        else
            ResetMe();
    }

    public void UnloadMe()
    {
        if (myButton != null)
            myButton.transform.localScale = Vector3.zero;
        buildingTest.Clear();
    }

    public void ResetMe()
    {
        areaStage = 0;
        UserDataManager.UpdateBuildStage(buildID, 0, false);
        buildingTest.AnimationState.SetAnimation(0, "ani_build_1_null", false);
        myButton.SetStage(areaStage, CurrentCost, true);
    }
}
