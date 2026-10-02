using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 장의 배경 레이어를 배치 파일(JSON)대로 조립한다. 로비(Assets/Art/Lobby/lobby_layout.json)와
/// 메인(Assets/Art/Main/Depth/main_layout.json)이 같이 쓴다.
/// 좌표는 1920×1080 화면 기준 픽셀(왼쪽 위가 0,0)이고, x·y는 이미지 중심, width는 화면에서의 폭이다.
/// 배치를 바꾸려면 JSON 숫자를 고치면 된다(Unity 없이 미리보기: python3 Tools/Art/preview_layers.py out.png [--lobby]).
/// depth(0=먼 배경, 1=가장 앞)에 따라 마우스를 움직이면 살짝 시차가 생긴다.
/// </summary>
public class LayeredBackground : MonoBehaviour
{
    [Serializable]
    class LayoutLayer
    {
        public string name;
        public float x, y, width, depth;
        public bool flipX;
        [Tooltip("0보다 크면 이 정도로 밝기가 은은하게 깜빡인다(창문 불빛 등)")]
        public float flicker;
    }

    [Serializable]
    class Layout
    {
        public float canvasWidth = 1920f;
        public float canvasHeight = 1080f;
        public string background = "#14122a";
        public LayoutLayer[] layers = new LayoutLayer[0];
    }

    class Placed
    {
        public Transform transform;
        public Vector3 basePosition;
        public Vector3 baseScale;
        public float depth;
        public float height;
        public bool breathes;
        public float phase;
        public SpriteRenderer renderer;
        public float flicker;
    }

    public TextAsset layoutJson;
    [Tooltip("레이어 이름과 같은 이름의 스프라이트")]
    public Sprite[] sprites = new Sprite[0];
    public float pixelsPerUnit = 100f;
    [Tooltip("가장 앞 레이어가 움직이는 최대 픽셀")]
    public float parallaxPixels = 14f;
    [Tooltip("숨쉬기 연출을 넣을 레이어 이름")]
    public string[] breathingLayers = new string[0];

    /// <summary>화면 안 마우스 위치(0~1). UI 스크립트가 넣어 준다.</summary>
    public static Vector2 Pointer = new Vector2(0.5f, 0.5f);

    readonly List<Placed> placed = new List<Placed>();
    Vector2 smoothedPointer = new Vector2(0.5f, 0.5f);

    void Start()
    {
        if (layoutJson == null) return;
        Layout layout = JsonUtility.FromJson<Layout>(layoutJson.text);

        Camera cam = Camera.main;
        Color background;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = layout.canvasHeight / pixelsPerUnit / 2f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            if (ColorUtility.TryParseHtmlString(layout.background, out background)) cam.backgroundColor = background;
        }

        var byName = new Dictionary<string, Sprite>();
        foreach (Sprite sprite in sprites)
            if (sprite != null) byName[sprite.name] = sprite;

        for (int i = 0; i < layout.layers.Length; i++)
        {
            LayoutLayer layer = layout.layers[i];
            Sprite sprite;
            if (!byName.TryGetValue(layer.name, out sprite))
            {
                Debug.LogWarning($"[배경] 레이어 '{layer.name}' 이미지가 없습니다 ({layer.name}.png)");
                continue;
            }

            var go = new GameObject(layer.name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.flipX = layer.flipX;
            renderer.sortingOrder = i - layout.layers.Length; // UI(IMGUI)보다 항상 뒤

            float spriteWidth = sprite.rect.width / sprite.pixelsPerUnit;
            float scale = layer.width / pixelsPerUnit / spriteWidth;
            Vector3 position = new Vector3(
                (layer.x - layout.canvasWidth / 2f) / pixelsPerUnit,
                (layout.canvasHeight / 2f - layer.y) / pixelsPerUnit,
                0f);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            placed.Add(new Placed
            {
                transform = go.transform,
                basePosition = position,
                baseScale = go.transform.localScale,
                depth = layer.depth,
                height = sprite.rect.height / sprite.pixelsPerUnit * scale,
                breathes = Array.IndexOf(breathingLayers, layer.name) >= 0,
                phase = i * 1.7f,
                renderer = renderer,
                flicker = layer.flicker,
            });
        }
    }

    void Update()
    {
        smoothedPointer = Vector2.Lerp(smoothedPointer, Pointer, 1f - Mathf.Exp(-4f * Time.deltaTime));
        Vector2 offset = (smoothedPointer - new Vector2(0.5f, 0.5f)) * 2f; // -1 ~ 1
        float t = Time.time;

        foreach (Placed p in placed)
        {
            Vector3 parallax = new Vector3(-offset.x, offset.y, 0f) * (parallaxPixels * p.depth / pixelsPerUnit);
            Vector3 position = p.basePosition + parallax;
            if (p.breathes)
            {
                // 아래쪽을 고정한 채 세로로만 살짝 늘었다 줄었다(숨쉬기).
                float k = 1f + 0.006f * Mathf.Sin(t * 1.6f + p.phase);
                p.transform.localScale = new Vector3(p.baseScale.x, p.baseScale.y * k, 1f);
                position.y += p.height * (k - 1f) * 0.5f;
            }
            p.transform.localPosition = position;

            if (p.flicker > 0f)
            {
                // 느린 물결 두 개를 섞어 규칙적이지 않게 깜빡인다.
                float wave = 0.5f + 0.25f * Mathf.Sin(t * 2.3f + p.phase) + 0.25f * Mathf.Sin(t * 5.1f + p.phase * 2f);
                float brightness = 1f - p.flicker * wave;
                p.renderer.color = new Color(brightness, brightness, brightness, 1f);
            }
        }
    }
}
