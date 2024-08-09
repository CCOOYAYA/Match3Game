using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Powerup : Piece
{
    public List<MatchShape> shapes;
    public List<MatchShape> coreShapes;

    [SerializeField] private AudioSource activateAudio;


    public override void Damage(Damage sourceDamage,
                                Action<Vector3, AnimationReferenceAsset> onDamagePlay, 
                                Action<int, Vector3> onDamageCollectTarget, 
                                Action<Piece> onDamageControlPosition)
    {
        return;
    }


    public virtual void MarkAsUsed(float alpha = 0f, bool invokeOnDamageCallback = true)
    {
        Used = true;
        SetAlpha(alpha);

        if (invokeOnDamageCallback)
        {
            OnDamageCallback?.Invoke();
        }
    }


    /// <summary>
    /// 销毁自身
    /// </summary>
    public virtual void DestroySelf()
    {
        GameBoardManager.instance.DestroyPowerup(this);
    }


    public virtual void StandAloneActivateCallback(Func<IGameBoardAction> standAloneActivateCallback)
    {
        SkeletonAnimation.AnimationState.Complete += delegate
        {
            GameBoardManager.instance.AddAction(standAloneActivateCallback?.Invoke());      // 动画完成后激活
        };
    }


    public GridPosition GetMatchCenterPosition(List<GridPosition> matchedPositions)
    {
        var res = matchedPositions.FirstOrDefault();
        int maxConnectCount = 0;
        int maxConnectDirections = 0;
        int maxConnectVH = 0;

        var directions = new List<GridPosition>() { GridPosition.Up, GridPosition.Right, GridPosition.Down, GridPosition.Left };
        matchedPositions.ForEach(position =>
        {
            // 计算每个位置十字连通的棋子数量
            int thisConnectCount = 0;
            int thisConnectDirections = 0;
            bool thisConnectV = false, thisConnectH = false;

            foreach (var direction in directions)
            {
                var lookupDirection = position + direction;
                bool thisDirectionConnect = false;

                while (matchedPositions.Contains(lookupDirection))
                {
                    thisConnectCount++;

                    if (!thisDirectionConnect)
                    {
                        thisDirectionConnect = true;
                        thisConnectDirections++;
                    }

                    if (!thisConnectV &&
                        (direction.Equals(GridPosition.Down) || direction.Equals(GridPosition.Up)))
                    {
                        thisConnectV = true;
                    }

                    if (!thisConnectH &&
                        (direction.Equals(GridPosition.Left) || direction.Equals(GridPosition.Right)))
                    {
                        thisConnectH = true;
                    }

                    lookupDirection += direction;
                }
            }

            int thisConnectVH = (thisConnectV ? 1 : 0) + (thisConnectH ? 1 : 0);
            if (thisConnectVH > maxConnectVH ||
                (thisConnectVH == maxConnectVH && thisConnectDirections > maxConnectDirections) ||
                (thisConnectVH == maxConnectVH && thisConnectDirections > maxConnectDirections && thisConnectCount > maxConnectCount))
            {
                maxConnectCount = thisConnectCount;
                maxConnectDirections = thisConnectDirections;
                maxConnectVH = thisConnectVH;
                res = position;
            }
        });

        return res;
    }
}


[Serializable]
public class MatchShape : ISerializationCallbackReceiver
{
    [Tooltip("镜像检测")] public bool canMirror;
    [Tooltip("旋转检测")] public bool canRotate;
    public List<GridPosition> positionList = new();
    public GridPosition centerPosition;

    public RectInt bounds = new(0, 0, 0, 0);

    private List<GridPosition> pos90Rot = new();
    private List<GridPosition> pos180Rot = new();
    private List<GridPosition> pos270Rot = new();

    private List<GridPosition> posHMirror = new();
    private List<GridPosition> posVMirror = new();


    public void OnBeforeSerialize()
    {

    }


    public void OnAfterDeserialize()
    {
        if (positionList.Count == 0)
        {
            positionList.Add(GridPosition.Zero);
        }

        bounds = GetBoundOf(positionList);

        pos90Rot.Clear();
        pos180Rot.Clear();
        pos270Rot.Clear();

        posHMirror.Clear();
        posVMirror.Clear();

        foreach (var position in positionList)
        {
            GetRotation(new GridPosition(bounds.min.x, bounds.min.y), position, out var rot90, out var rot180, out var rot270);

            pos90Rot.Add(rot90 + new GridPosition(0, bounds.width));
            pos180Rot.Add(rot180 + new GridPosition(bounds.width, bounds.height));
            pos270Rot.Add(rot270 + new GridPosition(bounds.height, 0));

            var x = bounds.xMax - (position.X - bounds.xMin);
            posHMirror.Add(new GridPosition(x, position.Y));

            var y = bounds.yMax - (position.Y - bounds.yMin);
            posVMirror.Add(new GridPosition(position.X, y));
        }
    }

    public static RectInt GetBoundOf(List<GridPosition> slotList)
    {
        if (slotList.Count == 0)
            return new RectInt(0, 0, 0, 0);

        RectInt rect = new RectInt(slotList[0].X, slotList[0].Y, 0, 0);
        for (int i = 1; i < slotList.Count; i++)
        {
            var slot = slotList[i];
            if (rect.xMin > slot.X)
            {
                rect.xMin = slot.X;
            }
            else if (rect.xMax < slot.X)
            {
                rect.xMax = slot.X;
            }

            if (rect.yMin > slot.Y)
            {
                rect.yMin = slot.Y;
            }
            else if (rect.yMax < slot.Y)
            {
                rect.yMax = slot.Y;
            }
        }
        return rect;
    }


    private void GetRotation(GridPosition pivot, GridPosition point,
        out GridPosition rot90, out GridPosition rot180, out GridPosition rot270)
    {
        var toPoint = point - pivot;

        rot90 = new GridPosition(toPoint.Y, -toPoint.X) + pivot;
        rot180 = new GridPosition(-toPoint.X, -toPoint.Y) + pivot;
        rot270 = new GridPosition(-toPoint.Y, toPoint.X) + pivot;
    }

    public bool FitIn(List<GridPosition> positions, ref List<GridPosition> matchedPositions)
    {
        var targetBound = GetBoundOf(positions);

        var largestBoundSize = Mathf.Max(targetBound.width, targetBound.height);
        var smallestBoundSize = Mathf.Min(targetBound.width, targetBound.height);

        for (int y = targetBound.yMin; y <= targetBound.yMax - smallestBoundSize + 1; y++)
        {
            for (int x = targetBound.xMin; x <= targetBound.xMax - smallestBoundSize + 1; x++)
            {
                List<GridPosition> matchingPos = new();
                List<GridPosition> matching90Pos = new();
                List<GridPosition> matching180Pos = new();
                List<GridPosition> matching270Pos = new();
                List<GridPosition> matchingHMirrorPos = new();
                List<GridPosition> matchingVMirrorPos = new();

                for (int iy = 0; iy <= largestBoundSize; iy++)
                {
                    for (int ix = 0; ix <= largestBoundSize; ix++)
                    {
                        var normalShapePos = new GridPosition(ix + bounds.xMin, iy + bounds.yMin);
                        var localPos = new GridPosition(x + ix, y + iy);

                        if (positions.Contains(localPos))
                        {
                            if (positionList.Contains(normalShapePos))
                                matchingPos.Add(localPos);

                            if (pos90Rot.Contains(normalShapePos))
                                matching90Pos.Add(localPos);

                            if (pos180Rot.Contains(normalShapePos))
                                matching180Pos.Add(localPos);

                            if (pos270Rot.Contains(normalShapePos))
                                matching270Pos.Add(localPos);

                            if (posHMirror.Contains(normalShapePos))
                                matchingHMirrorPos.Add(localPos);

                            if (posVMirror.Contains(normalShapePos))
                                matchingVMirrorPos.Add(localPos);
                        }
                    }
                }

                List<GridPosition> usableList = null;
                int count = positionList.Count;
                if (matchingPos.Count == count)
                {
                    usableList = matchingPos;
                }

                if (usableList == null && canRotate)
                {
                    if (matching90Pos.Count == count)
                    {
                        usableList = matching90Pos;
                    }
                    else if (matching180Pos.Count == count)
                    {
                        usableList = matching180Pos;
                    }
                    else if (matching270Pos.Count == count)
                    {
                        usableList = matching270Pos;
                    }
                }

                if (usableList == null && canMirror)
                {
                    if (matchingHMirrorPos.Count == count)
                    {
                        usableList = matchingHMirrorPos;
                    }
                    else if (matchingVMirrorPos.Count == count)
                    {
                        usableList = matchingVMirrorPos;
                    }
                }

                if (usableList != null)
                {
                    matchedPositions.AddRange(usableList);
                    return true;
                }
            }
        }

        return false;
    }


    public bool FitInCore(List<GridPosition> positions, ref List<GridPosition> matchedPositions, out int rotation)
    {
        var targetBound = GetBoundOf(positions);

        var largestBoundSize = Mathf.Max(targetBound.width, targetBound.height);
        var smallestBoundSize = Mathf.Min(targetBound.width, targetBound.height);

        rotation = 0;
        for (int y = targetBound.yMin; y <= targetBound.yMax - smallestBoundSize + 1; y++)
        {
            for (int x = targetBound.xMin; x <= targetBound.xMax - smallestBoundSize + 1; x++)
            {
                List<GridPosition> matchingPos = new();
                List<GridPosition> matching90Pos = new();
                List<GridPosition> matching180Pos = new();
                List<GridPosition> matching270Pos = new();

                for (int iy = 0; iy <= largestBoundSize; iy++)
                {
                    for (int ix = 0; ix <= largestBoundSize; ix++)
                    {
                        var normalShapePos = new GridPosition(ix + bounds.xMin, iy + bounds.yMin);
                        var localPos = new GridPosition(x + ix, y + iy);

                        if (positions.Contains(localPos))
                        {
                            if (positionList.Contains(normalShapePos))
                                matchingPos.Add(localPos);

                            if (pos90Rot.Contains(normalShapePos))
                                matching90Pos.Add(localPos);

                            if (pos180Rot.Contains(normalShapePos))
                                matching180Pos.Add(localPos);

                            if (pos270Rot.Contains(normalShapePos))
                                matching270Pos.Add(localPos);
                        }
                    }
                }

                List<GridPosition> usableList = null;
                int count = positionList.Count;
                if (matchingPos.Count == count)
                {
                    usableList = matchingPos;
                }

                if (usableList == null)
                {
                    if (matching90Pos.Count == count)
                    {
                        rotation = 90;
                        usableList = matching90Pos;
                    }
                    else if (matching180Pos.Count == count)
                    {
                        rotation = 180;
                        usableList = matching180Pos;
                    }
                    else if (matching270Pos.Count == count)
                    {
                        rotation = 270;
                        usableList = matching270Pos;
                    }
                }


                if (usableList != null)
                {
                    matchedPositions.AddRange(usableList);
                    return true;
                }
            }
        }

        return false;
    }
}