using System;
using System.Collections.Generic;
using UnityEngine;

public class Slot : MonoBehaviour
{
    // content
    public Floor floor;             // ���²�: �ذ�
    public Piece bottomPiece;       // �²�: �ײ�
    public Piece piece;             // �м��: ����������
    public Piece incomingPiece;     // Ŀ��Ϊ�˲�λ������
    public Piece upperPiece;        // �ϲ�: ����


    public GridPosition GridPosition { get; private set; }          // �������������е�λ��
    public int EnterLock { get; private set; }                      // ��λ��������, > 0 ���ֹ���ӽ��������λ
    public int LeaveLock { get; private set; }                      // ��λ�뿪����, > 0 ���ֹ�����뿪�����λ
    public bool IsActive { get; private set; }                      // �Ƿ�Ϊ����/����״̬
    public bool IsSpawner { get; private set; }                     // �Ƿ�Ϊ���ɿ�
    public Slot Spawner { get; set; }                               // ��Ӧ��(��ֱ�����ϵ�)���ɿ�
    public int FillType { get; set; }                               // �������(-1: �޷����, 0: ��ֱ���; 1: б�����)
    public float LastFireTime { get; set; }                         // ��󷢳����ӵ�ʱ��
    public float LastSpawnTime { get; set; }                        // ����������ӵ�ʱ��(�������ɿ�)


    public bool IsEmpty => piece == null && incomingPiece == null;  // �Ƿ񲻰���������û����������ΪĿ��λ��
    /// <summary>
    /// ��λ�Ƿ��в����ƶ�������
    /// </summary>
    public bool HasUnMovablePiece => piece != null && !piece.CanMove;
    /// <summary>
    /// ��λ�Ƿ����������ƶ�
    /// </summary>
    public bool HasMoveConstrain => EnterLock > 0 || LeaveLock > 0;
    /// <summary>
    /// ��λ�Ƿ��������ӽ���
    /// </summary>
    public bool CanEnter => EnterLock <= 0 && piece == null;
    /// <summary>
    /// ��λ�Ƿ����������뿪
    /// </summary>
    public bool CanLeave => LeaveLock <= 0 && (piece == null || (piece.CanMove && piece.CurrentMatch == null));


    // �Ƿ��ܹ���Ӧ����
    public bool CanReceiveSelect
    {
        get 
        {
            if (!IsActive || 
                HasMoveConstrain ||
                incomingPiece != null)
            {
                return false;
            }

            var topPiece = GetTopPiece();
            if (topPiece != null &&
                (topPiece.CurrentMatch != null || topPiece.CurrentState != State.Still))
            {
                return false;
            }
            return true;
        } 
    }
    public bool CanReceiveSwap 
    {
        get
        {
            if (!IsActive || 
                HasMoveConstrain ||
                HasUnMovablePiece ||
                upperPiece != null || incomingPiece != null)
            {
                return false;
            }

            if (piece != null &&
                (piece.CurrentMatch != null || piece.CurrentState != State.Still))
            {
                return false;
            }
            return true;
        }
    }




    /// <summary>
    /// ��ʼ����λ
    /// </summary>
    /// <param name="gridPosition">λ��</param>
    /// <param name="isActive">�Ƿ���</param>
    /// <param name="isSpawner">�Ƿ�Ϊ���ɿ�</param>
    public void InitializeSlot(GridPosition gridPosition, bool isActive, bool isSpawner)
    {
        GridPosition = gridPosition;
        IsActive = isActive;
        IsSpawner = isSpawner;
    }

    /// <summary>
    /// ��ȡ���ϲ������
    /// </summary>
    public Piece GetTopPiece()
    {
        if (upperPiece != null)
        {
            return upperPiece;
        }
        else if (piece != null)
        {
            return piece;
        }
        else if (incomingPiece != null)
        {
            return incomingPiece;
        }
        else if (bottomPiece != null)
        {
            return bottomPiece;
        }
        else return null;
    }


    // ���Ӻͼ��ٽ��������Ĳ���
    public void IncreaseEnterLock(uint lockValue) => EnterLock += (int)lockValue;
    public void DecreaseEnterLock(uint lockValue) => EnterLock = (EnterLock - (int)lockValue) < 0 ? 0 : EnterLock - (int)lockValue;

    // ���Ӻͼ����뿪�����Ĳ���
    public void IncreaseLeaveLock(uint lockValue) => LeaveLock += (int)lockValue;
    public void DecreaseLeaveLock(uint lockValue) => LeaveLock = (LeaveLock - (int)lockValue) < 0 ? 0 : LeaveLock - (int)lockValue;

    // ͬʱ���Ӻͼ���
    public void IncreaseEnterAndLeaveLock(uint enterLockValue = 1, uint leaveLockValue = 1)
    {
        IncreaseEnterLock(enterLockValue);
        IncreaseLeaveLock(leaveLockValue);
    }
    public void DecreaseEnterAndLeaveLock(uint enterLockValue = 1, uint leaveLockValue = 1)
    {
        DecreaseEnterLock(enterLockValue);
        DecreaseLeaveLock(leaveLockValue);
    }
}
