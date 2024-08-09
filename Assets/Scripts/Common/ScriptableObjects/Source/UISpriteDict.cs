using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UISpriteDict", menuName = "SO/UISpriteDict")]
public class UISpriteDict : ScriptableObject
{
    [SerializedDictionary("id", "sprite")]
    public SerializedDictionary<int, Sprite> SpriteDictionary;

    public Sprite FindSpriteByID(int id)
    {
        if (SpriteDictionary.ContainsKey(id))
            return SpriteDictionary[id];
        else
        {
            Debug.Log("Can't Find Sprite!");
            return null;
        }
    }
}
