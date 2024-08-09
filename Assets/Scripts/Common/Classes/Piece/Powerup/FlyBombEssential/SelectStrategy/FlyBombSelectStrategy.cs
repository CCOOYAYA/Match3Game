using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public abstract class FlyBombSelectStrategy
{
    /// <summary>
    /// 选择飞弹目标
    /// </summary>
    public abstract Piece SelectTarget(Vector3 flyBombActionWorldPosition);


    /// <summary>
    /// 挑选随机的未被flyBomb选中的基础棋子
    /// </summary>
    /// <param name="slotGrid"></param>
    /// <param name="selectCount"></param>
    /// <returns></returns>
    protected IEnumerable<Piece> SelectRandomBasicPiece(int selectCount)
    {
        var slotGrid = GameBoardManager.instance.slotGrid;
        var allAvailableBasicPieces = new List<Piece>();
        foreach (var slot in slotGrid)
        {
            Piece selectPiece = slot switch
            {
                var x when x.upperPiece != null     => null,
                var x when x.piece != null          => x.piece,
                var x when x.incomingPiece != null  => x.incomingPiece,
                _                                   => null
            };

            if (selectPiece == null ||
                GameBoardManager.instance.AllowedBasicPieceIds.Contains(selectPiece.Id) == false ||
                selectPiece.SelectedToReplace ||
                selectPiece.SelectedByFlyBomb > 0)
            { 
                continue; 
            }

            // 选中棋子 != null && 选中棋子是允许的基础棋子 && 选中棋子未被Rainbow选中 && 选中棋子未被其他FlyBomb选中
            allAvailableBasicPieces.Add(selectPiece);
        }

        if (allAvailableBasicPieces.Count == 0) { return Enumerable.Empty<Piece>(); }

        var random = new Random();
        allAvailableBasicPieces = allAvailableBasicPieces.OrderBy(x => random.Next()).ToList();

        var res = new List<Piece>();
        res.AddRange(allAvailableBasicPieces.Take(selectCount));
        while (res.Count < selectCount)
        {
            res.Add(null);
        }
        return res;
    }
}
