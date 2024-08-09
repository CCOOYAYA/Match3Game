using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildScene : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] SkeletonGraphic sceneSpine;
    [SerializeField] private BuildArea protoArea;
    [SerializeField] Material multiplyMaterial;
    [SerializeField] Material screenMaterial;

    private List<BuildArea> areaList = new List<BuildArea>();

    public async UniTask LoadBuildScene(AssetBundle assetBundle, BuildSceneInfo sceneInfo, bool reviewMode = false)
    {
        AssetBundleRequest request = assetBundle.LoadAssetAsync<SkeletonDataAsset>(sceneInfo.assetName);
        await request;
        sceneSpine.skeletonDataAsset = request.asset as SkeletonDataAsset;
        sceneSpine.allowMultipleCanvasRenderers = true;
        sceneSpine.multiplyMaterial = multiplyMaterial;
        sceneSpine.screenMaterial = screenMaterial;
        sceneSpine.enableSeparatorSlots = true;
        sceneSpine.SetAllDirty();
        sceneSpine.AnimationState.SetAnimation(0, "animation", true);
        if (areaList.Count < sceneInfo.areaCnt)
        {
            for (int i = areaList.Count; i < sceneInfo.areaCnt; i++)
            {
                var area = Instantiate(protoArea, Vector3.zero, Quaternion.identity, transform);
                areaList.Add(area);
            }
        }
        else if (sceneInfo.areaCnt < areaList.Count)
        {
            for (int i = sceneInfo.areaCnt; i < areaList.Count; i++)
            {
                areaList[i].BuildButton.gameObject.SetActive(false);
                areaList[i].transform.parent = transform;
                areaList[i].gameObject.SetActive(false);
            }
        }
        
        sceneSpine.separatorSlots.Clear();
        for (int i = 0; i < sceneInfo.SeparatorCount; i++)
        {
            var slotName = sceneInfo.SeparatorName[i];
            var slot = sceneSpine.Skeleton.FindSlot(slotName);
            sceneSpine.separatorSlots.Add(slot);
        }
        await UniTask.WaitUntil(() => sceneInfo.SeparatorCount + 1 <= sceneSpine.SeparatorParts.Count);
        foreach (var part in sceneSpine.SeparatorParts)
            part.gameObject.SetActive(true);
        
        for (int i = 0; i < sceneInfo.areaCnt; i++)
        {
            foreach (var part in sceneSpine.SeparatorParts)
            {
                if (part.name == sceneInfo.buildings[i].transformParent)
                    areaList[i].transform.SetParent(part);
            }
            float meshScale = sceneSpine.MeshScale;
            areaList[i].transform.localPosition = sceneSpine.Skeleton.FindBone(sceneInfo.buildings[i].boneName).GetLocalPosition() * meshScale;
            areaList[i].LoadMe(i, assetBundle, sceneInfo.buildings[i], reviewMode);
        }
    }

    public void SaveBuildScene()
    {
        BuildSceneInfo sceneInfo;
        sceneInfo.assetName = sceneSpine.SkeletonDataAsset.name;
        sceneInfo.SeparatorCount = sceneSpine.separatorSlots.Count;
        sceneInfo.SeparatorName = new string[sceneInfo.SeparatorCount];
        for (int i = 0; i < sceneInfo.SeparatorCount; i++)
            sceneInfo.SeparatorName[i] = sceneSpine.separatorSlots[i].ToString();
        sceneInfo.areaCnt = areaList.Count;
        sceneInfo.buildings = new BuildingInfo[sceneInfo.areaCnt];
        for (int i = 0; i < sceneInfo.areaCnt; i++)
            sceneInfo.buildings[i] = areaList[i].SaveMe();
        string path = Path.Combine(Application.persistentDataPath, "buildSceneTest.txt");
        StreamWriter writer = new StreamWriter(path, false);
        var setting = new JsonSerializerSettings();
        setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        writer.Write(JsonConvert.SerializeObject(sceneInfo, setting));
        writer.Close();
        Debug.Log("Save Success");
    }

    public void ResetAll()
    {
        foreach (var area in areaList)
            area.ResetMe();
        UserDataManager.SaveToFile();
    }

    public void UnloadMe()
    {
        sceneSpine.Clear();
        for (int i = 0; i < areaList.Count; i++)
            areaList[i].UnloadMe();
    }

    public void SetSortingOrder(int value)
    {
        canvas.sortingOrder = value;
    }

    public void SetTransparent(float alpha)
    {
        sceneSpine.color = new Color(1f, 1f, 1f, alpha);
    }

    public async UniTask TweenTransparent(float duration)
    {
        await sceneSpine.DOColor(Color.white, duration);
    }

    public int AvailableBuildCount()
    {
        int result = 0;
        for (int i = 0; i < areaList.Count; i++)
        {
            if (areaList[i].CanBuild)
                result++;
        }
        return result;
    }
}
