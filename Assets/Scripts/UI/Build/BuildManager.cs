using Cysharp.Threading.Tasks;
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

public class BuildManager : MonoBehaviour
{
    [SerializeField] BuildAssetBundleManager assetBundleManager;
    [SerializeField] BuildScene mainScene;
    [SerializeField] BuildScene backScene;
    [SerializeField] private SimpleTimer timer;
    [SerializeField] private TextMeshProUGUI sceneText;
    [SerializeField] BuildUIManager buildUIManager;

    [SerializeField] ParticleSystem newAreaEffect;
    [SerializeField] BuildCongratsDisplayer areaFinishEffect;

    private AssetBundle buildAssetBundle;
    private AssetBundle reviewAssetBundle;
    private BuildSceneInfo sceneInfo = new BuildSceneInfo();
    private BuildArea buildingArea;

    private static BuildManager Instance;

    public static int AvailableBuildCount => Instance.mainScene.AvailableBuildCount();

    public static void BuildCheck(BuildArea area, int cost)
    {
        if (UserDataManager.Stars < cost)
            HomeSceneUIManager.ShowStarHint();
        else
        {
            UserDataManager.CostStar(area.AreaID, cost);
            Instance.buildUIManager.DoBuild(area).Forget();
        }
    }

    public async void OnBuildComplete()
    {
        if (UserDataManager.TotalBuildStage == 15)
        {
            await areaFinishEffect.ShowMe();
            HomeSceneUIManager.ExitBuildView();
        }
        else
            buildUIManager.ShowMe().Forget();
    }

    public async void LoadScene(int sceneID)
    {
        Instance = this;
        timer.TimerStart();
        sceneText.text = "Scene" + UserDataManager.CurrentSceneID;

        string sceneName = "ab_scene00" + sceneID + ".ab";
        var task1 = assetBundleManager.LoadBuildAssetBundle(sceneName);
        var task2 = LoadSceneInfo(sceneID);
        var result = await UniTask.WhenAll(task1, task2);

        AssetBundle oldAsssetBundle = buildAssetBundle;
        buildAssetBundle = result.Item1;
        sceneInfo = result.Item2;

        await mainScene.LoadBuildScene(buildAssetBundle,sceneInfo);

        UserDataManager.BuildSceneLoadComplete = true;
        timer.TimerStop();

        if ((oldAsssetBundle != null)&&(oldAsssetBundle != buildAssetBundle))
            oldAsssetBundle.Unload(true);
    }

    public async void LoadNewScene(int sceneID)
    {
        timer.TimerStart();

        string sceneName = "ab_scene00" + sceneID + ".ab";
        var task1 = assetBundleManager.LoadBuildAssetBundle(sceneName);
        var task2 = LoadSceneInfo(sceneID);
        var result = await UniTask.WhenAll(task1, task2);

        AssetBundle oldAsssetBundle = buildAssetBundle;
        buildAssetBundle = result.Item1;
        sceneInfo = result.Item2;

        await backScene.LoadBuildScene(buildAssetBundle, sceneInfo);
        BuildScene tmp = mainScene;
        mainScene = backScene;
        backScene = tmp;

        mainScene.SetTransparent(0f);
        mainScene.SetSortingOrder(-10);
        backScene.SetSortingOrder(-20);

        sceneText.text = "Scene" + UserDataManager.CurrentSceneID;
        timer.TimerStop();

        await mainScene.TweenTransparent(1f);
        backScene.UnloadMe();
        if ((oldAsssetBundle != null)&&(oldAsssetBundle != buildAssetBundle))
            oldAsssetBundle.Unload(true);
    }

    public static async UniTask LoadReviewScene(int sceneID) => await Instance._LoadReviewScene(sceneID);

    private async UniTask _LoadReviewScene(int sceneID)
    {
        timer.TimerStart();

        string sceneName = "ab_scene00" + sceneID + ".ab";
        var task1 = assetBundleManager.LoadBuildAssetBundle(sceneName);
        var task2 = LoadSceneInfo(sceneID);
        var result = await UniTask.WhenAll(task1, task2);

        reviewAssetBundle = result.Item1;
        sceneInfo = result.Item2;

        await backScene.LoadBuildScene(reviewAssetBundle, sceneInfo, true);

        backScene.SetSortingOrder(-9);

        timer.TimerStop();
    }

    public static void UnloadReviewScene() => Instance._UnloadReviewScene();

    private void _UnloadReviewScene()
    {
        backScene.SetSortingOrder(-20);
        backScene.UnloadMe();
        if (reviewAssetBundle != buildAssetBundle)
            reviewAssetBundle.Unload(true);
    }

    private async UniTask<BuildSceneInfo> LoadSceneInfo(int sceneID)
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "BuildSceneConfig/BSC_" + sceneID + ".json");
        UnityWebRequest request = UnityWebRequest.Get(configPath);
        await request.SendWebRequest();
        if (request.error == null)
        {
            string content = DownloadHandlerBuffer.GetContent(request);
            return JsonConvert.DeserializeObject<BuildSceneInfo>(content);
        }
        else
            return DefaultBuildSceneInfo(sceneID);
    }

    private BuildSceneInfo DefaultBuildSceneInfo(int sceneID)
    {
        BuildSceneInfo result;
        result.assetName = "building_00" + sceneID + "_SkeletonData";
        result.SeparatorCount = 5;
        result.areaCnt = 5;
        result.SeparatorName = new string[5];
        result.buildings = new BuildingInfo[5];
        for (int i = 0; i < 5; i++)
        {
            result.SeparatorName[i] = "build_scene00" + sceneID + "_0" + (i + 1);
            result.buildings[i].assetName = "build_scene00" + sceneID + "_0" + (i + 1) + "_SkeletonData";
            result.buildings[i].transformParent = "Part[" + (i + 1) + "]";
            result.buildings[i].boneName = "build_scene00" + sceneID + "_0" + (i + 1);
            result.buildings[i].VFXOffset = Vector3.zero;
            result.buildings[i].ButtonOffset = Vector3.zero;
        }
        return result;
    }

    public static BuildButton InstantiateBuildButton(int buildID)
    {
        return Instance.buildUIManager.InstantiateBuildButton(buildID);
    }

    /// <summary>
    /// Test Functions
    /// </summary>
    public void ResetAll()
    {
        mainScene.ResetAll();
    }

    public void SaveBuildScene()
    {
        mainScene.SaveBuildScene();
    }

    public void LoadButton()
    {
        LoadScene(UserDataManager.CurrentSceneID);
    }

    public void PrevButton()
    {
        ResetAll();
        UserDataManager.PrevScene_Test();
        LoadScene(UserDataManager.CurrentSceneID);
        HomeSceneUIManager.RefreshAreaDisplay();
        HomeSceneUIManager.UpdateBuildButton();
    }

    public void NextScene(bool closePopupPage = false)
    {
        //ResetAll();
        UserDataManager.NextScene();
        LoadNewScene(UserDataManager.CurrentSceneID);
        //LoadScene(UserDataManager.CurrentSceneID);
        HomeSceneUIManager.RefreshAreaDisplay();
        HomeSceneUIManager.UpdateBuildButton();
        if (closePopupPage)
        {
            PopupManager.CloseCurrentPageAsync().Forget();
            newAreaEffect.Play();
        }
    }
}
