using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AssetSwitcher : MonoBehaviour
{
    public enum DifficultyLevel { Normal, Hard, SuperHard }
    public abstract void SetDifficultyLevel(DifficultyLevel difficulty);

    public virtual void SetDifficultyLevel()
    {
        SetDifficultyLevel((DifficultyLevel)UserDataManager.GameLevel.levelType);
    }
}
