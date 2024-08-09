using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SimpleCountdown : MonoBehaviour, IUpdatePerSecond
{
    [SerializeField] protected TextMeshProUGUI text;
    private float targetTime;

    public virtual void Tick()
    {
        float deltaTime = targetTime - Time.unscaledTime;
        if (0 < deltaTime)
            TimeDisplay(deltaTime);
        else
        {
            text.text = "Finished";
            MainClock.UnregisterCUPS(this);
        }
    }

    public void SetTargetTime(float time)
    {
        targetTime = Time.unscaledTime + time;
        MainClock.RegisterCUPS(this);
    }

    public void TimeDisplay(float time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        text.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }
}
