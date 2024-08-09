using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


/// <summary>
/// 对每一种id的棋子都创建对象池
/// </summary>
public class PiecePool : MonoBehaviour
{
    [SerializeField] private bool disabled;
    [SerializeField] private int initialCount;
    [SerializeField] private int maxCount;
    [SerializeField] private PiecePrefabs piecePrefabs;


    [Serializable]
    private class PiecePrefabs
    {
        [Header("Fail Callback Piece")]
        public Piece defaultPrefab;


        [Header("Basic Pieces")]
        public Piece redPrefab;
        public Piece yellowPrefab;
        public Piece bluePrefab;
        public Piece greenPrefab;
        public Piece purplePrefab;


        [Header("Special Pieces")]
        public Piece cratePrefab;
        public Piece grassPrefab;
        public Piece refrigeratorPrefab;
        //public Piece mailBoxPrefab;
        //public Piece ballPrefab;
        //public Piece sodaPrefab;
        //public Piece vasePrefab;
        //public Piece sculpturePrefab;
        //public Piece lawnPrefab;
        //public Piece butterPrefab;
        //public Piece safePrefab;
        //public Piece bridPrefab;
        //public Piece crateRedPrefab;
        //public Piece crateYellowPrefab;
        //public Piece crateBluePrefab;
        //public Piece crateGreenPrefab;
        //public Piece cratePurplePrefab;
        //public Piece marblePrefab;
        //public Piece coinjarPrefab;
        //public Piece luggagePrefab;
        //public Piece magicHatPrefab;
        //public Piece potFlowerPrefab;
        //public Piece flowerFieldPrefab;


        [Header("Powerup Pieces")]
        public Piece flyBombPrefab;
        public Piece bombPrefab;
        public Piece hRocketPrefab;
        public Piece vRocketPrefab;
        public Piece rainbowPrefab;


        //[Header("Coin")]
        //public Piece coinPrefab;
    }


    private Action<int> onReleasePieceRecordCallback;
    private Dictionary<int, ObjectPool<Piece>> poolDic;

    public void InitializePool(LevelSpawnerRule levelSpawnerRule, Action<int> onReleasePieceRecordCallback)
    {
        if (!disabled)
        {
            poolDic = new()
            {
                { Constants.PieceRedId, new ObjectPool<Piece>(CreateRed, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
                { Constants.PieceYellowId, new ObjectPool<Piece>(CreateYellow, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
                { Constants.PieceBlueId, new ObjectPool<Piece>(CreateBlue, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
                { Constants.PieceGreenId, new ObjectPool<Piece>(CreateGreen, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
                { Constants.PiecePurpleId, new ObjectPool<Piece>(CreatePurple, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },

                { Constants.PieceFlyBombId, new ObjectPool<Piece>(CreateFlyBomb, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
                { Constants.PieceBombId, new ObjectPool<Piece>(CreateBomb, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
                { Constants.PieceHRocketId, new ObjectPool<Piece>(CreateHRocket, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
                { Constants.PieceVRocketId, new ObjectPool<Piece>(CreateVRocket, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
                { Constants.PieceRainbowId, new ObjectPool<Piece>(CreateRainbow, OnGetPiece, OnReleasePiece, OnDestroyPiece, true, initialCount, maxCount) },
            };
        }

        this.onReleasePieceRecordCallback = onReleasePieceRecordCallback;
    }

    /// <summary>
    /// 在指定位置生成棋子, 并进行初始化
    /// 需要设置位置上的引用
    /// </summary>
    public Piece NewPieceAt(Vector3 position, int id, bool withinBoard, GridPosition gridPosition,
                            bool overridePieceClearNum = false, int overrideClearNum = 1,
                            bool overridePieceColor = false, PieceColors overrideColors = PieceColors.Colorless,
                            SpawnTypeEnum spawnTypeEnum = SpawnTypeEnum.NormalSpawn)
    {
        var piece = Get(id);
        piece.InitializePiece(withinBoard, gridPosition, overridePieceClearNum, overrideClearNum, overridePieceColor, overrideColors, spawnTypeEnum);
        piece.Transform.position = position;
        return piece;
    }


    /// <summary>
    /// 释放棋子
    /// </summary>
    public void ReleasePiece(Piece piece)
    {
        // 记录本局游戏中被消除的棋子
        onReleasePieceRecordCallback?.Invoke(piece.Id);

        if (!disabled)
        {
            piece.Dispose();
            poolDic[piece.Id].Release(piece);
        }
        else Destroy(piece.gameObject);
    }

    private Piece Get(int id)
    {
        if (!disabled && poolDic != null)
        {
            if (!poolDic.ContainsKey(id))
            {
                poolDic.Add(id, new ObjectPool<Piece>(id switch
                {
                    var x when x == Constants.PieceRedId => CreateRed,
                    var x when x == Constants.PieceYellowId => CreateYellow,
                    var x when x == Constants.PieceBlueId => CreateBlue,
                    var x when x == Constants.PieceGreenId => CreateGreen,
                    var x when x == Constants.PiecePurpleId => CreatePurple,

                    var x when x == Constants.PieceCrateId => CreateCrate,
                    var x when x == Constants.PieceGrassId => CreateGrass,
                    var x when x == Constants.PieceRefrigeratorId => CreateRefrigerator,

                    var x when x == Constants.PieceFlyBombId => CreateFlyBomb,
                    var x when x == Constants.PieceBombId => CreateBomb,
                    var x when x == Constants.PieceHRocketId => CreateHRocket,
                    var x when x == Constants.PieceVRocketId => CreateVRocket,
                    var x when x == Constants.PieceRainbowId => CreateRainbow,

                    _ => CreateDefault
                },
                    OnGetPiece,
                    OnReleasePiece,
                    OnDestroyPiece,
                    true,
                    initialCount,
                    maxCount));
            }

            return poolDic[id].Get();
        }
        else return id switch
        {
            var x when x == Constants.PieceRedId => CreateRed(),
            var x when x == Constants.PieceYellowId => CreateYellow(),
            var x when x == Constants.PieceBlueId => CreateBlue(),
            var x when x == Constants.PieceGreenId => CreateGreen(),
            var x when x == Constants.PiecePurpleId => CreatePurple(),

            var x when x == Constants.PieceCrateId => CreateCrate(),
            var x when x == Constants.PieceGrassId => CreateGrass(),
            var x when x == Constants.PieceRefrigeratorId => CreateRefrigerator(),

            var x when x == Constants.PieceFlyBombId => CreateFlyBomb(),
            var x when x == Constants.PieceBombId => CreateBomb(),
            var x when x == Constants.PieceHRocketId => CreateHRocket(),
            var x when x == Constants.PieceVRocketId => CreateVRocket(),
            var x when x == Constants.PieceRainbowId => CreateRainbow(),

            _ => CreateDefault()
        };
    }


    private Piece CreateRed() => Instantiate(piecePrefabs.redPrefab, transform);

    private Piece CreateYellow() => Instantiate(piecePrefabs.yellowPrefab, transform);

    private Piece CreateBlue() => Instantiate(piecePrefabs.bluePrefab, transform);

    private Piece CreateGreen() => Instantiate(piecePrefabs.greenPrefab, transform);

    private Piece CreatePurple() => Instantiate(piecePrefabs.purplePrefab, transform);


    private Piece CreateCrate() => Instantiate(piecePrefabs.cratePrefab, transform);

    private Piece CreateGrass() => Instantiate(piecePrefabs.grassPrefab, transform);

    private Piece CreateRefrigerator() => Instantiate(piecePrefabs.refrigeratorPrefab, transform);


    private Piece CreateFlyBomb() => Instantiate(piecePrefabs.flyBombPrefab, transform);

    private Piece CreateBomb() => Instantiate(piecePrefabs.bombPrefab, transform);

    private Piece CreateHRocket() => Instantiate(piecePrefabs.hRocketPrefab, transform);

    private Piece CreateVRocket() => Instantiate(piecePrefabs.vRocketPrefab, transform);

    private Piece CreateRainbow() => Instantiate(piecePrefabs.rainbowPrefab, transform);


    private Piece CreateDefault() => Instantiate(piecePrefabs.defaultPrefab, transform);



    private void OnGetPiece(Piece piece)
    {
        piece.gameObject.SetActive(true);
    }

    private void OnReleasePiece(Piece piece)
    {
        piece.gameObject.SetActive(false);
    }

    private void OnDestroyPiece(Piece piece)
    {
        Destroy(piece.gameObject);
    }


    public void ClearPoolAndDestroyAllPooledPieces()
    {
        foreach(var kvp in poolDic)
        {
            kvp.Value.Clear();
        }
        poolDic.Clear();
    }
}