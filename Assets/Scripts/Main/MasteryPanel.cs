using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기술(꿈 수틀) 화면. 메인 화면 아래 탭 "기술"로 연다.
/// 왼쪽: 도구 목록(해금 안 된 도구는 자물쇠). 가운데: 고른 도구의 수틀 — 거미줄 모양 강화 나무(Mastery.Tree).
///   가운데 도구 → 안쪽 고리 기본 손질 3개 → 바깥 살 3줄 속성 분기(각성 → 심화). 속성 매듭은 속성 색으로 빛난다.
/// 오른쪽: 실마리, 고른 매듭 설명, "수놓기", 속성 고르기(전투에는 하나만), 이 도구 모두 풀기.
/// 그림: Resources/Mastery/ms_*.png (Tools/Art/build_mastery_ui.py), 양피지·도장·아이콘은 도감 그림(Resources/Codex) 재사용.
/// IMGUI, 1080p 기준 좌표. 선 회전 없이 작은 매듭 점을 늘어놓아 실을 그린다.
/// </summary>
public class MasteryPanel
{
    static readonly Color Ink = new Color(0.18f, 0.13f, 0.10f), InkMuted = new Color(0.42f, 0.34f, 0.27f),
        Brown = new Color(0.54f, 0.33f, 0.13f), Light = new Color(0.95f, 0.92f, 0.87f), Gold = new Color(0.88f, 0.75f, 0.49f);

    // 수틀 그림(1109×1200) 안 리넨 원: 중심(비율)과 반지름(가로 비율)
    const float HoopAspect = 1200f / 1109f, LinenCx = 0.4994f, LinenCy = 0.537f, LinenR = 0.46f;

    string towerId;
    Mastery.Node selected;
    string message;
    float messageUntil, learnedFlash;
    Mastery.Node flashNode;

    readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    Mastery.Tree Tree => Mastery.TreeOf(towerId);

    /// <summary>기술 화면을 그린다. 닫기를 누르면 false.</summary>
    public bool Draw(float w, float h)
    {
        UIKit.DimScreen(0.6f);
        if (towerId == null) Select(Mastery.TowerOrder[0]);

        const float listW = 250f, panelW = 450f, gap = 28f;
        float hoopW = Mathf.Min(700f, (h - 170f) / HoopAspect);
        float total = listW + gap + hoopW + gap + panelW;
        float x0 = (w - total) / 2f;
        var list = new Rect(x0, 96f, listW, h - 230f);
        var hoop = new Rect(list.xMax + gap, 46f, hoopW, hoopW * HoopAspect);
        var panel = new Rect(hoop.xMax + gap, 96f, panelW, h - 230f);

        DrawTowerList(list);
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

    void Select(string id)
    {
        towerId = id;
        selected = Mastery.TreeOf(id).center;
    }

    // ───────── 왼쪽: 도구 목록 ─────────

    void DrawTowerList(Rect r)
    {
        GUI.Label(new Rect(r.x, r.y - 50f, r.width, 40f), "수선 도구", Style(24, Light, TextAnchor.MiddleLeft, true));
        float rowH = Mathf.Min(104f, (r.height - 5 * 10f) / 6f);
        float y = r.y;
        foreach (string id in Mastery.TowerOrder)
        {
            var card = new Rect(r.x, y, r.width, rowH);
            bool unlocked = TowerUnlocks.IsUnlocked(id), on = id == towerId;
            Color old = GUI.color;
            if (!unlocked) GUI.color = new Color(0.55f, 0.52f, 0.5f, 1f); // 잠긴 도구는 어둡게
            Frame(card, on ? "Codex/cx_slot_on" : "Codex/cx_slot", 40);
            GUI.color = old;
            var art = new Rect(card.x + 14f, card.y + 10f, rowH - 20f, rowH - 20f);
            if (unlocked) Image(art, $"Codex/Entries/{id}_1");
            else Image(Grow(art, 0.6f), "Codex/cx_icon_unknown");
            string name = unlocked ? ToolName(id) : "???";
            GUI.Label(new Rect(art.xMax + 10f, card.y + 14f, card.xMax - art.xMax - 20f, 30f), name, Style(19, Ink, TextAnchor.MiddleLeft, true));
            Mastery.Branch active = unlocked ? Mastery.ActiveBranch(id) : null;
            string sub = !unlocked ? "잠김" : active != null ? active.name : "속성 없음";
            Color subColor = active != null ? Color.Lerp(active.color, Ink, 0.45f) : InkMuted;
            GUI.Label(new Rect(art.xMax + 10f, card.y + 44f, card.xMax - art.xMax - 20f, 26f), sub, Style(15, subColor, TextAnchor.MiddleLeft, active != null));
            if (UIKit.ClickButton(card, GUIContent.none, GUIStyle.none) && !on) Select(id);
            y += rowH + 10f;
        }
    }

    // ───────── 가운데: 수틀(거미줄) ─────────

    Mastery.Node DrawWeb(Vector2 center, float radius)
    {
        Mastery.Tree t = Tree;
        bool repaint = Event.current.type == EventType.Repaint;
        if (repaint)
        {
            // 장식 실: 6갈래 살과 세 고리(거미줄 모양). 기술과 상관없이 흐리게.
            for (int k = 0; k < 6; k++)
            {
                float a = -90f + k * 60f;
                Stitch(Pos(0.12f, a, center, radius), Pos(1f, a, center, radius), 0f, 0f, false, 0.3f, Color.white);
                foreach (float ring in new[] { 0.32f, 0.6f, 0.86f })
                    Stitch(Pos(ring, a, center, radius), Pos(ring, a + 60f, center, radius), 8f, 8f, false, 0.3f, Color.white);
            }
            foreach (var thread in Mastery.Threads(t))
            {
                Mastery.Node a = thread.a, b = thread.b;
                bool lit = Mastery.IsLearned(a) && Mastery.IsLearned(b);
                bool open = Mastery.IsLearned(a) || Mastery.IsLearned(b);
                Mastery.Branch br = b.branch ?? a.branch;
                Color tint = lit && br != null ? Color.Lerp(Color.white, br.color, 0.7f) : Color.white;
                Stitch(Pos(a, center, radius), Pos(b, center, radius), SizeOf(a) * 0.5f, SizeOf(b) * 0.5f, lit, open ? 1f : 0.7f, tint);
            }
        }

        Mastery.Node hovered = null;
        Vector2 mouse = Event.current.mousePosition;
        foreach (Mastery.Node n in t.nodes)
        {
            Vector2 p = Pos(n, center, radius);
            float size = SizeOf(n);
            var r = new Rect(p.x - size / 2f, p.y - size / 2f, size, size);
            if (Vector2.Distance(mouse, p) < size * 0.5f) hovered = n;
            DrawNode(n, r);
            if (UIKit.ClickButton(r, GUIContent.none, GUIStyle.none)) selected = n;
        }
        // 분기 이름표(심화 매듭 바깥)
        foreach (Mastery.Branch b in t.branches)
        {
            Vector2 p = Pos(b.deep, center, radius);
            bool active = Mastery.ActiveBranch(t.towerId) == b;
            // 아래쪽 매듭은 밑에, 맨 위 매듭은 오른쪽 옆에(수틀 나사와 겹치지 않게)
            Rect label = b.deep.angle > 0f && b.deep.angle < 180f ? new Rect(p.x - 110f, p.y + 58f, 220f, 30f)
                : new Rect(p.x + SizeOf(b.deep) * 0.5f + 8f, p.y - 15f, 220f, 30f);
            UIKit.ShadowLabel(label, b.name + (active ? " ✦" : ""),
                Style(17, Color.Lerp(b.color, Color.white, 0.25f), label.y > p.y ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, true));
        }
        return hovered;
    }

    void DrawNode(Mastery.Node n, Rect r)
    {
        bool unlocked = TowerUnlocks.IsUnlocked(n.towerId);
        bool learned = Mastery.IsLearned(n), reachable = Mastery.IsReachable(n);
        Mastery.Branch br = n.branch;
        bool active = br != null && Mastery.ActiveBranch(n.towerId) == br;
        if (n == selected) Image(Grow(r, 1.34f), "Mastery/ms_knot_on", 0.55f);

        // 속성 매듭은 속성 색 후광(배웠으면 진하게, 쓰는 속성이면 숨 쉬듯)
        if (br != null && unlocked)
        {
            float a = learned ? (active ? 0.55f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f) : 0.4f) : 0.18f;
            Color old0 = GUI.color;
            GUI.color = new Color(br.color.r, br.color.g, br.color.b, a);
            Image(Grow(r, 1.5f), "Mastery/ms_knot_on");
            GUI.color = old0;
        }

        string frame;
        float grow = 1f;
        switch (n.kind)
        {
            case Mastery.NodeKind.Center: frame = "ms_center"; break;
            case Mastery.NodeKind.Deep: frame = learned ? "ms_key_on" : "ms_key_locked"; grow = learned ? 1.16f : 1f; break;
            default:
                if (learned) { frame = "ms_node_on"; grow = 1.09f; }
                else if (reachable) { frame = "ms_node_open"; grow = 1.06f; }
                else frame = "ms_node_locked";
                break;
        }
        float alpha = 1f;
        if (!learned && reachable && Mastery.FreePoints >= n.cost) alpha = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f);
        float pop = n == flashNode ? 1f + 0.25f * Mathf.Clamp01((learnedFlash - Time.unscaledTime) / 0.4f) : 1f;
        Color old = GUI.color;
        if (!unlocked) GUI.color = new Color(0.5f, 0.48f, 0.46f, 0.7f);
        Image(Grow(r, grow * pop), "Mastery/" + frame, alpha);
        GUI.color = old;

        // 안쪽 그림: 가운데·기본 = 도구, 각성·심화 = 도구 2·3단계를 속성 색으로 물들임
        string inner;
        float innerScale;
        switch (n.kind)
        {
            case Mastery.NodeKind.Center: inner = unlocked ? $"Codex/Entries/{n.towerId}_1" : "Codex/cx_icon_unknown"; innerScale = 0.62f; break;
            case Mastery.NodeKind.Base: inner = BaseIcon(n); innerScale = 0.5f; break;
            case Mastery.NodeKind.Awaken: inner = $"Codex/Entries/{n.towerId}_2"; innerScale = 0.62f; break;
            default: inner = $"Codex/Entries/{n.towerId}_3"; innerScale = 0.5f; break;
        }
        if (!unlocked && n.kind != Mastery.NodeKind.Center) return;
        Color tint = br != null ? Color.Lerp(Color.white, br.color, 0.5f) : Color.white;
        if (!learned) tint = reachable ? tint * new Color(0.85f, 0.82f, 0.78f, 0.9f) : new Color(0.55f, 0.52f, 0.5f, 0.6f);
        GUI.color = tint;
        Image(Grow(r, innerScale), inner);
        GUI.color = old;
    }

    /// <summary>기본 손질 아이콘: 첫째 공격력, 둘째 속도, 셋째 사거리(도감 아이콘 재사용).</summary>
    static string BaseIcon(Mastery.Node n)
    {
        if (n.id.EndsWith("_B0")) return "Codex/cx_icon_damage";
        if (n.id.EndsWith("_B1")) return "Codex/cx_icon_speed";
        return "Codex/cx_icon_range";
    }

    void Stitch(Vector2 a, Vector2 b, float padA, float padB, bool lit, float alpha, Color tint)
    {
        float len = Vector2.Distance(a, b);
        if (len <= padA + padB + 4f) return;
        Vector2 dir = (b - a) / len;
        float step = lit ? 13f : 15f, size = lit ? 16f : 12f;
        Texture2D t = Tex(lit ? "Mastery/ms_knot_on" : "Mastery/ms_knot_off");
        if (t == null) return;
        Color old = GUI.color;
        GUI.color = new Color(tint.r, tint.g, tint.b, alpha);
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
            case Mastery.NodeKind.Center: return 132f;
            case Mastery.NodeKind.Deep: return 96f;
            case Mastery.NodeKind.Awaken: return 80f;
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
        float x = p.x + 36f, wInner = p.width - 72f, y = p.y + 28f;
        bool unlocked = TowerUnlocks.IsUnlocked(n.towerId);

        GUI.Label(new Rect(x, y, wInner, 36f), "기술 · 꿈 수틀", Style(26, Ink, TextAnchor.MiddleLeft, true));
        y += 40f;
        Image(new Rect(x, y, 30f, 44f), "Mastery/ms_point");
        GUI.Label(new Rect(x + 40f, y, wInner - 40f, 44f), $"실마리  {Mastery.FreePoints} / {Mastery.TotalPoints}", Style(22, Brown, TextAnchor.MiddleLeft, true));
        y += 44f;
        int pending = CodexRewards.Pending;
        GUI.Label(new Rect(x, y, wInner, 22f), pending > 0 ? $"도감에 받을 기록 보상이 {pending}개 있어요" : "도감 기록 보상으로 실마리를 얻어요",
            Style(14, pending > 0 ? new Color(0.5f, 0.12f, 0.18f) : InkMuted, TextAnchor.MiddleLeft, pending > 0));
        y += 26f;
        Image(new Rect(p.center.x - 180f, y, 360f, 38f), "Codex/cx_divider");
        y += 46f;

        if (!unlocked)
        {
            GUI.Label(new Rect(x, y, wInner, 34f), "??? · 잠긴 도구", Style(24, Ink, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(x, y + 40f, wInner, 90f), TowerUnlocks.UnlockHint(n.towerId) + ".\n해금하면 이 수틀에 수놓을 수 있어요.", Style(17, InkMuted, TextAnchor.UpperLeft, false, true));
            return;
        }

        // 고른 매듭
        var icon = new Rect(x, y, 88f, 88f);
        DrawNode(n, icon);
        float tx = icon.xMax + 14f, tw = p.xMax - 36f - tx;
        string title = n.kind == Mastery.NodeKind.Center ? ToolName(n.towerId) : n.name;
        GUI.Label(new Rect(tx, y + 2f, tw, 36f), title, Style(24, Ink, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(tx, y + 38f, tw, 22f), KindLabel(n), Style(15, n.branch != null ? Color.Lerp(n.branch.color, Ink, 0.5f) : InkMuted, TextAnchor.MiddleLeft, n.branch != null));
        if (n.kind != Mastery.NodeKind.Center)
            GUI.Label(new Rect(tx, y + 60f, tw, 22f), $"실마리 {n.cost}개", Style(15, Brown, TextAnchor.MiddleLeft, true));
        y += 98f;

        string desc = n.kind == Mastery.NodeKind.Center
            ? "안쪽 고리의 기본 손질은 배우면 늘 적용돼요. 바깥 세 갈래는 속성 분기예요. 여러 속성을 배워도 전투에는 고른 속성 하나만 들어가요."
            : (n.branch != null ? n.branch.summary + " " : "") + n.desc;
        GUIStyle body = Style(17, Ink, TextAnchor.UpperLeft, false, true);
        float bh = body.CalcHeight(new GUIContent(desc), wInner);
        GUI.Label(new Rect(x, y, wInner, bh), desc, body);
        y += bh + 10f;

        bool learned = Mastery.IsLearned(n), reachable = Mastery.IsReachable(n);
        if (n.kind != Mastery.NodeKind.Center)
        {
            string state = learned ? (n.kind == Mastery.NodeKind.Base ? "수놓았어요. 늘 적용돼요." : "수놓았어요.")
                : !reachable ? "이어진 매듭을 먼저 수놓아 주세요."
                : Mastery.FreePoints < n.cost ? "실마리가 모자라요. 도감 기록 보상을 받아 보세요." : "수놓을 수 있어요.";
            GUI.Label(new Rect(x, y, wInner, 24f), state, Style(15, learned ? Brown : InkMuted, TextAnchor.MiddleLeft, learned));
            y += 28f;
        }

        // 속성 고르기(전투에는 하나만)
        DrawBranchPicker(new Rect(x, p.yMax - 232f, wInner, 110f));

        // 수놓기 리본
        var button = new Rect(p.center.x - 140f, p.yMax - 118f, 280f, 96f);
        if (n.kind != Mastery.NodeKind.Center && !learned)
        {
            bool can = Mastery.CanLearn(n);
            Image(button, can ? "Mastery/ms_ribbon" : "Mastery/ms_ribbon_off");
            GUI.Label(new Rect(button.x, button.y - 8f, button.width, button.height), "수놓기", Style(22, can ? Light : new Color(0.9f, 0.88f, 0.85f, 0.8f), TextAnchor.MiddleCenter, true));
            if (UIKit.ClickButton(button, GUIContent.none, GUIStyle.none))
            {
                if (Mastery.Learn(n))
                {
                    flashNode = n;
                    learnedFlash = Time.unscaledTime + 0.4f;
                    SoundFx.Play("upgrade", 0.8f);
                }
                else Show(Mastery.FreePoints < n.cost ? "실마리가 모자라요" : "이어진 매듭을 먼저 수놓아 주세요");
            }
        }

        var reset = new Rect(p.xMax - 36f - 200f, p.yMax - 44f, 200f, 24f);
        bool any = false;
        foreach (Mastery.Node m in Tree.nodes) if (m.kind != Mastery.NodeKind.Center && Mastery.IsLearned(m)) any = true;
        GUI.Label(reset, "<u>이 도구 모두 풀기</u>", Style(14, any ? Brown : InkMuted, TextAnchor.MiddleRight, true));
        if (any && UIKit.ClickButton(reset, GUIContent.none, GUIStyle.none))
        {
            Mastery.Unlearn(towerId);
            Show("실을 모두 풀었어요. 실마리를 돌려받았어요.");
        }

        if (!string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
            GUI.Label(new Rect(p.x, p.yMax + 8f, p.width, 30f), message, Style(18, Gold, TextAnchor.MiddleCenter, true));
    }

    /// <summary>각성한 속성 중 전투에 쓸 하나를 고른다.</summary>
    void DrawBranchPicker(Rect r)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 24f), "전투에 쓸 속성 (하나만)", Style(15, Ink, TextAnchor.MiddleLeft, true));
        Mastery.Branch active = Mastery.ActiveBranch(towerId);
        float bw = (r.width - 16f) / 3f;
        for (int i = 0; i < Tree.branches.Count; i++)
        {
            Mastery.Branch b = Tree.branches[i];
            bool has = Mastery.IsLearned(b.awaken), on = b == active;
            var chip = new Rect(r.x + i * (bw + 8f), r.y + 30f, bw, 64f);
            Frame(chip, on ? "Codex/cx_slot_on" : "Codex/cx_slot", 30);
            Color old = GUI.color;
            GUI.color = has ? b.color : new Color(0.6f, 0.58f, 0.55f, 0.5f);
            Image(new Rect(chip.x + 8f, chip.center.y - 11f, 22f, 22f), "Mastery/ms_knot_on");
            GUI.color = old;
            GUI.Label(new Rect(chip.x + 32f, chip.y, chip.width - 36f, chip.height), b.name + (on ? "\n<size=12>사용 중</size>" : has ? "" : "\n<size=12>배우지 않음</size>"),
                Style(14, has ? Ink : InkMuted, TextAnchor.MiddleLeft, on));
            if (has && !on && UIKit.ClickButton(chip, GUIContent.none, GUIStyle.none))
            {
                Mastery.SetActive(b);
                SoundFx.Play("ui_click", 0.7f);
                Show($"{b.name}을(를) 전투에 써요");
            }
        }
    }

    static string KindLabel(Mastery.Node n)
    {
        switch (n.kind)
        {
            case Mastery.NodeKind.Center: return "수선 도구";
            case Mastery.NodeKind.Base: return "기본 손질 · 늘 적용";
            case Mastery.NodeKind.Awaken: return "속성 각성 · " + n.branch.name;
            default: return "속성 심화 · " + n.branch.name;
        }
    }

    static string ToolName(string id)
    {
        foreach (CodexData.Entry e in CodexData.Tools) if (e.id == id) return e.name;
        return id;
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
        string key = "frame:" + path + ":" + border;
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
