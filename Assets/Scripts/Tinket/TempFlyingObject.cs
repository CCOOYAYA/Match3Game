using BezierSolution;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TempFlyingObject : MonoBehaviour
{
    [Header("Spline Control")]
    [SerializeField] private Vector3 dropCurveStartPointControlPointPositionOffset = new Vector3(-0.25f, -0.7f, 0f);
    [SerializeField] private float dropCurveEndPointPositionX = 1f;
    [SerializeField] private float dropCurveEndPointPositionY = -1.25f;
    [SerializeField] private float dropCurveEndPointControlPointPositionX = -0.6f;
    [SerializeField] private int reachInvokeDelay = 0;

    private Vector3 targetPosition;
    private System.Action OnArrive = null;
    private BezierSpline dropSpline;
    private BezierSpline liftSpline;
    private BezierWalkerForUIComponent walker;

    private void Awake()
    {
        walker = GetComponent<BezierWalkerForUIComponent>();
    }

    public void SetTargetPosition(Vector3 position, System.Action onArrive = null)
    {
        targetPosition = position;
        OnArrive = onArrive;
    }


    #region Reward Display
    public void StartBezierMoveToRewardDisplay()
    {
        dropSpline = new GameObject().AddComponent<BezierSpline>();
        dropSpline.Initialize(2);

        dropSpline[0].transform.position = transform.position;
        dropSpline[0].followingControlPointPosition = transform.position + dropCurveStartPointControlPointPositionOffset + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0f), 0f);

        dropSpline[1].transform.position = transform.position + new Vector3(dropCurveEndPointPositionX, dropCurveEndPointPositionY, 0f);
        dropSpline[1].precedingControlPointPosition = dropSpline[1].transform.position + new Vector3(dropCurveEndPointControlPointPositionX, 0f, 0f);

        walker.Initialize(dropSpline);
        TickBezierDropMove();
    }
    #endregion

    private async void TickBezierDropMove()
    {
        while (walker.NormalizedT < 1f)
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
        liftSpline[1].transform.position = targetPosition;

        liftSpline[0].followingControlPointPosition = dropSpline[1].transform.position + 2 * (dropSpline[1].followingControlPointPosition - dropSpline[1].transform.position);
        var reversedNormalized = (dropSpline[1].transform.position - targetPosition).normalized;
        liftSpline[1].precedingControlPointPosition = liftSpline[1].transform.position + reversedNormalized * 1.5f;

        Destroy(dropSpline.gameObject);
        walker.SwitchToLiftCurve(liftSpline);
        TickBezierLiftMove();
    }


    private async void TickBezierLiftMove()
    {
        while (walker.NormalizedT < 1f)
        {
            walker.Execute(Time.deltaTime);
            await UniTask.NextFrame();
        }

        // 完成路径后延迟一段时间触发回调并销毁自己
        await UniTask.DelayFrame(reachInvokeDelay);
        OnArrive?.Invoke();

        Destroy(liftSpline.gameObject);
        Destroy(gameObject);
    }
}
