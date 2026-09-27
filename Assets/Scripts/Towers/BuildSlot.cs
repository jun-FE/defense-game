using System.Collections.Generic;
using UnityEngine;

/// <summary>타워를 지을 수 있는 자리.</summary>
public class BuildSlot : MonoBehaviour
{
    public static readonly List<BuildSlot> All = new List<BuildSlot>();

    public float size = 0.9f;

    public Tower Built { get; private set; }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public bool Contains(Vector2 worldPoint)
    {
        Vector2 d = worldPoint - (Vector2)transform.position;
        float half = size * 0.5f;
        return Mathf.Abs(d.x) <= half && Mathf.Abs(d.y) <= half;
    }

    public void Build(Tower prefab)
    {
        Built = Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
