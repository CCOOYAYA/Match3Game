using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FontMatSwitcher : AssetSwitcher
{
    [SerializeField] TextMeshProUGUI[] texts;
    [SerializeField] Material[] mats;

    public override void SetDifficultyLevel(DifficultyLevel difficulty)
    {
        foreach (var text in texts)
            text.fontMaterial = mats[(int)difficulty];
    }
}
