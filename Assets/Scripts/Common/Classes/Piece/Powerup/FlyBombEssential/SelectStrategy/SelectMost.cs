using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// ��Ѱ�ҹ���Ѱ����Χ�������Ŀ�����ӵ��ұ�����Ŀ�����ӵ�����
/// </summary>
public class SelectMost : FlyBombSelectStrategy, IDisposable
{
    private SelectType _type;

    public enum SelectType
    {
        Undefined,  // δ����״̬
        Row,        // һ�� (HRocket)
        Column,     // һ�� (VRocket)
        Square      // 5x5 (Bomb)
    }

    public void SetSelectType(SelectType type) => _type = type;

    public override Piece SelectTarget(Vector3 flyBombActionWorldPosition)
    {
        if (_type == SelectType.Undefined)
        {
            throw new InvalidOperationException("Call SetSelectType(SelectType type) before to specify which area you want to serach for");
        }

        var slotGrid = GameBoardManager.instance.slotGrid;
        var targets = GameManager.LevelTarget;
        var targetIdList = targets.GetRemainTargetPiecesId();
        var bottomTargetIdList = targetIdList.Where(x => Constants.BottomPieceIds.Contains(x)).ToList();

        var selectPieces = new List<Piece>();
        Piece checkPiece;

        if (_type == SelectType.Row)
        {
            // Ѱ�Ұ������Ŀ�����ӵ�һ��
            for (int i = 0; i < slotGrid.YMax; i++)
            {
                var rowSelectPieces = new List<Piece>();
                for (int j = 0; j < slotGrid.XMax; j++)
                {
                    var pos = new GridPosition(j, i);
                    if (GridMath.IsPositionOnBoard(slotGrid, pos, out Slot checkSlot))
                    {
                        // ����ϲ������
                        checkPiece = checkSlot.upperPiece;
                        if (checkPiece != null)
                        {
                            if (targetIdList.Contains(checkPiece.Id) &&
                                checkPiece.SelectedToReplace == false)
                            {
                                rowSelectPieces.Add(checkPiece);
                            }
                            continue;
                        }

                        // ���ײ������
                        checkPiece = checkSlot.bottomPiece;
                        if (checkPiece != null &&
                            bottomTargetIdList.Contains(checkPiece.Id) &&
                            checkPiece.SelectedToReplace == false)
                        {
                            rowSelectPieces.Add(checkPiece);
                        }

                        // ����м�������
                        checkPiece = checkSlot.piece != null ? checkSlot.piece : checkSlot.incomingPiece != null ? checkSlot.incomingPiece : null;
                        if (checkPiece != null &&
                            targetIdList.Contains(checkPiece.Id) &&
                            checkPiece.SelectedToReplace == false)
                        {
                            rowSelectPieces.Add(checkPiece);
                        }
                    }
                }

                if (rowSelectPieces.Count > selectPieces.Count)
                {
                    selectPieces = rowSelectPieces;
                }
            }
        }
        else if (_type == SelectType.Column)
        {
            // Ѱ�Ұ������Ŀ�����ӵ�һ��
            for (int i  = 0; i < slotGrid.XMax; i++)
            {
                var colSelectPieces = new List<Piece>();
                for (int j = 0; j < slotGrid.YMax; j++)
                {
                    var pos = new GridPosition(i, j);
                    if (GridMath.IsPositionOnBoard(slotGrid, pos, out Slot checkSlot))
                    {
                        // ����ϲ������
                        checkPiece = checkSlot.upperPiece;
                        if (checkPiece != null)
                        {
                            if (targetIdList.Contains(checkPiece.Id) &&
                                checkPiece.SelectedToReplace == false)
                            {
                                colSelectPieces.Add(checkPiece);
                            }
                            continue;
                        }

                        // ���ײ������
                        checkPiece = checkSlot.bottomPiece;
                        if (checkPiece != null &&
                            bottomTargetIdList.Contains(checkPiece.Id) &&
                            checkPiece.SelectedToReplace == false)
                        {
                            colSelectPieces.Add(checkPiece);
                        }

                        // ����м�������
                        checkPiece = checkSlot.piece != null ? checkSlot.piece : checkSlot.incomingPiece != null ? checkSlot.incomingPiece : null;
                        if (checkPiece != null &&
                            targetIdList.Contains(checkPiece.Id) &&
                            checkPiece.SelectedToReplace == false)
                        {
                            colSelectPieces.Add(checkPiece);
                        }
                    }
                }

                if (colSelectPieces.Count > selectPieces.Count)
                {
                    selectPieces = colSelectPieces;
                }
            }
        }
        else if (_type == SelectType.Square)
        {
            // Ѱ�Ұ������Ŀ�����ӵ�5x5����
            int maxCount = 0;
            foreach (var slot in slotGrid)
            {
                if (!slot.IsActive) { continue; }

                bool containsTarget = false;

                checkPiece = slot.upperPiece;
                if (checkPiece != null)
                {
                    if (targetIdList.Contains(checkPiece.Id) &&
                        checkPiece.SelectedToReplace == false)
                    {
                        containsTarget = true;
                    }
                    else continue;
                }

                if (containsTarget == false)
                {
                    checkPiece = slot.bottomPiece;
                    if (checkPiece != null &&
                        bottomTargetIdList.Contains(checkPiece.Id) &&
                        checkPiece.SelectedToReplace == false)
                    {
                        containsTarget = true;
                    }
                    else
                    {
                        checkPiece = slot.piece != null ? slot.piece : slot.incomingPiece != null ? slot.incomingPiece : null;
                        if (checkPiece != null &&
                            targetIdList.Contains(checkPiece.Id) &&
                            checkPiece.SelectedToReplace == false)
                        {
                            containsTarget = true;
                        }
                    }
                }

                if (containsTarget)
                {
                    // ֻ�б�����Ŀ�����Ӳ���Ҫ����Ѱ����Χ��Χ��
                    int count = 0;
                    int X = slot.GridPosition.X, Y = slot.GridPosition.Y;
                    Piece aroundPiece;

                    for (int i = X - 2; i <= X + 2; i++)
                    {
                        for (int j = Y - 2; j <= Y + 2; j++)
                        {
                            var pos = new GridPosition(i, j);
                            if (GridMath.IsPositionOnBoard(slotGrid, pos, out Slot checkSlot))
                            {
                                // ����ϲ������
                                aroundPiece = checkSlot.upperPiece;
                                if (aroundPiece != null)
                                {
                                    if (targetIdList.Contains(aroundPiece.Id) &&
                                        aroundPiece.SelectedToReplace == false)
                                    {
                                        count++;
                                    }
                                    continue;
                                }

                                // ���ײ������
                                aroundPiece = checkSlot.bottomPiece;
                                if (aroundPiece != null &&
                                    bottomTargetIdList.Contains(aroundPiece.Id) &&
                                    aroundPiece.SelectedToReplace == false)
                                {
                                    count++;
                                }

                                // ����м�������
                                aroundPiece = checkSlot.piece != null ? checkSlot.piece : checkSlot.incomingPiece != null ? checkSlot.incomingPiece : null;
                                if (aroundPiece != null &&
                                    targetIdList.Contains(aroundPiece.Id) &&
                                    aroundPiece.SelectedToReplace == false)
                                {
                                    count++;
                                }
                            }
                        }
                    }

                    if (count > 0)
                    {
                        if (count == maxCount)
                        {
                            selectPieces.Add(checkPiece);
                        }
                        else if (count > maxCount)
                        {
                            selectPieces.Clear();
                            selectPieces = new List<Piece> { checkPiece };
                            maxCount = count;
                        }
                    }
                }
            }
        }


        if (selectPieces.Count <= 0)
        {
            // ���δ���ҵ��κ�λ�õķ�Χ�ڰ���Ŀ������, �����ѡ��һ����������
            selectPieces = SelectRandomBasicPiece(1).ToList();
        }

        Dispose();
        var random = new Random();
        return selectPieces.OrderBy(x => random.Next()).FirstOrDefault();
    }

    public void Dispose()
    {
        _type = SelectType.Undefined;
    }
}
