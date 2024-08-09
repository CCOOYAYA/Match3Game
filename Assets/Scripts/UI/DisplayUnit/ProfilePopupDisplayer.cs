using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProfilePopupDisplayer : TopBannerPopup
{
    [SerializeField] private PopupPage setNamePage;
    [SerializeField] private TextMeshProUGUI userNameText;
    [SerializeField] private TextMeshProUGUI startDateText;
    [SerializeField] private AvatarDisplayer avatar;
    [SerializeField] private UISpriteList themeList_bg;
    [SerializeField] private UISpriteList themeList_label;
    [SerializeField] private Image themeBgImage;
    [SerializeField] private Image themeBgImage2;
    [SerializeField] private Image themeLabelImage;
    [SerializeField] private TextMeshProUGUI levelCountText;

    public void UpdateDisplay()
    {
        userNameText.text = UserDataManager.UserName;
        //Todo:startDate
        avatar.UpdateDisplay();
        themeBgImage.sprite = themeList_bg.FindSpriteByID(UserDataManager.UserThemeID);
        themeBgImage2.sprite = themeList_bg.FindSpriteByID(UserDataManager.UserThemeID);
        themeLabelImage.sprite = themeList_label.FindSpriteByID(UserDataManager.UserThemeID);
        levelCountText.text = (UserDataManager.NextLevelID - 1).ToString();
    }

    public override void ShowMe()
    {
        UpdateDisplay();
        base.ShowMe();
        if (UserDataManager.DefaultUserName)
            PopupManager.ShowPageAsync(setNamePage, false, true).Forget();
    }

    public override UniTask HideMe()
    {
        HomeSceneUIManager.UpdateAvatar();
        return base.HideMe();
    }
}
