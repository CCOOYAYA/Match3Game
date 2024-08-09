using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class RewardUnit : MonoBehaviour
{
    [SerializeField] Image itemImage;
    [SerializeField] Sprite[] itemSprites;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] Vector3 targetPos;
    [SerializeField] float delayTime;
    [SerializeField] float appearTime;
    [SerializeField] float flyTime;
    [SerializeField] float targetScale;

    private int propID;
    public Action<int> OnComplete;

    public void SetProp(int propID,Sprite propSprite)
    {
        this.propID = propID;
        itemImage.sprite = propSprite;
        transform.localScale = Vector3.zero;
    }

    public void SetProp(RewardType rewardType)
    {
        itemImage.sprite = itemSprites[(int)rewardType];
    }

    public void SetSize(float size)
    {
        itemImage.rectTransform.sizeDelta = Vector2.one * size;
    }

    public void SetTargetPos(Vector3 newPos)
    {
        targetPos = newPos;
    }

    public void SetDelayTime(float time)
    {
        delayTime = time;
    }

    public async UniTask Animation_Show()
    {
        _ = transform.DOMove(targetPos, appearTime).SetDelay(delayTime);
        await transform.DOScale(Vector3.one, appearTime).SetDelay(delayTime);
    }

    public async UniTask Animation_Claim()
    {
        await transform.DOScale(Vector3.one, appearTime).SetDelay(delayTime);
        _ = transform.DOScale(Vector3.one * 1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);
        await transform.DOMove(targetPos, flyTime).SetEase(Ease.InBack);
        transform.DOComplete();
        OnComplete?.Invoke(propID);
        Destroy(gameObject);
    }

    public void HideMe()
    {
        transform.localScale = Vector3.zero;
    }
}
