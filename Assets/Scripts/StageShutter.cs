using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 가게 셔터(철판) 오픈/클로즈 연출을 담당하는 스크립트
/// - 씬 시작: 셔터가 닫힌 채 현재 라운드 텍스트 표시 → 위로 슬라이드 (오픈)
/// - 씬 종료: 다음 라운드/GAME OVER 텍스트로 바꾸며 아래로 슬라이드 (클로즈) → 씬 전환
///
/// [패널 설정 방법]
/// 1. Canvas 안에 Image 오브젝트를 만들고 셔터 이미지를 할당합니다.
/// 2. RectTransform 설정:
///    - Anchor Presets: 하단 스트레칭 (anchorMin=(0,0), anchorMax=(1,0))
///    - Pivot: (0.5, 0) — 하단 중앙
///    - Height: Canvas 세로 크기와 동일하게 설정 (예: 1080)
///    - Pos Y: 0 (기본값 = 닫힌 상태, 화면을 꽉 채움)
/// 3. 텍스트는 위 패널의 자식 TextMeshProUGUI로 배치합니다.
/// 4. 이 스크립트의 shutterPanel, shutterText 필드에 각각 할당합니다.
///
/// [슬라이드 동작]
/// - 닫힌 상태: anchoredPosition.y = 0 (화면 하단 기준, 패널이 화면을 가림)
/// - 열린 상태: anchoredPosition.y = 패널 높이 (패널이 화면 위로 완전히 사라짐)
/// </summary>
public class StageShutter : MonoBehaviour
{
    [Header("패널 연결")]
    [Tooltip("셔터 패널 RectTransform (설정 방법: 상단 주석 참고)")]
    public RectTransform shutterPanel;

    [Tooltip("패널 안 텍스트 (TextMeshProUGUI)")]
    public TextMeshProUGUI shutterText;

    [Header("애니메이션 설정")]
    [Tooltip("슬라이드 속도 (px/초, 실제 시간 기준 — Time.timeScale 영향 없음)")]
    public float slideSpeed = 1500f;

    [Tooltip("씬 시작 후 셔터가 열리기 전 대기 시간 (실제 초)")]
    public float openDelay = 0.8f;

    [Tooltip("셔터가 완전히 닫힌 후 씬 전환 전 대기 시간 (실제 초)")]
    public float closedHoldDelay = 0.5f;

    // 패널 높이 (닫힌 Y=0, 열린 Y=panelHeight)
    private float panelHeight;

    void Awake()
    {
        // 패널을 닫힌 상태(Y=0)로 즉시 고정합니다.
        // rect.height는 이 시점에 0일 수 있으므로 높이는 코루틴에서 재계산합니다.
        if (shutterPanel != null)
            SetPanelY(0f);
    }

    void Start()
    {
        // Canvas 레이아웃 강제 갱신 후 높이 캐시 시도
        if (shutterPanel != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(shutterPanel);
            panelHeight = shutterPanel.rect.height;
        }
    }

    // ─────────────────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────────────────

    /// <summary>패널 텍스트를 변경합니다.</summary>
    public void SetText(string text)
    {
        if (shutterText != null)
            shutterText.text = text;
    }

    /// <summary>
    /// [StartScene 전용] 셔터를 화면 위로 완전히 숨긴 상태(오픈 상태)로 초기화합니다.
    /// PlayButton.Start()에서 호출하여 StartScene에서 셔터가 화면을 가리지 않게 합니다.
    /// </summary>
    public void InitializeOpen()
    {
        if (shutterPanel == null) return;
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(shutterPanel);
        float h = shutterPanel.rect.height > 10f ? shutterPanel.rect.height : Screen.height;
        panelHeight = h;
        SetPanelY(h);
    }

    /// <summary>
    /// [씬 시작 시 호출] 셔터를 닫힌 상태에서 위로 올려 게임을 드러냅니다.
    /// onComplete: 열림 완료 후 호출할 콜백 (= 게임 시작 시점)
    /// </summary>
    public void PlayOpenSequence(string text, Action onComplete)
    {
        SetText(text);
        if (shutterPanel != null) SetPanelY(0f);
        StartCoroutine(OpenCoroutine(onComplete));
    }

    /// <summary>
    /// [씬 종료 시 호출] 셔터를 아래로 내려 화면을 가립니다.
    /// Time.timeScale = 0 상태에서도 정상 동작합니다 (unscaled 기준).
    /// onComplete: 닫힘 완료 후 호출할 콜백 (= 씬 전환 시점)
    /// </summary>
    public void PlayCloseSequence(string text, Action onComplete)
    {
        SetText(text);
        StopAllCoroutines();
        StartCoroutine(CloseCoroutine(onComplete));
    }

    // ─────────────────────────────────────────────────────────
    // 내부 코루틴
    // ─────────────────────────────────────────────────────────

    private IEnumerator OpenCoroutine(Action onComplete)
    {
        SetPanelY(0f);

        // 한 프레임 대기 → Canvas 레이아웃 완전 초기화 보장
        yield return null;

        // 높이를 이 시점에 다시 읽어야 올바른 값이 나옵니다.
        panelHeight = shutterPanel.rect.height;
        // 그래도 0이면 화면 높이를 폴백으로 사용합니다.
        float target = panelHeight > 10f ? panelHeight : Screen.height;

        yield return new WaitForSecondsRealtime(openDelay);

        while (GetPanelY() < target - 2f)
        {
            float next = Mathf.MoveTowards(GetPanelY(), target, slideSpeed * Time.unscaledDeltaTime);
            SetPanelY(next);
            yield return null;
        }
        SetPanelY(target);
        onComplete?.Invoke();
    }

    private IEnumerator CloseCoroutine(Action onComplete)
    {
        // 닫기 시작 전에도 높이를 확인합니다
        if (panelHeight < 10f)
            panelHeight = shutterPanel.rect.height > 10f ? shutterPanel.rect.height : Screen.height;

        float target = 0f;
        while (GetPanelY() > target + 2f)
        {
            float next = Mathf.MoveTowards(GetPanelY(), target, slideSpeed * Time.unscaledDeltaTime);
            SetPanelY(next);
            yield return null;
        }
        SetPanelY(target);
        yield return new WaitForSecondsRealtime(closedHoldDelay);
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼
    // ─────────────────────────────────────────────────────────

    private void SetPanelY(float y)
    {
        Vector2 pos = shutterPanel.anchoredPosition;
        pos.y = y;
        shutterPanel.anchoredPosition = pos;
    }

    private float GetPanelY()
    {
        return shutterPanel.anchoredPosition.y;
    }
}
