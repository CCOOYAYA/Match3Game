using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleScrollRectSyncher : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] ScrollRect target;

    public void OnBeginDrag(PointerEventData eventData)
    {
        target.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        target.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        target.OnEndDrag(eventData);
    }
}
