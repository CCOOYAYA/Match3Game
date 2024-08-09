using BezierSolution;
using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ����FlyBomb�ĸ��������˶�
/// </summary>
[AddComponentMenu("Bezier Solution/Bezier Walker For FlyBomb")]
public class BezierWalkerForFlyBomb : BezierWalker
{
    [Header("Animation Options")]
    [SerializeField]                private float maxSpeed;
    [SerializeField]                private AnimationCurve launchSpeedCurve;    // ����ʱ�ٶ�����, ������ΪTֵ, ������Ϊ�ٶȰٷֱ�
    [SerializeField]                private AnimationCurve moveSpeedCurve;      // ������ٶ�����, ������ΪTֵ, ������Ϊ�ٶȰٷֱ�
    [SerializeField][Range(0f, 1f)] private float _normalizedT = 0f;            // ��һ����Tֵ

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
    /// ��ʼ��������
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
            // �������·���ص�
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
