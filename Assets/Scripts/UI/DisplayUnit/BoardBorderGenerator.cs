using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardBorderGenerator : MonoBehaviour
{
    [SerializeField] SpriteRenderer protoSprite;
    [SerializeField] UISpriteDict spriteDict;
    [SerializeField] float distance = 1.2f;

    public void GenerateBorder()
    {
        var gameLevel = UserDataManager.GameLevel;

        var xMax = gameLevel.xMax;
        var yMax = gameLevel.yMax;

        bool[,] board = new bool[xMax + 4, yMax + 4];
        int[,] intboard = new int[xMax + 4, yMax + 4];

        for (int i = 0; i < xMax + 4; i++)
            for (int j = 0; j < yMax + 4; j++)
            {
                board[i, j] = false;
                intboard[i, j] = 0;
            }


        foreach (var array in gameLevel.slotInfo)
        {
            int x = array[0] + 2, y = array[1] + 2;
            board[x, y] = array[2] == 1;
        }

        for (int i = 1; i < xMax + 3; i++)
            for (int j = 1; j < yMax + 3; j++)
                if (!board[i, j])
                {
                    if (board[i, j - 1])
                        intboard[i, j] += 1;
                    if (board[i + 1, j])
                        intboard[i, j] += 2;
                    if (board[i, j + 1])
                        intboard[i, j] += 4;
                    if (board[i - 1, j])
                        intboard[i, j] += 8;
                    if ((!board[i + 1, j]) && (!board[i, j - 1]) && (board[i + 1, j - 1]))
                        intboard[i, j] += 16;
                    if ((!board[i + 1, j]) && (!board[i, j + 1]) && (board[i + 1, j + 1]))
                        intboard[i, j] += 32;
                    if ((!board[i - 1, j]) && (!board[i, j + 1]) && (board[i - 1, j + 1]))
                        intboard[i, j] += 64;
                    if ((!board[i - 1, j]) && (!board[i, j - 1]) && (board[i - 1, j - 1]))
                        intboard[i, j] += 128;
                }

        Vector3 topLeftPos = Vector3.left * (xMax + 1) * distance * 0.5f + Vector3.up * (yMax + 1) * distance * 0.5f;

        for (int i = 1; i < xMax + 3; i++)
            for (int j = 1; j < yMax + 3; j++)
                if (0< intboard[i,j])
                {
                    var border = Instantiate<SpriteRenderer>(protoSprite, Vector3.zero, Quaternion.identity, transform);
                    border.transform.localPosition = topLeftPos + Vector3.right * (i - 1) * distance + Vector3.down * (j - 1) * distance;
                    border.sprite = spriteDict.SpriteDictionary[intboard[i, j]];
                }
    }
}
