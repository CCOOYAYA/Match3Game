using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从Rainbow发出的射线
/// </summary>
public class RainbowLine : MonoBehaviour
{
    private TrailRenderer _trailRenderer;
    public TrailRenderer TrailRenderer => _trailRenderer;

    [SerializeField] private float moveSpeed;                   // 最大速度
    [SerializeField] private AnimationCurve moveSpeedCurve;     // 速度曲线
    [SerializeField] private int reachInvokeDelay;              // 到达目的地后等待延迟在唤起回调

    private Piece destinationPiece;
    private float moveTime;

    private Action<Piece> reachCallback;
    private Action<RainbowLine> destroyCallback;

    private void Awake()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
    }

    public void Initialize(Vector3 startPosition, Piece targetPiece, Action<Piece> reachCallback, Action<RainbowLine> destroyCallback)
    {
        transform.position = startPosition;
        destinationPiece = targetPiece;

        this.reachCallback = reachCallback;
        this.destroyCallback = destroyCallback;

        StartMoveLineToEndPosition().Forget();
    }


    private async UniTask StartMoveLineToEndPosition()
    {
        while (Vector3.Distance(transform.position, destinationPiece.GetWorldPosition()) >= 0.025f)
        {
            MoveToEndPosition();
            await UniTask.NextFrame();
        }

        // 完成连线回调
        await UniTask.Delay(reachInvokeDelay);
        reachCallback?.Invoke(destinationPiece);
    }


    private void MoveToEndPosition()
    {
        moveTime += Time.deltaTime;
        var moveDistance = moveSpeedCurve.Evaluate(moveTime) * Time.deltaTime * moveSpeed;
        transform.position = Vector3.MoveTowards(transform.position, destinationPiece.GetWorldPosition(), moveDistance);
    }


    private void OnDestroy()
    {
        destroyCallback?.Invoke(this);
    }
}
