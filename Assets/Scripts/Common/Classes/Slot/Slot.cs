using System;
using System.Collections.Generic;
using UnityEngine;

public class Slot : MonoBehaviour
{
    // content
    public Floor floor;             // 最下层: 地板
    public Piece bottomPiece;       // 下层: 底层
    public Piece piece;             // 中间层: 包含的棋子
    public Piece incomingPiece;     // 目标为此槽位的棋子
    public Piece upperPiece;        // 上层: 覆盖


    public GridPosition GridPosition { get; private set; }          // 处在棋盘网格中的位置
    public int EnterLock { get; private set; }                      // 槽位进入锁定, > 0 则禁止棋子进入这个槽位
    public int LeaveLock { get; private set; }                      // 槽位离开锁定, > 0 则禁止棋子离开这个槽位
    public bool IsActive { get; private set; }                      // 是否为启用/激活状态
    public bool IsSpawner { get; private set; }                     // 是否为生成口
    public Slot Spawner { get; set; }                               // 对应的(垂直方向上的)生成口
    public int FillType { get; set; }                               // 填充类型(-1: 无法填充, 0: 垂直填充; 1: 斜向填充)
    public float LastFireTime { get; set; }                         // 最后发出棋子的时间
    public float LastSpawnTime { get; set; }                        // 最后生成棋子的时间(仅限生成口)


    public bool IsEmpty => piece == null && incomingPiece == null;  // 是否不包含棋子且没有棋子以它为目标位置
    /// <summary>
    /// 槽位是否含有不可移动的棋子
    /// </summary>
    public bool HasUnMovablePiece => piece != null && !piece.CanMove;
    /// <summary>
    /// 槽位是否限制棋子移动
    /// </summary>
    public bool HasMoveConstrain => EnterLock > 0 || LeaveLock > 0;
    /// <summary>
    /// 槽位是否允许棋子进入
    /// </summary>
    public bool CanEnter => EnterLock <= 0 && piece == null;
    /// <summary>
    /// 槽位是否允许棋子离开
    /// </summary>
    public bool CanLeave => LeaveLock <= 0 && (piece == null || (piece.CanMove && piece.CurrentMatch == null));


    // 是否能够响应交互
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
    /// 初始化槽位
    /// </summary>
    /// <param name="gridPosition">位置</param>
    /// <param name="isActive">是否开启</param>
    /// <param name="isSpawner">是否为生成口</param>
    public void InitializeSlot(GridPosition gridPosition, bool isActive, bool isSpawner)
    {
        GridPosition = gridPosition;
        IsActive = isActive;
        IsSpawner = isSpawner;
    }

    /// <summary>
    /// 获取最上层的棋子
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


    // 增加和减少进入锁定的层数
    public void IncreaseEnterLock(uint lockValue) => EnterLock += (int)lockValue;
    public void DecreaseEnterLock(uint lockValue) => EnterLock = (EnterLock - (int)lockValue) < 0 ? 0 : EnterLock - (int)lockValue;

    // 增加和减少离开锁定的层数
    public void IncreaseLeaveLock(uint lockValue) => LeaveLock += (int)lockValue;
    public void DecreaseLeaveLock(uint lockValue) => LeaveLock = (LeaveLock - (int)lockValue) < 0 ? 0 : LeaveLock - (int)lockValue;

    // 同时增加和减少
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
