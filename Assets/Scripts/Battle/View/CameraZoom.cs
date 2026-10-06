using UnityEngine;

/// <summary>
/// 전투 카메라 확대·축소와 이동. 맵이 넓어지면(단계별로 커지는 칸 맵) 전체가 작게 보이므로 직접 당겨 볼 수 있게 한다.
/// - 마우스 휠: 마우스 위치를 기준으로 확대/축소(가장 멀리 = 맵 전체가 보이는 크기)
/// - 가운데 버튼 드래그, 방향키: 이동(확대했을 때만, 맵 밖으로는 나가지 않음)
/// - F: 전체 보기로 돌아가기
/// MapView가 화면비에 맞춘 "전체 보기" 크기를 SetFit으로 알려 준다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Tooltip("전체 보기 대비 최대 확대 배율")]
    public float maxZoom = 3f;
    [Tooltip("휠 한 칸에 바뀌는 비율")]
    public float wheelStep = 0.12f;
    [Tooltip("방향키 이동 속도(화면 높이 비율/초)")]
    public float keyPanSpeed = 0.9f;

    Camera cam;
    Rect bounds;
    float fitSize;
    bool hasFit;
    Vector3 lastMouse;

    void Awake() => cam = GetComponent<Camera>();

    /// <summary>전체 보기 상태로 맞춘다. bounds는 맵이 차지하는 월드 영역.</summary>
    public void SetFit(Rect mapBounds, float size)
    {
        bounds = mapBounds;
        fitSize = size;
        hasFit = true;
        ResetView();
    }

    public void ResetView()
    {
        if (!hasFit) return;
        cam.orthographicSize = fitSize;
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, cam.transform.position.z);
    }

    void Update()
    {
        if (!hasFit) return;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            Vector3 before = cam.ScreenToWorldPoint(Input.mousePosition);
            float size = cam.orthographicSize * Mathf.Pow(1f - wheelStep, wheel);
            cam.orthographicSize = Mathf.Clamp(size, fitSize / Mathf.Max(1f, maxZoom), fitSize);
            Vector3 after = cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += before - after; // 마우스 아래 지점이 그대로 있게
        }

        if (Input.GetMouseButtonDown(2)) lastMouse = Input.mousePosition;
        if (Input.GetMouseButton(2))
        {
            Vector3 now = Input.mousePosition;
            float unitsPerPixel = cam.orthographicSize * 2f / Mathf.Max(1, Screen.height);
            cam.transform.position -= (now - lastMouse) * unitsPerPixel;
            lastMouse = now;
        }

        var keys = new Vector3(
            (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f),
            (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f), 0f);
        if (keys.sqrMagnitude > 0f) cam.transform.position += keys * (cam.orthographicSize * 2f * keyPanSpeed * Time.unscaledDeltaTime);

        if (Input.GetKeyDown(KeyCode.F)) ResetView();

        Clamp();
    }

    /// <summary>보이는 영역이 맵보다 작으면 맵 안에 머물게, 크면 맵 가운데에 둔다.</summary>
    void Clamp()
    {
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        Vector3 p = cam.transform.position;
        p.x = halfW * 2f >= bounds.width ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
        p.y = halfH * 2f >= bounds.height ? bounds.center.y : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
        cam.transform.position = p;
    }
}
