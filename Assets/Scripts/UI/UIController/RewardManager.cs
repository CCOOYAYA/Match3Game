using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    [SerializeField] GameObject rewardDisplayArea;
    [SerializeField] Transform boxPos;

    [SerializeField] Transform coinTarget;
    [SerializeField] Transform lifeTarget;
    [SerializeField] Transform starTarget;
    [SerializeField] Transform levelButton;
    [SerializeField] RewardCoinsDisplayer rewardCoins;
    [SerializeField] TempFlyingObject protoStar;
    [SerializeField] RewardUnit protoRewardUnit;

    [SerializeField] PositionSet propUnlock;
    [SerializeField] PositionSet test1;
    [SerializeField] PositionSet test2;

    private List<RewardUnit> rewardUnits = new();
    private List<Tuple<RewardType, int>> rewards = new();
    private int rewardCoin = 0;

    public Action ClaimCallback;

    public async UniTask LevelStdRewardCheck()
    {
        if (UserDataManager.WinFlag)
        {
            var fStar = Instantiate<TempFlyingObject>(protoStar, transform);
            fStar.SetTargetPosition(starTarget.position);
            fStar.StartBezierMoveToRewardDisplay();
            await UniTask.WaitUntil(() => fStar == null);
            UserDataManager.AddStar();
            HomeSceneUIManager.UpdateStarCount();
            HomeSceneUIManager.UpdateBuildButton();
            int newCoin = (int)UserDataManager.NewCoinToAdd;
            rewardCoins.UpdateStartCoin();
            UserDataManager.AddNewCoins();
            await rewardCoins.CoinGo(newCoin);
        }
    }

    public void ShowTest()
    {
        AddReward(RewardType.hammer, 1);
        AddReward(RewardType.gun, 1);
        AddReward(RewardType.coin, 300);
        AddReward(RewardType.cannon, 1);
        AddReward(RewardType.dice, 1);
        AddReward(RewardType.rocket, 1);
        AddReward(RewardType.bomb, 1);
        AddReward(RewardType.lightball, 1);
        ShowReward(test1);
    }

    public async void ShowReward(PositionSet positionSet)
    {
        ButtonBase.Lock();
        rewardDisplayArea.SetActive(true);
        UniTask lastItem = UniTask.CompletedTask;
        for (int i = 0; i < rewards.Count; i++)
        {
            RewardUnit item;
            if ((positionSet != null) && (i < positionSet.PosCount))
                item = Instantiate<RewardUnit>(protoRewardUnit, positionSet.PosTransform(i));
            else
                item = Instantiate<RewardUnit>(protoRewardUnit, transform);
            item.HideMe();
            item.transform.position = boxPos.position;
            item.SetDelayTime(0.1f * i);
            item.SetTargetPos(positionSet.Pos(i));
            item.SetProp(rewards[i].Item1);
            lastItem = item.Animation_Show();
            rewardUnits.Add(item);
        }
        await lastItem;
        ButtonBase.Unlock();
    }

    public void HideRewardTest()
    {
        rewardDisplayArea.SetActive(false);
        ResetReward();
        AddCoin(300);
        AddReward(RewardType.hammer, 1);
        AddReward(RewardType.gun, 1);
        AddReward(RewardType.cannon, 1);
        AddReward(RewardType.dice, 1);
        AddReward(RewardType.rocket, 1);
        AddReward(RewardType.bomb, 1);
        AddReward(RewardType.lightball, 1);
        ClaimReward(test2);
    }

    public async void ClaimReward(PositionSet positionSet = null, bool coinFirst = false)
    {
        ButtonBase.Lock();
        rewardCoins.UpdateStartCoin();
        if ((0 < rewardCoin)&&(coinFirst))
            await rewardCoins.CoinGo(rewardCoin);
        UniTask lastItem = UniTask.CompletedTask;
        int currentPos = 0;
        for (int i = 0; i < rewards.Count; i++)
            if (rewards[i].Item1 != RewardType.coin)
            {
                RewardUnit item;
                if ((positionSet != null)&& (currentPos < positionSet.PosCount))
                    item = Instantiate<RewardUnit>(protoRewardUnit, positionSet.PosTransform(currentPos));
                else
                    item = Instantiate<RewardUnit>(protoRewardUnit, transform);
                item.HideMe();
                item.SetDelayTime(0.1f * currentPos);
                item.SetTargetPos(levelButton.position);
                item.SetProp(rewards[i].Item1);
                lastItem = item.Animation_Claim();
                currentPos++;
            }
        await lastItem;
        if ((0 < rewardCoin) && (!coinFirst))
            await rewardCoins.CoinGo(rewardCoin);
        ResetReward();
        ButtonBase.Unlock();
        var tmpAction = ClaimCallback;
        ClaimCallback = null;
        tmpAction?.Invoke();
    }

    public void SetReward(List<Tuple<RewardType, int>> rewards)
    {
        this.rewards = rewards;
    }

    public void AddReward(RewardType type,int count)
    {
        rewards.Add(new Tuple<RewardType, int>(type, count));
        if (type == RewardType.coin)
            rewardCoin += count;
    }

    public void AddCoin(int count)
    {
        rewardCoin += count;
    }

    public void ResetReward()
    {
        rewards.Clear();
        rewardCoin = 0;
        foreach (var unit in rewardUnits)
            if (unit != null)
                Destroy(unit.gameObject);
        rewardUnits.Clear();
    }

    public void PropUnlockReward(RewardType rewardType)
    {
        ResetReward();
        AddReward(rewardType, 1);
        AddReward(rewardType, 1);
        AddReward(rewardType, 1);
        ClaimReward(propUnlock);
    }

    public async UniTask SimpleFlyingObject(TempFlyingObject prototype, Transform targetPos)
    {
        var fo = Instantiate<TempFlyingObject>(prototype, transform);
        fo.SetTargetPosition(targetPos.position);
        fo.StartBezierMoveToRewardDisplay();
        await UniTask.WaitUntil(() => fo == null);
    }
}

public enum RewardType { coin, life, lifeTime, rocket, rocketTime, bomb, bombTime, lightball, lightballTime, hammer, gun, cannon, dice, bundle, doubleTokenTime };

