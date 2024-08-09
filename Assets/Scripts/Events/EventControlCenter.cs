using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventControlCenter : MonoBehaviour
{
    [SerializeField] EventBase[] events;
    [SerializeField] Transform[] eventDisplayerParent;

    public void EventInit()
    {
        foreach (var eventUnit in events)
        {
            if (eventUnit.ActiveCheck())
                eventUnit.ShowMe(eventDisplayerParent[(int)eventUnit.EventType]);
        }
    }

    public async UniTask EventStartCheck()
    {
        foreach (var eventUnit in events)
            eventUnit.OnSceneStart();
        foreach (var eventUnit in events)
            await eventUnit.StartCallback();
    }
}
