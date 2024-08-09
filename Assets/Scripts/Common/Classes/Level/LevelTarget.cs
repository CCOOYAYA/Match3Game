using System;
using System.Collections.Generic;

public class LevelTarget
{
    // Key = collectId, Value = leftCount
    public Dictionary<int, int> TargetDic { get; private set; } = new();
    private readonly Dictionary<int, int> originalTargetDic = new();
    private readonly int originalTargetTotalCount;

    private PieceConfigSO pieceConfigSO;


    public float TargetProgression
    {
        get
        {
            var leftCount = 0;
            foreach (var kvp in TargetDic)
            {
                leftCount += kvp.Value;
            }
            return 1f - (float)leftCount / originalTargetTotalCount;
        }
    }

    public bool IsAllTargetsCompleted 
    {
        get
        {
            foreach (var kvp in TargetDic)
            {
                if (kvp.Value > 0)
                {
                    return false;
                }
            }
            return true;
        }
    }


    public LevelTarget(int[][] targetInfo, PieceConfigSO pieceConfigSO)
    {
        if (targetInfo == null)
            return;

        foreach (var target in targetInfo)
        {
            if (target[0] == 0) 
            { 
                continue; 
            }
            else if (!TargetDic.TryAdd(target[0], target[1]) || TargetDic.Count > 4)
            {
                throw new Exception("Error occurs when setting level targets: duplicate targets or exceed limits");
            }
        }

        // Deep copy an originalTargetDic
        foreach (var kvp in TargetDic)
        {
            originalTargetDic.TryAdd(kvp.Key, kvp.Value);
            originalTargetTotalCount += kvp.Value;
        }
        this.pieceConfigSO = pieceConfigSO;
    }


    public bool IsThisPieceTarget(int pieceId, out int remainCount)
    {
        if (pieceConfigSO.allRegisteredPieces.TryGetValue(pieceId, out var registeredPiece))
        {
            var collectId = registeredPiece.pieceTargetReference.collectId;
            return TargetDic.TryGetValue(collectId, out remainCount);
        }

        remainCount = 0;
        return false;
    }


    /// <summary>
    /// 当消除一个棋子时, 更新其对应的目标棋子的数量
    /// </summary>
    /// <returns>其他棋子目标是否已被全部完成</returns>
    public bool UpdateTargetPieces(int collectId)
    {
        TargetDic[collectId] = Math.Max(TargetDic[collectId] - 1, 0);

        return IsAllTargetsCompleted;
    }


    /// <summary>
    /// 获取全部未完成的目标棋子id(不是collectId)
    /// </summary>
    public List<int> GetRemainTargetPiecesId()
    {
        var res = new List<int>();
        foreach (var kvp in pieceConfigSO.allRegisteredPieces)
        {
            if (TargetDic.TryGetValue(kvp.Value.pieceTargetReference.collectId, out var leftCount) && leftCount > 0)
            {
                res.Add(kvp.Key);
            }
        }
        return res;
    }


    /// <summary>
    /// 按剩余数量排序目标棋子的id, 小于等于0的将不会计入
    /// </summary>
    /// <param name="ascending">排序方式, 默认关闭增序, 从大到小</param>
    public List<int> SortRemainTargetPiecesId(bool ascending = false)
    {
        var res = GetRemainTargetPiecesId();
        res.Sort((a, b) => ascending ? TargetDic[a].CompareTo(TargetDic[b]) : TargetDic[b].CompareTo(TargetDic[a]));
        return res;
    }
}
