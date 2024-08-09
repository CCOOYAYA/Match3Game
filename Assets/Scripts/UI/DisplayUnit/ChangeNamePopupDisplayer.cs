using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class ChangeNamePopupDisplayer : PopupPage
{
    [SerializeField] GameObject bannerText;
    [SerializeField] GameObject bannerText_First;
    [SerializeField] GameObject askText;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] int ypos;
    [SerializeField] int ypos_First;
    [SerializeField] GameObject buttonText;
    [SerializeField] GameObject buttonText_First;
    [SerializeField] GameObject closeButton;
    [SerializeField] LocalizedString nameWarning;

    private void UpdateDisplay()
    {
        bool firstflag = UserDataManager.DefaultUserName;
        bannerText.SetActive(!firstflag);
        bannerText_First.SetActive(firstflag);
        askText.SetActive(firstflag);
        inputField.transform.localPosition = Vector3.up * (firstflag ? ypos_First : ypos);
        inputField.text = "";
        buttonText.SetActive(!firstflag);
        buttonText_First.SetActive(firstflag);
        closeButton.SetActive(!firstflag);
    }

    private bool CheckName(ref string name)
    {
        while (name.Contains("  "))
            name.Replace("  ", " ");
        if (name.StartsWith(' '))
            name.Remove(0);
        if (name.EndsWith(' '))
            name.Remove(name.Length - 1);
        if (name.Length < 3)
            return false;
        return true;
    }

    public void TrySaveName()
    {
        string name = inputField.text;
        if (CheckName(ref name))
        {
            UserDataManager.SetUserName(name);
            PopupManager.CloseCurrentPageAsync().Forget();
        }
        else
            HomeSceneUIManager.PopupText(nameWarning.GetLocalizedString());
    }

    public override void ShowMe()
    {
        UpdateDisplay();
        base.ShowMe();
    }
}
