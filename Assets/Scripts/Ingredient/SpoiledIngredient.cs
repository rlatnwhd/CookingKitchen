using UnityEngine;

/// <summary>
/// 상한 식재료 오브젝트에 부착하는 스크립트
/// - IngredientItem과 별도의 컴포넌트로, PlayerCollector에서 구분하여 처리합니다.
/// - 플레이어가 수집하면 모든 소지 재료를 드랍하고 HP가 감소합니다.
///
/// [프리팹 설정 안내]
/// 1. 빈 오브젝트를 만들고 상한 식재료 스프라이트를 SpriteRenderer에 할당합니다.
/// 2. Rigidbody2D 추가 (Gravity Scale > 0, 중력으로 낙하)
/// 3. Collider2D 추가 (isTrigger = ON)
/// 4. 이 스크립트(SpoiledIngredient) 부착
/// 5. IngredientSpawner의 spoiledPrefab에 이 프리팹을 할당합니다.
/// </summary>
public class SpoiledIngredient : MonoBehaviour
{
    [Header("삭제 설정")]
    [Tooltip("이 Y좌표 아래로 떨어지면 오브젝트가 삭제됩니다")]
    public float destroyYPosition = -6f;

    void Update()
    {
        if (transform.position.y < destroyYPosition)
        {
            Destroy(gameObject);
        }
    }
}
