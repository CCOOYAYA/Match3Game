using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarDisplayer : MonoBehaviour
{
    [SerializeField] private UISpriteList avatarList;
    [SerializeField] private UISpriteList frameList;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image frameImage;

    public void UpdateDisplay()
    {
        avatarImage.sprite = avatarList.FindSpriteByID(UserDataManager.UserAvatarID);
        frameImage.sprite = frameList.FindSpriteByID(UserDataManager.UserAvatarFrameID);
    }

    public void UpdateDisplay(int avatarID, int frameID)
    {
        avatarImage.sprite = avatarList.FindSpriteByID(avatarID);
        frameImage.sprite = frameList.FindSpriteByID(frameID);
    }
}
