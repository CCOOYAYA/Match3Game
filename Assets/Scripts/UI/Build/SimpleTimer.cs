using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SimpleTimer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;
    private float startTime;
    private float stopTime;
    private bool isactive = false;

    // Update is called once per frame
    void Update()
    {
        if (isactive)
            text.text = (Time.time - startTime).ToString("F2");
    }

    public void TimerStart()
    {
        if (!isactive)
            startTime = Time.time;
        isactive = true;
    }

    public void TimerStop()
    {
        stopTime = Time.time;
        isactive = false;
    }
}
