using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasInputSystem : MonoBehaviour, IInputSystem
{
    // 输入
    [SerializeField]    private Canvas _canvas;
    [SerializeField]    private Camera _camera;
    [SerializeField]    private EventTrigger _eventTrigger;

                        public event EventHandler<PointerEventArgs> PointerDown;
                        public event EventHandler<PointerEventArgs> PointerDrag;
                        public event EventHandler<PointerEventArgs> PointerUp;



    // Camera Rescaler
    private readonly float referenceResolutionWidth = 1080f;
    private readonly float referenceResolutionHeight = 1920f;
    private readonly float pixelsPerUnit = 100f;


    // (一半的)屏幕宽度(世界坐标)
    public float ScreenWidth => _camera.orthographicSize / _canvas.pixelRect.height  * _canvas.pixelRect.width;
    // (一半的)屏幕高度(世界坐标)
    public float ScreenHeight => _camera.orthographicSize;

    private void Awake()
    {
        // 进行摄像机大小的适配
        _camera.orthographicSize = (referenceResolutionHeight / pixelsPerUnit) / 2.0f;
        float referenceRatio = referenceResolutionHeight / referenceResolutionWidth;
        float ratio = (float)Screen.height / Screen.width;

        if (ratio < referenceRatio)
        {
            // Heigth
            _camera.orthographicSize *= referenceRatio / ratio;
        }
        else
        {
            // Width
            _camera.orthographicSize *= ratio / referenceRatio;
        }


        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener(data => { OnPointerDown((PointerEventData)data); });

        var pointerDrag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        pointerDrag.callback.AddListener(data => { OnPointerDrag((PointerEventData)data); });

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener(data => { OnPointerUp((PointerEventData)data); });

        _eventTrigger.triggers.Add(pointerDown);
        _eventTrigger.triggers.Add(pointerDrag);
        _eventTrigger.triggers.Add(pointerUp);
    }

    private void OnPointerDown(PointerEventData e)
    {
        PointerDown?.Invoke(this, GetPointerEventArgs(e));
    }

    private void OnPointerDrag(PointerEventData e)
    {
        PointerDrag?.Invoke(this, GetPointerEventArgs(e));
    }

    private void OnPointerUp(PointerEventData e)
    {
        PointerUp?.Invoke(this, GetPointerEventArgs(e));
    }

    private PointerEventArgs GetPointerEventArgs(PointerEventData e)
    {
        return new PointerEventArgs(e.button, GetWorldPosition(e.position));
    }

    private Vector2 GetWorldPosition(Vector2 screenPosition)
    {
        return _camera.ScreenToWorldPoint(screenPosition);
    }
}