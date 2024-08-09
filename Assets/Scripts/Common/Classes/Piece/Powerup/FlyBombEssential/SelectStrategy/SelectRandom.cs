using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
using UnityEngine;

/// <summary>
/// 随机寻找未完成的目标棋子作为飞弹的目标
/// </summary>
public class SelectRandom : FlyBombSelectStrategy
{
    public override Piece SelectTarget(Vector3 flyBombActionWorldPosition)
    {
        var slotGrid = GameBoardManager.instance.slotGrid;
        var targets = GameManager.LevelTarget;
        var targetIdList = targets.GetRemainTargetPiecesId();
        var bottomTargetIdList = targetIdList.Where(x => Constants.BottomPieceIds.Contains(x)).ToList();

        // 优先遍历全部位置寻找处于最上层的棋子是否为目标棋子
        // 保证每次优先选择不同的棋子
        var selectPiecesDic = new Dictionary<Piece ,int>();     // <棋子, 选择次数>
        Piece checkPiece;
        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive)
                continue;

            // 首先检查upperPiece
            checkPiece = slot.upperPiece;
            if (checkPiece != null)
            {
                if (targetIdList.Contains(checkPiece.Id) &&
                    checkPiece.SelectedToReplace == false &&
                    checkPiece.SelectedByFlyBomb <= 0)
                {
                    // 找到了位于最上层的目标棋子
                    selectPiecesDic.TryAdd(checkPiece, 1);
                }
                continue;
            }

            // 再检查bottomPiece
            checkPiece = slot.bottomPiece;
            if (checkPiece != null &&
                bottomTargetIdList.Contains(checkPiece.Id) &&
                checkPiece.SelectedToReplace == false &&
                checkPiece.SelectedByFlyBomb <= 0)
            {
                // 找到了位于最上层的目标棋子
                selectPiecesDic.TryAdd(checkPiece, 1);
            }

            // 最后检查piece
            checkPiece = slot.piece != null ? slot.piece : slot.incomingPiece != null ? slot.incomingPiece : null;
            if (checkPiece != null &&
                targetIdList.Contains(checkPiece.Id) &&
                checkPiece.SelectedToReplace == false &&
                checkPiece.SelectedByFlyBomb <= 0)
            {
                // 找到了位于最上层的目标棋子
                selectPiecesDic.TryAdd(checkPiece, 1);
            }
        }


        // 若数量不足, 则需要再选择被选择过的棋子
        // 保证选择的一定是以目标棋子为最高优先
        if (selectPiecesDic.Count <= 0)
        {
            foreach (var slot in slotGrid)
            {
                bool containsTargetPiece = true;
                while (containsTargetPiece && selectPiecesDic.Count <= 0)
                {
                    checkPiece = null;
                    if (slot.upperPiece != null &&
                        selectPiecesDic.TryGetValue(slot.upperPiece, out var upperPieceSelectcount) &&
                        slot.upperPiece.ClearNum - upperPieceSelectcount > 0)
                    {
                        // 顶层棋子仍未被完全消除
                        checkPiece = slot.upperPiece;
                    }
                    else
                    {
                        if (slot.bottomPiece != null &&
                            selectPiecesDic.TryGetValue(slot.bottomPiece, out var bottomPieceSelectCount) &&
                            slot.bottomPiece.ClearNum - bottomPieceSelectCount > 0)
                        {
                            // 底层棋子仍未被完全消除
                            checkPiece = slot.bottomPiece;
                        }

                        if (slot.piece != null &&
                            selectPiecesDic.TryGetValue(slot.piece, out var pieceSelectCount) &&
                            slot.piece.ClearNum - pieceSelectCount > 0)
                        {
                            // 中层棋子未被完全消除
                            checkPiece = slot.piece;
                        }
                        else if (slot.incomingPiece != null &&
                                 selectPiecesDic.TryGetValue(slot.incomingPiece, out var incomingPieceSelectCount) &&
                                 slot.incomingPiece.ClearNum - incomingPieceSelectCount > 0)
                        {
                            // 中层棋子(incoming)未被完全消除
                            checkPiece = slot.incomingPiece;
                        }
                    }

                    if (checkPiece != null && targetIdList.Any(x => x == checkPiece.Id))
                    {
                        // 找到了目标棋子, 记录入Dic
                        if (!selectPiecesDic.TryAdd(checkPiece, 1))
                        {
                            selectPiecesDic[checkPiece]++;
                        }
                    }
                    else containsTargetPiece = false;
                }

                // 找到了目标棋子, 中止
                if (selectPiecesDic.Count > 0) { break; }
            }
        }

        // 将dic中的棋子加入到列表中, 注意被多次消除的同一棋子会被加入多次
        var selectPieces = new List<Piece>();
        foreach (var kvp in selectPiecesDic)
        {
            for (int time = 1; time <= kvp.Value; time++)
            {
                selectPieces.Add(kvp.Key);
            }
        }

        // 若数量不足, 则需要添加基础棋子
        // 如果基础棋子数量也不足, 则添加空棋子(NULL)
        if (selectPiecesDic.Count <= 0)
        {
            selectPieces.AddRange(SelectRandomBasicPiece(1));
        }

        // 此时找到了足够数量的目标棋子, (有可能包含空棋子)
        // 打乱顺序并返回
        var random = new Random();
        var res = selectPieces
            .OrderBy(x => x == null ? 1 : 0)    // 将空棋子排最后
            .ThenBy(x => random.Next())         // 随机打乱非空棋子
            .ToList()
            .FirstOrDefault();
        return res;
    }
}
