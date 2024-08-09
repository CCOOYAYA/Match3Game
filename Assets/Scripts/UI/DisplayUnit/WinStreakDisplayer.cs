using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinStreakDisplayer : MonoBehaviour
{
    [SerializeField] Image boxImage;
    [SerializeField] Sprite[] boxSprites;
    [SerializeField] GameObject barP1;
    [SerializeField] GameObject barP2;
    [SerializeField] GameObject barP3;
    [SerializeField] TextMeshProUGUI progressText;
    [SerializeField] GameObject flag1;
    [SerializeField] GameObject flag2;
    [SerializeField] GameObject flag3;

    public void UpdateDisplay()
    {
        int winstreak = UserDataManager.WinStreak;
        switch (winstreak)
        {
            case 0:
                boxImage.sprite = boxSprites[0];
                barP1.SetActive(false);
                barP2.SetActive(false);
                barP3.SetActive(false);
                progressText.text = "0/3";
                break;
            case 1:
                boxImage.sprite = boxSprites[1];
                barP1.SetActive(true);
                barP2.SetActive(false);
                barP3.SetActive(false);
                progressText.text = "1/3";
                break;
            case 2:
                boxImage.sprite = boxSprites[2];
                barP1.SetActive(false);
                barP2.SetActive(true);
                barP3.SetActive(false);
                progressText.text = "2/3";
                break;
            default:
                boxImage.sprite = boxSprites[3];
                barP1.SetActive(false);
                barP2.SetActive(true);
                barP3.SetActive(true);
                progressText.text = "3/3";
                break;
        }
        if (flag1 != null)
            flag1?.SetActive(winstreak == 1);
        if (flag2 != null)
            flag2?.SetActive(winstreak == 2);
        if (flag3 != null)
            flag3?.SetActive(winstreak == 3);
    }

}
