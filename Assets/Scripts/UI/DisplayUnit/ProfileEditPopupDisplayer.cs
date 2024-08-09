using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProfileEditPopupDisplayer : PopupPage
{
    [SerializeField] private TextMeshProUGUI userNameText;
    [SerializeField] private UISpriteList avatarList;
    [SerializeField] private UISpriteList frameList;
    [SerializeField] private UISpriteList themeList;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image themeImage;
    [SerializeField] private ProfileEditSubPage avatarPage;
    [SerializeField] private ProfileEditSubPage framePage;
    [SerializeField] private ProfileEditSubPage themePage;
    [SerializeField] private GameObject saveButton;
    [SerializeField] private GameObject graySaveButton;
    [SerializeField] private PopupPage saveConfirmPage;

    private ProfileEditSubPage currentPage;

    public void Init()
    {
        userNameText.text = UserDataManager.UserName;

        avatarPage.activeButtonID = UserDataManager.UserAvatarID;
        framePage.activeButtonID = UserDataManager.UserAvatarFrameID;
        themePage.activeButtonID = UserDataManager.UserThemeID;

        UpdateDisplay();

        avatarPage.Init();
        framePage.Init();
        themePage.Init();

        currentPage = avatarPage;
        avatarPage.ShowMe();
    }

    private bool NeedSave()
    {
        if (avatarPage.activeButtonID != UserDataManager.UserAvatarID)
            return true;
        if (framePage.activeButtonID != UserDataManager.UserAvatarFrameID)
            return true;
        if (themePage.activeButtonID != UserDataManager.UserThemeID)
            return true;
        return false;
    }
    
    public void UpdateDisplay()
    {
        avatarImage.sprite = avatarList.FindSpriteByID(avatarPage.activeButtonID);
        frameImage.sprite = frameList.FindSpriteByID(framePage.activeButtonID);
        themeImage.sprite = themeList.FindSpriteByID(themePage.activeButtonID);
        if (NeedSave())
        {
            saveButton.SetActive(true);
            graySaveButton.SetActive(false);
        }
        else
        {
            saveButton.SetActive(false);
            graySaveButton.SetActive(true);
        }
    }

    public void ShowSubPage(ProfileEditSubPage newpage)
    {
        if (currentPage == newpage)
            return;
        currentPage.HideMe();
        currentPage = newpage;
        newpage.ShowMe();
    }

    public void SaveMe()
    {
        UserDataManager.SaveProfile(avatarPage.activeButtonID, framePage.activeButtonID, themePage.activeButtonID);
        PopupManager.CloseCurrentPageAsync().Forget();
    }

    public void TryCloseMe()
    {
        if (NeedSave())
            PopupManager.ShowPageAsync(saveConfirmPage, true).Forget();
        else
            PopupManager.CloseCurrentPageAsync().Forget();
    }

    public override void ShowMe()
    {
        Init();
        base.ShowMe();
    }
}
