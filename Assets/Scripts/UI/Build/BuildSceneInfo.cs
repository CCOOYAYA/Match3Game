using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BuildSceneInfo
{
    public string assetName;
    public int SeparatorCount;
    public string[] SeparatorName;
    public int areaCnt;
    public BuildingInfo[] buildings;
}
