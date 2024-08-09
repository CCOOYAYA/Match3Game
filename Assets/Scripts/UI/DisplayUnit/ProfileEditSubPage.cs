using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ProfileEditSubPage : MonoBehaviour
{
    [SerializeField] private UnityEvent OnSelectChange;
    [SerializeField] private GameObject buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Material selectedMat;
    [SerializeField] private Material unselectedMat;
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private SimpleButton[] buttons;
    [SerializeField] private GameObject selectMark;

    public int activeButtonID = -1;

    public void Init()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int k = i;
            buttons[k].clickEvent.RemoveAllListeners();
            buttons[k].clickEvent.AddListener(() => { OnSelect(k); });
        }
        HideMe();
    }

    public void ShowMe()
    {
        contentArea.localPosition = Vector2.zero;
        selectMark.transform.position = buttons[activeButtonID].transform.position;
        buttonImage.SetActive(true);
        buttonText.material = selectedMat;
        gameObject.SetActive(true);
    }

    public void HideMe()
    {
        buttonImage.SetActive(false);
        buttonText.material = unselectedMat;
        gameObject.SetActive(false);
    }

    public void OnSelect(int buttonID)
    {
        if (buttonID == activeButtonID)
            return;
        activeButtonID = buttonID;
        selectMark.transform.position = buttons[buttonID].transform.position;
        OnSelectChange.Invoke();
    }
}
