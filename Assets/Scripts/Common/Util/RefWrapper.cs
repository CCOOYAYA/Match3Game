public struct RefWrapper<T>
{
    public T Value { get; private set; }

    public RefWrapper(ref T value)
    {
        Value = value;
    }
}
