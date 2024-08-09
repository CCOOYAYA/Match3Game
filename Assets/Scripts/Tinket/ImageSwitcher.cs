using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSwitcher : AssetSwitcher
{
    [SerializeField] Image[] images;
    [SerializeField] Sprite[] sprites;

    public override void SetDifficultyLevel(DifficultyLevel difficulty)
    {
        foreach (var image in images)
            image.sprite = sprites[((int)difficulty)];
    }
}
