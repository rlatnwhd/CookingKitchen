using UnityEngine;

/// <summary>
/// 플레이어 상단 재료 수집 콜라이더에 부착하는 스크립트
/// 
/// [설정 안내]
/// - 이 스크립트는 플레이어의 자식 오브젝트(수집 전용 콜라이더)에 부착합니다.
/// - 해당 자식 오브젝트의 Collider2D는 반드시 isTrigger = ON 으로 설정하세요.
/// - 재료 프리팹의 Collider2D도 isTrigger = ON 이어야 합니다.
/// - 재료 프리팹에는 Rigidbody2D가 있어야 트리거 이벤트가 발생합니다.
/// </summary>
public class PlayerCollector : MonoBehaviour
{
    /// <summary>
    /// 수집 콜라이더에 재료가 닿으면 인벤토리에 추가하고 오브젝트를 삭제합니다.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 오브젝트에 IngredientItem이 없으면 무시
        IngredientItem item = other.GetComponent<IngredientItem>();
        if (item == null) return;

        // PlayerInventory에 재료 추가
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddIngredient(item.ingredientType);
        }

        // 재료 오브젝트 삭제
        Destroy(other.gameObject);
    }
}
