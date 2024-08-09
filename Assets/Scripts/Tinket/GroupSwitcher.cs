using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GroupSwitcher : AssetSwitcher
{
    [SerializeField] AssetSwitcher[] subAssets;

    public override void SetDifficultyLevel(DifficultyLevel difficulty)
    {
        foreach (var asset in subAssets)
            asset?.SetDifficultyLevel(difficulty);
    }
}
