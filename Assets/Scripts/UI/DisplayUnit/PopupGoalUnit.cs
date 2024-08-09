using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PopupGoalUnit : MonoBehaviour
{
    [SerializeField] private Image goalImage;
    [SerializeField] private PieceConfigSO pieceConfigSO;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private GameObject successIcon;
    [SerializeField] private GameObject failIcon;

    public void StartPopupMode(int collectId, int count)
    {
        try
        {
            // TODO(fix): use pieceId instead of collectId
            if (pieceConfigSO.allRegisteredPieces.TryGetValue(collectId, out var registeredPiece))
            {
                goalImage.sprite = registeredPiece.pieceTargetReference.pieceDialogTargetSprite;
            }

            successIcon.SetActive(false);
            failIcon.SetActive(false);
            countText.text = count.ToString();
            countText.gameObject.SetActive(true);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public void RetryPopupMode(int collectId)
    {
        try
        {
            // TODO(fix): use pieceId instead of collectId
            if (pieceConfigSO.allRegisteredPieces.TryGetValue(collectId, out var registeredPiece))
            {
                bool goalSuccess = MainGameUIManager.TargetComplete(collectId);


                goalImage.sprite = registeredPiece.pieceTargetReference.pieceDialogTargetSprite;
                countText.gameObject.SetActive(false);
                successIcon.SetActive(goalSuccess);
                failIcon.SetActive(!goalSuccess);
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
        
}
