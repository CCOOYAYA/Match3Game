using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;

public interface IUpdatePerSecond
{
    void Tick();
}

public class MainClock : MonoBehaviour
{
    private static MainClock Instance;
    private int lastSecond = -1;
    private List<IUpdatePerSecond> componentList = new();

    private void Awake()
    {
        Instance = this;
        componentList.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        //if (componentList.Count <= 0)
        //    return;

        TimeSpan timeSpan = TimeSpan.FromSeconds(Time.unscaledTime);
        if (timeSpan.Seconds != lastSecond)
        {
            UserDataManager.UpdateTick();
            for (int i = 0; i < componentList.Count; i++)
                componentList[i].Tick();
            lastSecond = timeSpan.Seconds;
        }
    }

    public static void RegisterCUPS(IUpdatePerSecond component)
    {
        if (!Instance.componentList.Contains(component))
            Instance.componentList.Add(component);
    }

    public static void UnregisterCUPS(IUpdatePerSecond component)
    {
        Instance.componentList.Remove(component);
    }
}
