using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RecipeUI 카드에 부착되는 원형 타이머입니다.
///
/// [사용 방법]
/// - RecipeQueueManager가 카드 생성 시 자동으로 AddComponent합니다
/// - Awake에서 원형 타이머 Image를 자동 생성합니다
///   (Unity 인스펙터에서 별도 Image 설정 불필요!)
///
/// [자동 생성 구조]
///   RecipeUI (카드)
///    └ TimerBG    ← 어두운 배경 원 (Image)
///       └ TimerFill ← Radial360 Fill 타이머 (Image)
/// </summary>
public class RecipeTimer : MonoBehaviour
{
    [Header("색상")]
    [Tooltip("잔여 시간 많을 때 색상 (초록)")]
    public Color fullColor  = new Color(0.20f, 0.85f, 0.20f, 1f);

    [Tooltip("잔여 시간 절반일 때 색상 (노랑)")]
    public Color halfColor  = new Color(1.00f, 0.85f, 0.00f, 1f);

    [Tooltip("잔여 시간 없을 때 색상 (빨강)")]
    public Color emptyColor = new Color(0.90f, 0.15f, 0.15f, 1f);

    [Header("자동 생성 설정")]
    [Tooltip("타이머 원의 직경 (픽셀)")]
    public float timerSize = 50f;

    [Tooltip("카드 우측 상단 기준 앵커 오프셋 (왼쪽/아래 = 음수)")]
    public Vector2 timerOffset = new Vector2(-5f, -5f);

    /// <summary>자동 생성된 타이머 Image. 필요 시 외부에서 직접 할당도 가능합니다.</summary>
    public Image timerImage;

    /// <summary>타이머가 0에 도달하면 발생하는 이벤트.</summary>
    public event Action OnTimeExpired;

    // ── 내부 상태 ──
    private float totalTime;
    private float remainingTime;
    private bool  isRunning;

    void Awake()
    {
        if (timerImage == null)
            CreateTimerUI();
    }

    // ─────────────────── 공개 메서드 ───────────────────

    /// <summary>타이머를 시작합니다. duration이 0 이하이면 즉시 만료됩니다.</summary>
    public void StartTimer(float duration)
    {
        if (duration <= 0f)
        {
            OnTimeExpired?.Invoke();
            return;
        }

        totalTime     = duration;
        remainingTime = duration;
        isRunning     = true;
        UpdateVisual();
    }

    /// <summary>타이머를 정지합니다 (만료 이벤트 없이).</summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    // ─────────────────── Unity 루프 ───────────────────

    void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning     = false;
            UpdateVisual();
            OnTimeExpired?.Invoke();
            return;
        }

        UpdateVisual();
    }

    // ─────────────────── 내부 메서드 ───────────────────

    private void UpdateVisual()
    {
        if (timerImage == null) return;

        float ratio = totalTime > 0f ? Mathf.Clamp01(remainingTime / totalTime) : 0f;
        timerImage.fillAmount = ratio;

        // 초록 → 노랑 → 빨강
        timerImage.color = ratio > 0.5f
            ? Color.Lerp(halfColor, fullColor,  (ratio - 0.5f) / 0.5f)
            : Color.Lerp(emptyColor, halfColor, ratio / 0.5f);
    }

    /// <summary>
    /// 원형 타이머 UI를 코드로 자동 생성합니다.
    ///
    /// 구조: TimerBG(Image, 배경 원) > TimerFill(Image, Radial360 Fill)
    /// </summary>
    private void CreateTimerUI()
    {
        Sprite circleSprite = CreateCircleSprite(64);

        // ── 배경 원 (어두운 반투명) ──
        GameObject bgObj = new GameObject("TimerBG");
        bgObj.transform.SetParent(transform, false);

        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.sizeDelta       = new Vector2(timerSize, timerSize);
        bgRt.anchorMin       = new Vector2(1f, 1f);
        bgRt.anchorMax       = new Vector2(1f, 1f);
        bgRt.pivot           = new Vector2(1f, 1f);
        bgRt.anchoredPosition = timerOffset;

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite       = circleSprite;
        bgImg.color        = new Color(0.08f, 0.08f, 0.08f, 0.65f);
        bgImg.type         = Image.Type.Simple;
        bgImg.raycastTarget = false;

        // ── 타이머 필 원 (Radial360) ──
        GameObject fillObj = new GameObject("TimerFill");
        fillObj.transform.SetParent(bgObj.transform, false);

        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin  = Vector2.zero;
        fillRt.anchorMax  = Vector2.one;
        fillRt.offsetMin  = Vector2.zero;
        fillRt.offsetMax  = Vector2.zero;

        timerImage             = fillObj.AddComponent<Image>();
        timerImage.sprite      = circleSprite;
        timerImage.type        = Image.Type.Filled;
        timerImage.fillMethod  = Image.FillMethod.Radial360;
        timerImage.fillClockwise = true;
        timerImage.fillOrigin  = (int)Image.Origin360.Top; // 12시 방향에서 시작
        timerImage.fillAmount  = 1f;
        timerImage.color       = fullColor;
        timerImage.raycastTarget = false;
    }

    /// <summary>
    /// 부드러운 안티앨리어싱이 적용된 원형 스프라이트를 절차적으로 생성합니다.
    /// (지름 = radius * 2 픽셀 Texture2D)
    /// </summary>
    private Sprite CreateCircleSprite(int radius)
    {
        int    size   = radius * 2;
        float  cx     = radius - 0.5f;
        float  cy     = radius - 0.5f;

        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode   = FilterMode.Bilinear;
        tex.wrapMode     = TextureWrapMode.Clamp;
        Color[] pixels   = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist  = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float alpha = Mathf.Clamp01(radius - dist); // 경계에서 0~1 앤티앨리어싱
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // pixelsPerUnit = size 로 설정해 RectTransform.sizeDelta와 1:1 매핑
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
