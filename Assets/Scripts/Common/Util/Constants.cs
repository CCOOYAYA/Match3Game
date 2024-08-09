using System.Collections.Generic;
using UnityEngine;

public static partial class Constants
{
    #region Piece Id
    public static int PieceNoneId => 0;


    public static int PieceRedId => 1;
    public static int PieceYellowId => 2;
    public static int PieceBlueId => 3;
    public static int PieceGreenId => 4;
    public static int PiecePurpleId => 5;


    public static int PieceCrateId => 101;
    public static int PieceGrassId => 102;
    public static int PieceRefrigeratorId => 103;
    public static int PieceFlowerFieldId => 123;


    public static int PieceFlyBombId => 1001;
    public static int PieceBombId => 1002;
    public static int PieceHRocketId => 1003;
    public static int PieceVRocketId => 1004;
    public static int PieceRainbowId => 1005;

    public static int PieceCoinId => 9999;
    #endregion


    #region Piece Id Collection
    private readonly static HashSet<int> _basicPieceIds = new() { PieceRedId, PieceYellowId, PieceBlueId, PieceGreenId, PiecePurpleId };
    private readonly static HashSet<int> _powerupPieceIds = new() { PieceFlyBombId, PieceBombId, PieceHRocketId, PieceVRocketId, PieceRainbowId };
    private readonly static HashSet<int> _bottomPieceIds = new() { PieceGrassId, PieceFlowerFieldId };
    private readonly static HashSet<int> _largePieceIds = new() { PieceRefrigeratorId };

    public static HashSet<int> BasicPieceIds => _basicPieceIds;
    public static HashSet<int> PowerupPieceIds => _powerupPieceIds;
    public static HashSet<int> BottomPieceIds => _bottomPieceIds;
    public static HashSet<int> LargePieceIds => _largePieceIds;
    #endregion


    #region Others
    private readonly static Rect _streakBoxBound = new (-2.83f, -2.41f, 4.72f, 4.04f);


    public static Rect StreakBoxBound => _streakBoxBound;
    #endregion
}
