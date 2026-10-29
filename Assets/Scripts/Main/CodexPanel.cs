using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 도감(수선 기록부) 화면. 메인 화면 아래 탭 "도감"으로 연다.
/// 펼친 가죽 책 한 권: 위쪽 책갈피로 "수선 도구"(타워) / "악몽"(몬스터)을 고르고,
/// 왼쪽 쪽은 항목 칸 목록, 오른쪽 쪽은 고른 항목의 그림·설정 글·수치·도하 메모.
/// 그림: Resources/Codex/cx_*.png (Artlist 생성 → Tools/Art/build_codex_ui.py). IMGUI, 1080p 기준 좌표.
/// 악몽은 전투에서 한 번 만나야 기록된다(그 전에는 실루엣과 ???).
/// </summary>
public class CodexPanel
{
    // 양피지 위 글자(잉크), 흐린 잉크, 갈색 강조, 밝은 글자
    static readonly Color Ink = new Color(0.18f, 0.13f, 0.10f), InkMuted = new Color(0.42f, 0.34f, 0.27f),
        Brown = new Color(0.54f, 0.33f, 0.13f), Light = new Color(0.95f, 0.92f, 0.87f), Wine = new Color(0.45f, 0.10f, 0.16f);

    // 책 그림(1800×1004) 안 두 쪽의 점선 안쪽 영역(비율)
    static readonly Rect LeftPage = new Rect(0.085f, 0.115f, 0.38f, 0.74f);
    static readonly Rect RightPage = new Rect(0.535f, 0.115f, 0.38f, 0.74f);

    CodexData.Kind tab = CodexData.Kind.Tool;
    int selectedTool, selectedNightmare, level = 1;

    readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    /// <summary>도감을 그린다. 닫기를 누르면 false.</summary>
    public bool Draw(float w, float h)
    {
        UIKit.DimScreen(0.6f);
        float bw = Mathf.Min(1440f, w - 120f), bh = bw * 1004f / 1800f;
        var book = new Rect((w - bw) / 2f, Mathf.Max(70f, (h - 110f - bh) / 2f + 24f), bw, bh);

        DrawTabs(book);
        Image(book, "cx_book");

        List<CodexData.Entry> entries = tab == CodexData.Kind.Tool ? CodexData.Tools : CodexData.Nightmares;
        int selected = tab == CodexData.Kind.Tool ? selectedTool : selectedNightmare;
        selected = Mathf.Clamp(selected, 0, entries.Count - 1);

        DrawList(Page(book, LeftPage), entries, ref selected);
        if (tab == CodexData.Kind.Tool) selectedTool = selected;
        else selectedNightmare = selected;
        DrawDetail(Page(book, RightPage), entries[selected]);

        // 닫기: 책 오른쪽 위 밀랍 봉인
        var close = new Rect(book.xMax - 96f, book.y - 22f, 88f, 90f);
        Image(close, "cx_seal");
        GUI.Label(close, "닫기", Style(17, Light, TextAnchor.MiddleCenter, true));
        return !UIKit.ClickButton(close, GUIContent.none, GUIStyle.none);
    }

    // ───────── 책갈피 ─────────

    void DrawTabs(Rect book)
    {
        // 책 위쪽 가장자리에 꽂힌 천 책갈피. 책 그림보다 먼저 그려 아랫부분이 책 뒤로 숨는다.
        DrawTab(new Rect(book.x + book.width * 0.60f, book.y - 96f, 78f, 160f), CodexData.Kind.Tool, "cx_mark_red", "도구");
        DrawTab(new Rect(book.x + book.width * 0.60f + 92f, book.y - 96f, 78f, 160f), CodexData.Kind.Nightmare, "cx_mark_blue", "악몽");
    }

    void DrawTab(Rect r, CodexData.Kind kind, string image, string label)
    {
        bool on = tab == kind;
        if (!on) r.y += 14f; // 고른 책갈피가 더 길게 나온다
        Image(r, on ? image : image + "_off");
        // 세로 글자(쇠고리 아래)
        GUI.Label(new Rect(r.x, r.y + 44f, r.width, 70f), label[0] + "\n" + label[1],
            Style(22, on ? Light : InkMuted, TextAnchor.UpperCenter, true));
        if (UIKit.ClickButton(r, GUIContent.none, GUIStyle.none) && !on)
        {
            tab = kind;
            level = 1;
        }
    }

    // ───────── 왼쪽 쪽: 목록 ─────────

    void DrawList(Rect page, List<CodexData.Entry> entries, ref int selected)
    {
        bool tools = tab == CodexData.Kind.Tool;
        int known = 0;
        foreach (CodexData.Entry e in entries) if (Known(e)) known++;

        GUI.Label(new Rect(page.x, page.y + 4f, page.width, 50f), tools ? "수선 도구" : "악몽 기록", Style(34, Ink, TextAnchor.MiddleCenter, true));
        GUI.Label(new Rect(page.x, page.y + 52f, page.width, 28f),
            tools ? $"공방에서 쓰는 도구  {known} / {entries.Count}" : $"만난 악몽  {known} / {entries.Count}",
            Style(17, InkMuted, TextAnchor.MiddleCenter));
        Image(new Rect(page.center.x - 200f, page.y + 82f, 400f, 45f), "cx_divider");

        const int cols = 3;
        float slot = Mathf.Min(150f, (page.width - 40f) / cols - 14f), gapX = (page.width - cols * slot) / (cols + 1);
        float top = page.y + 140f;
        for (int i = 0; i < entries.Count; i++)
        {
            CodexData.Entry e = entries[i];
            bool known1 = Known(e);
            var r = new Rect(page.x + gapX + (i % cols) * (slot + gapX), top + (i / cols) * (slot + 52f), slot, slot * 0.94f);
            Image(r, !known1 ? "cx_slot_lock" : i == selected ? "cx_slot_on" : "cx_slot");
            if (known1) Image(Inset(r, 0.16f), CodexData.ImagePath(e));
            GUI.Label(new Rect(r.x - 10f, r.yMax + 2f, r.width + 20f, 30f), known1 ? e.name : "???",
                Style(18, i == selected ? Brown : Ink, TextAnchor.MiddleCenter, i == selected));
            if (UIKit.ClickButton(r, GUIContent.none, GUIStyle.none) && i != selected)
            {
                selected = i;
                level = 1;
            }
        }

        string hint = tools ? "칸을 누르면 오른쪽 쪽에 자세히 나와요."
            : known < entries.Count ? "의뢰를 하며 악몽을 만나면 기록돼요." : "지금까지 나온 악몽을 모두 기록했어요.";
        GUI.Label(new Rect(page.x, page.yMax - 40f, page.width, 30f), hint, Style(16, InkMuted, TextAnchor.MiddleCenter));
    }

    // ───────── 오른쪽 쪽: 자세히 ─────────

    void DrawDetail(Rect page, CodexData.Entry e)
    {
        bool known = Known(e);
        bool tool = e.kind == CodexData.Kind.Tool;

        // 그림 액자
        var frame = new Rect(page.x + 6f, page.y + 6f, 220f, 215f);
        Image(frame, "cx_frame");
        Rect art = Inset(frame, 0.2f);
        if (known) Image(art, CodexData.ImagePath(e, level));
        else
        {
            Color old = GUI.color;
            GUI.color = new Color(0.12f, 0.09f, 0.14f, 0.85f); // 실루엣
            Image(art, CodexData.ImagePath(e));
            GUI.color = old;
            Image(Inset(art, 0.25f), "cx_icon_unknown");
        }

        float tx = frame.xMax + 18f, tw = page.xMax - tx;
        GUI.Label(new Rect(tx, page.y + 10f, tw, 50f), known ? e.name : "???", Style(34, Ink, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(tx, page.y + 58f, tw, 28f), tool ? "수선 도구 · " + e.role : known ? "악몽 · " + e.role : "악몽 · 아직 만나지 못함",
            Style(17, InkMuted, TextAnchor.MiddleLeft));

        if (tool)
        {
            GUI.Label(new Rect(tx, page.y + 98f, tw, 26f), $"짓기 {e.tower.BuildCost} 꿈실", Style(17, Brown, TextAnchor.MiddleLeft, true));
            for (int lv = 1; lv <= 3; lv++)
            {
                var b = new Rect(tx + (lv - 1) * 92f, page.y + 136f, 84f, 52f);
                Image(b, lv == level ? "cx_slot_on" : "cx_slot");
                GUI.Label(b, $"{lv}단계", Style(17, lv == level ? Brown : Ink, TextAnchor.MiddleCenter, lv == level));
                if (UIKit.ClickButton(b, GUIContent.none, GUIStyle.none)) level = lv;
            }
        }
        else if (known)
        {
            GUI.Label(new Rect(tx, page.y + 98f, tw, 26f), $"처치한 수  {CodexProgress.Kills(e.id)}", Style(17, Brown, TextAnchor.MiddleLeft, true));
            Image(new Rect(page.xMax - 92f, page.y + 120f, 84f, 86f), "cx_seal"); // 기록됨 도장
            GUI.Label(new Rect(page.xMax - 150f, page.y + 206f, 200f, 22f), "기록됨", Style(14, Wine, TextAnchor.MiddleCenter, true));
        }

        Image(new Rect(page.x, frame.yMax + 6f, page.width, 40f), "cx_divider");
        float y = frame.yMax + 54f;

        if (!known)
        {
            GUI.Label(new Rect(page.x + 10f, y, page.width - 20f, 120f),
                "아직 만나지 못한 악몽이에요.\n의뢰를 수행하다 이 악몽이 꿈에 나타나면 도하가 기록해 둘 거예요.",
                Style(19, InkMuted, TextAnchor.UpperLeft, false, true));
            return;
        }

        GUIStyle body = Style(18, Ink, TextAnchor.UpperLeft, false, true);
        float loreH = Mathf.Min(150f, body.CalcHeight(new GUIContent(e.lore), page.width - 20f));
        GUI.Label(new Rect(page.x + 10f, y, page.width - 20f, loreH), e.lore, body);
        y += loreH + 12f;

        if (tool) y = ToolStats(page, y, e.tower.Levels[level - 1]);
        else y = NightmareStats(page, y, e.enemy);

        GUIStyle memo = Style(17, Brown, TextAnchor.UpperLeft, false, true);
        string memoText = "도하의 메모 — " + e.memo;
        float memoH = memo.CalcHeight(new GUIContent(memoText), page.width - 20f);
        GUI.Label(new Rect(page.x + 10f, Mathf.Min(y + 6f, page.yMax - memoH), page.width - 20f, memoH), memoText, memo);
    }

    float ToolStats(Rect page, float y, TowerLevelDef l)
    {
        string hits = l.HitCount > 1 ? $"  × {l.HitCount}명" : "";
        y = StatRow(page, y, "cx_icon_damage", "공격력", $"{l.Damage:0}{hits}", l.Damage * Mathf.Max(1, l.HitCount) / 130f);
        y = StatRow(page, y, "cx_icon_speed", "공격 간격", $"{l.AttackSec:0.##}초", 0.4f / Mathf.Max(0.2f, l.AttackSec));
        y = StatRow(page, y, "cx_icon_range", "사거리", $"{l.Range:0.#}칸", l.Range / 6f);

        var extra = new List<string>();
        if (l.CritChance > 0f) extra.Add($"치명타 {l.CritChance * 100f:0}% (×{l.CritMult:0.#})");
        if (l.Slows) extra.Add($"느리게 {(1f - l.SlowMult) * 100f:0}% · {l.SlowSec:0.#}초");
        if (l.ArmorShred > 0f) extra.Add($"방어 -{l.ArmorShred:0} · {l.MarkSec:0.#}초");
        if (l.Vulnerability > 0f) extra.Add($"받는 피해 +{l.Vulnerability * 100f:0}% · {l.MarkSec:0.#}초");
        if (l.UpgradeCost > 0) extra.Add($"다음 단계 {l.UpgradeCost} 꿈실");
        if (extra.Count > 0)
        {
            Image(new Rect(page.x + 10f, y + 4f, 26f, 26f), "cx_icon_reward");
            GUI.Label(new Rect(page.x + 46f, y, page.width - 56f, 34f), string.Join("   ", extra), Style(16, InkMuted, TextAnchor.MiddleLeft, false, true));
            y += 38f;
        }
        return y;
    }

    float NightmareStats(Rect page, float y, EnemyDef d)
    {
        y = StatRow(page, y, "cx_icon_core", "체력", d.MaxHp.ToString(), d.MaxHp / 260f);
        y = StatRow(page, y, "cx_icon_armor", "실 껍질(방어)", $"{d.Armor:0}", d.Armor / 60f);
        y = StatRow(page, y, "cx_icon_move", "이동 속도", $"{d.MoveSpeed:0.#}", d.MoveSpeed / 2.6f);
        y = StatRow(page, y, "cx_icon_damage", "중심 피해", d.CoreDamage.ToString(), d.CoreDamage / 12f);
        GUI.Label(new Rect(page.x + 10f, y, page.width - 20f, 26f), $"처치 보상 {d.KillCoin} 꿈실   (의뢰마다 체력이 더 높을 수 있어요)",
            Style(15, InkMuted, TextAnchor.MiddleLeft));
        return y + 30f;
    }

    float StatRow(Rect page, float y, string icon, string label, string value, float ratio)
    {
        Image(new Rect(page.x + 10f, y + 2f, 30f, 30f), icon);
        GUI.Label(new Rect(page.x + 48f, y, 140f, 34f), label, Style(17, Ink, TextAnchor.MiddleLeft, true));
        var track = new Rect(page.x + 190f, y + 9f, page.width - 300f, 18f);
        Frame(track, "cx_bar_track", 22);
        float fill = Mathf.Clamp01(ratio);
        if (fill > 0f) Frame(new Rect(track.x + 2f, track.y + 1f, Mathf.Max(30f, (track.width - 4f) * fill), track.height - 2f), "cx_bar_fill", 20);
        GUI.Label(new Rect(track.xMax + 10f, y, page.xMax - track.xMax - 10f, 34f), value, Style(17, Ink, TextAnchor.MiddleLeft));
        return y + 36f;
    }

    // ───────── 도구 ─────────

    static bool Known(CodexData.Entry e) => e.kind == CodexData.Kind.Tool || CodexProgress.Seen(e.id);

    static Rect Page(Rect book, Rect f) => new Rect(book.x + book.width * f.x, book.y + book.height * f.y, book.width * f.width, book.height * f.height);

    static Rect Inset(Rect r, float f) => new Rect(r.x + r.width * f, r.y + r.height * f, r.width * (1f - 2f * f), r.height * (1f - 2f * f));

    Texture2D Tex(string name)
    {
        string path = name.StartsWith("Codex/") ? name : "Codex/" + name;
        if (!tex.TryGetValue(path, out Texture2D t))
        {
            t = Resources.Load<Texture2D>(path);
            tex[path] = t;
        }
        return t;
    }

    void Image(Rect r, string name)
    {
        Texture2D t = Tex(name);
        if (t != null) GUI.DrawTexture(r, t, ScaleMode.ScaleToFit);
    }

    /// <summary>가로로 늘리는 막대(양 끝 border 픽셀은 늘리지 않음).</summary>
    void Frame(Rect r, string name, int border)
    {
        if (Event.current.type != EventType.Repaint) return;
        string key = "frame:" + name;
        if (!styles.TryGetValue(key, out GUIStyle style))
        {
            style = new GUIStyle { border = new RectOffset(border, border, 0, 0) };
            style.normal.background = Tex(name);
            styles[key] = style;
        }
        if (style.normal.background != null) style.Draw(r, false, false, false, false);
    }

    GUIStyle Style(int size, Color color, TextAnchor anchor, bool bold = false, bool wrap = false)
    {
        string key = $"{size}:{color}:{anchor}:{bold}:{wrap}";
        if (styles.TryGetValue(key, out GUIStyle style)) return style;
        style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, wordWrap = wrap, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
        styles[key] = style;
        return style;
    }
}
