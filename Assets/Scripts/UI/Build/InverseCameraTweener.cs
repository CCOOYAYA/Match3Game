using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InverseCameraTweener : MonoBehaviour
{
    [SerializeField] BuildManager manager;
    [SerializeField] Image backgroundImage;
    [SerializeField] float ZoomMultiplier = 1.3f;
    protected Vector2 imageSize, canvasSize;
    protected float xmax, ymax;

    // Start is called before the first frame update
    void Start()
    {
        imageSize = backgroundImage.rectTransform.rect.size;
        canvasSize = backgroundImage.canvas.renderingDisplaySize;
        xmax = (imageSize.x * ZoomMultiplier - canvasSize.x) * 0.5f;
        ymax = (imageSize.y * ZoomMultiplier - canvasSize.y) * 0.5f;
    }

    protected Vector2 RectClamp(Vector2 pos)
    {
        Vector2 result;
        result.x = Mathf.Clamp(pos.x, xmax * -1f, xmax);
        result.y = Mathf.Clamp(pos.y, ymax * -1f, ymax);
        return result;
    }

    public void CameraZoomIn(Vector3 center, float time, float delaytime)
    {
        Sequence sizeTweenSequence = DOTween.Sequence();
        Sequence posTweenSequence = DOTween.Sequence();
        Vector3 tmpCenter = transform.InverseTransformPoint(center);
        tmpCenter = tmpCenter * ZoomMultiplier * -1f;
        tmpCenter = RectClamp(tmpCenter);
        sizeTweenSequence.Append(transform.DOScale(ZoomMultiplier, time)).AppendInterval(delaytime).Append(transform.DOScale(1f, time).OnComplete(manager.OnBuildComplete));
        posTweenSequence.Append(transform.DOLocalMove(tmpCenter, time)).AppendInterval(delaytime).Append(transform.DOLocalMove(Vector3.zero, time));
    }
}
