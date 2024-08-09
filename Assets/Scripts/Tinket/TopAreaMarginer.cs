using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopAreaMarginer : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    [SerializeField] float deltaPos = 84f;

    public void Resize()
    {
        rectTransform.anchoredPosition = Vector2.down * (UserDataManager.TopMargin ? deltaPos : 0f);
    }
}
