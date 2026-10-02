using UnityEngine;

/// <summary>설정 패널(타이틀·수선소 공용). UIKit.Begin() 안에서 그린다.</summary>
public class SettingsPanel
{
    bool confirmReset;

    public const float Width = 1000f;
    public const float Height = 620f;

    /// <summary>panel 위치에 설정 항목을 그린다.</summary>
    public void Draw(Rect panel)
    {
        UIKit.DrawPanel(panel);
        float labelX = panel.x + 60f;
        float valueX = panel.x + 380f;
        float y = panel.y + 60f;

        // 볼륨
        GUI.Label(new Rect(labelX, y, 300f, 60f), "마스터 볼륨", UIKit.Body);
        float volume = GUI.HorizontalSlider(new Rect(valueX, y + 24f, 420f, 30f), GameSettings.MasterVolume, 0f, 1f);
        if (!Mathf.Approximately(volume, GameSettings.MasterVolume)) GameSettings.MasterVolume = volume;
        GUI.Label(new Rect(valueX + 440f, y, 120f, 60f), $"{Mathf.RoundToInt(volume * 100f)}%", UIKit.Body);
        y += 120f;

        // 텍스트 속도
        GUI.Label(new Rect(labelX, y, 300f, 60f), "스토리 텍스트 속도", UIKit.Body);
        for (int i = 0; i < GameSettings.TextSpeedNames.Length; i++)
        {
            GUIStyle style = i == GameSettings.TextSpeed ? UIKit.ButtonSelected : UIKit.ButtonSmall;
            if (GUI.Button(new Rect(valueX + i * 135f, y + 4f, 125f, 56f), GameSettings.TextSpeedNames[i], style))
                GameSettings.TextSpeed = i;
        }
        y += 120f;

        // 전체 화면
        GUI.Label(new Rect(labelX, y, 300f, 60f), "전체 화면", UIKit.Body);
        bool fullscreen = GameSettings.Fullscreen;
        if (GUI.Button(new Rect(valueX, y + 4f, 125f, 56f), "켜기", fullscreen ? UIKit.ButtonSelected : UIKit.ButtonSmall)) GameSettings.Fullscreen = true;
        if (GUI.Button(new Rect(valueX + 135f, y + 4f, 125f, 56f), "끄기", fullscreen ? UIKit.ButtonSmall : UIKit.ButtonSelected)) GameSettings.Fullscreen = false;
        y += 120f;

        // 진행 초기화
        GUI.Label(new Rect(labelX, y, 300f, 60f), "진행 상황", UIKit.Body);
        string resetText = confirmReset ? "정말 초기화할까요?" : "초기화";
        if (GUI.Button(new Rect(valueX, y + 4f, 395f, 56f), resetText, confirmReset ? UIKit.ButtonSelected : UIKit.ButtonSmall))
        {
            if (confirmReset)
            {
                Progress.ResetAll();
                QuestProgress.ResetAll();
            }
            confirmReset = !confirmReset;
        }
    }

    /// <summary>패널을 닫을 때 호출(저장, 확인 상태 초기화).</summary>
    public void Close()
    {
        confirmReset = false;
        GameSettings.Save();
    }
}
