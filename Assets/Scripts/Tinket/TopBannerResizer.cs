using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopBannerResizer : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;

    public void Resize()
    {
        rectTransform.offsetMax = Vector2.up * (UserDataManager.TopMargin ? 0f : 84f);
    }
}
