using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 클리어 패널 스크립트 (3라운드 종료 후 표시)
/// - 씬 로드 시 위에서 자동으로 슬라이드 인
/// - Total Score 레이블 + GameData.CurrentScore 한 글자씩 타이핑
/// - 점수 완료 후 점수 커트라인에 맞는 등급 이미지가 축소되며 등장
/// - 이후 Restart / Main 버튼 페이드인
///
/// ─────────────── 등급 커트라인 (인스펙터에서 조정 가능) ───────────────
///  S : scoreForS 이상
///  A : scoreForA 이상
///  B : scoreForB 이상
///  C : scoreForC 이상
///  D : 그 미만
///
/// ─────────────── 유니티 계층 구조 예시 ───────────────
/// Canvas
/// └── GameClearPanel         ← 이 스크립트 부착, Image 컴포넌트
///     │  Anchor: Stretch All, Pivot (0.5, 0.5), LRTB = 0, Pos Y = 0
///     │
///     ├── TotalScoreLabel    ← TextMeshProUGUI, 텍스트: "Total Score"
///     │                         (왼쪽 상단 배치, 항상 보임)
///     │
///     ├── ScoreText          ← TextMeshProUGUI, 초기 텍스트: 비어있음
///     │                         (TotalScoreLabel 아래 배치)
///     │
///     ├── GradeImage         ← Image (스프라이트는 런타임에 할당), Scale = 1
///     │                         (우측 상단 배치)
///     │
///     └── ButtonGroup        ← 빈 GameObject, CanvasGroup 컴포넌트 부착
///         │  (우측 하단 배치)
///         ├── RestartButton  ← Button + TextMeshProUGUI "Restart"
///         │                     OnClick → GameClearPanel.OnRestartClick()
///         └── MainButton     ← Button + TextMeshProUGUI "Main"
///                               OnClick → GameClearPanel.OnMainClick()
///
/// ─────────────── 이 스크립트 인스펙터 설정 ───────────────
/// · panel          : GameClearPanel 의 RectTransform
/// · scoreText      : ScoreText 의 TextMeshProUGUI
/// · gradeImage     : GradeImage 의 Image 컴포넌트
/// · gradeSprites   : [0]=D, [1]=C, [2]=B, [3]=A, [4]=S 순서로 스프라이트 할당
/// · buttonGroup    : ButtonGroup 의 CanvasGroup
/// · stageShutter   : 씬의 StageShutter 컴포넌트
/// </summary>
public class GameClearPanel : MonoBehaviour
{
    [Header("패널 (Anchor: Stretch All / Pivot: 0.5,0.5 / LRTB=0)")]
    public RectTransform panel;

    [Tooltip("슬라이드 속도 (px/초)")]
    public float slideSpeed = 1800f;

    // ── 점수 ─────────────────────────────────────────────
    [Header("점수 텍스트")]
    [Tooltip("점수를 표시할 TextMeshProUGUI (초기 텍스트는 비워두세요)")]
    public TextMeshProUGUI scoreText;

    [Tooltip("한 글자 표시 간격 (초)")]
    public float typeInterval = 0.08f;

    // ── 등급 ─────────────────────────────────────────────
    [Header("등급 이미지")]
    [Tooltip("GradeImage 의 Image 컴포넌트")]
    public Image gradeImage;

    [Tooltip("등급 스프라이트 배열\n[0]=D  [1]=C  [2]=B  [3]=A  [4]=S")]
    public Sprite[] gradeSprites = new Sprite[5];

    [Tooltip("애니메이션 시작 스케일 (이 크기에서 1로 축소됩니다)")]
    public float gradeStartScale = 1.5f;

    [Tooltip("스케일 애니메이션 지속 시간 (초)")]
    public float gradeScaleDuration = 0.35f;

    // ── 등급 커트라인 ────────────────────────────────────
    [Header("★ 등급 커트라인 (인스펙터에서 직접 수정하세요)")]
    [Tooltip("이 점수 이상이면 S 등급")]
    public int scoreForS = 8000;

    [Tooltip("이 점수 이상이면 A 등급")]
    public int scoreForA = 5000;

    [Tooltip("이 점수 이상이면 B 등급")]
    public int scoreForB = 2500;

    [Tooltip("이 점수 이상이면 C 등급  /  미만이면 D 등급")]
    public int scoreForC = 1000;

    // ── 버튼 ─────────────────────────────────────────────
    [Header("버튼 그룹")]
    [Tooltip("ButtonGroup 오브젝트에 부착된 CanvasGroup")]
    public CanvasGroup buttonGroup;

    [Tooltip("버튼 페이드인 지속 시간 (초)")]
    public float buttonFadeDuration = 0.3f;

    // ── 셔터 ─────────────────────────────────────────────
    [Header("셔터 (뒤에 있는 StageShutter 연결)")]
    [Tooltip("GameClearScene 의 StageShutter 컴포넌트를 할당하세요")]
    public StageShutter stageShutter;

    // ── 씬 이름 ──────────────────────────────────────────
    [Header("씬 이름")]
    public string stage1SceneName = "Stage1Scene";
    public string mainSceneName   = "StartScene";

    // ── 내부 ─────────────────────────────────────────────
    private float visibleY;
    private float hiddenY;
    private bool isTransitioning = false;

    // ─────────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────────

    void Awake()
    {
        if (panel != null)
            SetY(9999f);

        if (gradeImage != null)
            gradeImage.gameObject.SetActive(false);

        if (buttonGroup != null)
        {
            buttonGroup.alpha          = 0f;
            buttonGroup.interactable   = false;
            buttonGroup.blocksRaycasts = false;
        }

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
        // 2프레임 대기 → Canvas 레이아웃 완전 확정
        yield return null;
        yield return null;

        float canvasHeight = GetCanvasHeight();
        visibleY = 0f;
        hiddenY  = canvasHeight;
        SetY(hiddenY);

        // ① 뒤 셔터 텍스트를 GAME CLEAR 로 설정
        if (stageShutter != null)
            stageShutter.SetText("GAME CLEAR");

        // ② 패널 슬라이드 인
        yield return StartCoroutine(SlideTo(visibleY));
        yield return new WaitForSeconds(0.3f);

        // ③ GameData 에서 최종 점수 가져와 타이핑
        int finalScore = GameData.CurrentScore;
        yield return StartCoroutine(TypeScore(finalScore.ToString()));
        yield return new WaitForSeconds(0.2f);

        // ④ 점수에 맞는 등급 이미지 결정 → 축소 등장
        Sprite gradeSprite = GetGradeSprite(finalScore);
        if (gradeImage != null && gradeSprite != null)
        {
            gradeImage.sprite = gradeSprite;
            yield return StartCoroutine(ScaleGrade(gradeImage.rectTransform));
        }
        yield return new WaitForSeconds(0.15f);

        // ⑤ 버튼 페이드인
        if (buttonGroup != null)
            yield return StartCoroutine(FadeButtons());
    }

    // ─────────────────────────────────────────────────────
    // 연출 코루틴
    // ─────────────────────────────────────────────────────

    private IEnumerator TypeScore(string target)
    {
        if (scoreText == null) yield break;

        for (int i = 1; i <= target.Length; i++)
        {
            scoreText.text = target.Substring(0, i);
            yield return new WaitForSeconds(typeInterval);
        }
    }

    private IEnumerator ScaleGrade(RectTransform rt)
    {
        rt.gameObject.SetActive(true);
        rt.localScale = Vector3.one * gradeStartScale;

        float elapsed = 0f;
        while (elapsed < gradeScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / gradeScaleDuration);
            float s = Mathf.Lerp(gradeStartScale, 1f, 1f - Mathf.Pow(1f - t, 3f)); // EaseOut Cubic
            rt.localScale = Vector3.one * s;
            yield return null;
        }

        rt.localScale = Vector3.one;
    }

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
    // 등급 계산
    // ─────────────────────────────────────────────────────

    /// <summary>점수에 맞는 등급 스프라이트를 반환합니다.</summary>
    private Sprite GetGradeSprite(int score)
    {
        // gradeSprites: [0]=D [1]=C [2]=B [3]=A [4]=S
        int index;
        if      (score >= scoreForS) index = 4; // S
        else if (score >= scoreForA) index = 3; // A
        else if (score >= scoreForB) index = 2; // B
        else if (score >= scoreForC) index = 1; // C
        else                         index = 0; // D

        if (gradeSprites == null || index >= gradeSprites.Length)
            return null;
        return gradeSprites[index];
    }

    // ─────────────────────────────────────────────────────
    // 버튼 콜백
    // ─────────────────────────────────────────────────────

    /// <summary>Restart: 셔터 텍스트 → ROUND 1, 패널 슬라이드 아웃 → Stage1Scene</summary>
    public void OnRestartClick()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        if (buttonGroup != null) { buttonGroup.interactable = false; buttonGroup.blocksRaycasts = false; }
        StartCoroutine(LeaveSequence("ROUND 1", () =>
        {
            GameData.ResetAll(3);
            SceneManager.LoadScene(stage1SceneName);
        }));
    }

    /// <summary>Main: 셔터 텍스트 → Welcome, 패널 슬라이드 아웃 → StartScene</summary>
    public void OnMainClick()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        if (buttonGroup != null) { buttonGroup.interactable = false; buttonGroup.blocksRaycasts = false; }
        StartCoroutine(LeaveSequence("WELCOME", () =>
        {
            SceneManager.LoadScene(mainSceneName);
        }));
    }

    private IEnumerator LeaveSequence(string shutterLabel, System.Action loadScene)
    {
        if (stageShutter != null)
            stageShutter.SetText(shutterLabel);

        yield return StartCoroutine(SlideTo(hiddenY));

        loadScene?.Invoke();
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
