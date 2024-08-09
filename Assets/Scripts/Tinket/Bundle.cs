using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bundle : MonoBehaviour
{
    [SerializeField] public bool CoinOnly = false;
    [SerializeField] public int Coins;
    [SerializeField] public int[] ItemCount;
    [SerializeField] public int[] StartItemTime;
    [SerializeField] public int LifeTime;
    [SerializeField] public int Price;
    [SerializeField] public SimpleSlideTweener Tweener;

    public void BuyMe()
    {
        UserDataManager.BuyBundle(this);
        PopupManager.CloseCurrentPageAsync().Forget();
    }
}
