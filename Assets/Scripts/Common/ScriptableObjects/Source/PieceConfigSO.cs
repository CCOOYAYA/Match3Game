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
/// 棋子标签枚举
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
/// 棋子层级枚举
/// </summary>
[Serializable]
public enum PieceLayer
{
    Piece,
    Bottom,
    Upper
}


/// <summary>
/// 棋子颜色枚举
/// </summary>
[Flags]
[Serializable]
public enum PieceColors
{
    Colorless = 0,          // 0 : 无色
    Red = 1 << 0,           // 1 : 红色
    Yellow = 1 << 1,        // 2 : 黄色
    Blue = 1 << 2,          // 4 : 蓝色
    Green = 1 << 3,         // 8 : 绿色
    Purple = 1 << 4,        // 16: 紫色
}