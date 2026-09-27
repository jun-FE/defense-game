using UnityEngine;

/// <summary>적이 따라 걷는 경로. 자식 Transform들을 순서대로 웨이포인트로 쓴다.</summary>
public class PathRoute : MonoBehaviour
{
    public Transform[] waypoints;

    public int Count => waypoints.Length;

    public Vector3 GetPoint(int index) => waypoints[index].position;

    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
