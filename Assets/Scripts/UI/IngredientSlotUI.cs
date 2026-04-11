using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리 UI에서 재료 슬롯 하나를 담당하는 컴포넌트
/// 
/// [슬롯 구조]
///   ┌──────┐
///   │ 이미지 │  ← ingredientImage (0개면 어둡게, 1개↑ 밝게)
///   │  x0  │  ← countText
///   └──────┘
/// </summary>
public class IngredientSlotUI : MonoBehaviour
{
    [Header("슬롯 UI 요소")]
    [Tooltip("재료 이미지 (Image 컴포넌트)")]
    public Image ingredientImage;

    [Tooltip("수량 텍스트 (x0, x1 형식으로 표시)")]
    public TextMeshProUGUI countText;

    [Header("색상 설정")]
    [Tooltip("재료를 1개 이상 보유 중일 때 이미지 색상 (밝게)")]
    public Color ownedColor = Color.white;

    [Tooltip("재료를 0개 보유 시 이미지 색상 (어둡게)")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    // 이 슬롯이 나타내는 재료 타입 (외부에서 읽기 전용)
    public IngredientType IngredientType { get; private set; }

    /// <summary>
    /// 슬롯을 초기화합니다.
    /// - 재료 타입과 스프라이트를 설정하고, 수량을 0으로 초기화합니다.
    /// </summary>
    public void Initialize(IngredientType type, Sprite sprite)
    {
        IngredientType = type;

        if (ingredientImage != null)
        {
            ingredientImage.sprite = sprite;
            // 처음에는 재료 없으므로 어둡게
            ingredientImage.color = emptyColor;
        }

        if (countText != null)
            countText.text = "x0";
    }

    /// <summary>
    /// 수량이 바뀔 때 호출합니다.
    /// 0개 → 이미지 어둡게 / 1개 이상 → 이미지 밝게
    /// </summary>
    public void UpdateCount(int count)
    {
        if (countText != null)
            countText.text = $"x{count}";

        if (ingredientImage != null)
            ingredientImage.color = count > 0 ? ownedColor : emptyColor;
    }
}
