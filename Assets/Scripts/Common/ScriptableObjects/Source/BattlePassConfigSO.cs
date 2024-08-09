using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BattlePassConfigSO", menuName = "SO/BattlePassConfigSO")]
public class BattlePassConfigSO : ScriptableObject
{
    [SerializeField] public BattlePassReward[] freeReward;
    [SerializeField] public BattlePassReward[] advancedReward;
    [SerializeField] public SerializedDictionary<int,BattlePassRewardBundle> rewardBundles; //特殊配置(头像框等)特殊处理
    [SerializeField] public SerializedDictionary<RewardType, Sprite> rewardSprites;
    [SerializeField] public SerializedDictionary<int, Sprite> bundleSprites;

    [Serializable]
    public struct BattlePassReward
    {
        public RewardType type;
        public int value;
    }

    [Serializable]
    public struct BattlePassRewardBundle
    {
        public int imageID;
        public BattlePassReward[] rewards;
    }
}