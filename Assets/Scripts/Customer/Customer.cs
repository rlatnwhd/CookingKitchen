using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 개별 손님 오브젝트를 제어하는 스크립트
///
/// [프리팹 구성 — 최소 설정]
/// - 루트 GameObject에 SpriteRenderer 부착 → spriteRenderer 필드에 할당
/// - satisfiedSprite / unsatisfiedSprite 스프라이트만 할당
/// - 말풍선·음식·리액션 자식 오브젝트는 Awake에서 자동 생성됩니다
///   (별도 자식 오브젝트 추가 불필요!)
///
/// [Z 위치 설정]
/// - CustomerManager 슬롯 Transform의 Z값으로 배경 레이어 사이에 배치
/// </summary>
public class Customer : MonoBehaviour
{
    [Header("손님 스프라이트")]
    [Tooltip("루트 오브젝트에 있는 SpriteRenderer (손님 이미지)")]
    public SpriteRenderer spriteRenderer;

    [Header("말풍선 설정")]
    [Tooltip("말풍선 배경 스프라이트 (비워두면 배경 없이 음식 아이콘만 표시)")]
    public Sprite bubbleBackgroundSprite;

    [Tooltip("손님 기준 말풍선 최종 로컬 오프셋 (위쪽 = Y 증가)")]
    public Vector3 bubbleOffset = new Vector3(0f, 1.5f, -0.1f);

    [Tooltip("말풍선 전체 크기 배율")]
    public float bubbleScale = 1f;

    [Tooltip("말풍선 안 음식 아이콘 크기 배율 (말풍선 기준 상대값)")]
    public float foodIconScale = 0.6f;

    [Tooltip("말풍선 팝업 애니메이션 시간 (초)")]
    public float bubblePopDuration = 0.3f;

    [Tooltip("말풍선이 자동으로 사라질 때까지 대기 시간 (초, 0 = 자동 숨김 없음)")]
    public float bubbleAutoHideDuration = 3f;

    [Header("리액션 아이콘")]
    [Tooltip("레시피 완성 시 표시할 만족(♥) 스프라이트")]
    public Sprite satisfiedSprite;

    [Tooltip("시간 초과 시 표시할 불만(✕) 스프라이트")]
    public Sprite unsatisfiedSprite;

    [Tooltip("손님 기준 리액션 아이콘 로컬 오프셋")]
    public Vector3 reactionOffset = new Vector3(0f, 2f, -0.1f);

    [Tooltip("리액션 아이콘 크기 배율")]
    public float reactionIconScale = 0.6f;

    [Header("소팅 설정")]
    [Tooltip("자동 생성 자식 오브젝트에 사용할 소팅 레이어 이름")]
    public string sortingLayerName = "Default";

    [Tooltip("말풍선 소팅 오더 (손님 이미지보다 높게 설정)")]
    public int bubbleSortingOrder = 5;

    [Header("등장/퇴장 애니메이션")]
    [Tooltip("등장 애니메이션 시간 (초)")]
    public float appearDuration = 0.5f;

    [Tooltip("퇴장 애니메이션 시간 (초)")]
    public float disappearDuration = 0.4f;

    [Tooltip("시작 위치 Y 오프셋 (슬롯 아래에서 이만큼 올라옴, 양수 입력)")]
    public float hiddenYOffset = 2f;

    [Header("리액션 플로팅")]
    [Tooltip("리액션 아이콘이 위로 떠오르는 거리 (월드 단위)")]
    public float reactionFloatDistance = 1f;

    [Tooltip("리액션 아이콘 페이드아웃 시간 (초)")]
    public float reactionFadeDuration = 1f;

    // ── 외부 접근 프로퍼티 ──
    public RecipeUI LinkedCard { get; private set; }
    public int SlotIndex { get; private set; }

    /// <summary>등장 완료 후 호출되는 콜백 (말풍선 표시 → 레시피 카드 스폰 연동)</summary>
    public event Action OnAppearComplete;

    // ── Awake에서 자동 생성되는 자식 오브젝트 ──
    private GameObject bubbleObj;         // 말풍선 배경 + 음식 아이콘 부모
    private SpriteRenderer bubbleRenderer;      // 말풍선 배경
    private SpriteRenderer bubbleFoodRenderer;  // 음식 아이콘
    private GameObject reactionObj;             // 리액션 아이콘
    private SpriteRenderer reactionRenderer;

    // ── 내부 상태 ──
    private Vector3 targetPosition;
    private Vector3 hiddenPosition;
    private Coroutine bubbleCoroutine;

    void Awake()
    {
        // ── 자식 오브젝트 자동 생성 ──
        // 루트 오브젝트(spriteRenderer가 있는 곳)는 절대 SetActive(false) 하지 않음
        // 대신 자식 GameObject를 별도로 생성해서 독립적으로 제어

        // [1] 말풍선 + 음식 아이콘
        bubbleObj = new GameObject("Bubble");
        bubbleObj.transform.SetParent(transform, false);
        bubbleObj.transform.localPosition = bubbleOffset;
        bubbleObj.transform.localScale    = Vector3.one * bubbleScale;

        // 루트 SpriteRenderer의 sortingLayer/Order를 기준으로 자식들의 order를 계산
        // → 손님 스프라이트가 어떤 레이어·오더에 있더라도 반드시 그 위에 렌더링됨
        string baseLayer = (spriteRenderer != null) ? spriteRenderer.sortingLayerName : sortingLayerName;
        int    baseOrder = (spriteRenderer != null) ? spriteRenderer.sortingOrder     : 0;

        bubbleRenderer = bubbleObj.AddComponent<SpriteRenderer>();
        bubbleRenderer.sprite           = bubbleBackgroundSprite;
        bubbleRenderer.sortingLayerName = baseLayer;
        bubbleRenderer.sortingOrder     = baseOrder + bubbleSortingOrder;      // 손님 + offset

        GameObject foodObj = new GameObject("FoodIcon");
        foodObj.transform.SetParent(bubbleObj.transform, false);
        foodObj.transform.localPosition = Vector3.zero;
        foodObj.transform.localScale    = Vector3.one * foodIconScale;

        bubbleFoodRenderer = foodObj.AddComponent<SpriteRenderer>();
        bubbleFoodRenderer.sortingLayerName = baseLayer;
        bubbleFoodRenderer.sortingOrder     = baseOrder + bubbleSortingOrder + 1;

        bubbleObj.SetActive(false); // 자식이므로 루트에 영향 없음

        // [2] 리액션 아이콘
        reactionObj = new GameObject("Reaction");
        reactionObj.transform.SetParent(transform, false);
        reactionObj.transform.localPosition = reactionOffset;
        reactionObj.transform.localScale    = Vector3.one * reactionIconScale;

        reactionRenderer = reactionObj.AddComponent<SpriteRenderer>();
        reactionRenderer.sortingLayerName = baseLayer;
        reactionRenderer.sortingOrder     = baseOrder + bubbleSortingOrder + 2;

        reactionObj.SetActive(false);
    }

    // ─────────────────── 공개 메서드 ───────────────────

    /// <summary>
    /// 손님을 초기화하고 등장 애니메이션을 시작합니다.
    /// CustomerManager에서 호출합니다.
    /// </summary>
    public void Initialize(int slotIndex, Vector3 worldPos, RecipeUI card,
                           Sprite customerSprite, Sprite foodSprite)
    {
        SlotIndex = slotIndex;
        LinkedCard = card;
        targetPosition = worldPos;
        hiddenPosition = new Vector3(worldPos.x, worldPos.y - hiddenYOffset, worldPos.z);

        if (spriteRenderer != null && customerSprite != null)
            spriteRenderer.sprite = customerSprite;

        // 음식 아이콘은 말풍선이 활성화될 때 보임
        if (bubbleFoodRenderer != null && foodSprite != null)
            bubbleFoodRenderer.sprite = foodSprite;

        transform.position = hiddenPosition;
        StartCoroutine(AppearCoroutine());
    }

    /// <summary>말풍선(음식 아이콘 포함)을 손님 뒤에서 뾰로롱 올라오는 애니메이션으로 표시합니다.</summary>
    public void ShowBubble()
    {
        if (bubbleObj == null) return;
        // 진행 중인 말풍선 코루틴 취소 후 재시작
        if (bubbleCoroutine != null) StopCoroutine(bubbleCoroutine);
        bubbleCoroutine = StartCoroutine(BubblePopUpCoroutine());
    }

    /// <summary>말풍선을 즉시 숨깁니다 (퇴장 시 호출).</summary>
    public void HideBubble()
    {
        if (bubbleCoroutine != null)
        {
            StopCoroutine(bubbleCoroutine);
            bubbleCoroutine = null;
        }
        if (bubbleObj != null) bubbleObj.SetActive(false);
    }

    /// <summary>만족 리액션 플로팅 후 퇴장합니다.</summary>
    public void DisappearSatisfied()
    {
        HideBubble();
        StartCoroutine(ReactionThenDisappear(satisfiedSprite));
    }

    /// <summary>불만 리액션 플로팅 후 퇴장합니다.</summary>
    public void DisappearUnsatisfied()
    {
        HideBubble();
        StartCoroutine(ReactionThenDisappear(unsatisfiedSprite));
    }

    /// <summary>리액션 없이 즉시 퇴장합니다. (라운드 초기화 등)</summary>
    public void DisappearImmediate()
    {
        HideBubble();
        StartCoroutine(DisappearCoroutine());
    }

    // ─────────────────── 내부 코루틴 ───────────────────

    /// <summary>
    /// 말풍선이 손님 뒤(Y=0)에서 위로 뾰로롱 올라오는 팝업 애니메이션.
    /// 완료 후 bubbleAutoHideDuration초 뒤 자동으로 사라집니다.
    /// </summary>
    private IEnumerator BubblePopUpCoroutine()
    {
        // 손님 뒤(Y=0) 위치에서 시작 → bubbleOffset까지 상승
        Vector3 startPos   = new Vector3(bubbleOffset.x, 0f, bubbleOffset.z);
        Vector3 targetPos  = bubbleOffset;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one * bubbleScale;

        bubbleObj.transform.localPosition = startPos;
        bubbleObj.transform.localScale    = startScale;
        bubbleObj.SetActive(true);

        float elapsed = 0f;
        while (elapsed < bubblePopDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / bubblePopDuration);
            float smooth = 1f - Mathf.Pow(1f - t, 2f); // EaseOut Quad
            bubbleObj.transform.localPosition = Vector3.Lerp(startPos, targetPos, smooth);
            bubbleObj.transform.localScale    = Vector3.Lerp(startScale, targetScale, smooth);
            yield return null;
        }
        bubbleObj.transform.localPosition = targetPos;
        bubbleObj.transform.localScale    = targetScale;

        // 자동 숨김 (0이면 유지)
        if (bubbleAutoHideDuration > 0f)
        {
            yield return new WaitForSeconds(bubbleAutoHideDuration);
            bubbleObj.SetActive(false);
        }
        bubbleCoroutine = null;
    }

    private IEnumerator AppearCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / appearDuration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f); // EaseOut Cubic
            transform.position = Vector3.Lerp(hiddenPosition, targetPosition, smooth);
            yield return null;
        }
        transform.position = targetPosition;

        // 등장 완료 → CustomerManager가 구독해 말풍선 표시 + 딜레이 + 레시피 카드 스폰
        OnAppearComplete?.Invoke();
    }

    private IEnumerator ReactionThenDisappear(Sprite reactionSprite)
    {
        if (reactionObj != null && reactionSprite != null)
        {
            reactionRenderer.sprite = reactionSprite;
            reactionRenderer.color = Color.white;
            reactionObj.SetActive(true);

            Vector3 startLocal = reactionObj.transform.localPosition;
            Vector3 endLocal   = startLocal + Vector3.up * reactionFloatDistance;

            float elapsed = 0f;
            while (elapsed < reactionFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / reactionFadeDuration);
                reactionObj.transform.localPosition = Vector3.Lerp(startLocal, endLocal, t);
                reactionRenderer.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }

            reactionObj.SetActive(false);
            reactionObj.transform.localPosition = startLocal;
        }

        yield return StartCoroutine(DisappearCoroutine());
    }

    private IEnumerator DisappearCoroutine()
    {
        Vector3 startPos = transform.position;
        float   elapsed  = 0f;
        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / disappearDuration);
            float smooth = t * t; // EaseIn Quad
            transform.position = Vector3.Lerp(startPos, hiddenPosition, smooth);
            yield return null;
        }
        transform.position = hiddenPosition;
        Destroy(gameObject);
    }
}
