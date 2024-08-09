using System.Collections.Generic;
using System.Linq;

public class GameLevel
{
    // 关卡参数
    public int level;
    public int levelType;
    public int steps;
    public int xMax;
    public int yMax;
    public int fillType;
    public string levelName;

    // 目标棋子参数
    public int[][] targetInfo;

    // 槽位参数
    public int[][] slotInfo;

    // 槽位内容参数
    public int[][] bottomInfo;
    public int[][] pieceInfo;
    public int[][] upperInfo;

    // (初始化时)棋盘空位置填充策略
    public List<int> emptyFillRule;

    // 生成口策略
    public List<SpawnerRule> spawnerRuleInfo;
    public List<List<int>> spawnerRuleAllocation;

    public void GetDataByGridPositon(GridPosition findPosition, out int[] getBottomInfo, out int[] getPieceInfo, out int[] getUpperInfo)
    {
        getBottomInfo = bottomInfo.Where(data => findPosition.Equals(new GridPosition(data[0], data[1]))).FirstOrDefault();
        getPieceInfo = pieceInfo.Where(data => findPosition.Equals(new GridPosition(data[0], data[1]))).FirstOrDefault();
        getUpperInfo = upperInfo.Where(data => findPosition.Equals(new GridPosition(data[0], data[1]))).FirstOrDefault();
    }
}
