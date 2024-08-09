using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugCanvasOverlay : MonoBehaviour
{
    private Canvas debugCanvas;

    [Header("FPS Counter")]
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text ftText;

    private Dictionary<int, string> CachedNumberStrings = new();
    private int[] _frameRateSamples;
    private readonly int _cacheNumbersAmount = 300;
    private readonly int _averageFromAmount = 30;
    private int _averageCounter = 0;
    private int _currentAveraged;

    private void Awake()
    {
        // Cache strings and create array
        {
            for (int i = 0; i < _cacheNumbersAmount; i++)
            {
                CachedNumberStrings[i] = i.ToString();
            }
            _frameRateSamples = new int[_averageFromAmount];
        }
    }


    private void Update()
    {
        // Sample
        var currentFrame = (int)Mathf.Round(1f / Time.smoothDeltaTime);
        _frameRateSamples[_averageCounter] = currentFrame;

        // Average
        var average = 0f;

        foreach (var frameRate in _frameRateSamples)
        {
            average += frameRate;
        }

        _currentAveraged = (int)Mathf.Round(average / _averageFromAmount);
        _averageCounter = (_averageCounter + 1) % _averageFromAmount;

        // Assign to UI
        fpsText.text = _currentAveraged switch
        {
            var x when x >= 0 && x < _cacheNumbersAmount => CachedNumberStrings[x],
            var x when x >= _cacheNumbersAmount => $"> {_cacheNumbersAmount}",
            var x when x < 0 => "< 0",
            _ => "?"
        };
        fpsText.text += " FPS";

        ftText.text = (Math.Round(Time.deltaTime, 4) * 1000).ToString() + " ms";
    }
}
