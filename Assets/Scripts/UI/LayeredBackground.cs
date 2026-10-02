using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 장의 배경 레이어를 배치 파일(JSON)대로 조립한다. 로비(Assets/Art/Lobby/lobby_layout.json)와
/// 메인(Assets/Art/Main/Depth/main_layout.json)이 같이 쓴다.
/// 좌표는 1920×1080 화면 기준 픽셀(왼쪽 위가 0,0)이고, x·y는 이미지 중심, width는 화면에서의 폭이다.
/// 배치를 바꾸려면 JSON 숫자를 고치면 된다(Unity 없이 미리보기: python3 Tools/Art/preview_layers.py out.png [--lobby]).
/// depth(0=먼 배경, 1=가장 앞)에 따라 마우스를 움직이면 살짝 시차가 생긴다.
///
/// 분위기(선택 항목):
/// - ambient: 모든 레이어에 곱하는 색(어둡고 푸르게). 레이어별 shade로 더 어둡게(1 미만)·밝게(1 초과) 조절.
/// - lights: 랜턴·수정구 위치에 빛 번짐을 더하고 twinkle만큼 일렁이게 한다. layer를 주면 그 레이어와 같이 움직인다.
/// - vignette: 화면 가장자리를 어둡게(0~1).
/// - 설정의 "배경 밝기"(GameSettings.BackgroundBrightness)가 ambient에 곱해진다.
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
        [Tooltip("ambient에 더 곱하는 밝기. 0이면 1로 본다.")]
        public float shade;
    }

    [Serializable]
    class LayoutLight
    {
        public float x, y, size;
        public string color = "#ffb257";
        [Tooltip("일렁임 정도(0~1)")]
        public float twinkle = 0.25f;
        public string layer;
    }

    [Serializable]
    class Layout
    {
        public float canvasWidth = 1920f;
        public float canvasHeight = 1080f;
        public string background = "#14122a";
        public string ambient = "#ffffff";
        public float vignette;
        public LayoutLayer[] layers = new LayoutLayer[0];
        public LayoutLight[] lights = new LayoutLight[0];
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
        public float shade;
    }

    class GlowLight
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 basePosition;
        public float baseScale;
        public float depth;
        public Color color;
        public float twinkle;
        public float seed;
        /// <summary>이번 프레임 밝기 배율(1 = 기본).</summary>
        public float intensity = 1f;
    }

    public TextAsset layoutJson;
    [Tooltip("레이어 이름과 같은 이름의 스프라이트")]
    public Sprite[] sprites = new Sprite[0];
    public float pixelsPerUnit = 100f;
    [Tooltip("가장 앞 레이어가 움직이는 최대 픽셀")]
    public float parallaxPixels = 14f;
    [Tooltip("숨쉬기 연출을 넣을 레이어 이름")]
    public string[] breathingLayers = new string[0];
    [Tooltip("조명 빛 번짐에 쓰는 가산(Additive) 머티리얼. 비어 있으면 일반 반투명으로 그린다.")]
    public Material glowMaterial;

    /// <summary>화면 안 마우스 위치(0~1). UI 스크립트가 넣어 준다.</summary>
    public static Vector2 Pointer = new Vector2(0.5f, 0.5f);

    readonly List<Placed> placed = new List<Placed>();
    readonly List<GlowLight> lights = new List<GlowLight>();
    Vector2 smoothedPointer = new Vector2(0.5f, 0.5f);
    Color ambient = Color.white;

    static Sprite glowSprite;

    void Start()
    {
        if (layoutJson == null) return;
        Layout layout = JsonUtility.FromJson<Layout>(layoutJson.text);

        Camera cam = Camera.main;
        Color parsed;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = layout.canvasHeight / pixelsPerUnit / 2f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            if (ColorUtility.TryParseHtmlString(layout.background, out parsed)) cam.backgroundColor = parsed;
        }
        if (!string.IsNullOrEmpty(layout.ambient) && ColorUtility.TryParseHtmlString(layout.ambient, out parsed)) ambient = parsed;

        var byName = new Dictionary<string, Sprite>();
        foreach (Sprite sprite in sprites)
            if (sprite != null) byName[sprite.name] = sprite;

        int count = layout.layers.Length;
        var layerIndex = new Dictionary<string, int>();
        for (int i = 0; i < count; i++)
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
            // 레이어 사이에 조명이 끼도록 2칸씩. 전부 음수라 UI(IMGUI)보다 항상 뒤.
            renderer.sortingOrder = 2 * (i - count) - 2;

            float spriteWidth = sprite.rect.width / sprite.pixelsPerUnit;
            float scale = layer.width / pixelsPerUnit / spriteWidth;
            Vector3 position = ToWorld(layout, layer.x, layer.y);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            layerIndex[layer.name] = placed.Count;
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
                shade = layer.shade > 0f ? layer.shade : 1f,
            });
        }

        if (layout.lights != null)
            for (int i = 0; i < layout.lights.Length; i++) CreateLight(layout, layout.lights[i], layerIndex, i);
        if (layout.vignette > 0f) CreateVignette(layout);
        ApplyColors(Time.time);
    }

    Vector3 ToWorld(Layout layout, float x, float y) => new Vector3(
        (x - layout.canvasWidth / 2f) / pixelsPerUnit,
        (layout.canvasHeight / 2f - y) / pixelsPerUnit,
        0f);

    void CreateLight(Layout layout, LayoutLight data, Dictionary<string, int> layerIndex, int index)
    {
        Color color;
        if (!ColorUtility.TryParseHtmlString(data.color, out color)) color = new Color(1f, 0.7f, 0.35f);

        int owner = -1;
        bool hasOwner = !string.IsNullOrEmpty(data.layer) && layerIndex.TryGetValue(data.layer, out owner);

        var go = new GameObject("Light " + index);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GlowSprite();
        if (glowMaterial != null) renderer.sharedMaterial = glowMaterial;
        renderer.sortingOrder = hasOwner ? placed[owner].renderer.sortingOrder + 1 : -2;

        Vector3 position = ToWorld(layout, data.x, data.y);
        float scale = data.size / pixelsPerUnit; // 빛 스프라이트는 1유닛 폭
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        lights.Add(new GlowLight
        {
            transform = go.transform,
            renderer = renderer,
            basePosition = position,
            baseScale = scale,
            depth = hasOwner ? placed[owner].depth : 0.5f,
            color = color,
            twinkle = Mathf.Clamp01(data.twinkle),
            seed = index * 13.37f + 3.1f,
        });
    }

    void CreateVignette(Layout layout)
    {
        const int w = 192, h = 108;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                float a = layout.vignette * Mathf.Pow(Mathf.Clamp01((d - 0.35f) / 0.65f), 1.6f);
                pixels[y * w + x] = new Color32(4, 3, 12, (byte)(Mathf.Clamp01(a) * 255f));
            }
        texture.SetPixels32(pixels);
        texture.Apply();

        var go = new GameObject("Vignette");
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        // 픽셀/유닛 = 폭 → 스프라이트 크기 1 × (h/w) 유닛
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        renderer.sortingOrder = -1;
        float width = layout.canvasWidth / pixelsPerUnit * 1.04f; // 시차로 가장자리가 드러나지 않게 살짝 크게
        go.transform.localScale = new Vector3(width, width, 1f);
    }

    /// <summary>가운데가 밝고 가장자리로 부드럽게 사라지는 원형 빛(1유닛 폭).</summary>
    static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), 2.2f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        texture.SetPixels32(pixels);
        texture.Apply();
        glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return glowSprite;
    }

    void Update()
    {
        smoothedPointer = Vector2.Lerp(smoothedPointer, Pointer, 1f - Mathf.Exp(-4f * Time.deltaTime));
        Vector2 offset = (smoothedPointer - new Vector2(0.5f, 0.5f)) * 2f; // -1 ~ 1
        float t = Time.time;

        foreach (Placed p in placed)
        {
            Vector3 position = p.basePosition + Parallax(offset, p.depth);
            if (p.breathes)
            {
                // 아래쪽을 고정한 채 세로로만 살짝 늘었다 줄었다(숨쉬기).
                float k = 1f + 0.006f * Mathf.Sin(t * 1.6f + p.phase);
                p.transform.localScale = new Vector3(p.baseScale.x, p.baseScale.y * k, 1f);
                position.y += p.height * (k - 1f) * 0.5f;
            }
            p.transform.localPosition = position;
        }

        foreach (GlowLight light in lights)
        {
            // 촛불처럼: 느린 숨결 + 빠른 떨림, 가끔 확 밝아지는 반짝임.
            float slow = Mathf.PerlinNoise(t * 0.7f, light.seed);
            float fast = Mathf.PerlinNoise(t * 6f, light.seed + 50f);
            float sparkle = Mathf.Pow(Mathf.PerlinNoise(t * 1.3f, light.seed + 100f), 6f) * 2.5f;
            light.intensity = 1f - light.twinkle * (0.6f * (1f - slow) + 0.4f * (1f - fast) - sparkle);
            light.transform.localPosition = light.basePosition + Parallax(offset, light.depth);
            float size = light.baseScale * (0.92f + 0.08f * light.intensity);
            light.transform.localScale = new Vector3(size, size, 1f);
        }

        ApplyColors(t);
    }

    Vector3 Parallax(Vector2 offset, float depth) =>
        new Vector3(-offset.x, offset.y, 0f) * (parallaxPixels * depth / pixelsPerUnit);

    void ApplyColors(float t)
    {
        float brightness = GameSettings.BackgroundBrightness;

        foreach (Placed p in placed)
        {
            float k = p.shade * brightness;
            if (p.flicker > 0f)
            {
                // 느린 물결 두 개를 섞어 규칙적이지 않게 깜빡인다.
                float wave = 0.5f + 0.25f * Mathf.Sin(t * 2.3f + p.phase) + 0.25f * Mathf.Sin(t * 5.1f + p.phase * 2f);
                k *= 1f - p.flicker * wave;
            }
            p.renderer.color = new Color(
                Mathf.Clamp01(ambient.r * k), Mathf.Clamp01(ambient.g * k), Mathf.Clamp01(ambient.b * k), 1f);
        }

        // 배경을 밝게 할수록 빛 번짐은 줄여서 화면이 뿌옇지 않게 한다.
        float glow = 0.85f * Mathf.Lerp(1.2f, 0.7f, Mathf.InverseLerp(0.6f, 1.4f, brightness));
        foreach (GlowLight light in lights)
        {
            Color c = light.color;
            c.a = Mathf.Clamp01(glow * Mathf.Clamp(light.intensity, 0f, 1.6f));
            light.renderer.color = c;
        }
    }
}
