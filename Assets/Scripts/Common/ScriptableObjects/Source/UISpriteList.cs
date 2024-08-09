using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UISpriteList", menuName = "SO/UISpriteList")]
public class UISpriteList : ScriptableObject
{
    public Sprite[] sprites;

    public Sprite FindSpriteByID(int id)
    {
        if ((-1 < id) && (id < sprites.Length))
            return sprites[id];
        else
        {
            Debug.Log("Can't Find Sprite!");
            return null;
        }
    }
}
