using UnityEngine;

/// <summary>
/// 프롤로그 화면 구성 기획(ArtSource/Prologue/프롤로그_화면구성.xlsx)의 대사창·조작 안내 툴팁.
/// - 대사창: 화면 아래 둥근 남색 반투명 패널, 은은한 가장자리 명암, 크림색 "화자 | 대사" 왼쪽 정렬.
/// - 조작 안내 툴팁: 화면 아래 가운데 작은 둥근 상자. 키는 작은 키캡(W A S D)으로 그린다.
/// - 획득 알림: "✦ 몽결정 획득 ✦" 같은 짧은 문구(보랏빛).
/// 패널 그림은 코드로 만든 둥근 사각형(9-slice)이라 그림 파일이 필요 없다. IMGUI, UIKit.Begin 뒤 1080p 기준 좌표.
/// </summary>
public static class DreamUI
{
    public static readonly Color Cream = new Color(0.96f, 0.91f, 0.80f);
    public static readonly Color CreamMuted = new Color(0.80f, 0.76f, 0.68f);
    public static readonly Color Violet = new Color(0.78f, 0.66f, 1f);

    static Texture2D dialogTex, tooltipTex, keyTex;
    static GUIStyle dialogStyle, tooltipStyle, keyStyle, noticeStyle;
    static GUIStyle dialogPanel, tooltipPanel, keyPanel;

    /// <summary>대사창 자리(화면 아래, 가로 84%).</summary>
    public static Rect DialogRect(float w, float h) => new Rect(w * 0.08f, h * 0.775f, w * 0.84f, h * 0.15f);

    /// <summary>대사창: "화자 | 대사". 화자가 없으면 대사만(나레이션). 다 나왔으면 오른쪽 아래 ▼ 깜빡임.</summary>
    public static void Dialog(float w, float h, string speaker, string text, bool complete)
    {
        Ensure();
        Rect r = DialogRect(w, h);
        if (Event.current.type == EventType.Repaint) dialogPanel.Draw(r, false, false, false, false);
        string line = string.IsNullOrEmpty(speaker) ? text : $"{speaker}   <color=#{Hex(CreamMuted)}>|</color>   {text}";
        GUI.Label(new Rect(r.x + 56f, r.y + 18f, r.width - 140f, r.height - 36f), line, dialogStyle);
        if (complete && Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0)
            GUI.Label(new Rect(r.xMax - 70f, r.yMax - 52f, 40f, 36f), "▼", tooltipStyle);
    }

    /// <summary>조작 안내 툴팁(화면 아래 가운데). keys를 주면 글 앞에 키캡을 그린다(예: W A S D).</summary>
    public static void Tooltip(float w, float h, string text, params string[] keys)
    {
        Ensure();
        float keySize = 34f, gap = 8f;
        float keysW = keys.Length > 0 ? keys.Length * (keySize + gap) + 10f : 0f;
        float textW = tooltipStyle.CalcSize(new GUIContent(text)).x;
        float bw = Mathf.Clamp(keysW + textW + 64f, 260f, w - 80f), bh = 58f;
        var r = new Rect((w - bw) / 2f, h * 0.865f, bw, bh);
        if (Event.current.type == EventType.Repaint) tooltipPanel.Draw(r, false, false, false, false);
        float x = r.x + 32f;
        foreach (string k in keys)
        {
            var kr = new Rect(x, r.center.y - keySize / 2f, keySize, keySize);
            if (Event.current.type == EventType.Repaint) keyPanel.Draw(kr, false, false, false, false);
            GUI.Label(kr, k, keyStyle);
            x += keySize + gap;
        }
        if (keys.Length > 0) x += 10f;
        GUI.Label(new Rect(x, r.y, r.xMax - 32f - x, bh), text, tooltipStyle);
    }

    /// <summary>획득 알림 같은 짧은 문구(가운데 정렬, 보랏빛, 그림자).</summary>
    public static void Notice(Rect r, string text, float alpha = 1f)
    {
        Ensure();
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        UIKit.ShadowLabel(r, text, noticeStyle);
        GUI.color = old;
    }

    static void Ensure()
    {
        if (dialogStyle != null) return;
        dialogTex = RoundedTexture(new Color(0.07f, 0.08f, 0.19f, 0.80f), new Color(0.02f, 0.02f, 0.07f, 0.92f), 30, 0.55f);
        tooltipTex = RoundedTexture(new Color(0.06f, 0.06f, 0.14f, 0.82f), new Color(0.03f, 0.03f, 0.08f, 0.9f), 22, 0.4f, new Color(0.92f, 0.86f, 0.72f, 0.55f));
        keyTex = RoundedTexture(new Color(0.16f, 0.16f, 0.26f, 0.95f), new Color(0.10f, 0.10f, 0.18f, 0.95f), 8, 0.2f, new Color(0.92f, 0.86f, 0.72f, 0.8f));
        dialogPanel = Panel(dialogTex, 40);
        tooltipPanel = Panel(tooltipTex, 28);
        keyPanel = Panel(keyTex, 10);

        dialogStyle = Label(30, Cream, TextAnchor.MiddleLeft, false);
        dialogStyle.wordWrap = true;
        tooltipStyle = Label(20, Cream, TextAnchor.MiddleLeft, false);
        keyStyle = Label(18, Cream, TextAnchor.MiddleCenter, true);
        noticeStyle = Label(22, Violet, TextAnchor.MiddleCenter, true);
    }

    static GUIStyle Panel(Texture2D t, int border)
    {
        var s = new GUIStyle { border = new RectOffset(border, border, border, border) };
        s.normal.background = t;
        return s;
    }

    static GUIStyle Label(int size, Color color, TextAnchor anchor, bool bold)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, richText = true, font = UIKit.FontFor(bold), fontStyle = UIKit.StyleFor(bold) };
        s.normal.textColor = s.hover.textColor = s.active.textColor = color;
        return s;
    }

    static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

    /// <summary>둥근 사각형 그림(9-slice용, 128px). 가운데 색 → 가장자리로 갈수록 edge 색(은은한 명암), 선택적으로 얇은 테두리.</summary>
    static Texture2D RoundedTexture(Color inner, Color edge, int radius, float edgeWidth, Color? outline = null)
    {
        const int n = 128;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // 둥근 사각형까지의 거리(안쪽이 음수)
                float cx = Mathf.Clamp(x + 0.5f, radius, n - radius), cy = Mathf.Clamp(y + 0.5f, radius, n - radius);
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy)) - radius;
                float a = Mathf.Clamp01(0.5f - d);  // 가장자리 1px 부드럽게
                // 가장자리 명암: 테두리에서 안쪽으로 edgeWidth*반폭 만큼 edge 색으로
                float inside = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y)) / (n * 0.5f);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(inside / Mathf.Max(0.01f, edgeWidth)));
                Color c = Color.Lerp(edge, inner, k);
                if (outline.HasValue && d > -2.2f && d <= 0.5f) c = Color.Lerp(c, outline.Value, outline.Value.a);
                c.a *= a;
                px[y * n + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
