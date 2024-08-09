using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EventBase : MonoBehaviour
{
    [SerializeField] protected int eventID;
    public int EventID => eventID;
    [SerializeField] protected EventType type;
    public EventType EventType => type;
    private bool lockFlag = false;
    public bool LockFlag => lockFlag;

    /// <summary>
    /// 读取存储数据/检查开启状态/优先级
    /// </summary>
    /// <returns></returns>
    public abstract bool ActiveCheck();

    /// <summary>
    /// UI进场前初始化显示/后台结算数据
    /// </summary>
    /// <param name="parent"></param>
    public abstract void ShowMe(Transform parent);

    /// <summary>
    /// 进场动画表现
    /// </summary>
    public abstract void OnSceneStart();

    /// <summary>
    /// 需要中断/弹窗的部分/需要包含解锁按钮操作
    /// </summary>
    /// <returns></returns>
    public abstract UniTask StartCallback();
}

public enum EventType { Collect, Normal, Purchase };
