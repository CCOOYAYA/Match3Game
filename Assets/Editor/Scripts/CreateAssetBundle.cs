using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateAssetBundle : MonoBehaviour
{
    [MenuItem("AssetBundles/Build AssetBundles(None)")]
    static void BuildAssetBundle_None()
    {
        string dir = Application.streamingAssetsPath + "/AssetBundles";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }

    [MenuItem("AssetBundles/Build AssetBundles(Uncompressed_Android)")]
    static void AndroidBuildAssetBundle_None()
    {
        string dir = Application.streamingAssetsPath + "/AssetBundles";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.Android);
    }

    [MenuItem("AssetBundles/Build AssetBundles(Compressed_LZ4_Android)")]
    static void AndroidBuildAssetBundle_LZ4()
    {
        string dir = Application.streamingAssetsPath + "/AssetBundles";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.Android);
    }

    [MenuItem("AssetBundles/Build AssetBundles(Compressed_LZMA_Android)")]
    static void AndroidBuildAssetBundle_LZMA()
    {
        string dir = Application.streamingAssetsPath + "/AssetBundles";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.None, BuildTarget.Android);
    }
}
