using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��Rainbow����������
/// </summary>
public class RainbowLine : MonoBehaviour
{
    private TrailRenderer _trailRenderer;
    public TrailRenderer TrailRenderer => _trailRenderer;

    [SerializeField] private float moveSpeed;                   // ����ٶ�
    [SerializeField] private AnimationCurve moveSpeedCurve;     // �ٶ�����
    [SerializeField] private int reachInvokeDelay;              // ����Ŀ�ĵغ�ȴ��ӳ��ڻ���ص�

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

        // ������߻ص�
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
