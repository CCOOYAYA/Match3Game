using UnityEngine;

public class TopBannerPopup : PopupPage
{
    [SerializeField] RectTransform transform2;
    public override void ShowMe()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.offsetMax = Vector2.up * (UserDataManager.TopMargin ? 0f : 84f);
        base.ShowMe();
    }
}
