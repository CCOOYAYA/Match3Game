using UnityEngine;

public class Floor : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private SpriteMask _spriteMask;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteMask = GetComponent<SpriteMask>();
    }

    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    public SpriteMask SpriteMask => _spriteMask;

    public GridPosition GridPosition { get; private set; }



    public void InitializeFloor(Sprite sprite, float alpha, GridPosition gridPosition)
    {
        SpriteRenderer.sprite = sprite;
        SpriteRenderer.SetAlpha(alpha);
        GridPosition = gridPosition;
    }
}
