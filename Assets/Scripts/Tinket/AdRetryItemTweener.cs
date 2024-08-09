using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdRetryItemTweener : MonoBehaviour
{
    [SerializeField] Image itemImage;
    [SerializeField] Sprite[] itemSprites;

    private Sequence itemSequence;
    private Color clearWhite = new Color(1, 1, 1, 0);
    private int itemID = 0;
    public int ItemID => 1;// itemID;

    private void ItemLoop()
    {
        if (!gameObject.activeSelf)
            return;
        itemID++;
        itemID = itemID % 3;
        itemImage.sprite = itemSprites[itemID];
        itemImage.color = clearWhite;
        itemSequence.Restart();
    }

    public void ShowMe()
    {
        transform.DOScale(Vector3.one * 1.1f, 0.6f).SetLoops(-1, LoopType.Yoyo);
        itemID = UnityEngine.Random.Range(1, 99);
        itemSequence = DOTween.Sequence();
        itemSequence.Append(itemImage.DOColor(Color.white, 0.2f));
        itemSequence.Append(itemImage.DOColor(clearWhite, 0.2f).SetDelay(0.8f));
        itemSequence.OnComplete(ItemLoop);
        itemSequence.SetAutoKill(false);
        itemSequence.Pause();
        ItemLoop();
    }

    public void HideMe()
    {
        transform.DOKill();
        itemSequence.Kill();
    }
}
