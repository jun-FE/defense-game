using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기술(꿈 수틀) 화면. 메인 화면 아래 탭 "기술"로 연다.
/// 왼쪽: 나무 수틀 위 거미줄 모양 기술 나무(Mastery). 매듭(단추)을 눌러 고르고, 실(금빛 매듭 점)이 수놓은 매듭끼리 이어진다.
/// 오른쪽: 실마리 수, 고른 매듭 설명, "수놓기" 리본 버튼, 모두 풀기.
/// 그림: Resources/Mastery/ms_*.png (Artlist 생성 → Tools/Art/build_mastery_ui.py), 패널·도장은 도감 그림(Resources/Codex) 재사용.
/// IMGUI, 1080p 기준 좌표. 선 회전 없이 작은 매듭 점을 늘어놓아 실을 그린다.
/// </summary>
public class MasteryPanel
{
    static readonly Color Ink = new Color(0.18f, 0.13f, 0.10f), InkMuted = new Color(0.42f, 0.34f, 0.27f),
        Brown = new Color(0.54f, 0.33f, 0.13f), Light = new Color(0.95f, 0.92f, 0.87f), Gold = new Color(0.88f, 0.75f, 0.49f);

    // 수틀 그림(1109×1200) 안 리넨 원: 중심(비율)과 반지름(가로 비율)
    const float HoopAspect = 1200f / 1109f, LinenCx = 0.4994f, LinenCy = 0.537f, LinenR = 0.46f;

    Mastery.Node selected;
    string message;
    float messageUntil, learnedFlash;
    Mastery.Node flashNode;

    readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    /// <summary>기술 화면을 그린다. 닫기를 누르면 false.</summary>
    public bool Draw(float w, float h)
    {
        UIKit.DimScreen(0.6f);
        if (selected == null) selected = Mastery.Nodes[0];

        const float panelW = 470f;
        float hoopW = Mathf.Min(780f, (h - 150f) / HoopAspect);
        float total = hoopW + 50f + panelW;
        var hoop = new Rect((w - total) / 2f, 52f, hoopW, hoopW * HoopAspect);
        var panel = new Rect(hoop.xMax + 50f, hoop.y + 70f, panelW, Mathf.Min(720f, hoop.height - 90f));

        Image(hoop, "Mastery/ms_hoop");
        var center = new Vector2(hoop.x + hoop.width * LinenCx, hoop.y + hoop.height * LinenCy);
        float radius = hoop.width * LinenR * 0.98f;

        Mastery.Node hovered = DrawWeb(center, radius);
        DrawPanel(panel, hovered ?? selected);

        var close = new Rect(panel.xMax - 70f, panel.y - 40f, 88f, 90f);
        Image(close, "Codex/cx_seal");
        GUI.Label(close, "닫기", Style(17, Light, TextAnchor.MiddleCenter, true));
        return !UIKit.ClickButton(close, GUIContent.none, GUIStyle.none);
    }

    // ───────── 수틀(거미줄) ─────────

    Mastery.Node DrawWeb(Vector2 center, float radius)
    {
        bool repaint = Event.current.type == EventType.Repaint;
        if (repaint)
        {
            // 장식 실(기술과 상관없음): 큰 매듭에서 수틀 테두리까지 버팀실, 큰 매듭끼리 바깥 고리
            Mastery.Node prevKey = null, firstKey = null;
            foreach (Mastery.Node n in Mastery.Nodes)
            {
                if (n.kind != Mastery.NodeKind.Keystone) continue;
                Stitch(Pos(n, center, radius), Pos(1.0f, n.angle, center, radius), SizeOf(n) * 0.5f, 0f, false, 0.45f);
                if (prevKey != null) Stitch(Pos(prevKey, center, radius), Pos(n, center, radius), SizeOf(n) * 0.5f, SizeOf(n) * 0.5f, false, 0.45f);
                if (firstKey == null) firstKey = n;
                prevKey = n;
            }
            if (prevKey != null && firstKey != prevKey)
                Stitch(Pos(prevKey, center, radius), Pos(firstKey, center, radius), SizeOf(prevKey) * 0.5f, SizeOf(firstKey) * 0.5f, false, 0.45f);
            // 기술 실
            foreach (var thread in Mastery.Threads())
            {
                Mastery.Node a = thread.a, b = thread.b;
                bool lit = Mastery.IsLearned(a) && Mastery.IsLearned(b);
                bool open = Mastery.IsLearned(a) || Mastery.IsLearned(b);
                Stitch(Pos(a, center, radius), Pos(b, center, radius), SizeOf(a) * 0.5f, SizeOf(b) * 0.5f, lit, open ? 1f : 0.75f);
            }
        }

        Mastery.Node hovered = null;
        Vector2 mouse = Event.current.mousePosition;
        foreach (Mastery.Node n in Mastery.Nodes)
        {
            Vector2 p = Pos(n, center, radius);
            float size = SizeOf(n);
            var r = new Rect(p.x - size / 2f, p.y - size / 2f, size, size);
            if (Vector2.Distance(mouse, p) < size * 0.5f) hovered = n;
            DrawNode(n, r);
            if (UIKit.ClickButton(r, GUIContent.none, GUIStyle.none)) selected = n;
        }
        return hovered;
    }

    void DrawNode(Mastery.Node n, Rect r)
    {
        bool learned = Mastery.IsLearned(n), reachable = Mastery.IsReachable(n);
        bool keystone = n.kind == Mastery.NodeKind.Keystone;
        // 고른 매듭: 금빛 테두리 고리
        if (n == selected) Image(Grow(r, 1.32f), "Mastery/ms_knot_on", 0.55f);

        string frame;
        float grow;
        if (n.kind == Mastery.NodeKind.Center) { frame = "ms_center"; grow = 1f; }
        else if (keystone) { frame = learned ? "ms_key_on" : "ms_key_locked"; grow = learned ? 1.16f : 1f; }
        else if (learned) { frame = "ms_node_on"; grow = 1.09f; }
        else if (reachable) { frame = "ms_node_open"; grow = 1.06f; }
        else { frame = "ms_node_locked"; grow = 1f; }

        // 수놓을 수 있는 매듭은 숨 쉬듯 반짝
        float alpha = 1f;
        if (!learned && reachable && Mastery.FreePoints >= n.cost) alpha = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f);
        // 방금 수놓은 매듭은 잠깐 크게
        float pop = n == flashNode ? 1f + 0.25f * Mathf.Clamp01((learnedFlash - Time.unscaledTime) / 0.4f) : 1f;
        Image(Grow(r, grow * pop), "Mastery/" + frame, alpha);

        // 안쪽 그림: 도구 매듭은 그 도구(고리 단계 = 도구 단계), 공방 매듭은 아이콘
        string inner = null;
        if (n.towerId != null)
            inner = $"Codex/Entries/{n.towerId}_{(n.kind == Mastery.NodeKind.Inner ? 1 : n.kind == Mastery.NodeKind.Spoke ? 2 : 3)}";
        else if (n.icon != null) inner = "Codex/" + n.icon;
        if (inner == null) return;
        Color old = GUI.color;
        if (!learned) GUI.color = reachable ? new Color(0.85f, 0.82f, 0.78f, 0.9f) : new Color(0.55f, 0.52f, 0.5f, 0.6f);
        Image(Grow(r, keystone ? 0.5f : 0.6f), inner);
        GUI.color = old;
    }

    /// <summary>두 매듭 사이 실: 매듭 점을 일정 간격으로 늘어놓는다. 양끝은 매듭 반지름만큼 비운다.</summary>
    void Stitch(Vector2 a, Vector2 b, float padA, float padB, bool lit, float alpha)
    {
        Vector2 d = b - a;
        float len = Vector2.Distance(a, b);
        if (len <= padA + padB + 4f) return;
        Vector2 dir = d / len;
        float step = lit ? 13f : 15f, size = lit ? 16f : 13f;
        Texture2D t = Tex(lit ? "Mastery/ms_knot_on" : "Mastery/ms_knot_off");
        if (t == null) return;
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        for (float s = padA + step * 0.5f; s < len - padB; s += step)
        {
            Vector2 p = a + dir * s;
            GUI.DrawTexture(new Rect(p.x - size / 2f, p.y - size / 2f, size, size), t);
        }
        GUI.color = old;
    }

    static float SizeOf(Mastery.Node n)
    {
        switch (n.kind)
        {
            case Mastery.NodeKind.Center: return 124f;
            case Mastery.NodeKind.Keystone: return 100f;
            case Mastery.NodeKind.Spoke: return 74f;
            case Mastery.NodeKind.Weave: return 60f;
            default: return 66f;
        }
    }

    static Vector2 Pos(Mastery.Node n, Vector2 c, float r) => Pos(n.radius, n.angle, c, r);

    static Vector2 Pos(float radius, float angle, Vector2 c, float r)
    {
        float a = angle * Mathf.Deg2Rad;
        return c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * r;
    }

    // ───────── 오른쪽 설명 ─────────

    void DrawPanel(Rect p, Mastery.Node n)
    {
        Frame(p, "Codex/cx_slot", 48);
        float x = p.x + 40f, wInner = p.width - 80f, y = p.y + 34f;

        GUI.Label(new Rect(x, y, wInner, 40f), "기술 · 꿈 수틀", Style(28, Ink, TextAnchor.MiddleLeft, true));
        y += 44f;
        Image(new Rect(x, y, 34f, 48f), "Mastery/ms_point");
        GUI.Label(new Rect(x + 44f, y, wInner - 44f, 48f), $"실마리  {Mastery.FreePoints} / {Mastery.TotalPoints}", Style(24, Brown, TextAnchor.MiddleLeft, true));
        y += 50f;
        GUI.Label(new Rect(x, y, wInner, 24f), $"기본 {Mastery.BasePoints}개 + 의뢰를 깰 때마다 {Mastery.PointsPerClear}개", Style(15, InkMuted, TextAnchor.MiddleLeft));
        y += 26f;
        Image(new Rect(p.center.x - 190f, y, 380f, 42f), "Codex/cx_divider");
        y += 52f;

        // 고른 매듭
        var icon = new Rect(x, y, 96f, 96f);
        DrawNode(n, icon);
        float tx = icon.xMax + 16f, tw = p.xMax - 40f - tx;
        GUI.Label(new Rect(tx, y + 4f, tw, 40f), n.name, Style(26, Ink, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(tx, y + 44f, tw, 24f), KindLabel(n), Style(16, InkMuted, TextAnchor.MiddleLeft));
        if (n.kind != Mastery.NodeKind.Center)
            GUI.Label(new Rect(tx, y + 68f, tw, 24f), $"실마리 {n.cost}개", Style(16, Brown, TextAnchor.MiddleLeft, true));
        y += 112f;

        GUIStyle body = Style(18, Ink, TextAnchor.UpperLeft, false, true);
        float bh = body.CalcHeight(new GUIContent(n.desc), wInner);
        GUI.Label(new Rect(x, y, wInner, bh), n.desc, body);
        y += bh + 12f;

        bool learned = Mastery.IsLearned(n), reachable = Mastery.IsReachable(n);
        string state = learned ? (n.kind == Mastery.NodeKind.Center ? "처음부터 수놓아져 있어요." : "수놓았어요. 전투에 적용돼요.")
            : !reachable ? "이어진 매듭을 먼저 수놓아 주세요."
            : Mastery.FreePoints < n.cost ? "실마리가 모자라요. 의뢰를 깨면 더 생겨요." : "수놓을 수 있어요.";
        GUI.Label(new Rect(x, y, wInner, 26f), state, Style(16, learned ? Brown : InkMuted, TextAnchor.MiddleLeft, learned));

        // 수놓기 리본
        var button = new Rect(p.center.x - 150f, p.yMax - 170f, 300f, 104f);
        bool can = Mastery.CanLearn(n);
        if (!learned)
        {
            Image(button, can ? "Mastery/ms_ribbon" : "Mastery/ms_ribbon_off");
            GUI.Label(new Rect(button.x, button.y - 8f, button.width, button.height), "수놓기", Style(24, can ? Light : new Color(0.9f, 0.88f, 0.85f, 0.8f), TextAnchor.MiddleCenter, true));
            if (UIKit.ClickButton(button, GUIContent.none, GUIStyle.none))
            {
                if (Mastery.Learn(n))
                {
                    flashNode = n;
                    learnedFlash = Time.unscaledTime + 0.4f;
                    SoundFx.Play("upgrade", 0.8f);
                }
                else Show(state);
            }
        }

        int count = 0;
        foreach (Mastery.Node m in Mastery.Nodes) if (m.kind != Mastery.NodeKind.Center && Mastery.IsLearned(m)) count++;
        GUI.Label(new Rect(x, p.yMax - 62f, wInner * 0.55f, 26f), $"수놓은 매듭 {count} / {Mastery.Nodes.Count - 1}", Style(15, InkMuted, TextAnchor.MiddleLeft));
        var reset = new Rect(p.xMax - 40f - 150f, p.yMax - 64f, 150f, 30f);
        GUI.Label(reset, "<u>모두 풀기</u>", Style(16, count > 0 ? Brown : InkMuted, TextAnchor.MiddleRight, true));
        if (count > 0 && UIKit.ClickButton(reset, GUIContent.none, GUIStyle.none))
        {
            Mastery.Unlearn();
            Show("실을 모두 풀었어요. 실마리를 돌려받았어요.");
        }

        if (!string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
            GUI.Label(new Rect(p.x, p.yMax + 8f, p.width, 30f), message, Style(18, Gold, TextAnchor.MiddleCenter, true));
    }

    static string KindLabel(Mastery.Node n)
    {
        switch (n.kind)
        {
            case Mastery.NodeKind.Center: return "시작 매듭";
            case Mastery.NodeKind.Weave: return "공방 매듭 · 모든 전투";
            case Mastery.NodeKind.Keystone: return "큰 매듭 · " + ToolName(n.towerId);
            default: return "도구 매듭 · " + ToolName(n.towerId);
        }
    }

    static string ToolName(string towerId)
    {
        foreach (CodexData.Entry e in CodexData.Tools) if (e.id == towerId) return e.name;
        return towerId;
    }

    void Show(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 2f;
    }

    // ───────── 그리기 도구 ─────────

    static Rect Grow(Rect r, float f) => new Rect(r.center.x - r.width * f / 2f, r.center.y - r.height * f / 2f, r.width * f, r.height * f);

    Texture2D Tex(string path)
    {
        if (!tex.TryGetValue(path, out Texture2D t))
        {
            t = Resources.Load<Texture2D>(path);
            tex[path] = t;
        }
        return t;
    }

    void Image(Rect r, string path, float alpha = 1f)
    {
        Texture2D t = Tex(path);
        if (t == null) return;
        Color old = GUI.color;
        if (alpha < 1f) GUI.color = new Color(old.r, old.g, old.b, old.a * alpha);
        GUI.DrawTexture(r, t, ScaleMode.ScaleToFit);
        GUI.color = old;
    }

    /// <summary>9-slice 틀(모서리 border 픽셀은 늘리지 않음).</summary>
    void Frame(Rect r, string path, int border)
    {
        if (Event.current.type != EventType.Repaint) return;
        string key = "frame:" + path;
        if (!styles.TryGetValue(key, out GUIStyle style))
        {
            style = new GUIStyle { border = new RectOffset(border, border, border, border) };
            style.normal.background = Tex(path);
            styles[key] = style;
        }
        if (style.normal.background != null) style.Draw(r, false, false, false, false);
    }

    GUIStyle Style(int size, Color color, TextAnchor anchor, bool bold = false, bool wrap = false)
    {
        string key = $"{size}:{color}:{anchor}:{bold}:{wrap}";
        if (styles.TryGetValue(key, out GUIStyle style)) return style;
        style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, wordWrap = wrap, richText = true, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
        styles[key] = style;
        return style;
    }
}
