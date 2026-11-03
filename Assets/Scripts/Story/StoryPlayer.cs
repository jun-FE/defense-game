using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스토리 화면. 대사를 한 글자씩 출력한다.
/// 클릭/Space/Enter: 출력 중이면 대사를 바로 완성, 다 나왔으면 다음 대사.
/// 우상단 스킵 버튼/Esc: 스토리를 건너뛰고 다음 화면으로.
///
/// 틀(그림은 나중에 넣는다):
/// - 장면 제목 카드: 대사에 sceneTitle이 있으면 그 대사 앞에 잠깐 띄운다.
/// - 배경: Resources/Stories/Backgrounds/이름. 바뀔 때 겹쳐 바뀐다. 그림이 없으면 색 배경 + 이름 자리 표시.
/// - 초상화: Resources/Stories/Portraits/이름(없으면 "_표정" 뗀 이름). 이안은 왼쪽, 나머지는 오른쪽.
///   말하는 쪽은 밝게, 듣는 쪽은 어둡게. 그림이 없으면 자리 표시.
/// - 시작·끝에 검은 화면으로 서서히 들어오고 나간다.
/// - 연출([효과]): 눈뜨기 = 반쯤 뜬 눈 시점(위아래 눈꺼풀이 천천히 열렸다 깜빡, 흐릿함·햇살이 걷힘),
///   흔들림 = 배경이 잠깐 흔들림, 암전 = 배경 대신 검은 화면, 초상화없음 = 화면 밖 목소리·삽화 장면. [소리]는 대사가 나올 때 한 번.
/// </summary>
public class StoryPlayer : MonoBehaviour
{
    [Tooltip("이 씬을 직접 실행했을 때 보여줄 스토리")]
    public StoryData fallbackStory;

    [Tooltip("그림이 없을 때 자리 표시(상자 + 이름)를 보여 준다")]
    public bool showPlaceholders = true;

    const string BackgroundDir = "Stories/Backgrounds/";
    const string PortraitDir = "Stories/Portraits/";
    const float CardSec = 2.4f;
    const float FadeSpeed = 1.8f;
    static readonly string[] LeftSpeakers = { "이안" };

    StoryData story;
    int index;
    float shownChars;
    bool finished, leaving;
    float fade = 1f;            // 검은 화면 덮개(1 = 완전히 검음)
    float cardTime = -1f;       // 0 이상이면 장면 제목 카드를 보여 주는 중

    string background, previousBackground;
    float backgroundBlend = 1f;
    string leftPortrait, rightPortrait;
    int activeSide;             // -1 왼쪽, 1 오른쪽, 0 나레이션

    readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    Texture2D white;

    float lineTime;             // 지금 대사가 나온 뒤 흐른 시간(연출용)
    Texture2D eyelid;

    StoryLine Line => story.lines[index];
    bool LineComplete => shownChars >= Line.text.Length;
    bool ShowingCard => cardTime >= 0f;

    void Start()
    {
        story = SceneFlow.PendingStory != null ? SceneFlow.PendingStory : fallbackStory;
        if (story == null || story.lines.Length == 0)
        {
            Finish();
            return;
        }
        if (Camera.main != null) Camera.main.backgroundColor = story.background;
        white = UIKit.MakeTexture(Color.white);
        EnterLine(0);
    }

    void Update()
    {
        if (story == null || finished) return;
        float dt = Time.deltaTime;

        fade = Mathf.MoveTowards(fade, leaving ? 1f : 0f, FadeSpeed * dt);
        if (leaving)
        {
            if (fade >= 1f) Finish();
            return;
        }

        backgroundBlend = Mathf.Min(1f, backgroundBlend + dt * 2f);
        lineTime += dt;
        if (ShowingCard)
        {
            cardTime += dt;
            if (cardTime >= CardSec) cardTime = -1f;
            return;
        }

        float speed = GameSettings.TextCharsPerSecond;
        shownChars = speed <= 0f ? Line.text.Length : Mathf.Min(Line.text.Length, shownChars + speed * dt);
    }

    void OnGUI()
    {
        if (story == null || finished) return;

        UIKit.Begin();
        float w = UIKit.Width;
        float h = UIKit.Height;

        bool showLine = !ShowingCard;
        Vector2 shake = showLine && Line.Has("흔들림") && lineTime < 0.6f
            ? new Vector2(Mathf.Sin(lineTime * 90f), Mathf.Cos(lineTime * 70f)) * 14f * (1f - lineTime / 0.6f) : Vector2.zero;
        DrawBackground(previousBackground, 1f, w, h, shake);
        DrawBackground(background, backgroundBlend, w, h, shake);
        if (showLine && Line.Has("눈뜨기")) DrawEyes(w, h);
        if (showLine && Line.Has("암전")) DrawFill(new Rect(0f, 0f, w, h), Color.black);
        if (ShowingCard)
        {
            DrawCard(w, h);
        }
        else
        {
            if (Line.style == "guide") DrawGuide(w, h);
            else if (Line.style == "letter") DrawLetter(w, h);
            else
            {
                if (!Line.Has("초상화없음")) DrawPortraits(w, h);
                DrawDialogue(w, h);
            }
        }
        DrawFill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, fade));

        bool skip = !leaving && UIKit.DrawButton(new Rect(w - 250f, 30f, 210f, 64f), "스킵 ▶▶", UIKit.ButtonSmall);
        UIKit.End();

        if (leaving) return;
        if (skip)
        {
            leaving = true;
            return;
        }

        // 스킵 버튼이 클릭을 먼저 가져가므로, 여기 오는 클릭은 화면 클릭이다.
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Advance();
            e.Use();
        }
        else if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { Advance(); e.Use(); }
            else if (e.keyCode == KeyCode.Escape) { leaving = true; e.Use(); }
        }
    }

    // ───────── 진행 ─────────

    void EnterLine(int i)
    {
        index = i;
        shownChars = 0f;
        lineTime = 0f;
        StoryLine line = Line;
        if (!string.IsNullOrEmpty(line.sound)) SoundFx.Play(line.sound, 0.9f, 0f, 0f);

        if (!string.IsNullOrEmpty(line.background) && line.background != background)
        {
            previousBackground = background;
            background = line.background;
            backgroundBlend = 0f;
        }
        if (!string.IsNullOrEmpty(line.sceneTitle))
        {
            cardTime = 0f;
            leftPortrait = rightPortrait = null; // 장면이 바뀌면 인물도 새로 등장
        }

        if (string.IsNullOrEmpty(line.speaker) || !string.IsNullOrEmpty(line.style)) // 나레이션·안내·편지는 초상화 없음
        {
            activeSide = 0;
            return;
        }
        string portrait = string.IsNullOrEmpty(line.portrait) ? line.speaker : line.portrait;
        activeSide = System.Array.IndexOf(LeftSpeakers, line.speaker) >= 0 ? -1 : 1;
        if (activeSide < 0) leftPortrait = portrait;
        else rightPortrait = portrait;
    }

    void Advance()
    {
        if (ShowingCard)
        {
            cardTime = -1f;
            return;
        }
        if (!LineComplete)
        {
            shownChars = Line.text.Length;
            return;
        }
        if (index + 1 < story.lines.Length) EnterLine(index + 1);
        else leaving = true;
    }

    void Finish()
    {
        if (finished) return;
        finished = true;
        SceneFlow.FinishStory();
    }

    // ───────── 그리기 ─────────

    void DrawBackground(string name, float alpha, float w, float h, Vector2 shake)
    {
        if (string.IsNullOrEmpty(name) || alpha <= 0f) return;
        // 흔들릴 때 가장자리가 비지 않게 살짝 크게
        var full = shake == Vector2.zero ? new Rect(0f, 0f, w, h) : new Rect(-20f + shake.x, -20f + shake.y, w + 40f, h + 40f);
        Texture2D tex = Load(BackgroundDir + name);
        if (tex != null)
        {
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(full, tex, ScaleMode.ScaleAndCrop);
            GUI.color = Color.white;
            return;
        }
        Color c = story.background;
        DrawFill(full, new Color(c.r, c.g, c.b, alpha));
        if (showPlaceholders) Placeholder(new Rect(40f, 30f, 520f, 60f), $"배경: {name}", alpha);
    }

    /// <summary>
    /// 반쯤 뜬 눈 시점(프롤로그 화면 구성 07): 배경을 몇 번 어긋나게 겹쳐 흐릿하게 하고 햇살을 덮은 뒤, 위아래 눈꺼풀(둥근 어둠)을 그린다.
    /// 눈꺼풀은 천천히 열리다 한 번 깜빡이고 반쯤 뜬 채 머문다. 흐림·햇살은 눈이 뜨일수록 걷힌다.
    /// </summary>
    void DrawEyes(float w, float h)
    {
        float t = lineTime;
        float open = t < 1.4f ? Mathf.SmoothStep(0f, 0.55f, t / 1.4f)
                   : t < 1.75f ? Mathf.Lerp(0.55f, 0.12f, (t - 1.4f) / 0.35f)
                   : Mathf.SmoothStep(0.12f, 0.72f, Mathf.Min(1f, (t - 1.75f) / 1.6f));
        Texture2D bg = string.IsNullOrEmpty(background) ? null : Load(BackgroundDir + background);
        float blur = 1f - open;
        if (bg != null && blur > 0.05f)
        {
            float r = 18f * blur;
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3f;
                GUI.color = new Color(1f, 1f, 1f, 0.22f * blur);
                GUI.DrawTexture(new Rect(Mathf.Cos(a) * r, Mathf.Sin(a) * r, w, h), bg, ScaleMode.ScaleAndCrop);
            }
            GUI.color = Color.white;
        }
        DrawFill(new Rect(0f, 0f, w, h), new Color(1f, 0.86f, 0.62f, 0.28f * blur + 0.06f));
        if (eyelid == null) eyelid = MakeEyelid();
        // 눈꺼풀: 화면 높이의 반씩, 열린 만큼 위·아래로 물러난다. 가장자리는 둥글고 부드럽게.
        float lid = h * 0.62f;
        float gap = h * 0.5f * open;
        Color dark = new Color(0.05f, 0.03f, 0.03f, 1f);
        GUI.color = dark;
        GUI.DrawTexture(new Rect(-w * 0.1f, h * 0.5f - gap - lid, w * 1.2f, lid), eyelid);
        GUI.DrawTextureWithTexCoords(new Rect(-w * 0.1f, h * 0.5f + gap, w * 1.2f, lid), eyelid, new Rect(0f, 1f, 1f, -1f));
        GUI.color = Color.white;
        DrawFill(new Rect(0f, 0f, w, Mathf.Max(0f, h * 0.5f - gap - lid + 2f)), dark);
        DrawFill(new Rect(0f, Mathf.Min(h, h * 0.5f + gap + lid - 2f), w, h), dark);
    }

    /// <summary>위 눈꺼풀 그림: 아래쪽 가장자리가 가운데로 볼록한 둥근 어둠(부드러운 가장자리). 아래 눈꺼풀은 뒤집어 쓴다.</summary>
    static Texture2D MakeEyelid()
    {
        const int tw = 256, th = 128;
        var t = new Texture2D(tw, th, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[tw * th];
        for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                float u = x / (tw - 1f) * 2f - 1f;          // -1~1
                float edge = 0.08f + 0.5f * u * u;          // 가장자리 높이(아래에서): 가운데가 가장 아래로 내려옴
                float v = y / (th - 1f);                    // 0 = 텍스처 아래(Unity 텍스처 좌표)
                float a = Mathf.Clamp01((v - edge) / 0.18f + 0.5f);
                px[y * tw + x] = new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    void DrawCard(float w, float h)
    {
        // 들어올 때·나갈 때 서서히
        float a = Mathf.Min(1f, Mathf.Min(cardTime / 0.5f, (CardSec - cardTime) / 0.5f));
        DrawFill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, 0.55f * a));
        GUI.color = new Color(1f, 1f, 1f, a);
        UIKit.ShadowLabel(new Rect(0f, h * 0.5f - 110f, w, 120f), Line.sceneTitle, UIKit.Title);
        GUI.color = Color.white;
        DrawFill(new Rect(w * 0.5f - 160f, h * 0.5f + 18f, 320f, 2f), new Color(0.95f, 0.82f, 0.5f, 0.8f * a));
        if (!string.IsNullOrEmpty(Line.sceneSubtitle))
        {
            GUI.color = new Color(1f, 1f, 1f, a);
            UIKit.ShadowLabel(new Rect(0f, h * 0.5f + 40f, w, 70f), Line.sceneSubtitle, UIKit.Heading);
            GUI.color = Color.white;
        }
    }

    void DrawPortraits(float w, float h)
    {
        const float pw = 460f, ph = 620f;
        float bottom = DreamUI.DialogRect(w, h).y + 40f; // 대사창 바로 위(아랫부분은 대사창 뒤로)
        DrawPortrait(leftPortrait, new Rect(150f, bottom - ph, pw, ph), activeSide < 0);
        DrawPortrait(rightPortrait, new Rect(w - 150f - pw, bottom - ph, pw, ph), activeSide > 0);
    }

    void DrawPortrait(string name, Rect rect, bool speaking)
    {
        if (string.IsNullOrEmpty(name)) return;
        float shade = speaking ? 1f : 0.45f;
        Texture2D tex = Load(PortraitDir + name);
        int mood = name.IndexOf('_');
        if (tex == null && mood > 0) tex = Load(PortraitDir + name.Substring(0, mood));
        if (!speaking) rect.y += 16f; // 듣는 쪽은 살짝 내려 앉힌다

        if (tex != null)
        {
            GUI.color = new Color(shade, shade, shade, 1f);
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
            GUI.color = Color.white;
        }
        else if (showPlaceholders)
        {
            DrawFill(rect, new Color(1f, 1f, 1f, 0.06f * shade));
            Placeholder(new Rect(rect.x, rect.center.y - 30f, rect.width, 60f), $"초상화: {name}", shade);
        }
    }

    /// <summary>대사창(프롤로그 화면 구성 기획): 둥근 남색 패널에 크림색 "화자 | 대사" 왼쪽 정렬.</summary>
    void DrawDialogue(float w, float h)
    {
        string text = Line.text.Substring(0, Mathf.FloorToInt(shownChars));
        DreamUI.Dialog(w, h, Line.speaker, text, LineComplete);
    }

    /// <summary>플레이 안내 카드: 화면 가운데 남색 판에 금색 제목.</summary>
    void DrawGuide(float w, float h)
    {
        var panel = new Rect(w / 2f - 520f, h / 2f - 170f, 1040f, 300f);
        DrawFill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, 0.35f));
        UIKit.DrawPanel(panel);
        DrawFill(new Rect(panel.x + 40f, panel.y + 86f, panel.width - 80f, 2f), new Color(0.95f, 0.82f, 0.5f, 0.6f));
        GUI.Label(new Rect(panel.x, panel.y + 24f, panel.width, 56f), "플레이 안내 · " + Line.speaker, Style("guideTitle", UIKit.Speaker, 32, new Color(0.95f, 0.82f, 0.5f), TextAnchor.MiddleCenter));
        string text = Line.text.Substring(0, Mathf.FloorToInt(shownChars));
        GUI.Label(new Rect(panel.x + 60f, panel.y + 108f, panel.width - 120f, panel.height - 150f), text, Style("guideBody", UIKit.Dialogue, 30, Color.white, TextAnchor.UpperCenter));
        if (LineComplete && Mathf.FloorToInt(Time.time * 2f) % 2 == 0)
            GUI.Label(new Rect(panel.xMax - 70f, panel.yMax - 55f, 40f, 40f), "▼", UIKit.Small);
        GUI.Label(new Rect(0f, h - 55f, w, 40f), "클릭 또는 Space: 다음", UIKit.Small);
    }

    /// <summary>편지: 양피지(도감 칸 그림 재사용) 위 잉크 글씨.</summary>
    void DrawLetter(float w, float h)
    {
        var paper = new Rect(w / 2f - 460f, h / 2f - 300f, 920f, 560f);
        DrawFill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, 0.45f));
        Texture2D parchment = Load("Codex/cx_slot");
        if (parchment != null) GUI.DrawTexture(paper, parchment, ScaleMode.StretchToFill);
        else UIKit.DrawPanel(paper);
        var ink = new Color(0.2f, 0.14f, 0.1f);
        GUI.Label(new Rect(paper.x + 80f, paper.y + 60f, paper.width - 160f, 50f), Line.speaker, Style("letterTitle", UIKit.Speaker, 30, new Color(0.45f, 0.27f, 0.12f), TextAnchor.MiddleLeft));
        string text = Line.text.Substring(0, Mathf.FloorToInt(shownChars));
        GUI.Label(new Rect(paper.x + 80f, paper.y + 130f, paper.width - 160f, paper.height - 200f), text, Style("letterBody", UIKit.Dialogue, 28, ink, TextAnchor.UpperLeft));
        GUI.Label(new Rect(0f, h - 55f, w, 40f), "클릭 또는 Space: 다음", UIKit.Small);
    }

    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    GUIStyle Style(string key, GUIStyle from, int size, Color color, TextAnchor anchor)
    {
        if (styles.TryGetValue(key, out GUIStyle style)) return style;
        style = new GUIStyle(from) { fontSize = size, alignment = anchor, wordWrap = true, richText = true };
        style.normal.textColor = style.hover.textColor = style.active.textColor = color;
        styles[key] = style;
        return style;
    }

    void Placeholder(Rect rect, string text, float alpha)
    {
        GUI.color = new Color(1f, 1f, 1f, 0.5f * alpha);
        GUI.Label(rect, $"[ {text} ]", UIKit.Small);
        GUI.color = Color.white;
    }

    void DrawFill(Rect rect, Color color)
    {
        if (color.a <= 0f) return;
        GUI.color = color;
        GUI.DrawTexture(rect, white);
        GUI.color = Color.white;
    }

    Texture2D Load(string path)
    {
        if (!textures.TryGetValue(path, out Texture2D tex))
        {
            tex = Resources.Load<Texture2D>(path);
            textures[path] = tex;
        }
        return tex;
    }
}
