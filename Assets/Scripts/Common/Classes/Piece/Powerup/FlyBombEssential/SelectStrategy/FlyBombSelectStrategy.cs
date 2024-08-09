using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public abstract class FlyBombSelectStrategy
{
    /// <summary>
    /// ѡ��ɵ�Ŀ��
    /// </summary>
    public abstract Piece SelectTarget(Vector3 flyBombActionWorldPosition);


    /// <summary>
    /// ��ѡ�����δ��flyBombѡ�еĻ�������
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

            // ѡ������ != null && ѡ������������Ļ������� && ѡ������δ��Rainbowѡ�� && ѡ������δ������FlyBombѡ��
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
