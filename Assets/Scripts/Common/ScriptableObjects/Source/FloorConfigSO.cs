using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

[CreateAssetMenu(fileName = "DefinedFloorConfig", menuName = "Match3 Unity SO/Create Defined Floor Config", order = 0)]
public class FloorConfigSO : ScriptableObject
{
    public float spritePixels;
    public Floor floorPrefab;
    public SpriteRenderer framePrefab;

    [SerializedDictionary("id", "sprite")]
    public SerializedDictionary<int, Sprite> SpriteDictionary;
}