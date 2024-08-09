using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Random = UnityEngine.Random;
using UnityEngine;
using System.Linq;
using Unity.Mathematics;

public static class Util
{
    /// <summary>
    /// Set spriterenderer color alpha value
    /// </summary>
    /// <param name="spriteRenderer"></param>
    /// <param name="alpha"></param>
    public static void SetAlpha(this SpriteRenderer spriteRenderer, float alpha)
    {
        Color iniColor = spriteRenderer.color;
        spriteRenderer.color = new Color(iniColor.r, iniColor.g, iniColor.b, alpha);
    }


    /// <summary>
    /// 获取逆时针旋转后的向量
    /// </summary>
    public static Vector3 RotateAroundAxis(this Vector3 v, Vector3 axis, float degrees)
    {
        Quaternion rotation = Quaternion.AngleAxis(degrees, axis);
        return rotation * v;
    }


    public static bool WithinRangeX(this AnimationCurve curve, float value)
    {
        var range = GetRangeX(curve);
        return value >= range.x && value <= range.y;
    }


    public static float2 GetRangeX(this AnimationCurve curve) => new float2(curve.MinX(), curve.MaxX());


    public static float MinX(this AnimationCurve curve)
    {
        if (curve.keys.Length == 0)
            throw new ArgumentException("The AnimationCurve is empty.");

        float minX = curve.keys[0].time;
        foreach (Keyframe key in curve.keys)
        {
            if (key.time < minX)
                minX = key.time;
        }
        return minX;
    }


    public static float MaxX(this AnimationCurve curve)
    {
        if (curve.keys.Length == 0)
            throw new ArgumentException("The AnimationCurve is empty.");

        float maxX = curve.keys[0].time;
        foreach (Keyframe key in curve.keys)
        {
            if (key.time > maxX)
                maxX = key.time;
        }
        return maxX;
    }


    /// <summary>
    /// 获取棋子颜色数量
    /// </summary>
    public static int CountSelectedColors(PieceColors colors)
    {
        int count = 0;
        foreach (PieceColors color in Enum.GetValues(typeof(PieceColors)))
        {
            if (color != PieceColors.Colorless && colors.HasFlag(color))
            {
                count++;
            }
        }
        return count;
    }


    /// <summary>
    /// 获取一个随机数
    /// </summary>
    public static float GetRandomValue(Vector2 range) => Random.Range(range.x, range.y);


    public static string PrintArray<T> (T array) where T : IEnumerable
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in array)
        {
            sb.Append($"{item}, ");
        }
        return sb.ToString();
    }
}