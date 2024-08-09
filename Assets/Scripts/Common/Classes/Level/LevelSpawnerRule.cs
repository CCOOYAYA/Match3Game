using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpawnerRule
{
    public int index;
    public List<int> rule;
    public int totalWeights;

    public SpawnerRule(int index, List<int> rule)
    {
        this.index = index;
        this.rule = rule;
        for (int i = 1; i < rule.Count; i += 2)
        {
            totalWeights += rule[i];
        }
    }
}

public class LevelSpawnerRule
{
    public SpawnerRule DefaultSpawnerRule { get; private set; }

    public Dictionary<GridPosition, Queue<SpawnerRule>> SpawnerRuleDic { get; private set; } = new();

    public HashSet<int> AppearPieceIds { get; private set; } = new();


    public LevelSpawnerRule(List<SpawnerRule> ruleInfo, List<List<int>> allocationInfo)
    {
        // 对不合法的规则抛出异常
        if (ruleInfo == null || ruleInfo.Count <= 0)
        {
            throw new NullReferenceException("Spawn rule is null or empty");
        }
        if (allocationInfo == null || allocationInfo.Count <= 0)
        {
            throw new NullReferenceException("Spawn rule allocation config is invalid");
        }

        ruleInfo.ForEach(rule =>
        {
            for (int i = 0; i < rule.rule.Count; i += 2)
            {
                AppearPieceIds.Add(rule.rule[i]);
            }
        });

        // 找到默认掉落规则
        DefaultSpawnerRule = ruleInfo.FirstOrDefault(x => x.index == 0) ?? throw new NullReferenceException("Default spawner rule is null");

        // 对每个生成口分配规则
        allocationInfo.ForEach(info =>
        {
            var spawnerPosition = new GridPosition(info[0], info[1]);
            SpawnerRuleDic.TryAdd(spawnerPosition, new Queue<SpawnerRule>());

            var allocatedSpawnerRuleIndex = info[2];
            if (allocatedSpawnerRuleIndex == 0)
            {
                SpawnerRuleDic[spawnerPosition].Enqueue(DefaultSpawnerRule);
            }
            else
            {
                var allocatedSpawnerRule = ruleInfo.FirstOrDefault(x => x.index == allocatedSpawnerRuleIndex);
                if (allocatedSpawnerRule == null)
                {
                    throw new NullReferenceException("Spawn rule is null");
                }

                // 加入特殊的掉落规则后也需要加入默认规则
                SpawnerRuleDic[spawnerPosition].Enqueue(allocatedSpawnerRule);
                SpawnerRuleDic[spawnerPosition].Enqueue(DefaultSpawnerRule);
            }
        });
    }


    /// <summary>
    /// 抛弃所有index相同的生成口
    /// </summary>
    public void DiscardSpawnerRule(int spawnerRuleIndex)
    {
        if (spawnerRuleIndex == 0)
        {
            throw new InvalidOperationException("Cannot discard default spawner rule");
        }

        foreach (var kvp in SpawnerRuleDic)
        {
            if (kvp.Value.Count > 1 &&
                kvp.Value.Peek().index == spawnerRuleIndex)
            {
                kvp.Value.Dequeue();
            }
        }
    }
}