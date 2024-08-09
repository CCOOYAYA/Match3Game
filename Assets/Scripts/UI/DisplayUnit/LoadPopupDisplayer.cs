using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class LoadPopupDisplayer : PopupPage
{
    [SerializeField] GameObject[] items;
    [SerializeField] Image[] imageList;
    [SerializeField] TextMeshProUGUI loadText;
    [SerializeField] float fadeTime = 0.2f;
    [SerializeField] Image gameBG;
    [SerializeField] Sprite[] sprites; 

    private Color clearWhite = new Color(1, 1, 1, 0);

    public override void ShowMe()
    {
        int randomInt = Random.Range(0, 4);
        items[randomInt].SetActive(true);
        gameBG.sprite = sprites[UserDataManager.CurrentSceneID - 1];
        base.ShowMe();
    }

    public async UniTask FadeMe()
    {
        foreach (var image in imageList)
            if (image.gameObject.activeSelf)
                _ = image.DOColor(clearWhite, fadeTime);
        await loadText.DOColor(clearWhite, fadeTime);
    }
}
