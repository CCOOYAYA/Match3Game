using System;

public interface IInputSystem
{
    event EventHandler<PointerEventArgs> PointerDown;
    event EventHandler<PointerEventArgs> PointerDrag;
    event EventHandler<PointerEventArgs> PointerUp;
}
