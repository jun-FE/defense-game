using UnityEngine;

/// <summary>선택된 타워 종류를 기억하고, 클릭한 자리에 타워를 짓는다.</summary>
public class BuildManager : MonoBehaviour
{
    public Tower[] towerPrefabs;

    public int SelectedIndex { get; private set; }
    public Tower Selected => towerPrefabs[SelectedIndex];

    public void Select(int index)
    {
        if (index >= 0 && index < towerPrefabs.Length) SelectedIndex = index;
    }

    /// <summary>worldPoint에 있는 자리에 타워를 짓는다. 결과 메시지를 돌려준다(없으면 null).</summary>
    public string TryBuildAt(Vector2 worldPoint)
    {
        foreach (BuildSlot slot in BuildSlot.All)
        {
            if (!slot.Contains(worldPoint)) continue;
            if (slot.Built != null) return "이미 타워가 있습니다";
            if (!GameManager.Instance.TrySpend(Selected.cost)) return "골드가 부족합니다";
            slot.Build(Selected);
            return null;
        }
        return null;
    }
}
