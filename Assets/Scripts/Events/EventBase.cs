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
    /// ��ȡ�洢����/��鿪��״̬/���ȼ�
    /// </summary>
    /// <returns></returns>
    public abstract bool ActiveCheck();

    /// <summary>
    /// UI����ǰ��ʼ����ʾ/��̨��������
    /// </summary>
    /// <param name="parent"></param>
    public abstract void ShowMe(Transform parent);

    /// <summary>
    /// ������������
    /// </summary>
    public abstract void OnSceneStart();

    /// <summary>
    /// ��Ҫ�ж�/�����Ĳ���/��Ҫ����������ť����
    /// </summary>
    /// <returns></returns>
    public abstract UniTask StartCallback();
}

public enum EventType { Collect, Normal, Purchase };
