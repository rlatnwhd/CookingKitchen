using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레시피 큐를 관리하는 스크립트
/// 
/// [동작 흐름]
/// 1. 5초마다 현재 라운드의 레시피 중 무작위로 하나를 선택
/// 2. RecipeUI 프리팹을 생성하여 왼쪽에서 슬라이드 인 애니메이션으로 등장
/// 3. 최대 4개까지 상단부터 세로로 쌓임
/// 4. 가장 위(첫 번째) 레시피만 조합 가능
/// 5. 재료가 모두 모이면 자동으로 완성 → 왼쪽으로 슬라이드 아웃 → 아래 패널들이 위로 이동
/// 6. 재료 획득마다 레시피 슬롯 색상 갱신
/// 
/// [cardParent RectTransform 설정 필수]
/// - Anchor Min/Max: (0, 1) — 좌상단 고정
/// - Pivot: (0, 1) — 좌상단 기준
/// - Pos: 화면 좌측 상단 원하는 위치
/// </summary>
public class RecipeQueueManager : MonoBehaviour
{
    [Header("프리팹")]
    [Tooltip("RecipeUI 컴포넌트가 붙어있는 레시피 카드 프리팹")]
    public GameObject recipeCardPrefab;

    [Header("배치 설정")]
    [Tooltip("레시피 카드들이 배치될 부모 RectTransform (Anchor/Pivot = 좌상단)")]
    public RectTransform cardParent;

    [Tooltip("최대 동시 레시피 수")]
    public int maxRecipeCount = 4;

    [Tooltip("새 레시피 등장 간격 (초)")]
    public float spawnInterval = 5f;

    [Header("슬라이드 애니메이션")]
    [Tooltip("카드가 등장할 때 시작하는 X 위치 (화면 왼쪽 밖, 음수)")]
    public float slideStartX = -400f;

    [Tooltip("카드가 등장한 후 최종 X 위치 (보통 0)")]
    public float slideEndX = 0f;

    [Tooltip("슬라이드 애니메이션 시간 (초)")]
    public float slideDuration = 0.35f;

    [Header("카드 간격")]
    [Tooltip("카드 사이 Y 간격 (픽셀)")]
    public float cardSpacing = 10f;

    [Header("라운드 설정")]
    [Tooltip("현재 라운드 (1~3)")]
    public int currentRound = 1;

    [Header("자동 시작")]
    [Tooltip("게임 시작 시 자동으로 레시피 생성을 시작합니다")]
    public bool autoStart = true;

    [Header("완성 쿨다운")]
    [Tooltip("레시피 완성 후 다음 완성 판정까지 대기 시간 (초). 레시피 큐가 한꺼번에 사라지는 버그 방지")]
    public float completionCooldown = 1f;

    [Header("손님 연동")]
    [Tooltip("CustomerManager를 할당하면 레시피 등장/완성 시 손님이 함께 등장/퇴장합니다")]
    public CustomerManager customerManager;

    // 현재 화면에 표시 중인 레시피 카드 목록 (인덱스 0 = 상단)
    private List<RecipeUI> activeCards = new List<RecipeUI>();

    // 카드 높이 (프리팹에서 자동 계산)
    private float cardHeight;

    // 카드 너비 (프리팹에서 자동 계산)
    private float cardWidth;

    // 슬라이드 아웃/ShiftUp 애니메이션 진행 중 여부
    // true 동안은 새 카드 생성 및 조합 체크를 잠금
    private bool isAnimating = false;

    // 완성 쿨다운 진행 중 여부 (true 동안 완성 판정 차단)
    private bool isInCooldown = false;

    // 스폰 코루틴 참조
    private Coroutine spawnCoroutine;

    void Start()
    {
        // 카드 크기를 프리팹에서 가져옴 (sizeDelta가 0이면 실제 rect로 시도)
        if (recipeCardPrefab != null)
        {
            RectTransform rt = recipeCardPrefab.GetComponent<RectTransform>();
            if (rt != null)
            {
                cardHeight = rt.sizeDelta.y > 0 ? rt.sizeDelta.y : rt.rect.height;
                cardWidth  = rt.sizeDelta.x > 0 ? rt.sizeDelta.x : rt.rect.width;
            }
        }

        // PlayerInventory 이벤트 구독
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnIngredientChanged += OnIngredientChanged;

        if (autoStart)
            StartSpawning();
    }

    void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnIngredientChanged -= OnIngredientChanged;
    }

    // ──────────────────────── 스폰 제어 ────────────────────────

    public void StartSpawning()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        // 첫 번째 레시피는 즉시 생성
        TrySpawnRecipe();

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawnRecipe();
        }
    }

    private void TrySpawnRecipe()
    {
        // 애니메이션 중이거나 최대 개수면 생성 안 함
        if (isAnimating) return;
        if (activeCards.Count >= maxRecipeCount) return;

        SpawnRandomRecipe();
    }

    // ──────────────────────── 카드 생성 ────────────────────────

    private void SpawnRandomRecipe()
    {
        if (RecipeDatabase.Instance == null) return;

        var recipes = RecipeDatabase.Instance.GetRecipesForRound(currentRound);
        if (recipes.Count == 0) return;

        RecipeData selected = recipes[Random.Range(0, recipes.Count)];

        GameObject cardObj = Instantiate(recipeCardPrefab, cardParent);
        RecipeUI card = cardObj.GetComponent<RecipeUI>();
        if (card == null) { Destroy(cardObj); return; }

        // ── 카드 RectTransform을 상단 좌측 기준으로 강제 설정 ──
        // cardParent가 상단 좌측(pivot 0,1) 기준이어야 올바르게 쌓임
        RectTransform rt = card.RectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);

        // 프리팹 읽기 실패로 cardHeight가 0이면 생성된 카드에서 직접 읽음
        if (cardHeight <= 0)
        {
            cardHeight = rt.sizeDelta.y > 0 ? rt.sizeDelta.y : rt.rect.height;
            cardWidth  = rt.sizeDelta.x > 0 ? rt.sizeDelta.x : rt.rect.width;
        }
        if (cardHeight > 0 && cardWidth > 0)
            rt.sizeDelta = new Vector2(cardWidth, cardHeight);

        card.SetRecipe(selected);
        activeCards.Add(card);

        // 이 카드의 목표 Y: 상단부터 차례로 아래로
        float targetY = CalculateCardY(activeCards.Count - 1);
        StartCoroutine(SlideIn(rt, targetY));

        RefreshCardSlots(card);

        // 손님 등장 (CustomerManager가 할당된 경우)
        customerManager?.OnRecipeSpawned(card);

        // 스폰 후 조합 체크는 슬라이드 완료 후에 실행 (SlideIn 안에서 호출)
    }

    // ──────────────────────── 위치 계산 ────────────────────────

    /// <summary>
    /// 인덱스 0 = Y 0 (최상단), 인덱스 1 = Y -(cardHeight+spacing), ...
    /// cardParent의 pivot이 상단(y=1)이어야 정확함
    /// </summary>
    private float CalculateCardY(int index)
    {
        return -(index * (cardHeight + cardSpacing));
    }

    // ──────────────────────── 애니메이션 ────────────────────────

    private IEnumerator SlideIn(RectTransform rt, float targetY)
    {
        Vector2 start = new Vector2(slideStartX, targetY);
        Vector2 end   = new Vector2(slideEndX,   targetY);
        rt.anchoredPosition = start;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f); // EaseOut Cubic
            rt.anchoredPosition = Vector2.Lerp(start, end, smooth);
            yield return null;
        }
        rt.anchoredPosition = end;

        // 슬라이드 완료 후 조합 체크
        TryCompleteTopRecipe();
    }

    private IEnumerator SlideOut(RectTransform rt)
    {
        Vector2 start = rt.anchoredPosition;
        Vector2 end   = new Vector2(slideStartX, start.y);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float smooth = t * t; // EaseIn Quad
            rt.anchoredPosition = Vector2.Lerp(start, end, smooth);
            yield return null;
        }
    }

    private IEnumerator ShiftCardsUp()
    {
        if (activeCards.Count == 0) yield break;

        // 이동 전 위치 snapshots
        Vector2[] starts  = new Vector2[activeCards.Count];
        Vector2[] targets = new Vector2[activeCards.Count];

        for (int i = 0; i < activeCards.Count; i++)
        {
            starts[i]  = activeCards[i].RectTransform.anchoredPosition;
            targets[i] = new Vector2(slideEndX, CalculateCardY(i));
        }

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / slideDuration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f);

            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i] != null)
                    activeCards[i].RectTransform.anchoredPosition =
                        Vector2.Lerp(starts[i], targets[i], smooth);
            }
            yield return null;
        }

        // 최종 위치 확정
        for (int i = 0; i < activeCards.Count; i++)
        {
            if (activeCards[i] != null)
                activeCards[i].RectTransform.anchoredPosition = targets[i];
        }
    }

    // ──────────────────────── 조합 완성 ────────────────────────

    private void TryCompleteTopRecipe()
    {
        if (isAnimating) return;
        if (isInCooldown) return;
        if (activeCards.Count == 0) return;

        RecipeUI topCard = activeCards[0];
        if (topCard == null) return;

        if (topCard.CanComplete())
        {
            // ★ ConsumeIngredients가 OnIngredientChanged를 즉시 발생시키므로
            //   isAnimating을 먼저 true로 설정해 무한 재귀를 방지합니다.
            isAnimating = true;
            isInCooldown = true;
            activeCards.RemoveAt(0);      // 목록에서 먼저 제거 (재진입 시 Count=0으로 차단)
            topCard.ConsumeIngredients(); // 이벤트 발생 → 재진입하더라도 위에서 가로막힘
            StartCoroutine(CompleteSequence(topCard));
        }
    }

    private IEnumerator CompleteSequence(RecipeUI completedCard)
    {
        // isAnimating, isInCooldown은 TryCompleteTopRecipe에서 이미 true로 설정됨

        // 손님 퇴장 시작 (레시피 슬라이드 아웃과 동시에 진행)
        customerManager?.OnRecipeCompleted(completedCard);

        // 1. 완성 카드 슬라이드 아웃
        yield return StartCoroutine(SlideOut(completedCard.RectTransform));
        Destroy(completedCard.gameObject);

        // 2. 남은 카드들을 위로 이동
        yield return StartCoroutine(ShiftCardsUp());

        isAnimating = false;

        // 3. 인벤토리 갱신
        foreach (var card in activeCards)
            if (card != null) RefreshCardSlots(card);

        // 4. 완성 쿨다운 대기 (레시피가 연속으로 사라지는 버그 방지)
        yield return new WaitForSeconds(completionCooldown);
        isInCooldown = false;

        // 5. 연쇄 완성 체크
        TryCompleteTopRecipe();
    }

    // ──────────────────────── 인벤토리 이벤트 ────────────────────────

    private void OnIngredientChanged(IngredientType type, int count)
    {
        foreach (var card in activeCards)
            if (card != null) RefreshCardSlots(card);

        TryCompleteTopRecipe();
    }

    private void RefreshCardSlots(RecipeUI card)
    {
        if (PlayerInventory.Instance == null) return;
        card.UpdateOwnedIngredients(PlayerInventory.Instance.GetOwnedTypes());
    }

    // ──────────────────────── 외부 제어 ────────────────────────

    /// <summary>
    /// 현재 활성 레시피 카드들이 필요로 하는 재료 목록을 반환합니다.
    /// 플레이어가 아직 보유하지 않은 재료만 포함합니다.
    /// IngredientSpawner에서 우선 스폰 재료를 결정할 때 사용합니다.
    /// </summary>
    public List<IngredientType> GetNeededIngredients()
    {
        List<IngredientType> needed = new List<IngredientType>();
        foreach (var card in activeCards)
        {
            if (card == null) continue;
            RecipeData recipe = card.GetCurrentRecipe();
            if (recipe == null) continue;
            foreach (var ingredient in recipe.ingredients)
            {
                // 플레이어가 아직 없는 재료만 추가
                if (PlayerInventory.Instance == null ||
                    !PlayerInventory.Instance.HasIngredient(ingredient))
                    needed.Add(ingredient);
            }
        }
        return needed;
    }

    /// <summary>
    /// 라운드를 변경합니다. 기존 카드를 모두 제거하고 초기화합니다.
    /// </summary>
    public void SetRound(int round)
    {
        currentRound = round;
        foreach (var card in activeCards)
            if (card != null) Destroy(card.gameObject);
        activeCards.Clear();
        isAnimating = false;
        isInCooldown = false;
        customerManager?.ClearAll();
    }
}
