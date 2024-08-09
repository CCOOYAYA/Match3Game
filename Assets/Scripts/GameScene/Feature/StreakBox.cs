using Spine;
using Spine.Unity;
using Animation = Spine.Animation;
using UnityEngine;
using BezierSolution;
using System;

public class StreakBox : MonoBehaviour
{
    private SkeletonAnimation _skeletonAnimation;
    private SkeletonAnimation SkeletonAnimation => _skeletonAnimation;

    [Header("Animations && Images")]
    [SerializeField] private BoneFollower bombStreakFollower;
    [SerializeField] private BoneFollower rocketStreakFollower;
    [SerializeField] private BoneFollower rainbowStreakFollower;

    [SerializeField] private AnimationReferenceAsset level1Enter;
    [SerializeField] private AnimationReferenceAsset level1Leave;
    [SerializeField] private AnimationReferenceAsset level2Enter;
    [SerializeField] private AnimationReferenceAsset level2Leave;
    [SerializeField] private AnimationReferenceAsset level3Enter;
    [SerializeField] private AnimationReferenceAsset level3Leave;

    private readonly string launchEventName = "streak_launch";

    //private readonly string bone1Name = "powerup_01_bone";
    //private readonly string bone2Name = "powerup_02_bone";
    //private readonly string bone3Name = "powerup_03_bone";

    public AssignStreakBoxPowerupProgress AssignProgress { get; private set; }
    private Animation enterAnimation;
    private Animation leaveAnimation;


    public enum AssignStreakBoxPowerupProgress
    {
        Initializing,
        Entering,
        Assigning,
        Leaving,
        Complete
    }


    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    public void Initialize(int streakLevel)
    {
        AssignProgress = AssignStreakBoxPowerupProgress.Initializing;
        gameObject.SetActive(true);

        if (streakLevel == 1)
        {
            enterAnimation = level1Enter.Animation;
            leaveAnimation = level1Leave.Animation;

            SkeletonAnimation.AnimationState.SetAnimation(0, enterAnimation, false);
            SkeletonAnimation.AnimationState.Event += HandleLaunchEvent;
            //rocketStreakFollower.boneName = bone2Name;

            rocketStreakFollower.gameObject.SetActive(true);
        }
        else if (streakLevel == 2)
        {
            enterAnimation = level2Enter.Animation;
            leaveAnimation = level2Leave.Animation;

            SkeletonAnimation.AnimationState.SetAnimation(0, enterAnimation, false);
            SkeletonAnimation.AnimationState.Event += HandleLaunchEvent;
            //bombStreakFollower.boneName = bone1Name;
            //rocketStreakFollower.boneName = bone2Name;

            bombStreakFollower.gameObject.SetActive(true);
            rocketStreakFollower.gameObject.SetActive(true);
        }
        else if (streakLevel == 3)
        {
            enterAnimation = level3Enter.Animation;
            leaveAnimation = level3Leave.Animation;

            SkeletonAnimation.AnimationState.SetAnimation(0, enterAnimation, false);
            SkeletonAnimation.AnimationState.Event += HandleLaunchEvent;
            //bombStreakFollower.boneName = bone1Name;
            //rocketStreakFollower.boneName = bone2Name;
            //rainbowStreakFollower.boneName = bone3Name;

            bombStreakFollower.gameObject.SetActive(true);
            rocketStreakFollower.gameObject.SetActive(true);
            rainbowStreakFollower.gameObject.SetActive(true);
        }

        AssignProgress = AssignStreakBoxPowerupProgress.Entering;
    }


    private void HandleLaunchEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == launchEventName)
        {
            AssignProgress = AssignStreakBoxPowerupProgress.Assigning;
        }
    }


    /// <summary>
    /// ∑≈÷√Powerup
    /// </summary>
    public void AssignPowerup(Slot replaceSlot, int assignPieceId, Action completeAssiognCallback)
    {
        var transform = assignPieceId switch
        {
            var x when x == Constants.PieceBombId                                       => bombStreakFollower.transform,
            var x when x == Constants.PieceHRocketId || x == Constants.PieceVRocketId   => rocketStreakFollower.transform,
            var x when x == Constants.PieceRainbowId                                    => rainbowStreakFollower.transform,
            _ => null
        };

        if (assignPieceId == Constants.PieceBombId)
        {
            bombStreakFollower.enabled = false;
        }
        else if (assignPieceId == Constants.PieceHRocketId || assignPieceId == Constants.PieceVRocketId)
        {
            rocketStreakFollower.enabled = false;
        }
        else if (assignPieceId == Constants.PieceRainbowId)
        {
            rainbowStreakFollower.enabled = false;
        }
        transform.parent = null;
        //transform.GetComponent<SortingGroup>().sortingOrder = 999;

        var spline = new GameObject().AddComponent<BezierSpline>();
        spline.Initialize(2);

        spline[0].transform.position = transform.position;
        spline[1].transform.position = replaceSlot.transform.position;

        var slotInterval = GameBoardManager.instance.slotOriginalInterval;
        var vector = replaceSlot.transform.position - transform.position;
        var direction = vector.x > 0;
        var startPointControlPointXOffset = Mathf.Abs(Mathf.Min(0.5f * slotInterval, 0.35f * vector.x));
        var endPointControlPointXOffset = Mathf.Abs(Mathf.Min(0.5f * slotInterval, 0.35f * vector.x));
        spline[0].followingControlPointPosition = transform.position + new Vector3(direction ? 1 : -1 * startPointControlPointXOffset, 5f * slotInterval, 0f);
        spline[1].precedingControlPointPosition = new Vector3(replaceSlot.transform.position.x, spline[0].followingControlPointPosition.y, 0f);

        var walker = transform.GetComponent<BezierWalkerForStreakPowups>();
        walker.Initialize(spline, completeAssiognCallback);
    }

    public void Leave()
    {
        AssignProgress = AssignStreakBoxPowerupProgress.Leaving;

        SkeletonAnimation.AnimationState.SetAnimation(0, leaveAnimation, false);
        SkeletonAnimation.AnimationState.Complete += delegate 
        { 
            AssignProgress = AssignStreakBoxPowerupProgress.Complete;
            gameObject.SetActive(false);
        };
    }
}