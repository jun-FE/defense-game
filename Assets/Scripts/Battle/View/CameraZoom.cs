using UnityEngine;

/// <summary>
/// 전투 카메라 확대·축소와 이동. 맵이 넓어지면(단계별로 커지는 칸 맵) 전체가 작게 보이므로 직접 당겨 볼 수 있게 한다.
/// - 마우스 휠: 마우스 위치를 기준으로 확대/축소. 전체 보기보다 더 작게(maxZoomOut배)까지 줄일 수 있다.
/// - 마우스 드래그(왼쪽·오른쪽·가운데 아무 버튼): 화면 이동. 조금이라도 끌면 클릭으로 보지 않는다(LastClickWasDrag).
/// - 방향키: 이동, F: 전체 보기로 돌아가기
/// MapView가 화면비와 HUD 자리를 뺀 "전체 보기" 크기·중심을 SetFit으로 알려 준다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Tooltip("전체 보기 대비 최대 확대 배율")]
    public float maxZoom = 3f;
    [Tooltip("전체 보기보다 더 멀리 볼 수 있는 배율(1이면 전체 보기가 가장 멀리)")]
    public float maxZoomOut = 1.6f;
    [Tooltip("휠 한 칸에 바뀌는 비율")]
    public float wheelStep = 0.12f;
    [Tooltip("방향키 이동 속도(화면 높이 비율/초)")]
    public float keyPanSpeed = 0.9f;
    [Tooltip("이만큼(화면 픽셀) 끌면 클릭이 아니라 화면 이동으로 본다")]
    public float dragThreshold = 8f;

    /// <summary>마우스가 HUD 위에 있으면 휠·드래그를 시작하지 않는다(BattleHUD가 매 프레임 알려 줌).</summary>
    public static bool PointerOverUI;
    /// <summary>방금 뗀 마우스 버튼이 드래그(화면 이동)였는지. HUD는 이때 클릭(건설·선택)을 하지 않는다.</summary>
    public static bool LastClickWasDrag;

    Camera cam;
    Rect bounds;
    Vector2 fitCenter;
    float fitSize;
    bool hasFit;
    int pressButton = -1;
    bool dragging;
    Vector3 pressPosition, lastMouse;

    void Awake() => cam = GetComponent<Camera>();

    /// <summary>전체 보기 상태로 맞춘다. bounds는 맵이 차지하는 월드 영역, center는 전체 보기 때 카메라 중심.</summary>
    public void SetFit(Rect mapBounds, float size, Vector2 center)
    {
        bounds = mapBounds;
        fitSize = size;
        fitCenter = center;
        hasFit = true;
        ResetView();
    }

    public void ResetView()
    {
        if (!hasFit) return;
        cam.orthographicSize = fitSize;
        cam.transform.position = new Vector3(fitCenter.x, fitCenter.y, cam.transform.position.z);
    }

    void Update()
    {
        if (!hasFit) return;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f && !PointerOverUI)
        {
            Vector3 before = cam.ScreenToWorldPoint(Input.mousePosition);
            float size = cam.orthographicSize * Mathf.Pow(1f - wheelStep, wheel);
            cam.orthographicSize = Mathf.Clamp(size, fitSize / Mathf.Max(1f, maxZoom), fitSize * Mathf.Max(1f, maxZoomOut));
            Vector3 after = cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += before - after; // 마우스 아래 지점이 그대로 있게
        }

        UpdateDrag();

        var keys = new Vector3(
            (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f),
            (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f), 0f);
        if (keys.sqrMagnitude > 0f) cam.transform.position += keys * (cam.orthographicSize * 2f * keyPanSpeed * Time.unscaledDeltaTime);

        if (Input.GetKeyDown(KeyCode.F)) ResetView();

        Clamp();
    }

    void UpdateDrag()
    {
        if (pressButton < 0)
        {
            for (int b = 0; b < 3; b++)
            {
                if (!Input.GetMouseButtonDown(b)) continue;
                LastClickWasDrag = false;
                if (PointerOverUI) return; // HUD 위에서 누른 건 HUD 몫
                pressButton = b;
                dragging = false;
                pressPosition = lastMouse = Input.mousePosition;
                return;
            }
            return;
        }

        Vector3 now = Input.mousePosition;
        if (Input.GetMouseButton(pressButton))
        {
            if (!dragging && (now - pressPosition).sqrMagnitude > dragThreshold * dragThreshold) dragging = true;
            if (dragging)
            {
                float unitsPerPixel = cam.orthographicSize * 2f / Mathf.Max(1, Screen.height);
                cam.transform.position -= (now - lastMouse) * unitsPerPixel;
            }
            lastMouse = now;
        }
        if (Input.GetMouseButtonUp(pressButton))
        {
            LastClickWasDrag = dragging;
            pressButton = -1;
            dragging = false;
        }
    }

    /// <summary>보이는 영역이 맵보다 작으면 맵 안에 머물게, 크면 전체 보기 중심에 둔다.</summary>
    void Clamp()
    {
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        Vector3 p = cam.transform.position;
        p.x = halfW * 2f >= bounds.width ? fitCenter.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
        p.y = halfH * 2f >= bounds.height ? fitCenter.y : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
        cam.transform.position = p;
    }
}
