using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DefinedPieceConfig", menuName = "Match3 Unity SO/Create Defined Piece Config", order = 1)]
public class PieceConfigSO : ScriptableObject
{
    [SerializedDictionary("id", "registeredPiece")]
    public SerializedDictionary<int, RegisteredPiece> allRegisteredPieces;
}


[Serializable]
public class RegisteredPiece
{
    [Header("Piece Id")]
    public int pieceId;
    public Piece piecePrefab;


    public PieceInsArgs pieceInsArgs;
    public PieceTargetReference pieceTargetReference;
    public PieceRewardReference pieceRewardReference;
}


[Serializable]
public class PieceInsArgs
{
    public Vector3Int pieceRotation;
    public Vector2Int pieceSize;
    public PieceTags pieceTags;
    public PieceLayer pieceLayer;
    public PieceColors pieceColors;
    public int pieceClearNum;
    public bool pieceCanMove;
    public bool pieceCanUse;
}

[Serializable]
public class PieceTargetReference
{
    public int collectId;
    public GameObject pieceCollectPrefab;
    public Sprite pieceLevelTargetSprite;
    public Sprite pieceDialogTargetSprite;
}


[Serializable]
public class PieceRewardReference
{
    public uint rewardClearCoin;
}

/// <summary>
/// ���ӱ�ǩö��
/// </summary>
[Flags]
[Serializable]
public enum PieceTags
{
    Nil,
    Adjacent,
    Bottom,
    Color
}


/// <summary>
/// ���Ӳ㼶ö��
/// </summary>
[Serializable]
public enum PieceLayer
{
    Piece,
    Bottom,
    Upper
}


/// <summary>
/// ������ɫö��
/// </summary>
[Flags]
[Serializable]
public enum PieceColors
{
    Colorless = 0,          // 0 : ��ɫ
    Red = 1 << 0,           // 1 : ��ɫ
    Yellow = 1 << 1,        // 2 : ��ɫ
    Blue = 1 << 2,          // 4 : ��ɫ
    Green = 1 << 3,         // 8 : ��ɫ
    Purple = 1 << 4,        // 16: ��ɫ
}