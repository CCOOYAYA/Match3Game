using BezierSolution;
using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 用于FlyBomb的跟随曲线运动
/// </summary>
[AddComponentMenu("Bezier Solution/Bezier Walker For FlyBomb")]
public class BezierWalkerForFlyBomb : BezierWalker
{
    [Header("Animation Options")]
    [SerializeField]                private float maxSpeed;
    [SerializeField]                private AnimationCurve launchSpeedCurve;    // 发射时速度曲线, 横坐标为T值, 纵坐标为速度百分比
    [SerializeField]                private AnimationCurve moveSpeedCurve;      // 发射后速度曲线, 横坐标为T值, 纵坐标为速度百分比
    [SerializeField][Range(0f, 1f)] private float _normalizedT = 0f;            // 归一化的T值

    public UnityEvent OnCompleteLaunchEvent;
    public UnityEvent OnCompleteFlyEvent;

    public bool Initialized { get; private set; } = false;
    public bool LaunchCompleted { get; private set; } = false;
    public override BezierSpline Spline => _spline;
    public override float NormalizedT 
    { 
        get => _normalizedT; 
        set => _normalizedT = value;
    }
    public override bool MovingForward => true;

    private BezierSpline _spline;
    private AnimationCurve _speedCurve;
    private bool _invokedCompleteLaunchCallback;
    private bool _invokedCompleteWalkeCallback;

    /// <summary>
    /// 初始化跟随器
    /// </summary>
    public void Initialize(BezierSpline launchSpline)
    {
        _spline = launchSpline;
        _speedCurve = launchSpeedCurve;
        Initialized = true;
    }


    public void SwitchToMoveSpline(BezierSpline moveSpline)
    {
        _spline = moveSpline;
        _speedCurve = moveSpeedCurve;
        NormalizedT = 0f;
        LaunchCompleted = true;
    }

    public override void Execute(float deltaTime)
    {
        if (NormalizedT < 1f)
        {
            float targetDistance = Mathf.Min(_speedCurve.Evaluate(NormalizedT), 1f) * maxSpeed * deltaTime;
            Vector3 targetPos = Spline.MoveAlongSpline(ref _normalizedT, targetDistance);
            transform.position = targetPos;
        }
        else
        {
            // 唤起完成路径回调
            if (!LaunchCompleted)
            {
                if (!_invokedCompleteLaunchCallback)
                {
                    OnCompleteLaunchEvent?.Invoke();
                    _invokedCompleteLaunchCallback = true;
                }
            }
            else
            {
                if (!_invokedCompleteWalkeCallback)
                {
                    OnCompleteFlyEvent?.Invoke();
                    _invokedCompleteWalkeCallback = true;
                }
            }
        }
    }
}
