using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 오버 패널 스크립트
/// - 씬 로드 시 위에서 자동으로 슬라이드 인
/// - Total Score 레이블 + 점수(-99999) 한 글자씩 타이핑
/// - 점수 완료 후 등급 이미지가 크게 나타났다가 축소되며 고정
/// - 이후 Restart / Main 버튼 페이드인
///
/// ─────────────── 유니티 계층 구조 예시 ───────────────
/// Canvas
/// └── GameOverPanel          ← 이 스크립트 부착, Image 컴포넌트
///     │  Anchor: Stretch All, Pivot (0.5, 0.5), LRTB = 0, Pos Y = 0
///     │
///     ├── TotalScoreLabel    ← TextMeshProUGUI, 텍스트: "Total Score"
///     │                         (왼쪽 상단 배치, 항상 보임)
///     │
///     ├── ScoreText          ← TextMeshProUGUI, 초기 텍스트: 비어있음
///     │                         (TotalScoreLabel 아래 배치)
///     │
///     ├── GradeImage         ← Image (등급 스프라이트 할당), Scale = 1
///     │                         (우측 상단 배치)
///     │
///     └── ButtonGroup        ← 빈 GameObject, CanvasGroup 컴포넌트 부착
///         │  (우측 하단 배치)
///         ├── RestartButton  ← Button + TextMeshProUGUI "Restart"
///         │                     OnClick → GameOverPanel.OnRestartClick()
///         └── MainButton     ← Button + TextMeshProUGUI "Main"
///                               OnClick → GameOverPanel.OnMainClick()
///
/// ─────────────── 이 스크립트 인스펙터 설정 ───────────────
/// · panel         : GameOverPanel 의 RectTransform
/// · scoreText     : ScoreText 의 TextMeshProUGUI
/// · gradeTransform: GradeImage 의 RectTransform
/// · buttonGroup   : ButtonGroup 의 CanvasGroup
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    [Header("패널 (Anchor: Stretch All / Pivot: 0.5,0.5 / LRTB=0)")]
    [Tooltip("GameOverPanel 의 RectTransform")]
    public RectTransform panel;

    [Tooltip("슬라이드 속도 (px/초)")]
    public float slideSpeed = 1800f;

    // ── 점수 ─────────────────────────────────────────────
    [Header("점수 텍스트")]
    [Tooltip("점수를 표시할 TextMeshProUGUI (초기 텍스트는 비어두세요)")]
    public TextMeshProUGUI scoreText;

    [Tooltip("한 글자 표시 간격 (초)")]
    public float typeInterval = 0.12f;

    // ── 등급 이미지 ──────────────────────────────────────
    [Header("등급 이미지")]
    [Tooltip("등급 이미지의 RectTransform (Source Image에 스프라이트 할당)")]
    public RectTransform gradeTransform;

    [Tooltip("애니메이션 시작 스케일 (이 크기에서 1로 축소됩니다)")]
    public float gradeStartScale = 1.5f;

    [Tooltip("스케일 애니메이션 지속 시간 (초)")]
    public float gradeScaleDuration = 0.35f;

    // ── 버튼 ─────────────────────────────────────────────
    [Header("버튼 그룹")]
    [Tooltip("ButtonGroup 오브젝트에 부착된 CanvasGroup")]
    public CanvasGroup buttonGroup;

    [Tooltip("버튼 페이드인 지속 시간 (초)")]
    public float buttonFadeDuration = 0.3f;

    // ── 씬 이름 ──────────────────────────────────────────
    [Header("씬 이름")]
    public string stage1SceneName = "Stage1Scene";
    public string mainSceneName   = "StartScene";

    // ── 내부 ─────────────────────────────────────────────
    private float visibleY;
    private float hiddenY;

    // ─────────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────────

    void Awake()
    {
        // 레이아웃 확정 전 화면 밖으로
        if (panel != null)
            SetY(9999f);

        // 등급 이미지: 슬라이드 중 안 보이도록 비활성
        if (gradeTransform != null)
            gradeTransform.gameObject.SetActive(false);

        // 버튼: 투명 + 조작 불가
        if (buttonGroup != null)
        {
            buttonGroup.alpha          = 0f;
            buttonGroup.interactable   = false;
            buttonGroup.blocksRaycasts = false;
        }

        // 점수 텍스트 비우기
        if (scoreText != null)
            scoreText.text = "";
    }

    void Start()
    {
        StartCoroutine(InitAndPlay());
    }

    // ─────────────────────────────────────────────────────
    // 전체 연출 흐름
    // ─────────────────────────────────────────────────────

    private IEnumerator InitAndPlay()
    {
        // 2프레임 대기 → Canvas 레이아웃 완전 확정 후 높이 읽기
        yield return null;
        yield return null;

        float canvasHeight = GetCanvasHeight();
        visibleY = 0f;
        hiddenY  = canvasHeight;
        SetY(hiddenY);

        // ① 패널 슬라이드 인 (위→아래)
        yield return StartCoroutine(SlideTo(visibleY));
        yield return new WaitForSeconds(0.3f);

        // ② 점수 한 글자씩 타이핑
        yield return StartCoroutine(TypeScore("-999999"));
        yield return new WaitForSeconds(0.2f);

        // ③ 등급 이미지: 크게 등장 후 축소 고정
        if (gradeTransform != null)
            yield return StartCoroutine(ScaleGrade());
        yield return new WaitForSeconds(0.15f);

        // ④ 버튼 페이드인
        if (buttonGroup != null)
            yield return StartCoroutine(FadeButtons());
    }

    // ─────────────────────────────────────────────────────
    // 연출 코루틴
    // ─────────────────────────────────────────────────────

    /// <summary>점수 문자열을 한 글자씩 표시합니다.</summary>
    private IEnumerator TypeScore(string target)
    {
        if (scoreText == null) yield break;

        for (int i = 1; i <= target.Length; i++)
        {
            scoreText.text = target.Substring(0, i);
            yield return new WaitForSeconds(typeInterval);
        }
    }

    /// <summary>등급 이미지가 gradeStartScale 크기에서 1로 축소되며 나타납니다.</summary>
    private IEnumerator ScaleGrade()
    {
        gradeTransform.gameObject.SetActive(true);
        gradeTransform.localScale = Vector3.one * gradeStartScale;

        float elapsed = 0f;
        while (elapsed < gradeScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / gradeScaleDuration);
            // EaseOut Cubic: 빠르게 시작 → 부드럽게 착지
            float s = Mathf.Lerp(gradeStartScale, 1f, 1f - Mathf.Pow(1f - t, 3f));
            gradeTransform.localScale = Vector3.one * s;
            yield return null;
        }

        gradeTransform.localScale = Vector3.one;
    }

    /// <summary>버튼 그룹을 서서히 나타냅니다.</summary>
    private IEnumerator FadeButtons()
    {
        float elapsed = 0f;
        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.deltaTime;
            buttonGroup.alpha = Mathf.Clamp01(elapsed / buttonFadeDuration);
            yield return null;
        }
        buttonGroup.alpha          = 1f;
        buttonGroup.interactable   = true;
        buttonGroup.blocksRaycasts = true;
    }

    /// <summary>패널을 targetY 까지 슬라이드합니다.</summary>
    private IEnumerator SlideTo(float targetY)
    {
        while (Mathf.Abs(GetY() - targetY) > 1f)
        {
            float next = Mathf.MoveTowards(GetY(), targetY, slideSpeed * Time.deltaTime);
            SetY(next);
            yield return null;
        }
        SetY(targetY);
    }

    // ─────────────────────────────────────────────────────
    // 버튼 콜백 (Button OnClick 에 연결하세요)
    // ─────────────────────────────────────────────────────

    /// <summary>Restart 버튼: GameData 초기화 후 Stage1Scene 으로 이동</summary>
    public void OnRestartClick()
    {
        GameData.ResetAll(3);
        SceneManager.LoadScene(stage1SceneName);
    }

    /// <summary>Main 버튼: StartScene(타이틀) 으로 이동</summary>
    public void OnMainClick()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    // ─────────────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────────────

    private float GetCanvasHeight()
    {
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            float h = canvas.GetComponent<RectTransform>().rect.height;
            if (h > 10f) return h;
        }
        float ph = panel.rect.height;
        return ph > 10f ? ph : 1080f;
    }

    private void SetY(float y)
    {
        Vector2 pos = panel.anchoredPosition;
        pos.y = y;
        panel.anchoredPosition = pos;
    }

    private float GetY() => panel.anchoredPosition.y;
}
