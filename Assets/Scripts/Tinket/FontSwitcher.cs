using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FontSwitcher : AssetSwitcher
{
    [SerializeField] TextMeshProUGUI[] texts;
    [SerializeField] TMP_FontAsset[] fonts;

    public override void SetDifficultyLevel(DifficultyLevel difficulty)
    {
        foreach (var text in texts)
            text.font = fonts[((int)difficulty)];
    }
}
