using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 재료를 하늘에서 떨어뜨리는 스포너 스크립트
/// - 인스펙터에서 재료별 프리팹 할당
/// - 현재 라운드에 맞는 재료만 생성
/// - 공평한 가중치 기반 랜덤 스폰 (결핍 보정 시스템)
///   → 한 번도 안 떨어진 재료 우선, 덜 떨어진 재료에 높은 확률 부여
/// </summary>
public class IngredientSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("재료가 떨어지는 간격 (초)")]
    public float spawnInterval = 2f;

    [Tooltip("재료가 생성되는 X축 범위 (-범위 ~ +범위)")]
    public float spawnXRange = 5f;

    [Tooltip("재료가 생성되는 Y 위치 (화면 위쪽)")]
    public float spawnYPosition = 6f;

    [Header("자동 시작")]
    [Tooltip("체크하면 게임 시작 시 자동으로 재료를 떨어뜨리기 시작합니다")]
    public bool autoStartOnPlay = true;

    [Header("라운드 설정")]
    [Tooltip("현재 라운드 (1: 1페이지만, 2: 1+2페이지, 3: 전체)")]
    public int currentRound = 1;

    [Header("레시피 우선 스폰")]
    [Tooltip("레시피 큐를 관리하는 RecipeQueueManager를 할당하세요. 링크되면 큐의 레시피가 필요로 하는 재료를 우선 스폰합니다")]
    public RecipeQueueManager recipeQueueManager;

    [Tooltip("레시피가 필요로 하는 재료의 가중치 배율 (기본 가중치 대비 롼로우, 1이면 효과 없음)")]
    public float recipeIngredientsBoostMultiplier = 4f;

    [Header("공평성 설정")]
    [Tooltip("결핍 보정 강도 (높을수록 덜 나온 재료가 더 자주 나옴, 기본값: 2)")]
    public float deficitBoostMultiplier = 2f;

    [Header("상한 식재료 설정")]
    [Tooltip("상한 식재료 프리팹 목록 (2종류 모두 할당, 스폰 시 랜덤 선택)")]
    public GameObject[] spoiledPrefabs;

    [Tooltip("일반 재료 대신 상한 식재료가 스폰될 확률 (0.0~1.0, 기본값 0.15 = 15%)")]
    [Range(0f, 1f)]
    public float spoiledSpawnChance = 0.15f;

    [Header("재료 프리팹 할당")]
    [Tooltip("각 재료 타입에 대응하는 프리팹을 할당하세요 (18종류)")]
    public IngredientPrefabEntry[] ingredientPrefabs;

    // 현재 라운드의 재료별 기본 가중치 (레시피에서의 등장 빈도)
    private Dictionary<IngredientType, int> baseWeights = new Dictionary<IngredientType, int>();

    // 재료별 실제 스폰 횟수 추적
    private Dictionary<IngredientType, int> spawnCounts = new Dictionary<IngredientType, int>();

    // 총 스폰 횟수
    private int totalSpawnCount = 0;

    // 프리팹 빠른 조회용 딕셔너리
    private Dictionary<IngredientType, GameObject> prefabMap = new Dictionary<IngredientType, GameObject>();

    // 스폰 코루틴 참조
    private Coroutine spawnCoroutine;

    void Start()
    {
        // 인스펙터에서 할당한 프리팹을 딕셔너리로 변환
        BuildPrefabMap();

        // 현재 라운드에 맞게 가중치 초기화
        InitializeRound(currentRound);

        // autoStartOnPlay가 체크되어 있으면 게임 시작 시 자동으로 스폰 시작
        if (autoStartOnPlay)
        {
            StartSpawning();
        }
    }

    /// <summary>
    /// 인스펙터에서 할당한 프리팹 배열을 딕셔너리로 변환합니다.
    /// </summary>
    private void BuildPrefabMap()
    {
        prefabMap.Clear();
        foreach (var entry in ingredientPrefabs)
        {
            if (entry.prefab != null && !prefabMap.ContainsKey(entry.type))
            {
                prefabMap[entry.type] = entry.prefab;
            }
        }
    }

    /// <summary>
    /// 지정한 라운드로 초기화합니다. 기존 스폰 기록을 리셋하고 가중치를 재계산합니다.
    /// </summary>
    public void InitializeRound(int round)
    {
        currentRound = round;

        // RecipeDatabase에서 현재 라운드의 재료별 가중치를 가져옴
        baseWeights = RecipeDatabase.Instance.GetIngredientWeightsForRound(round);

        // 스폰 횟수 초기화
        spawnCounts.Clear();
        foreach (var ingredient in baseWeights.Keys)
        {
            spawnCounts[ingredient] = 0;
        }
        totalSpawnCount = 0;
    }

    /// <summary>
    /// 재료 스폰을 시작합니다. (외부에서 호출)
    /// </summary>
    public void StartSpawning()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// 재료 스폰을 중지합니다. (외부에서 호출)
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>
    /// 일정 간격으로 재료를 스폰하는 코루틴
    /// </summary>
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnIngredient();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// 지정한 재료 타입의 프리팹에서 스프라이트를 반환합니다.
    /// PlayerCollector의 BurstOwnedIngredients()에서 드랍 비주얼 생성 시 사용합니다.
    /// </summary>
    public Sprite GetIngredientSprite(IngredientType type)
    {
        if (prefabMap.ContainsKey(type))
        {
            SpriteRenderer sr = prefabMap[type].GetComponent<SpriteRenderer>();
            if (sr != null) return sr.sprite;
        }
        return null;
    }

    /// <summary>
    /// 공평한 가중치를 계산하여 하나의 재료를 하늘에서 생성합니다.
    /// 일정 확률로 상한 식재료가 대신 스폰됩니다.
    /// </summary>
    private void SpawnIngredient()
    {
        // 스폰 위치: X는 범위 내 랜덤, Y는 설정값
        float randomX = Random.Range(-spawnXRange, spawnXRange);
        Vector2 spawnPosition = new Vector2(randomX, spawnYPosition);

        // 일정 확률로 상한 식재료 스폰 (배열에서 랜덤 선택)
        if (spoiledPrefabs != null && spoiledPrefabs.Length > 0 && Random.value < spoiledSpawnChance)
        {
            GameObject pick = spoiledPrefabs[Random.Range(0, spoiledPrefabs.Length)];
            if (pick != null)
            {
                Instantiate(pick, spawnPosition, Quaternion.identity);
                return;
            }
        }

        // 공평성 보정을 적용하여 스폰할 재료 선택
        IngredientType selectedType = SelectIngredientFairly();

        // 프리팹 존재 확인
        if (!prefabMap.ContainsKey(selectedType))
        {
            Debug.LogWarning($"[IngredientSpawner] {selectedType} 프리팹이 할당되지 않았습니다!");
            return;
        }

        // 재료 오브젝트 생성
        GameObject spawned = Instantiate(prefabMap[selectedType], spawnPosition, Quaternion.identity);

        // IngredientItem 컴포넌트의 재료 타입을 스포너에서 설정
        IngredientItem item = spawned.GetComponent<IngredientItem>();
        if (item != null)
        {
            item.ingredientType = selectedType;
        }

        // 스폰 기록 갱신
        spawnCounts[selectedType]++;
        totalSpawnCount++;
    }

    /// <summary>
    /// 재료를 우선순위에 따라 선택합니다.
    ///
    /// [우선순위]
    /// 1순위: 활성 레시피 카드가 필요로 하는 재료 (플레이어 미보유) → 가중치 × recipeIngredientsBoostMultiplier
    /// 2순위: 라운드별 기본 가중치 + 결핍 보정 (덜 나온 재료 우선)
    /// 레시피 큐가 없거나 필요 재료 없으면 2순위만 적용
    /// </summary>
    private IngredientType SelectIngredientFairly()
    {
        // 전체 기본 가중치 합산
        int totalBaseWeight = 0;
        foreach (var weight in baseWeights.Values)
            totalBaseWeight += weight;

        // 1순위: 레시피가 필요로 하는 재료 (플레이어 미보유) 수집
        HashSet<IngredientType> neededByRecipe = new HashSet<IngredientType>();
        if (recipeQueueManager != null)
        {
            List<IngredientType> needed = recipeQueueManager.GetNeededIngredients();
            foreach (var t in needed)
                if (baseWeights.ContainsKey(t))
                    neededByRecipe.Add(t);
        }

        // 각 재료의 보정된 가중치 계산
        List<IngredientType> ingredients     = new List<IngredientType>();
        List<float>          adjustedWeights = new List<float>();

        foreach (var pair in baseWeights)
        {
            IngredientType type       = pair.Key;
            int            baseWeight = pair.Value;

            float adjustedWeight;

            if (totalSpawnCount == 0)
            {
                // 아직 한 번도 스폰되지 않았으면 기본 가중치 그대로 사용
                adjustedWeight = baseWeight;
            }
            else
            {
                // 2순위: 기대 비율에 따른 결핍 보정
                float expectedRatio = (float)baseWeight / totalBaseWeight;
                float expectedCount = expectedRatio * totalSpawnCount;
                float actualCount   = spawnCounts[type];
                float deficit       = expectedCount - actualCount;
                adjustedWeight = Mathf.Max(baseWeight + deficit * deficitBoostMultiplier, 0.1f);
            }

            // 1순위: 레시피 필요 재료는 가중치를 크게 증가
            if (neededByRecipe.Count > 0 && neededByRecipe.Contains(type))
                adjustedWeight *= recipeIngredientsBoostMultiplier;

            ingredients.Add(type);
            adjustedWeights.Add(adjustedWeight);
        }

        // 보정된 가중치로 랜덤 선택
        return WeightedRandomSelect(ingredients, adjustedWeights);
    }

    /// <summary>
    /// 가중치 목록을 기반으로 랜덤하게 하나의 재료를 선택합니다.
    /// 가중치가 높을수록 선택 확률이 높습니다.
    /// </summary>
    private IngredientType WeightedRandomSelect(List<IngredientType> items, List<float> weights)
    {
        // 전체 가중치 합산
        float totalWeight = 0f;
        foreach (var w in weights)
        {
            totalWeight += w;
        }

        // 0 ~ 합산 가중치 사이의 랜덤 값 생성
        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        // 누적 가중치가 랜덤 값을 초과하는 첫 번째 항목 선택
        for (int i = 0; i < items.Count; i++)
        {
            cumulative += weights[i];
            if (random <= cumulative)
            {
                return items[i];
            }
        }

        // 안전 장치: 부동소수점 오차 대비 마지막 항목 반환
        return items[items.Count - 1];
    }
}

/// <summary>
/// 재료 타입과 프리팹을 연결하는 구조체
/// 인스펙터에서 각 재료(IngredientType)에 해당하는 프리팹(GameObject)을 할당합니다.
/// </summary>
[System.Serializable]
public struct IngredientPrefabEntry
{
    [Tooltip("재료 종류")]
    public IngredientType type;

    [Tooltip("해당 재료의 프리팹 (Rigidbody2D + Collider2D + IngredientItem 컴포넌트 필요)")]
    public GameObject prefab;
}
