using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tmp;

    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private float showTime;
    [SerializeField] private float flyTime;
    [SerializeField] private float fadeTime;

    private Sequence textTween;

    public void Init()
    {
        textTween = DOTween.Sequence();
        textTween.Append(tmp.transform.DOScale(Vector3.one,showTime));
        textTween.Append(tmp.transform.DOMove(endPos.position, flyTime));
        textTween.Append(tmp.DOColor(Color.clear, fadeTime));
        textTween.SetAutoKill(false);
        textTween.Pause();
    }

    public void ShowText(string text)
    {
        tmp.text = text;
        tmp.transform.localScale = Vector3.zero;
        tmp.transform.position = startPos.position;
        tmp.color = Color.white;
        textTween.Restart();
    }
}
