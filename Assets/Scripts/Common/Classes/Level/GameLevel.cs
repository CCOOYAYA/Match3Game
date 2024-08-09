using System.Collections.Generic;
using System.Linq;

public class GameLevel
{
    // �ؿ�����
    public int level;
    public int levelType;
    public int steps;
    public int xMax;
    public int yMax;
    public int fillType;
    public string levelName;

    // Ŀ�����Ӳ���
    public int[][] targetInfo;

    // ��λ����
    public int[][] slotInfo;

    // ��λ���ݲ���
    public int[][] bottomInfo;
    public int[][] pieceInfo;
    public int[][] upperInfo;

    // (��ʼ��ʱ)���̿�λ��������
    public List<int> emptyFillRule;

    // ���ɿڲ���
    public List<SpawnerRule> spawnerRuleInfo;
    public List<List<int>> spawnerRuleAllocation;

    public void GetDataByGridPositon(GridPosition findPosition, out int[] getBottomInfo, out int[] getPieceInfo, out int[] getUpperInfo)
    {
        getBottomInfo = bottomInfo.Where(data => findPosition.Equals(new GridPosition(data[0], data[1]))).FirstOrDefault();
        getPieceInfo = pieceInfo.Where(data => findPosition.Equals(new GridPosition(data[0], data[1]))).FirstOrDefault();
        getUpperInfo = upperInfo.Where(data => findPosition.Equals(new GridPosition(data[0], data[1]))).FirstOrDefault();
    }
}
