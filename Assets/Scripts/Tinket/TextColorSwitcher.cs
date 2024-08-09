using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextColorSwitcher : AssetSwitcher
{
    [SerializeField] TextMeshProUGUI[] texts;
    [SerializeField] Color[] colors;

    public override void SetDifficultyLevel(DifficultyLevel difficulty)
    {
        foreach (var text in texts)
            text.color = colors[((int)difficulty)];
    }
}
