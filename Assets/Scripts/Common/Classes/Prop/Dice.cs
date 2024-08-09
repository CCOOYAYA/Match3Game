using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dice : MonoBehaviour, IGameBoardAction
{
    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    private readonly string diceShuffleEventName = "shuffle";

    private readonly int maxShuffleAttempts = 25;
    private bool completed;

    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    public void PropActivate()
    {
        SkeletonAnimation.AnimationState.Event += HandleShuffleEvent;
        SkeletonAnimation.AnimationState.Complete += delegate { completed = true; };

        GameManager.instance.MutePlayerInputOnGameBoard();
    }

    private void HandleShuffleEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == diceShuffleEventName)
        {
            GameBoardManager.instance.HandleDiceShuffle(maxShuffleAttempts);
        }
    }

    public bool Tick()
    {
        if (completed)
        {
            GameManager.instance.ReceivePlayerInputOnGameBoard();
            Destroy(gameObject);
            return false;
        }

        return true;
    }
}
