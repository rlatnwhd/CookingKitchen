using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면 우측에 재료 보유 현황을 표시하는 인벤토리 UI 스크립트
/// - 현재 라운드에 등장하는 재료 슬롯을 자동으로 생성
/// - PlayerInventory 이벤트를 구독해 재료 획득 시 실시간 갱신
/// - 0개면 어둡게 + x0, 1개 이상이면 밝게 + x{수량}
/// </summary>
public class IngredientInventoryUI : MonoBehaviour
{
    [Header("슬롯 설정")]
    [Tooltip("IngredientSlotUI 컴포넌트가 붙어있는 슬롯 프리팹")]
    public GameObject slotPrefab;

    [Tooltip("슬롯이 배치될 부모 Transform (GridLayoutGroup 또는 VerticalLayoutGroup 권장)")]
    public Transform slotParent;

    [Header("라운드 설정")]
    [Tooltip("표시할 재료를 결정하는 라운드 (1/2/3)")]
    public int currentRound = 1;

    [Header("재료 스프라이트 할당")]
    [Tooltip("각 재료 타입에 대응하는 스프라이트 (18종류)")]
    public IngredientSpriteEntry[] ingredientSprites;

    // 스프라이트 딕셔너리 (빠른 조회)
    private Dictionary<IngredientType, Sprite> spriteMap = new Dictionary<IngredientType, Sprite>();

    // 재료 타입 → 슬롯 UI 매핑 (수량 갱신 시 사용)
    private Dictionary<IngredientType, IngredientSlotUI> slotMap = new Dictionary<IngredientType, IngredientSlotUI>();

    void Awake()
    {
        // 인스펙터에서 할당한 스프라이트 배열을 딕셔너리로 변환
        spriteMap.Clear();
        foreach (var entry in ingredientSprites)
        {
            if (entry.sprite != null && !spriteMap.ContainsKey(entry.type))
                spriteMap[entry.type] = entry.sprite;
        }
    }

    void Start()
    {
        // 현재 라운드의 재료 슬롯 생성
        BuildSlots(currentRound);

        // PlayerInventory 이벤트 구독 (재료 수량 변경 시 알림 받음)
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnIngredientChanged += OnIngredientChanged;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnIngredientChanged -= OnIngredientChanged;
    }

    /// <summary>
    /// 지정한 라운드에 해당하는 재료 슬롯을 생성합니다.
    /// 라운드가 바뀔 때 호출하면 슬롯이 새로 구성됩니다.
    /// </summary>
    public void BuildSlots(int round)
    {
        currentRound = round;

        // 기존 슬롯 제거
        for (int i = slotParent.childCount - 1; i >= 0; i--)
            Destroy(slotParent.GetChild(i).gameObject);
        slotMap.Clear();

        if (RecipeDatabase.Instance == null) return;

        // 현재 라운드의 재료 가중치 목록으로 슬롯 생성
        var weights = RecipeDatabase.Instance.GetIngredientWeightsForRound(round);
        foreach (var type in weights.Keys)
        {
            GameObject obj = Instantiate(slotPrefab, slotParent);
            IngredientSlotUI slot = obj.GetComponent<IngredientSlotUI>();
            if (slot == null) continue;

            // 슬롯 초기화 (스프라이트 없으면 null 전달)
            Sprite sprite = spriteMap.ContainsKey(type) ? spriteMap[type] : null;
            slot.Initialize(type, sprite);
            slotMap[type] = slot;
        }
    }

    /// <summary>
    /// PlayerInventory.OnIngredientChanged 이벤트 핸들러
    /// count 파라미터를 쓰지 않고 PlayerInventory에서 현재값을 직접 읽습니다.
    /// (이벤트 중첩 호출 시 stale count로 UI가 덮어써지는 버그 방지)
    /// </summary>
    private void OnIngredientChanged(IngredientType type, int count)
    {
        // 이벤트로 전달된 count는 중첩 호출 시 오래된 값일 수 있으므로 실제 재고를 직접 읽음
        int actualCount = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetCount(type)
            : count;

        if (slotMap.ContainsKey(type))
        {
            slotMap[type].UpdateCount(actualCount);
        }
        else if (actualCount > 0)
        {
            BuildSlots(currentRound);
            if (PlayerInventory.Instance != null)
            {
                foreach (var kv in slotMap)
                    kv.Value.UpdateCount(PlayerInventory.Instance.GetCount(kv.Key));
            }
        }
    }
}
