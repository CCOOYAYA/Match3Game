using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
using UnityEngine;

/// <summary>
/// ���Ѱ��δ��ɵ�Ŀ��������Ϊ�ɵ���Ŀ��
/// </summary>
public class SelectRandom : FlyBombSelectStrategy
{
    public override Piece SelectTarget(Vector3 flyBombActionWorldPosition)
    {
        var slotGrid = GameBoardManager.instance.slotGrid;
        var targets = GameManager.LevelTarget;
        var targetIdList = targets.GetRemainTargetPiecesId();
        var bottomTargetIdList = targetIdList.Where(x => Constants.BottomPieceIds.Contains(x)).ToList();

        // ���ȱ���ȫ��λ��Ѱ�Ҵ������ϲ�������Ƿ�ΪĿ������
        // ��֤ÿ������ѡ��ͬ������
        var selectPiecesDic = new Dictionary<Piece ,int>();     // <����, ѡ�����>
        Piece checkPiece;
        foreach (var slot in slotGrid)
        {
            if (!slot.IsActive)
                continue;

            // ���ȼ��upperPiece
            checkPiece = slot.upperPiece;
            if (checkPiece != null)
            {
                if (targetIdList.Contains(checkPiece.Id) &&
                    checkPiece.SelectedToReplace == false &&
                    checkPiece.SelectedByFlyBomb <= 0)
                {
                    // �ҵ���λ�����ϲ��Ŀ������
                    selectPiecesDic.TryAdd(checkPiece, 1);
                }
                continue;
            }

            // �ټ��bottomPiece
            checkPiece = slot.bottomPiece;
            if (checkPiece != null &&
                bottomTargetIdList.Contains(checkPiece.Id) &&
                checkPiece.SelectedToReplace == false &&
                checkPiece.SelectedByFlyBomb <= 0)
            {
                // �ҵ���λ�����ϲ��Ŀ������
                selectPiecesDic.TryAdd(checkPiece, 1);
            }

            // �����piece
            checkPiece = slot.piece != null ? slot.piece : slot.incomingPiece != null ? slot.incomingPiece : null;
            if (checkPiece != null &&
                targetIdList.Contains(checkPiece.Id) &&
                checkPiece.SelectedToReplace == false &&
                checkPiece.SelectedByFlyBomb <= 0)
            {
                // �ҵ���λ�����ϲ��Ŀ������
                selectPiecesDic.TryAdd(checkPiece, 1);
            }
        }


        // ����������, ����Ҫ��ѡ��ѡ���������
        // ��֤ѡ���һ������Ŀ������Ϊ�������
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
                        // ����������δ����ȫ����
                        checkPiece = slot.upperPiece;
                    }
                    else
                    {
                        if (slot.bottomPiece != null &&
                            selectPiecesDic.TryGetValue(slot.bottomPiece, out var bottomPieceSelectCount) &&
                            slot.bottomPiece.ClearNum - bottomPieceSelectCount > 0)
                        {
                            // �ײ�������δ����ȫ����
                            checkPiece = slot.bottomPiece;
                        }

                        if (slot.piece != null &&
                            selectPiecesDic.TryGetValue(slot.piece, out var pieceSelectCount) &&
                            slot.piece.ClearNum - pieceSelectCount > 0)
                        {
                            // �в�����δ����ȫ����
                            checkPiece = slot.piece;
                        }
                        else if (slot.incomingPiece != null &&
                                 selectPiecesDic.TryGetValue(slot.incomingPiece, out var incomingPieceSelectCount) &&
                                 slot.incomingPiece.ClearNum - incomingPieceSelectCount > 0)
                        {
                            // �в�����(incoming)δ����ȫ����
                            checkPiece = slot.incomingPiece;
                        }
                    }

                    if (checkPiece != null && targetIdList.Any(x => x == checkPiece.Id))
                    {
                        // �ҵ���Ŀ������, ��¼��Dic
                        if (!selectPiecesDic.TryAdd(checkPiece, 1))
                        {
                            selectPiecesDic[checkPiece]++;
                        }
                    }
                    else containsTargetPiece = false;
                }

                // �ҵ���Ŀ������, ��ֹ
                if (selectPiecesDic.Count > 0) { break; }
            }
        }

        // ��dic�е����Ӽ��뵽�б���, ע�ⱻ���������ͬһ���ӻᱻ������
        var selectPieces = new List<Piece>();
        foreach (var kvp in selectPiecesDic)
        {
            for (int time = 1; time <= kvp.Value; time++)
            {
                selectPieces.Add(kvp.Key);
            }
        }

        // ����������, ����Ҫ��ӻ�������
        // ���������������Ҳ����, ����ӿ�����(NULL)
        if (selectPiecesDic.Count <= 0)
        {
            selectPieces.AddRange(SelectRandomBasicPiece(1));
        }

        // ��ʱ�ҵ����㹻������Ŀ������, (�п��ܰ���������)
        // ����˳�򲢷���
        var random = new Random();
        var res = selectPieces
            .OrderBy(x => x == null ? 1 : 0)    // �������������
            .ThenBy(x => random.Next())         // ������ҷǿ�����
            .ToList()
            .FirstOrDefault();
        return res;
    }
}
