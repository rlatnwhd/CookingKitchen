using System;

/// <summary>
/// 하나의 레시피를 정의하는 데이터 클래스
/// - recipeName : 완성품 이름 (예: 피자)
/// - ingredients : 필요한 재료 배열
/// - page : 레시피가 속한 페이지 (1, 2, 3 = 라운드)
/// </summary>
[Serializable]
public class RecipeData
{
    // 완성품 이름 (예: 피자, 스테이크)
    public string recipeName;

    // 이 레시피에 필요한 재료 목록
    public IngredientType[] ingredients;

    // 레시피가 속한 페이지 (1, 2, 3)
    // 라운드 1 = 페이지 1만, 라운드 2 = 페이지 1+2, 라운드 3 = 전체
    public int page;

    /// <summary>
    /// 레시피 데이터를 생성합니다.
    /// </summary>
    /// <param name="name">완성품 이름</param>
    /// <param name="page">페이지 번호 (1~3)</param>
    /// <param name="ingredients">필요한 재료들</param>
    public RecipeData(string name, int page, params IngredientType[] ingredients)
    {
        this.recipeName = name;
        this.page = page;
        this.ingredients = ingredients;
    }
}
