using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine.Networking;

public class BuildAssetBundleManager : MonoBehaviour
{
    public enum LoadFrom { StreamingAsset, WebRequest };

    private Cache localCache;
    private string cachePath;
    private string testABuri = "https://res.topfire.io/match3Tr/";
    private UnityWebRequest uwr;
    private string lastABName;

    public void CacheCheck()
    {
        if (localCache.valid)
            return;
        cachePath = Path.Combine(Application.persistentDataPath, "AssetBundleCache");
        if (!Directory.Exists(cachePath))
            Directory.CreateDirectory(cachePath);
        localCache = Caching.GetCacheByPath(cachePath);
        if (!localCache.valid)
            localCache = Caching.AddCache(cachePath);
        Caching.currentCacheForWriting = localCache;
    }

    public void ClearCache()
    {
        localCache.ClearCache();
    }

    private async UniTask<AssetBundle> LoadAssetBundleFromFile(string abName)
    {
        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, "AssetBundles/" + abName));
        await UniTask.WaitUntil(() => request.isDone);
        return request.assetBundle;
    }

    private async UniTask<AssetBundle> DownLoadAssetBundleAsync(string abName)
    {
        CacheCheck();
        string uri = testABuri + abName;
        if (abName == lastABName)
        {
            await UniTask.WaitUntil(() => uwr.isDone);
            return DownloadHandlerAssetBundle.GetContent(uwr);
        }
        else
        {
            if (!uwr.isDone)
                uwr.Abort();
            abName = lastABName;
            uwr = UnityWebRequestAssetBundle.GetAssetBundle(uri, 0, 0);
            await uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(uwr.error);
                return null;
            }
            else
                return DownloadHandlerAssetBundle.GetContent(uwr);
        }
    }

    public void CacheAssetBundleAsync(string abName)
    {
        CacheCheck();
        if (abName != lastABName)
        {
            string uri = testABuri + abName;
            lastABName = abName;
            if (!uwr.isDone)
                uwr.Abort();
            uwr = UnityWebRequestAssetBundle.GetAssetBundle(uri, 0, 0);
            uwr.SendWebRequest();
        }
    }

    public async UniTask<AssetBundle> LoadBuildAssetBundle(string abName, LoadFrom loadFrom = LoadFrom.StreamingAsset)
    {
        var assetBundles = AssetBundle.GetAllLoadedAssetBundles();
        foreach (var ab in assetBundles)
            if (abName.Contains(ab.name))
                return ab;
        switch (loadFrom)
        {
            case LoadFrom.StreamingAsset:
                return await LoadAssetBundleFromFile(abName);
            case LoadFrom.WebRequest:
                return await DownLoadAssetBundleAsync(abName);
            default:
                return null;
        }
    }
}
