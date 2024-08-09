using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "AreaInfoListSO", menuName = "SO/AreaInfoListSO")]
public class AreaInfoListSO : ScriptableObject
{
    [Serializable]
    public struct AreaInfo
    {
        public LocalizedString name;
        public Sprite emptySprite;
        public Sprite completeSprite;
        public Sprite lockSprite;
    }

    public List<AreaInfo> areaInfos;
    public Sprite unlockedbg;
    public Sprite lockedbg;
    public Sprite unlockedbanner;
    public Sprite lockedbanner;
    public Material unlockedFontMat;
    public Material lockedFontMat;
    public Color unlockedNoColor;
    public Color lockedNoColor;
    public Color lockedNameColor;
}
