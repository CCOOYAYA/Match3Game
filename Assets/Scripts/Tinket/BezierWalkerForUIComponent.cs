using BezierSolution;
using UnityEngine;


/// <summary>
/// 用于UI组件的跟随曲线运动
/// </summary>
[AddComponentMenu("Bezier Solution/BezierWalkerForUIComponent")]
public class BezierWalkerForUIComponent : BezierWalker
{
    [Header("Animation Settings")]
    [SerializeField]                private float maxSpeed;
    [SerializeField]                private AnimationCurve dropSpeedCurve;
    [SerializeField]                private AnimationCurve liftSpeedCurve;
    [SerializeField][Range(0f, 1f)] private float _normalizedT;

    private BezierSpline _spline;
    private AnimationCurve _useCurve;

    public bool DropCurveCompleted { get; private set; }
    public override BezierSpline Spline => _spline;
    public override bool MovingForward => true;
    public override float NormalizedT 
    { 
        get => _normalizedT;
        set => _normalizedT = value; 
    }


    public void Initialize(BezierSpline spline)
    {
        _spline = spline;
        _useCurve = dropSpeedCurve;
    }

    public void SwitchToLiftCurve(BezierSpline spline)
    {
        if (!DropCurveCompleted)
        {
            _spline = spline;
            _useCurve = liftSpeedCurve;
            _normalizedT = 0f;
            DropCurveCompleted = true;
        }
    }


    public override void Execute(float deltaTime)
    {
        if (NormalizedT < 1f)
        {
            float targetDistance = Mathf.Min(_useCurve.Evaluate(NormalizedT), 1f) * maxSpeed * deltaTime;
            Vector3 targetPos = Spline.MoveAlongSpline(ref _normalizedT, targetDistance);
            transform.position = targetPos;
        }
    }
}
