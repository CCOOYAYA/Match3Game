using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfigSO", menuName = "SO/LevelConfigSO")]
public class LevelConfigSO : ScriptableObject
{
    //TODO �����������
    public int StreakUnlockLevel = 2;
    public int[] PropUnlockLevel;
}