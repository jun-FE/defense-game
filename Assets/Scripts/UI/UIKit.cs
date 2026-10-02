using UnityEngine;

/// <summary>
/// 메뉴/스토리 화면용 공통 IMGUI 스타일.
/// 1080p 기준 좌표로 그리면 해상도에 맞게 자동으로 확대/축소된다.
/// 사용: UIKit.Begin(); ... UIKit.End();
/// </summary>
public static class UIKit
{
    public const float ReferenceHeight = 1080f;

    public static float Scale => Screen.height / ReferenceHeight;
    public static float Width => Screen.width / Scale;
    public static float Height => ReferenceHeight;

    public static GUIStyle Title, Heading, Body, Small, Dialogue, Speaker;
    public static GUIStyle Button, ButtonSmall, ButtonSelected, Panel;

    static Texture2D dimTexture;

    public static void Begin()
    {
        EnsureStyles();
        GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
    }

    public static void End()
    {
        GUI.matrix = Matrix4x4.identity;
    }

    public static bool DrawButton(Rect rect, string text, GUIStyle style = null)
    {
        return GUI.Button(rect, text, style ?? Button);
    }

    public static void DrawPanel(Rect rect) => GUI.Box(rect, GUIContent.none, Panel);

    public static void DimScreen(float alpha = 0.65f)
    {
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Width, Height), dimTexture);
        GUI.color = old;
    }

    /// <summary>그림자가 있는 큰 글자.</summary>
    public static void ShadowLabel(Rect rect, string text, GUIStyle style)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(rect.x + 4f, rect.y + 4f, rect.width, rect.height), text, style);
        style.normal.textColor = old;
        GUI.Label(rect, text, style);
    }

    static void EnsureStyles()
    {
        // 텍스처는 씬 전환 후에도 남지만, 에디터에서 사라질 수 있어 매번 확인한다.
        if (Button != null && Button.normal.background != null) return;

        dimTexture = MakeTexture(Color.black);

        Title = MakeLabel(96, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Heading = MakeLabel(52, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Body = MakeLabel(30, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.92f, 0.92f, 0.95f));
        Small = MakeLabel(22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.75f, 0.78f, 0.85f));
        Dialogue = MakeLabel(34, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
        Dialogue.wordWrap = true;
        Speaker = MakeLabel(30, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));
        Speaker.normal.background = MakeTexture(new Color(0.12f, 0.14f, 0.22f, 0.95f));

        Button = MakeButton(32, new Color(0.17f, 0.22f, 0.32f, 0.92f), new Color(0.26f, 0.34f, 0.5f, 0.95f));
        ButtonSmall = MakeButton(24, new Color(0.17f, 0.22f, 0.32f, 0.85f), new Color(0.26f, 0.34f, 0.5f, 0.95f));
        ButtonSelected = MakeButton(24, new Color(0.85f, 0.6f, 0.2f, 0.95f), new Color(0.95f, 0.7f, 0.3f, 1f));

        Panel = new GUIStyle(GUI.skin.box);
        Panel.normal.background = MakeTexture(new Color(0.06f, 0.07f, 0.11f, 0.88f));
    }

    static GUIStyle MakeLabel(int size, FontStyle fontStyle, TextAnchor anchor, Color color)
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fontStyle, alignment = anchor, richText = true };
        style.normal.textColor = color;
        return style;
    }

    static GUIStyle MakeButton(int size, Color normal, Color hover)
    {
        var style = new GUIStyle(GUI.skin.button) { fontSize = size, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        style.normal.background = MakeTexture(normal);
        style.hover.background = MakeTexture(hover);
        style.active.background = MakeTexture(normal * 0.8f);
        style.normal.textColor = style.hover.textColor = style.active.textColor = Color.white;
        return style;
    }

    public static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
