using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BuildingInfo
{
    public string assetName;
    public string transformParent;
    public string boneName;
    public Vector3 VFXOffset;
    public Vector3 ButtonOffset;
    public int[] buildCost;
}
