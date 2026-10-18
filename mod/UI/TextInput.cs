using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace APKingdom2Crown;

// A minimal IMGUI text box. GUILayout.TextField can't be used in this game: Unity's TextEditor was
// stripped from the IL2CPP build ("Method unstripping failed"), so typing is read from Input.inputString
// in Update and the box is drawn as a button that takes focus when clicked.
internal sealed class TextInput
{
    private static TextInput _focused;
    private static int _escapeFrame = -10;

    public string Text = "";
    public string Placeholder = "";
    public bool Masked;
    public int MaxLength = 200;
    public Action<string> OnSubmit;

    // Stays true for a frame after Esc leaves a box, so that same Esc press doesn't also open the pause menu.
    public static bool AnyFocused => _focused != null || Time.frameCount <= _escapeFrame + 1;
    public bool Focused => _focused == this;

    public void Focus() => _focused = this;
    public static void ClearFocus() => _focused = null;

    public void Draw(GUIStyle style, Il2CppReferenceArray<GUILayoutOption> options)
    {
        string shown;
        if (Text.Length == 0 && !Focused)
        {
            shown = $"<color=#7A705C>{Placeholder}</color>";
        }
        else
        {
            shown = Masked ? new string('*', Text.Length) : AP.Escape(Text);
            if (Focused) shown += (Time.unscaledTime % 1f) < 0.55f ? "|" : " ";
        }
        if (Gui.Button(shown, Focused ? Styles.TextFieldFocused : style, options)) Focus();
    }

    // Main thread, once per frame.
    public static void HandleKeyboard()
    {
        var field = _focused;
        if (field == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _escapeFrame = Time.frameCount;
            ClearFocus();
            return;
        }

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrl && Input.GetKeyDown(KeyCode.V))
        {
            try { field.Append(GUIUtility.systemCopyBuffer); }
            catch { /* clipboard unavailable */ }
            return;
        }
        if (ctrl && Input.GetKeyDown(KeyCode.Backspace))
        {
            field.Text = "";
            return;
        }

        foreach (var c in Input.inputString)
        {
            if (c == '\b')
            {
                if (field.Text.Length > 0) field.Text = field.Text.Substring(0, field.Text.Length - 1);
            }
            else if (c == '\n' || c == '\r')
            {
                field.OnSubmit?.Invoke(field.Text);
                return;
            }
            else if (!char.IsControl(c))
            {
                field.Append(c.ToString());
            }
        }
    }

    private void Append(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        s = s.Replace("\r", "").Replace("\n", "");
        Text = (Text + s).Length > MaxLength ? (Text + s).Substring(0, MaxLength) : Text + s;
    }
}
