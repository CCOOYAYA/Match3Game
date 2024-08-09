using BezierSolution;
using Cysharp.Threading.Tasks;
using DG.Tweening.Core.Easing;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Bezier Solution/Bezier Walker For StreakPowerup")]
public class BezierWalkerForStreakPowups : BezierWalker
{
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private ParticleSystem trail;

    [Header("Animation Settings")]
    [SerializeField]                private float maxSpeed;
    [SerializeField]                private AnimationCurve moveCurve;
    [SerializeField]                private AnimationCurve scaleCurve;
    [SerializeField][Range(0f, 1f)] private float _normalizedT;

    private BezierSpline _spline;

    public override BezierSpline Spline => _spline;
    public override bool MovingForward => true;
    public override float NormalizedT
    {
        get => _normalizedT;
        set => _normalizedT = value;
    }

    public void Initialize(BezierSpline spline, Action assignCompleteCallback)
    {
        _spline = spline;
        TickMove(assignCompleteCallback).Forget();
    }

    private async UniTask TickMove(Action assignCompleteCallback)
    {
        trail.gameObject.SetActive(true);

        while (NormalizedT < 1f)
        {
            Execute(Time.deltaTime);
            await UniTask.NextFrame();
        }

        Color color = skeletonAnimation.skeleton.GetColor();
        color.a = 0f;
        skeletonAnimation.Skeleton.SetColor(color);

        var emission = trail.emission;
        emission.enabled = false;

        assignCompleteCallback?.Invoke();

        await UniTask.WaitForSeconds(trail.main.startLifetime.constantMax);
        Destroy(_spline.gameObject);
        Destroy(gameObject);
    }


    public override void Execute(float deltaTime)
    {
        if (NormalizedT < 1f)
        {
            float targetDistance = Mathf.Min(moveCurve.Evaluate(NormalizedT), 1f) * maxSpeed * deltaTime;
            Vector3 targetPos = Spline.MoveAlongSpline(ref _normalizedT, targetDistance);
            transform.position = targetPos;

            float scale = scaleCurve.Evaluate(NormalizedT);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
