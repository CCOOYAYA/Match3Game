using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(MatchShape))]
public class PowerupEditor : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement root = new VisualElement();
        root.style.width = Length.Percent(100);
        root.style.flexDirection = FlexDirection.Row;

        VisualElement shapeSection = new VisualElement();
        shapeSection.style.width = Length.Percent(50);

        VisualElement settingSection = new VisualElement();
        settingSection.style.width = Length.Percent(50);

        root.Add(shapeSection);
        root.Add(settingSection);

        VisualElement mirrorSection = new VisualElement()
        {
            style = { flexDirection = FlexDirection.Row }
        };
        mirrorSection.Add(new PropertyField(property.FindPropertyRelative(nameof(MatchShape.canMirror)), ""));
        mirrorSection.Add(new Label("Can be Mirrored"));

        VisualElement rotateSection = new VisualElement()
        {
            style = { flexDirection = FlexDirection.Row }
        };
        rotateSection.Add(new PropertyField(property.FindPropertyRelative(nameof(MatchShape.canRotate)), ""));
        rotateSection.Add(new Label("Can be Rotated"));

        settingSection.Add(mirrorSection);
        settingSection.Add(rotateSection);

        CreateUI(property, shapeSection);

        return root;
    }

    private int FindHeight(SerializedProperty property)
    {
        int yMin = int.MaxValue;
        int yMax = int.MinValue;

        for (int i = 0; i < property.arraySize; ++i)
        {
            var y = property.GetArrayElementAtIndex(i).vector3IntValue.y;

            if (y < yMin) yMin = y;
            else if (y > yMax) yMax = y;
        }

        return yMax - yMin + 1;
    }

    private void CreateUI(SerializedProperty property, VisualElement root)
    {
        root.Clear();

        var positions = property.FindPropertyRelative(nameof(MatchShape.positionList));
        List<GridPosition> rebuiltCells = new();

        for (int i = 0; i < positions.arraySize; ++i)
        {
            var element = positions.GetArrayElementAtIndex(i);
            int x = element.FindPropertyRelative("X").intValue;
            int y = element.FindPropertyRelative("Y").intValue;
            rebuiltCells.Add(new GridPosition(x, y));
        }

        var centerCell = property.FindPropertyRelative(nameof(MatchShape.centerPosition));
        var centerX = centerCell.FindPropertyRelative("X").intValue;
        var centerY = centerCell.FindPropertyRelative("Y").intValue;
        GridPosition centerPos = new(centerX, centerY);

        var bound = MatchShape.GetBoundOf(rebuiltCells);

        for (int y = bound.height + 1; y >= -1; --y)
        {
            var line = new VisualElement();
            line.name = "Line" + y;
            line.style.width = Length.Percent(100);
            line.style.height = 18;
            line.style.flexDirection = FlexDirection.Row;
            root.Add(line);

            for (int x = bound.x - 1; x <= bound.width + 2; ++x)
            {
                var realPos = new GridPosition(x, y + bound.yMin);

                VisualElement newElem = null;

                if (rebuiltCells.Contains(realPos))
                {
                    var l = new Label
                    {
                        text = realPos.Equals(centerPos) ? "C" : "-"
                    };

                    l.style.backgroundColor = realPos.Equals(centerPos) ? Color.red : Color.black;

                    l.RegisterCallback<ClickEvent>(evt =>
                    {
                        if (evt.shiftKey)
                        {
                            MarkCellAsCenter(property, realPos);
                        }
                        else
                        {
                            RemoveCell(property, rebuiltCells.IndexOf(realPos));
                        }
                        CreateUI(property, root);
                    });

                    newElem = l;
                }
                else if (rebuiltCells.Contains(realPos + GridPosition.Right) ||
                         rebuiltCells.Contains(realPos + GridPosition.Down) ||
                         rebuiltCells.Contains(realPos + GridPosition.Left) ||
                         rebuiltCells.Contains(realPos + GridPosition.Up))
                {
                    var l = new Label
                    {
                        text = "+"
                    };

                    l.RegisterCallback<ClickEvent>(evt =>
                    {
                        AddNewCell(property, realPos);
                        CreateUI(property, root);
                    });

                    newElem = l;
                }
                else
                {
                    var l = new Label
                    {
                        text = " "
                    };

                    newElem = l;
                }

                newElem.style.unityTextAlign = TextAnchor.MiddleCenter;
                newElem.style.width = 18;
                line.Add(newElem);
            }
        }
    }

    private void RemoveCell(SerializedProperty property, int index)
    {
        property.serializedObject.Update();

        var positions = property.FindPropertyRelative(nameof(MatchShape.positionList));
        positions.DeleteArrayElementAtIndex(index);

        property.serializedObject.ApplyModifiedProperties();
    }

    private void AddNewCell(SerializedProperty property, GridPosition cell)
    {
        property.serializedObject.Update();

        var positions = property.FindPropertyRelative(nameof(MatchShape.positionList));

        positions.InsertArrayElementAtIndex(positions.arraySize);
        var lastElement = positions.GetArrayElementAtIndex(positions.arraySize - 1);
        lastElement.FindPropertyRelative("X").intValue = cell.X;
        lastElement.FindPropertyRelative("Y").intValue = cell.Y;
        Debug.Log($"Addeed local position({cell.X}, {cell.Y})");

        property.serializedObject.ApplyModifiedProperties();
    }

    private void MarkCellAsCenter(SerializedProperty property, GridPosition cell)
    {
        property.serializedObject.Update();

        var centerCell = property.FindPropertyRelative(nameof(MatchShape.centerPosition));
        centerCell.FindPropertyRelative("X").intValue = cell.X;
        centerCell.FindPropertyRelative("Y").intValue = cell.Y;
        Debug.Log($"<color=red>Marked</color> local position({cell.X}, {cell.Y})");

        property.serializedObject.ApplyModifiedProperties();
    }
}
