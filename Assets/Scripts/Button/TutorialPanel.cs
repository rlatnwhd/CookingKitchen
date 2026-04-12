using System.Collections;
using UnityEngine;

/// <summary>
/// 튜토리얼 패널 슬라이드 애니메이션을 담당하는 스크립트
/// - 튜토리얼 버튼 클릭 → 위에서 아래로 슬라이드In (ShowPanel)
/// - ESC 키 or 닫기 버튼 → 위로 슬라이드Out (HidePanel)
///
/// [유니티 설정]
/// 1. Canvas 아래에 Image 오브젝트 생성 (튜토리얼 이미지 할당)
/// 2. RectTransform: Anchor = Stretch All, Pivot = (0.5, 0.5), LRTB = 0
///    → 인스펙터에서 Pos Y 를 0으로 두면 됩니다. (화면 중앙 = 표시 상태)
/// 3. 이 스크립트의 tutorialPanel 에 해당 RectTransform 을 할당합니다.
/// </summary>
public class TutorialPanel : MonoBehaviour
{
    [Header("패널 연결")]
    [Tooltip("튜토리얼 패널 RectTransform")]
    public RectTransform tutorialPanel;

    [Header("애니메이션 설정")]
    [Tooltip("슬라이드 속도 (px/초)")]
    public float slideSpeed = 2000f;

    // 보임/숨김 Y 값 (런타임에 자동 계산)
    private float visibleY;
    private float hiddenY;

    private bool isVisible = false;
    private bool initialized = false;
    private Coroutine slideCoroutine;

    void Awake()
    {
        // 레이아웃 확정 전 일단 크게 밀어서 보이지 않게
        if (tutorialPanel != null)
            SetY(9999f);
    }

    void Start()
    {
        StartCoroutine(InitCoroutine());
    }

    void Update()
    {
        if (isVisible && Input.GetKeyDown(KeyCode.Escape))
            HidePanel();
    }

    // ─────────────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────────────

    private IEnumerator InitCoroutine()
    {
        // 2프레임 대기 → Canvas 레이아웃이 완전히 확정된 뒤 height 읽기
        yield return null;
        yield return null;

        // 부모 Canvas 의 RectTransform 높이 = Canvas 유닛 기준 실제 높이
        float canvasHeight = GetCanvasHeight();

        // 표시 상태(Y=0), 숨김 상태(Y = +canvasHeight 위로 이동)
        visibleY = 0f;
        hiddenY  = canvasHeight;

        // 숨김 위치로 이동
        SetY(hiddenY);
        initialized = true;
    }

    private float GetCanvasHeight()
    {
        // tutorialPanel 의 부모 Canvas RT 에서 높이 취득 (Canvas Scaler 반영됨)
        Canvas canvas = tutorialPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            float h = canvas.GetComponent<RectTransform>().rect.height;
            if (h > 10f) return h;
        }

        // 폴백: 패널 자체 높이 (Stretch All 이면 Canvas 높이와 동일)
        float ph = tutorialPanel.rect.height;
        return ph > 10f ? ph : 1080f;
    }

    // ─────────────────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────────────────

    /// <summary>튜토리얼 버튼에서 호출합니다. 패널을 위에서 아래로 내립니다.</summary>
    public void ShowPanel()
    {
        if (isVisible || !initialized) return;
        isVisible = true;

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideTo(visibleY));
    }

    /// <summary>닫기 버튼이나 ESC 에서 호출합니다. 패널을 위로 올립니다.</summary>
    public void HidePanel()
    {
        if (!isVisible) return;
        isVisible = false;

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideTo(hiddenY));
    }

    // ─────────────────────────────────────────────────────────
    // 내부
    // ─────────────────────────────────────────────────────────

    private IEnumerator SlideTo(float targetY)
    {
        while (Mathf.Abs(GetY() - targetY) > 1f)
        {
            float next = Mathf.MoveTowards(GetY(), targetY, slideSpeed * Time.unscaledDeltaTime);
            SetY(next);
            yield return null;
        }
        SetY(targetY);
        slideCoroutine = null;
    }

    private void SetY(float y)
    {
        Vector2 pos = tutorialPanel.anchoredPosition;
        pos.y = y;
        tutorialPanel.anchoredPosition = pos;
    }

    private float GetY() => tutorialPanel.anchoredPosition.y;
}

