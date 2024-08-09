using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RewardCoinsDisplayer : MonoBehaviour
{
    [SerializeField] float randomRadius;
    [SerializeField] FlyingCoin protoCoin;
    [SerializeField] Transform coinTarget;
    [SerializeField] TextMeshProUGUI coinText;

    private int startCoin = 0;
    private int arriveCoinCnt = 0;
    private int totNewCoin = 0;
    private int coinStep = 0;

    public void UpdateStartCoin()
    {
        startCoin = UserDataManager.Coin;
    }

    public async UniTask CoinGo(int totCoin)
    {
        ButtonBase.Lock();
        arriveCoinCnt = 0;
        totNewCoin = totCoin;
        coinStep = totCoin / 10;

        for (int i = 0; i < 10; i++)
        {
            var newCoin = Instantiate<FlyingCoin>(protoCoin, transform);
            newCoin.transform.localPosition += (Vector3)Random.insideUnitCircle * randomRadius;
            newCoin.SetTargetPosition(coinTarget.position, CoinArrive);
            newCoin.MoveStart();
            if (i < 9)
                await UniTask.Delay(85);
        }
        await UniTask.WaitUntil(() => 9 < arriveCoinCnt);
        ButtonBase.Unlock();
    }

    public void CoinArrive()
    {
        arriveCoinCnt++;
        if (arriveCoinCnt < 10)
            coinText.text = (startCoin + arriveCoinCnt * coinStep).ToString();
        else
            coinText.text = (startCoin + totNewCoin).ToString();
    }
}
