using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Spine.Unity;
using System;
using Random = UnityEngine.Random;
using Animation = Spine.Animation;
using System.Collections.Generic;
using Spine;

public class FlybombAction : MonoBehaviour, IGameBoardAction
{
    [Header("FlyBomb Animations")]
    [SerializeField] private AnimationReferenceAsset standaloneAnimation;
    [SerializeField] private AnimationReferenceAsset carryBombAnimation;
    [SerializeField] private AnimationReferenceAsset carryHRocketAnimation;
    [SerializeField] private AnimationReferenceAsset carryVRocketAnimation;

    [Space]
    [SerializeField] private AnimationReferenceAsset launchCrossExplodeAnimation;
    [SerializeField] private AnimationReferenceAsset landStandaloneExplodeAnimation;
    [SerializeField] private AnimationReferenceAsset landBombExplodeAnimation;

    [Header("FlyBomb Setting")]
    [SerializeField] private FlyBombCurveSetting curveSetting;


    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    public SplineAnimate SplineAnimate => _splineAnimate;

    public FlyBombState State { get; private set; }
    public GridPosition TakeOffPosition { get; private set; }
    public int CarryPowerupId { get; private set; }
    public Action DestroyCallback { get; private set; }
    public Piece TargetPiece { get; private set; }
    public bool OverwriteTakeOff { get; private set; }
    public float ForceTakeOffDegree { get; private set; }
    public bool CrossExplodeClear { get; private set; }

    private AnimationReferenceAsset FlyAnimation { get; set; }
    private AnimationReferenceAsset LandExplodeAnimation { get; set; }


    // spline
    private bool CounterClockwise { get; set; }
    private List<float3> KnotPositions { get; set; } = new ();
    private Spline Spline { get; set; }
    private SplineContainer Container { get; set; }
    public Vector3 LaunchEndWorldPosition { get; private set; }
    public float Speed { get; private set; }


    private float Distance { get; set; }
    private Vector3 Direction { get; set; }
    private bool Locked { get; set; }
    private Slot LockSlot { get; set; }
    private Vector3 AroundAxis => Vector3.forward;


    private SkeletonAnimation _skeletonAnimation;
    private SplineAnimate _splineAnimate;

    private readonly string crossExplodeEventName = "crossExplode";
    private readonly string findTargetPieceEventName = "findTargetPiece";

    [Serializable]
    public enum FlyBombState
    {
        Initializing,
        TakingOff,
        Flying,
        Completed
    }


    [Serializable]
    public class FlyBombCurveSetting
    {
        [Header("General Setting")]
        public float decisionDistance;
        public float takeOffSpeed;
        public float minSpeed;
        public float maxSpeed;
        public AnimationCurve flyAccelerationCurve;
        public float takeOffScale;
        public float minScale;
        public float maxScale;
        public AnimationCurve flyScaleVariationCurve;

        [Header("Take Off Point Setting")]
        [MinMaxSlider(0f, 45f)]
        public Vector2 flyDirectionOffset;

        [Range(0f, 5f)]
        public float takeOffDistance;

        [Range(0f, 3f)]
        public float smoothness;

        [Header("Bend Point Setting")]
        [Range(0.5f, 1.5f)]
        public float allowedBoundOverflow;

        [MinMaxSlider(0f, 2f)]
        public Vector2 bendX;
        public float minimumBendX;

        [MinMaxSlider(0f, 2f)]
        public Vector2 bendY;
        public float minimunBendY;
    }

    private void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }


    public void Initialize(GridPosition takeOffGridPosition, int swappedPieceId, Action destroyCallback,
                           bool overwritePointDegree = false, float forceEndPointDegree = 0, bool crossExplodeClear = true)
    {
        State = FlyBombState.Initializing;

        TakeOffPosition = takeOffGridPosition;
        CarryPowerupId = swappedPieceId;
        DestroyCallback = destroyCallback;
        OverwriteTakeOff = overwritePointDegree;
        ForceTakeOffDegree = forceEndPointDegree;
        CrossExplodeClear = crossExplodeClear;

        (FlyAnimation, LandExplodeAnimation) = swappedPieceId switch
        {
            var x when x == Constants.PieceBombId => (carryBombAnimation, landBombExplodeAnimation),
            var x when x == Constants.PieceHRocketId => (carryHRocketAnimation, null),
            var x when x == Constants.PieceVRocketId => (carryVRocketAnimation, null),
            _ => (standaloneAnimation, landStandaloneExplodeAnimation)
        };

        TakeOff();
    }


    private void TakeOff()
    {
        DestroyCallback?.Invoke();
        SkeletonAnimation.AnimationState.SetAnimation(0, FlyAnimation, false);
        SkeletonAnimation.AnimationState.Event += HandleCrossExplodeEvent;
        SkeletonAnimation.AnimationState.Event += HandleFindTargetEvent;

        GameBoardManager.instance.PlayFlyBombLaunchAt(TakeOffPosition, launchCrossExplodeAnimation);

        float3 startLocalPosition = float3.zero;
        float3 endWorldPosition = (Vector3.up).RotateAroundAxis(AroundAxis, Random.Range(-25f, 25f) + (OverwriteTakeOff ? ForceTakeOffDegree : 0));
        float3 bendWorldPosition = endWorldPosition / 2 + new float3(Random.Range(-0.6f, 0.6f), Random.Range(-0.6f, 0.6f), 0f);

        KnotPositions = new List<float3>() { startLocalPosition, bendWorldPosition, endWorldPosition };
        Spline = SplineFactory.CreateCatmullRom(KnotPositions);

        Container = new GameObject("Fly Spline for Propeller").AddComponent<SplineContainer>();
        Container.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
        Container.Spline = Spline;

        _splineAnimate = gameObject.AddComponent<SplineAnimate>();

        SplineAnimate.ObjectUpAxis = SplineComponent.AlignAxis.ZAxis;
        SplineAnimate.ObjectForwardAxis = SplineComponent.AlignAxis.YAxis;
        SplineAnimate.Alignment = SplineAnimate.AlignmentMode.None;

        SplineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        SplineAnimate.MaxSpeed = 0.25f * curveSetting.takeOffSpeed;
        SplineAnimate.Easing = SplineAnimate.EasingMode.None;
        SplineAnimate.Loop = SplineAnimate.LoopMode.Once;

        SplineAnimate.Container = Container;

        State = FlyBombState.TakingOff;
    }


    private void StartFly()
    {
        if (TargetPiece != null) 
        {
            Vector3 startWorldPosition = transform.position;
            Vector3 endWorldPosition = TargetPiece.GetWorldPosition();

            float3 startLocalPosition = float3.zero,
                   takeOffLocalPosition = float3.zero,
                   bendLocalPosition = float3.zero,
                   endLocalPosition = float3.zero;

            Direction = endWorldPosition - startWorldPosition;

            Container.transform.position = startWorldPosition;

            startLocalPosition = Vector3.zero;
            endLocalPosition = endWorldPosition - startWorldPosition;

            CounterClockwise = Direction.y >= 0;
            var degreeOffset = (CounterClockwise ? 1 : -1) * Random.Range(curveSetting.flyDirectionOffset.x, curveSetting.flyDirectionOffset.y);

            Distance = Mathf.Min(curveSetting.takeOffDistance, Direction.magnitude * 0.25f);
            if (OverwriteTakeOff)
            {
                // When overwriting point degree
                takeOffLocalPosition = (Distance * Vector3.up).RotateAroundAxis(AroundAxis, ForceTakeOffDegree + degreeOffset);
            }
            else
            {
                // When using direction and degree offset
                takeOffLocalPosition = (Distance * Direction.normalized).RotateAroundAxis(AroundAxis, degreeOffset);
            }

            Vector3 takeOffToEnd = endLocalPosition - takeOffLocalPosition;
            var angle = Vector3.Angle((Vector3)takeOffLocalPosition, takeOffToEnd);
            if (angle >= 90f && angle <= 270f)
            {
                Vector3 smoothOffset = Vector3.zero;
                if (angle >= 90 && angle < 180f)
                {
                    smoothOffset = (curveSetting.smoothness * ((Vector3)takeOffLocalPosition).normalized).RotateAroundAxis(AroundAxis, angle / 2);
                }
                else
                {
                    smoothOffset = (curveSetting.smoothness * ((Vector3)takeOffLocalPosition).normalized).RotateAroundAxis(AroundAxis, angle / 2 + 180f);
                }

                float3 smoothLocalPosition = takeOffLocalPosition + (float3)smoothOffset;
                KnotPositions = new List<float3> { startLocalPosition, takeOffLocalPosition, smoothLocalPosition, endLocalPosition };
            }
            else KnotPositions = new List<float3> { startLocalPosition, takeOffLocalPosition, endLocalPosition };

            //var bound = CalculateSplineBound((Vector3)takeOffLocalPosition + startWorldPosition, endWorldPosition);
            //var maximumXBend = Mathf.Max(curveSetting.allowedBoundOverflow * bound.width / 2, );
            //var maximumYBend = Mathf.Max(curveSetting.allowedBoundOverflow * bound.height / 2, );
            //var bendX = Mathf.Clamp(Random.Range(curveSetting.bendX.x, curveSetting.bendX.y), curveSetting.minimumBendX, maximumXBend);
            //var bendY = Mathf.Clamp(Random.Range(curveSetting.bendY.x, curveSetting.bendY.y), curveSetting.minimunBendY, maximumYBend);
            //bendLocalPosition = Direction / 2 + Direction switch
            //{
            //    var d when d.x <= 0 && d.y > 0 => new Vector3(-bendX, -bendY, 0),
            //    var d when d.x > 0 && d.y > 0 => new Vector3(-bendX, bendY, 0),
            //    var d when d.x > 0 && d.y <= 0 => new Vector3(bendX, -bendY, 0),
            //    var d when d.x <= 0 && d.y <= 0 => new Vector3(-bendX, -bendY, 0),
            //    _ => Vector3.zero
            //};

            Spline = SplineFactory.CreateCatmullRom(KnotPositions);
            Container.Spline = Spline;

            if (SplineAnimate != null)
            {
                Destroy(SplineAnimate);
                _splineAnimate = null;
            }
            _splineAnimate = gameObject.AddComponent<SplineAnimate>();

            SplineAnimate.ObjectUpAxis = SplineComponent.AlignAxis.ZAxis;
            SplineAnimate.ObjectForwardAxis = SplineComponent.AlignAxis.YAxis;
            SplineAnimate.Alignment = SplineAnimate.AlignmentMode.None;

            SplineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
            SplineAnimate.MaxSpeed = curveSetting.takeOffSpeed;
            SplineAnimate.Easing = SplineAnimate.EasingMode.None;
            SplineAnimate.Loop = SplineAnimate.LoopMode.Once;

            SplineAnimate.Container = Container;
            SplineAnimate.Updated += OnSplineUpdate;
        }
        State = FlyBombState.Flying;
    }


    private void HandleCrossExplodeEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == crossExplodeEventName)
        {
            GameBoardManager.instance.FlyBombLaunchDamageAt(TakeOffPosition, CrossExplodeClear);
        }
    }


    private void HandleFindTargetEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == findTargetPieceEventName)
        {
            TargetPiece = GameBoardManager.instance.FindFlyBombTargetPiece(transform.position, CarryPowerupId);
            StartFly();
        }
    }


    private void OnSplineUpdate(Vector3 position, Quaternion rotatiion)
    {
        if (TargetPiece != null &&
            !TargetPiece.SelectedToReplace &&
            TargetPiece.CurrentState != global::State.Disposed)
        {
            Speed = Mathf.Clamp(Speed + curveSetting.flyAccelerationCurve.Evaluate(SplineAnimate.NormalizedTime) * Time.deltaTime, curveSetting.minSpeed, curveSetting.maxSpeed);
            SplineAnimate.MaxSpeed = Speed;

            var newScale = Mathf.Clamp(transform.localScale.x + curveSetting.flyScaleVariationCurve.Evaluate(SplineAnimate.NormalizedTime) * Time.deltaTime, curveSetting.minScale, curveSetting.maxScale);
            transform.localScale = new Vector3(newScale, newScale, newScale);

            Distance = Vector3.Distance(transform.position, TargetPiece.GetWorldPosition());
            if (Distance <= curveSetting.decisionDistance &&
                !Locked)
            {
                var lockPosition = TargetPiece.MovingToSlot == null ? TargetPiece.GridPosition : TargetPiece.MovingToSlot.GridPosition;
                LockSlot = GameBoardManager.instance.slotGrid[lockPosition];
                LockSlot.IncreaseEnterAndLeaveLock();

                Locked = true;
            }
        }
    }



    private void OnChangeTargetPiece()
    {
        TargetPiece = GameBoardManager.instance.FindFlyBombTargetPiece(transform.position, CarryPowerupId);

        if (TargetPiece != null)
        {
            if (Locked && LockSlot != null)
            {
                LockSlot.DecreaseEnterAndLeaveLock();
                Locked = false;
            }

            Vector3 startWorldPosition = transform.position;
            Vector3 endWorldPosition = TargetPiece.GetWorldPosition();

            Container.transform.SetPositionAndRotation(startWorldPosition, Quaternion.identity);

            Distance = Vector3.Distance(startWorldPosition, endWorldPosition);
            Direction = endWorldPosition - startWorldPosition;

            float3 startLocalPosition = Vector3.zero;
            float3 endLocalPosition = endWorldPosition - startWorldPosition;

            Vector3 tangent = Container.EvaluateTangent(SplineAnimate.NormalizedTime);
            float3 smoothLocalPosition = Vector3.zero;
            var angle = Vector3.Angle(tangent, Direction);
            if (angle >= 0f && angle < 180f)
            {
                smoothLocalPosition = (curveSetting.smoothness * tangent.normalized).RotateAroundAxis(AroundAxis, angle / 2);
            }
            else if (angle >= 180f && angle < 360f)
            {
                smoothLocalPosition = (curveSetting.smoothness * tangent.normalized).RotateAroundAxis(AroundAxis, 180f + angle / 2);
            }
            KnotPositions = new List<float3> { startLocalPosition, smoothLocalPosition, endLocalPosition };

            //var bound = CalculateSplineBound(startWorldPosition, endWorldPosition);
            //var bendX = Mathf.Min(curveSetting.allowedBoundOverflow * bound.width / 2, Random.Range(curveSetting.bendX.x, curveSetting.bendX.y));
            //var bendY = Mathf.Min(curveSetting.allowedBoundOverflow * bound.height / 2, Random.Range(curveSetting.bendY.x, curveSetting.bendY.y));
            //float3 bendPointLocalPosition = (Vector3)startLocalPosition + Direction * 0.8f + Direction switch
            //{
            //    var d when d.x <= 0 && d.y > 0 => new Vector3(-bendX, -bendY, 0),
            //    var d when d.x > 0 && d.y > 0 => new Vector3(-bendX, bendY, 0),
            //    var d when d.x > 0 && d.y <= 0 => new Vector3(bendX, -bendY, 0),
            //    var d when d.x <= 0 && d.y <= 0 => new Vector3(-bendX, -bendY, 0),
            //    _ => Vector3.zero
            //};

            Spline = SplineFactory.CreateCatmullRom(KnotPositions);
            Container.Spline = Spline;

            if (SplineAnimate != null)
            {
                Destroy(SplineAnimate);
                _splineAnimate = null;
            }

            _splineAnimate = gameObject.AddComponent<SplineAnimate>();

            SplineAnimate.ObjectUpAxis = SplineComponent.AlignAxis.ZAxis;
            SplineAnimate.ObjectForwardAxis = SplineComponent.AlignAxis.YAxis;
            SplineAnimate.Alignment = SplineAnimate.AlignmentMode.None;

            SplineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
            SplineAnimate.MaxSpeed = Speed;
            SplineAnimate.Easing = SplineAnimate.EasingMode.None;
            SplineAnimate.Loop = SplineAnimate.LoopMode.Once;

            SplineAnimate.Container = Container;
            SplineAnimate.Updated += OnSplineUpdate;
        }
    }


    public bool Tick()
    {
        if (State == FlyBombState.Initializing || State == FlyBombState.TakingOff)
        {
            return true;
        }
        else if (State == FlyBombState.Completed)
        {
            if (Locked && LockSlot != null)
            {
                LockSlot.DecreaseEnterAndLeaveLock();
                Locked = false;
            }

            Destroy(Container?.gameObject);
            Destroy(gameObject);
            return false;
        }

        if (Distance > curveSetting.decisionDistance)
        {
            if (TargetPiece == null ||
                TargetPiece.SelectedToReplace ||
                TargetPiece.CurrentState == global::State.Disposed)
            {
                OnChangeTargetPiece();
            }

            if (TargetPiece != null)
            {
                var knotNum = Spline.Count;
                var endKnot = Container.Spline[knotNum - 1];
                endKnot.Position = TargetPiece.GetWorldPosition() - Container.transform.position;

                Spline.SetKnot(knotNum - 1, endKnot);
            }
        }

        if (SplineAnimate.NormalizedTime >= 1f)
        {
            GameBoardManager.instance.FlyBombLandExplode(transform.position, CarryPowerupId, LandExplodeAnimation);

            State = FlyBombState.Completed;
        }
        return true;
    }


    private Rect CalculateSplineBound(Vector3 startPositionWorldPosition, Vector3 endPositionWorldPosition)
    {
        Rect bound = new(0, 0, 0, 0);
        bound.xMin = Mathf.Min(startPositionWorldPosition.x, endPositionWorldPosition.x);
        bound.xMax = Mathf.Max(startPositionWorldPosition.x, endPositionWorldPosition.x);
        bound.yMin = Mathf.Min(startPositionWorldPosition.x, endPositionWorldPosition.x);
        bound.yMax = Mathf.Max(startPositionWorldPosition.x, endPositionWorldPosition.x);
        return bound;
    }
}