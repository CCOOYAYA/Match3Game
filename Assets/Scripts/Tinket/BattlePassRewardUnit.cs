using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class BattlePassRewardUnit : MonoBehaviour
{
    [SerializeField] BattlePassConfigSO configSO;
    [SerializeField] Image itemImage;
    [SerializeField] TextMeshProUGUI countText;

    private int propID;

    public void SetReward(BattlePassConfigSO.BattlePassReward reward)
    {
        
    }
}
