using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfigSO", menuName = "SO/LevelConfigSO")]
public class LevelConfigSO : ScriptableObject
{
    //TODO 新手引导相关
    public int StreakUnlockLevel = 2;
    public int[] PropUnlockLevel;
}