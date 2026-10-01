using UnityEngine;

/// <summary>
/// 스토리 화면. 대사를 한 글자씩 출력한다.
/// 클릭/Space/Enter: 출력 중이면 대사를 바로 완성, 다 나왔으면 다음 대사.
/// 우상단 스킵 버튼/Esc: 스토리를 건너뛰고 게임 시작.
/// </summary>
public class StoryPlayer : MonoBehaviour
{
    [Tooltip("이 씬을 직접 실행했을 때 보여줄 스토리")]
    public StoryData fallbackStory;

    StoryData story;
    int index;
    float shownChars;
    bool finished;

    StoryLine Line => story.lines[index];
    bool LineComplete => shownChars >= Line.text.Length;

    void Start()
    {
        story = SceneFlow.PendingStory != null ? SceneFlow.PendingStory : fallbackStory;
        if (story == null || story.lines.Length == 0)
        {
            Finish();
            return;
        }
        if (Camera.main != null) Camera.main.backgroundColor = story.background;
    }

    void Update()
    {
        if (story == null || finished) return;
        float speed = GameSettings.TextCharsPerSecond;
        shownChars = speed <= 0f ? Line.text.Length : Mathf.Min(Line.text.Length, shownChars + speed * Time.deltaTime);
    }

    void OnGUI()
    {
        if (story == null || finished) return;

        UIKit.Begin();
        float w = UIKit.Width;
        float h = UIKit.Height;

        bool skip = UIKit.DrawButton(new Rect(w - 250f, 30f, 210f, 64f), "스킵 ▶▶", UIKit.ButtonSmall);
        if (!skip) DrawDialogue(w, h);
        UIKit.End();

        if (skip)
        {
            Finish();
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
            else if (e.keyCode == KeyCode.Escape) { Finish(); e.Use(); }
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

    void Advance()
    {
        if (!LineComplete)
        {
            shownChars = Line.text.Length;
            return;
        }
        index++;
        shownChars = 0f;
        if (index >= story.lines.Length) Finish();
    }

    void Finish()
    {
        if (finished) return;
        finished = true;
        SceneFlow.FinishStory();
    }
}
