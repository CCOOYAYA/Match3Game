using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserRankDisplayer : MonoBehaviour
{
    [SerializeField] Image bg;
    [SerializeField] Sprite[] bgSprites;
    [SerializeField] Image medalImage;
    [SerializeField] Sprite[] medelSprites;
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] AvatarDisplayer avatar;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] GameObject mask;

    public void UpdateDisplay(int rank, int avatarID, int frameID, string name, int levelCount, bool isPlayer = false)
    {
        bg.sprite = bgSprites[isPlayer ? 1 : 0];
        if (rank < 4)
        {
            medalImage.sprite = medelSprites[rank - 1];
            medalImage.gameObject.SetActive(true);
            rankText.gameObject.SetActive(false);
        }
        else
        {
            medalImage.gameObject.SetActive(false);
            rankText.text = rank.ToString();
            rankText.gameObject.SetActive(true);
        }
        avatar.UpdateDisplay(avatarID, frameID);
        nameText.text = name;
        levelText.text = levelCount.ToString();
    }

    public void UpdateUserInfo()
    {
        avatar.UpdateDisplay();
        nameText.text = UserDataManager.UserName;
    }

    public void ShowMe()
    {
        mask.SetActive(false);
    }

    public void HideMe()
    {
        mask.SetActive(true);
    }
}
