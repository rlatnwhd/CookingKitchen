using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레시피 카드 UI 스크립트 (하나의 레시피 패널)
/// 
/// [레이아웃 구조]
///   ┌─────────────────┐
///   │    [완성품]      │  ← CompletedDishImage
///   │  [0] [1] [2]   │  ← ingredientSlots[0~2] (상단 3칸 행)
///   │    [3] [4]     │  ← ingredientSlots[3~4] (하단 2칸 행)
///   └─────────────────┘
/// 
/// - 보유한 재료 슬롯: 밝게 (ownedColor)
/// - 미보유 재료 슬롯: 어둡게 (missingColor)
/// - 레시피의 재료 수보다 많은 슬롯은 자동으로 비활성화
/// - RecipeQueueManager가 이 패널을 프리팹으로 생성하고 관리합니다.
/// </summary>
public class RecipeUI : MonoBehaviour
{
    [Header("완성품 이미지 슬롯")]
    [Tooltip("상단에 표시되는 완성품 이미지")]
    public Image completedDishImage;

    [Header("재료 슬롯 (5개 고정)")]
    [Tooltip("슬롯 배치: [0]=상단좌, [1]=상단중, [2]=상단우, [3]=하단좌, [4]=하단우")]
    public Image[] ingredientSlots = new Image[5];

    [Header("색상 설정")]
    [Tooltip("재료를 보유 중일 때 슬롯 색상 (밝게)")]
    public Color ownedColor = Color.white;

    [Tooltip("재료를 미보유 시 슬롯 색상 (어둡게)")]
    public Color missingColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("완성품 스프라이트 할당")]
    [Tooltip("완성품 이름에 대응하는 스프라이트 (레시피 이름과 정확히 일치해야 함)")]
    public RecipeSpriteEntry[] recipeSprites;

    [Header("재료 스프라이트 할당")]
    [Tooltip("각 재료 타입에 대응하는 스프라이트 (18종류)")]
    public IngredientSpriteEntry[] ingredientSprites;

    // 빠른 조회용 딕셔너리
    private Dictionary<string, Sprite> recipeSpriteMap = new Dictionary<string, Sprite>();
    private Dictionary<IngredientType, Sprite> ingredientSpriteMap = new Dictionary<IngredientType, Sprite>();

    // 현재 표시 중인 레시피
    private RecipeData currentRecipe;

    // RectTransform 캐시 (슬라이드 애니메이션용, RecipeQueueManager에서 접근)
    public RectTransform RectTransform { get; private set; }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        BuildSpriteMaps();
    }

    /// <summary>
    /// 인스펙터에서 할당한 배열을 딕셔너리로 변환합니다.
    /// </summary>
    private void BuildSpriteMaps()
    {
        recipeSpriteMap.Clear();
        foreach (var entry in recipeSprites)
        {
            if (entry.sprite != null && !recipeSpriteMap.ContainsKey(entry.recipeName))
                recipeSpriteMap[entry.recipeName] = entry.sprite;
        }

        ingredientSpriteMap.Clear();
        foreach (var entry in ingredientSprites)
        {
            if (entry.sprite != null && !ingredientSpriteMap.ContainsKey(entry.type))
                ingredientSpriteMap[entry.type] = entry.sprite;
        }
    }

    /// <summary>
    /// 레시피를 UI에 설정합니다.
    /// 완성품 이미지 표시, 재료 슬롯 배치, 처음에는 모든 재료 어둡게 표시.
    /// </summary>
    public void SetRecipe(RecipeData recipe)
    {
        if (recipe == null) return;
        currentRecipe = recipe;

        // 완성품 이미지 설정
        if (completedDishImage != null)
        {
            completedDishImage.sprite = recipeSpriteMap.ContainsKey(recipe.recipeName)
                ? recipeSpriteMap[recipe.recipeName]
                : null;
            completedDishImage.color = Color.white;
        }

        // 재료 슬롯 설정
        for (int i = 0; i < ingredientSlots.Length; i++)
        {
            if (ingredientSlots[i] == null) continue;

            if (i < recipe.ingredients.Length)
            {
                // 재료가 있는 슬롯: 활성화 + 스프라이트 설정 + 처음엔 어둡게
                ingredientSlots[i].gameObject.SetActive(true);
                ingredientSlots[i].sprite = ingredientSpriteMap.ContainsKey(recipe.ingredients[i])
                    ? ingredientSpriteMap[recipe.ingredients[i]]
                    : null;
                ingredientSlots[i].color = missingColor;
            }
            else
            {
                // 재료 수를 초과하는 슬롯은 숨김
                ingredientSlots[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 플레이어가 보유한 재료 목록을 받아 슬롯 색상을 갱신합니다.
    /// 보유 중 → 밝게, 미보유 → 어둡게
    /// (재료 획득 후 인벤토리 전달 시 호출)
    /// </summary>
    public void UpdateOwnedIngredients(List<IngredientType> ownedIngredients)
    {
        if (currentRecipe == null) return;

        for (int i = 0; i < currentRecipe.ingredients.Length && i < ingredientSlots.Length; i++)
        {
            if (ingredientSlots[i] == null) continue;

            bool owned = ownedIngredients.Contains(currentRecipe.ingredients[i]);
            ingredientSlots[i].color = owned ? ownedColor : missingColor;
        }
    }

    /// <summary>
    /// 특정 재료 슬롯을 즉시 밝게 표시합니다.
    /// (재료를 막 획득했을 때 바로 호출)
    /// </summary>
    public void HighlightIngredient(IngredientType type)
    {
        if (currentRecipe == null) return;

        for (int i = 0; i < currentRecipe.ingredients.Length && i < ingredientSlots.Length; i++)
        {
            if (currentRecipe.ingredients[i] == type && ingredientSlots[i] != null)
            {
                ingredientSlots[i].color = ownedColor;
            }
        }
    }

    /// <summary>
    /// 현재 표시 중인 레시피를 반환합니다.
    /// </summary>
    public RecipeData GetCurrentRecipe()
    {
        return currentRecipe;
    }

    /// <summary>
    /// 현재 레시피의 완성품 스프라이트를 반환합니다.
    /// CustomerManager가 말풍선 아이콘으로 사용합니다.
    /// </summary>
    public Sprite GetDishSprite()
    {
        if (currentRecipe == null) return null;
        if (recipeSpriteMap.ContainsKey(currentRecipe.recipeName))
            return recipeSpriteMap[currentRecipe.recipeName];
        return null;
    }

    /// <summary>
    /// 현재 레시피의 모든 재료를 플레이어가 보유하고 있는지 확인합니다.
    /// (각 재료당 1개 이상 필요)
    /// </summary>
    public bool CanComplete()
    {
        if (currentRecipe == null) return false;
        if (PlayerInventory.Instance == null) return false;

        foreach (var ingredient in currentRecipe.ingredients)
        {
            if (!PlayerInventory.Instance.HasIngredient(ingredient))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 레시피를 완성합니다. 소지 재료를 차감합니다.
    /// CanComplete()가 true일 때만 호출하세요.
    /// </summary>
    public void ConsumeIngredients()
    {
        if (currentRecipe == null) return;

        foreach (var ingredient in currentRecipe.ingredients)
        {
            PlayerInventory.Instance.RemoveIngredient(ingredient);
        }
    }
}

/// <summary>
/// 완성품 이름과 스프라이트를 연결하는 구조체 (인스펙터 할당용)
/// </summary>
[System.Serializable]
public struct RecipeSpriteEntry
{
    [Tooltip("완성품 이름 (RecipeData.recipeName과 정확히 일치해야 함, 예: 피자)")]
    public string recipeName;

    [Tooltip("완성품 스프라이트")]
    public Sprite sprite;
}

/// <summary>
/// 재료 타입과 UI 스프라이트를 연결하는 구조체 (인스펙터 할당용)
/// </summary>
[System.Serializable]
public struct IngredientSpriteEntry
{
    [Tooltip("재료 종류")]
    public IngredientType type;

    [Tooltip("UI에 표시할 재료 스프라이트")]
    public Sprite sprite;
}
