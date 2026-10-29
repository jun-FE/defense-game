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

        DrawBackground(previousBackground, 1f, w, h);
        DrawBackground(background, backgroundBlend, w, h);
        if (ShowingCard)
        {
            DrawCard(w, h);
        }
        else
        {
            DrawPortraits(w, h);
            DrawDialogue(w, h);
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
        StoryLine line = Line;

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

        if (string.IsNullOrEmpty(line.speaker))
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

    void DrawBackground(string name, float alpha, float w, float h)
    {
        if (string.IsNullOrEmpty(name) || alpha <= 0f) return;
        var full = new Rect(0f, 0f, w, h);
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
        float bottom = h - 330f;
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

    void DrawDialogue(float w, float h)
    {
        var panel = new Rect(80f, h - 360f, w - 160f, 290f);
        UIKit.DrawPanel(panel);

        if (!string.IsNullOrEmpty(Line.speaker))
            GUI.Label(new Rect(panel.x + 30f, panel.y - 40f, 340f, 70f), Line.speaker, UIKit.Speaker);

        string text = Line.text.Substring(0, Mathf.FloorToInt(shownChars));
        if (string.IsNullOrEmpty(Line.speaker)) text = $"<i>{text}</i>";
        GUI.Label(new Rect(panel.x + 60f, panel.y + 60f, panel.width - 120f, panel.height - 100f), text, UIKit.Dialogue);

        GUI.Label(new Rect(panel.xMax - 200f, panel.yMax - 50f, 120f, 40f), $"{index + 1} / {story.lines.Length}", UIKit.Small);
        if (LineComplete && Mathf.FloorToInt(Time.time * 2f) % 2 == 0)
            GUI.Label(new Rect(panel.xMax - 70f, panel.yMax - 55f, 40f, 40f), "▼", UIKit.Small);

        GUI.Label(new Rect(0f, h - 55f, w, 40f), "클릭 또는 Space: 다음 대사", UIKit.Small);
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
