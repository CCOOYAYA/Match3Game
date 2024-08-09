using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameBgSwitcher : MonoBehaviour
{
    [SerializeField] Image bgImage;
    [SerializeField] Sprite[] bgSprites;

    private void Start()
    {
        UpdateBg();
    }

    public void UpdateBg()
    {
        bgImage.sprite = bgSprites[UserDataManager.CurrentSceneID - 1];
    }
}
