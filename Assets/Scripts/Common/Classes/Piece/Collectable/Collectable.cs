using BezierSolution;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class Collectable : MonoBehaviour
{
    [SerializeField] private int collectId;

    [SerializeField] private SpriteRenderer mainSpriteRenderer;
    [SerializeField] private SortingGroup mainSortingGroup;
    [SerializeField] private SpriteRenderer shadowSpriteRenderer;
    [SerializeField] private SortingGroup shadowSortingGroup;
    [SerializeField] private SortingGroup particleSortingGroup;
    [SerializeField] private List<ParticleSystem> trailParticles;
    
    [Header("Spline Control")]
    [SerializeField] private Vector3 dropCurveStartPointControlPointPositionOffset = new Vector3(-0.25f, -0.7f, 0f);
    [SerializeField] private float dropCurveEndPointPositionX = 1f;
    [SerializeField] private float dropCurveEndPointPositionY = -1.25f;
    [SerializeField] private float dropCurveEndPointControlPointPositionX = -0.6f;
    [SerializeField] private float reachInvokeDelay = 0f;


    private Vector3 levelTargetWorldPosition;
    private bool moveToLevelTarget;
    private BezierSpline dropSpline;
    private BezierSpline liftSpline;
    private BezierWalkerForCollectable walker;

    private void Awake()
    {
        walker = GetComponent<BezierWalkerForCollectable>();
    }

    #region Level Target Display
    public void StartBezierMoveToLevelTargetDisplay()
    {
        moveToLevelTarget = true;
        MainGameUIManager.Instance.OnGenerateCollectable(this, out int sortOrder);
        mainSortingGroup.sortingOrder = sortOrder;
        shadowSortingGroup.sortingOrder = sortOrder - 2;
        particleSortingGroup.sortingOrder = sortOrder - 1;

        levelTargetWorldPosition = MainGameUIManager.Instance.GetLevelTargetDisplayWorldPosition(collectId);

        dropSpline = new GameObject().AddComponent<BezierSpline>();
        dropSpline.Initialize(2);

        var slotInterval = GameBoardManager.instance.slotOriginalInterval;
        dropSpline[0].transform.position = transform.position;
        dropSpline[0].followingControlPointPosition = transform.position + dropCurveStartPointControlPointPositionOffset;
        //dropSpline[0].followingControlPointPosition = transform.position + new Vector3(0f, -GameBoardManager.instance.slotOriginalInterval * 0.75f, 0f);

        if (transform.position.x - 1.5 * slotInterval <= -GameManager.instance.ScreenWidth)
        {
            // 曲线向右
            dropSpline[1].transform.position = transform.position + new Vector3(dropCurveEndPointPositionX, dropCurveEndPointPositionY, 0f);
            dropSpline[1].precedingControlPointPosition = dropSpline[1].transform.position + new Vector3(dropCurveEndPointControlPointPositionX, 0f, 0f);
        }
        else
        {
            // 曲线向左
            dropSpline[1].transform.position = transform.position + new Vector3(-dropCurveEndPointPositionX, dropCurveEndPointPositionY, 0f);
            dropSpline[1].precedingControlPointPosition = dropSpline[1].transform.position + new Vector3(-dropCurveEndPointControlPointPositionX, 0f, 0f);
        }

        walker.Initialize(dropSpline, mainSpriteRenderer, shadowSpriteRenderer);
        TickBezierDropMove();
    }
    #endregion


    #region Reward Display
    public void StartBezierMoveToRewardDisplay()
    {
        MainGameUIManager.Instance.OnGenerateCollectable(this, out var order);
        mainSortingGroup.sortingOrder = order;
        shadowSortingGroup.sortingOrder = order - 2;
        particleSortingGroup.sortingOrder = order - 1;

        levelTargetWorldPosition = MainGameUIManager.Instance.GetRewardDisplayWorldPosition(collectId);

        dropSpline = new GameObject().AddComponent<BezierSpline>();
        dropSpline.Initialize(2);

        var slotInterval = GameBoardManager.instance.slotOriginalInterval;
        dropSpline[0].transform.position = transform.position;
        dropSpline[0].followingControlPointPosition = transform.position + dropCurveStartPointControlPointPositionOffset;

        if (transform.position.x - 1.5 * slotInterval <= -GameManager.instance.ScreenWidth)
        {
            // 曲线向右
            dropSpline[1].transform.position = transform.position + new Vector3(dropCurveEndPointPositionX + 0.75f * Random.Range(0, slotInterval), dropCurveEndPointPositionY, 0f);
            dropSpline[1].precedingControlPointPosition = dropSpline[1].transform.position + new Vector3(dropCurveEndPointControlPointPositionX + 0.4f * Random.Range(0, slotInterval), 0f, 0f);
        }
        else
        {
            // 曲线向左
            dropSpline[1].transform.position = transform.position + new Vector3(-dropCurveEndPointPositionX - 0.75f * Random.Range(0, slotInterval), dropCurveEndPointPositionY, 0f);
            dropSpline[1].precedingControlPointPosition = dropSpline[1].transform.position + new Vector3(-dropCurveEndPointControlPointPositionX- 0.4f * Random.Range(0, slotInterval), 0f, 0f);
        }

        walker.Initialize(dropSpline, mainSpriteRenderer, shadowSpriteRenderer);
        TickBezierDropMove();
    }
    #endregion

    private async void TickBezierDropMove()
    {
        while ((GameManager.instance.TokenSource == null || !GameManager.instance.TokenSource.Token.IsCancellationRequested)  &&
               walker.NormalizedT < 1f)
        {
            walker.Execute(Time.deltaTime);
            await UniTask.NextFrame();
        }

        SwitchBezierMoveToLift();
    }


    public void SwitchBezierMoveToLift()
    {
        liftSpline = new GameObject().AddComponent<BezierSpline>();
        liftSpline.Initialize(2);

        liftSpline[0].transform.position = transform.position;
        liftSpline[1].transform.position = levelTargetWorldPosition;

        liftSpline[0].followingControlPointPosition = dropSpline[1].transform.position + 2 * (dropSpline[1].followingControlPointPosition - dropSpline[1].transform.position);
        var reversedNormalized = (dropSpline[1].transform.position - levelTargetWorldPosition).normalized;
        liftSpline[1].precedingControlPointPosition = liftSpline[1].transform.position + reversedNormalized * 1.5f;

        Destroy(dropSpline.gameObject);
        walker.SwitchToLiftCurve(liftSpline);
        TickBezierLiftMove();
    }


    private async void TickBezierLiftMove()
    {
        while ((GameManager.instance.TokenSource == null || !GameManager.instance.TokenSource.Token.IsCancellationRequested) &&
               walker.NormalizedT < 1f)
        {
            walker.Execute(Time.deltaTime);
            await UniTask.NextFrame();
        }

        // 完成路径后延迟一段时间触发回调并销毁自己
        await UniTask.Delay(TimeSpan.FromSeconds(reachInvokeDelay));
        if (moveToLevelTarget)
        {
            MainGameUIManager.Instance.UpdateLevelTargetDisplay(collectId);
        }
        else
        {
            MainGameUIManager.Instance.UpdateRewardDisplay(collectId);
        }

        mainSpriteRenderer.SetAlpha(0f);
        // turn off particle system's emission
        var waitTime = 0f;
        trailParticles.ForEach(trail =>
        {
            if (trail != null) 
            {
                var emission = trail.emission;
                emission.enabled = false;

                var main = trail.main;
                waitTime = Mathf.Max(waitTime, main.startLifetime.constantMax);
            }
        });

        await UniTask.WaitForSeconds(waitTime); 
        MainGameUIManager.Instance.OnDestroyCollectable(this);
        Destroy(liftSpline.gameObject);
        Destroy(gameObject);
    }
}
