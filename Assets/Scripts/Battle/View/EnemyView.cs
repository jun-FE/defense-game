using Akmong.Battle;
using UnityEngine;

/// <summary>적 하나의 화면 표시: 두 틱 사이 위치 보간, 체력바, 사라지는 연출.</summary>
public class EnemyView : MonoBehaviour
{
    EnemyState state;
    Transform healthFill;
    SpriteRenderer body;
    float barWidth;
    bool finishing;

    public void Init(EnemyState enemy, EnemyAsset asset, Sprite circle, Sprite square)
    {
        state = enemy;
        float scale = asset != null ? asset.scale : 0.6f;

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(transform, false);
        bodyGo.transform.localScale = Vector3.one * scale;
        body = bodyGo.AddComponent<SpriteRenderer>();
        body.sprite = asset != null && asset.sprite != null ? asset.sprite : circle;
        body.color = asset != null ? asset.tint : new Color(0.85f, 0.3f, 0.35f);
        body.sortingOrder = 2;

        barWidth = Mathf.Max(0.6f, scale);
        float barY = scale * 0.5f + 0.2f;
        CreateBar("HealthBack", square, new Color(0.08f, 0.08f, 0.1f), barY, 3);
        healthFill = CreateBar("HealthFill", square, new Color(0.45f, 0.9f, 0.5f), barY, 4).transform;

        transform.position = BattleContentBuilder.ToUnity(enemy.Position);
    }

    public void Sync(float alpha)
    {
        if (finishing || state == null) return;
        transform.position = Vector2.Lerp(BattleContentBuilder.ToUnity(state.PreviousPosition), BattleContentBuilder.ToUnity(state.Position), alpha);

        float t = Mathf.Clamp01(state.Hp / state.MaxHp);
        healthFill.localScale = new Vector3(barWidth * t, 0.08f, 1f);
        healthFill.localPosition = new Vector3(-barWidth * (1f - t) * 0.5f, healthFill.localPosition.y, 0f);
    }

    /// <summary>처치되면 작아지며 사라지고, 중심에 닿으면 바로 사라진다.</summary>
    public void Finish(bool killed)
    {
        finishing = true;
        if (!killed)
        {
            Destroy(gameObject);
            return;
        }
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        float duration = 0.25f;
        Vector3 start = transform.localScale;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = 1f - t / duration;
            transform.localScale = start * k;
            Color c = body.color;
            c.a = k;
            body.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }

    GameObject CreateBar(string objectName, Sprite square, Color color, float y, int order)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, y, 0f);
        go.transform.localScale = new Vector3(barWidth, 0.08f, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }
}
