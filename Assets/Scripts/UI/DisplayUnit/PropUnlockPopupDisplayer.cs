using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PropUnlockPopupDisplayer : PopupPage
{
    [SerializeField] LevelConfigSO levelConfig;
    [SerializeField] PropInfo[] propInfoList;
    [SerializeField] Image nameImage;
    [SerializeField] Image itemImage;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] RewardManager rewardManager;

    [Serializable]
    private struct PropInfo
    {
        public Sprite name;
        public Sprite sprite;
        public int setSize;
        public LocalizedString description;
    }

    private int itemID;

    public void PopupCheck()
    {
        for (int i = 0; i < levelConfig.PropUnlockLevel.Length; i++)
            if (levelConfig.PropUnlockLevel[i] == UserDataManager.LevelID)
            {
                SetItem(i);
                PopupManager.ShowPageAsync(this).Forget();
                return;
            }
    }

    public void SetItem(int id)
    {
        itemID = id;
    }

    public override void ShowMe()
    {
        nameImage.sprite = propInfoList[itemID].name;
        nameImage.SetNativeSize();
        itemImage.sprite = propInfoList[itemID].sprite;
        countText.text = "¡Á"+ propInfoList[itemID].setSize;
        descriptionText.text = propInfoList[itemID].description.GetLocalizedString();
        base.ShowMe();
    }

    public override UniTask HideMe()
    {
        PropAnimation();
        return base.HideMe();
    }

    private void PropAnimation()
    {
        switch (itemID)
        {
            case 0:
                rewardManager.PropUnlockReward(RewardType.hammer);
                return;
            case 1:
                rewardManager.PropUnlockReward(RewardType.gun);
                return;
            case 2:
                rewardManager.PropUnlockReward(RewardType.cannon);
                return;
            case 3:
                rewardManager.PropUnlockReward(RewardType.dice);
                return;
            default:
                return;
        }
    }

    public void Test()
    {
        PropAnimation();
    }
}
