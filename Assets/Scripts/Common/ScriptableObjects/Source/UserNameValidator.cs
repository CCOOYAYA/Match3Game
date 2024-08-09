using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "UserNameValidator", menuName = "SO/UserNameValidator")]
public class UserNameValidator : TMP_InputValidator
{
    [SerializeField] LocalizedString invalidChar;

    public override char Validate(ref string text, ref int pos, char ch)
    {
        bool canInsert = false;
        if ('a' <= ch && ch <= 'z')
            canInsert = true;
        if ('A' <= ch && ch <= 'Z')
            canInsert = true;
        if (char.IsNumber(ch))
            canInsert = true;
        if ((!canInsert) && (ch != ' '))
            HomeSceneUIManager.PopupText(invalidChar.GetLocalizedString());
        if ((ch == ' ') && (pos != 0) && (text[pos - 1] != ' ') && ((pos == text.Length) || (text[pos] != ' ')))
            canInsert = true;
        if (11 < text.Length)
            canInsert = false;

        if (canInsert)
        {
            //return ch;
            text = text.Insert(pos, ch.ToString());
            pos++;
        }
        return '\0';
    }
}
