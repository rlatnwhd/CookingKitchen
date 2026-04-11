using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 레시피 데이터를 관리하는 싱글톤 스크립트
/// - 18개 레시피 초기화 및 저장 (3페이지 × 6개)
/// - 라운드별 사용 가능한 레시피 목록 제공
/// - 라운드별 재료 가중치(등장 빈도) 계산
/// </summary>
public class RecipeDatabase : MonoBehaviour
{
    // 싱글톤 인스턴스 (다른 스크립트에서 RecipeDatabase.Instance로 접근)
    public static RecipeDatabase Instance { get; private set; }

    // 전체 레시피 목록 (코드에서 자동 초기화)
    private List<RecipeData> allRecipes = new List<RecipeData>();

    void Awake()
    {
        // 싱글톤 설정: 중복 방지
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeRecipes();
    }

    /// <summary>
    /// 게임에서 사용하는 전체 18개 레시피를 초기화합니다.
    /// </summary>
    private void InitializeRecipes()
    {
        // ===== 1 페이지 (라운드 1) : 6개 레시피 =====
        allRecipes.Add(new RecipeData("피자", 1,
            IngredientType.Flour, IngredientType.Sausage, IngredientType.Ketchup, IngredientType.Cheese));

        allRecipes.Add(new RecipeData("핫도그", 1,
            IngredientType.Sausage, IngredientType.Flour, IngredientType.Ketchup, IngredientType.Mustard));

        allRecipes.Add(new RecipeData("스테이크", 1,
            IngredientType.Meat, IngredientType.Salt));

        allRecipes.Add(new RecipeData("베이컨 구이", 1,
            IngredientType.Bacon, IngredientType.Salt));

        allRecipes.Add(new RecipeData("감자칩", 1,
            IngredientType.Potato, IngredientType.Salt));

        allRecipes.Add(new RecipeData("스파게티", 1,
            IngredientType.Flour, IngredientType.Ketchup));

        // ===== 2 페이지 (라운드 2) : 6개 레시피 =====
        allRecipes.Add(new RecipeData("스시", 2,
            IngredientType.Fish));

        allRecipes.Add(new RecipeData("라멘", 2,
            IngredientType.Flour, IngredientType.Egg, IngredientType.Meat));

        allRecipes.Add(new RecipeData("계란후라이", 2,
            IngredientType.Egg, IngredientType.Salt));

        allRecipes.Add(new RecipeData("만두", 2,
            IngredientType.Flour, IngredientType.Meat, IngredientType.Egg));

        allRecipes.Add(new RecipeData("연어 구이", 2,
            IngredientType.Salmon, IngredientType.Salt));

        allRecipes.Add(new RecipeData("샌드위치", 2,
            IngredientType.Flour, IngredientType.Cabbage, IngredientType.Meat, IngredientType.Egg, IngredientType.Cheese));

        // ===== 3 페이지 (라운드 3) : 6개 레시피 =====
        allRecipes.Add(new RecipeData("케이크", 3,
            IngredientType.Flour, IngredientType.Sugar, IngredientType.Milk, IngredientType.Butter));

        allRecipes.Add(new RecipeData("파이", 3,
            IngredientType.Flour, IngredientType.Jam, IngredientType.Milk, IngredientType.Butter));

        allRecipes.Add(new RecipeData("아이스크림", 3,
            IngredientType.Milk, IngredientType.Sugar));

        allRecipes.Add(new RecipeData("잼바른 빵", 3,
            IngredientType.Flour, IngredientType.Jam));

        allRecipes.Add(new RecipeData("팬케이크", 3,
            IngredientType.Flour, IngredientType.Milk, IngredientType.Butter, IngredientType.Sugar, IngredientType.Syrup));

        allRecipes.Add(new RecipeData("식빵", 3,
            IngredientType.Flour, IngredientType.Milk, IngredientType.Butter, IngredientType.Sugar));
    }

    /// <summary>
    /// 지정한 라운드에서 사용 가능한 레시피 목록을 반환합니다.
    /// 라운드 1 = 1페이지만, 라운드 2 = 1+2페이지, 라운드 3 = 전체
    /// </summary>
    public List<RecipeData> GetRecipesForRound(int round)
    {
        List<RecipeData> result = new List<RecipeData>();
        foreach (var recipe in allRecipes)
        {
            if (recipe.page <= round)
                result.Add(recipe);
        }
        return result;
    }

    /// <summary>
    /// 지정한 라운드에서 각 재료의 등장 빈도(가중치)를 계산하여 반환합니다.
    /// 가중치 = 해당 라운드의 레시피들에서 해당 재료가 사용된 총 횟수
    /// (예: 라운드1에서 밀가루는 피자+핫도그+스파게티 = 가중치 3)
    /// </summary>
    public Dictionary<IngredientType, int> GetIngredientWeightsForRound(int round)
    {
        Dictionary<IngredientType, int> weights = new Dictionary<IngredientType, int>();

        foreach (var recipe in GetRecipesForRound(round))
        {
            foreach (var ingredient in recipe.ingredients)
            {
                if (weights.ContainsKey(ingredient))
                    weights[ingredient]++;
                else
                    weights[ingredient] = 1;
            }
        }

        return weights;
    }

    /// <summary>
    /// 특정 페이지에 해당하는 레시피만 반환합니다. (레시피 북 UI 페이지 표시용)
    /// </summary>
    public List<RecipeData> GetRecipesByPage(int page)
    {
        List<RecipeData> result = new List<RecipeData>();
        foreach (var recipe in allRecipes)
        {
            if (recipe.page == page)
                result.Add(recipe);
        }
        return result;
    }

    /// <summary>
    /// 전체 레시피 목록을 반환합니다.
    /// </summary>
    public List<RecipeData> GetAllRecipes()
    {
        return new List<RecipeData>(allRecipes);
    }
}
