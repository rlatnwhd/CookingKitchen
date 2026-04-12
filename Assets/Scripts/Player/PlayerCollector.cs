using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 상단 재료 수집 콜라이더에 부착하는 스크립트
/// - 일반 재료: 인벤토리에 추가
/// - 상한 식재료: 소지 재료 드랍 + HP 감소 + 무적
/// 
/// [설정 안내]
/// - 이 스크립트는 플레이어의 자식 오브젝트(수집 전용 콜라이더)에 부착합니다.
/// - 해당 자식 오브젝트의 Collider2D는 반드시 isTrigger = ON 으로 설정하세요.
/// - 재료 프리팹의 Collider2D도 isTrigger = ON 이어야 합니다.
/// - 재료 프리팹에는 Rigidbody2D가 있어야 트리거 이벤트가 발생합니다.
/// - droppedIngredientPrefab: 드랍 비주얼 프리팹 할당
/// - ingredientSpawner: IngredientSpawner 할당 (재료 스프라이트 조회용)
/// </summary>
public class PlayerCollector : MonoBehaviour
{
    [Header("상한 식재료 처리")]
    [Tooltip("드랍 식재료 프리팹 (SpriteRenderer + Rigidbody2D + DroppedIngredient)")]
    public GameObject droppedIngredientPrefab;

    [Tooltip("IngredientSpawner (재료 스프라이트 조회용)")]
    public IngredientSpawner ingredientSpawner;

    [Tooltip("드랍 시 퍼지는 힘")]
    public float burstForce = 5f;

    [Tooltip("드랍 시 회전력")]
    public float burstTorque = 200f;

    /// <summary>
    /// 수집 콜라이더에 오브젝트가 닿으면 타입을 판별하여 처리합니다.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 상한 식재료 체크
        SpoiledIngredient spoiled = other.GetComponent<SpoiledIngredient>();
        if (spoiled != null)
        {
            HandleSpoiledIngredient(other.gameObject);
            return;
        }

        // 2. 일반 재료 수집
        IngredientItem item = other.GetComponent<IngredientItem>();
        if (item == null) return;

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddIngredient(item.ingredientType);
        }

        Destroy(other.gameObject);
    }

    /// <summary>
    /// 상한 식재료를 처리합니다.
    /// 무적 중이면 파괴만 하고, 아니면 소지 재료 드랍 + 인벤토리 초기화 + HP 감소.
    /// </summary>
    private void HandleSpoiledIngredient(GameObject spoiledObject)
    {
        // 무적 중이면 상한 식재료만 파괴하고 종료
        if (HpManager.Instance != null && HpManager.Instance.IsInvincible)
        {
            Destroy(spoiledObject);
            return;
        }

        // 소지 재료를 펑 터뜨리는 비주얼 이펙트
        BurstOwnedIngredients();

        // 인벤토리 초기화
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.ClearInventory();

        // HP 감소 (무적 시작됨)
        if (HpManager.Instance != null)
            HpManager.Instance.TakeDamage();

        // 상한 식재료 삭제
        Destroy(spoiledObject);
    }

    /// <summary>
    /// 현재 소지 중인 모든 재료를 플레이어 위치에서 사방으로 펑 터뜨립니다.
    /// 각 재료는 위쪽 부채꼴 방향으로 발사되며 살짝 회전합니다.
    /// 드랍된 재료는 DroppedIngredient 컴포넌트만 있어 다시 수집할 수 없습니다.
    /// </summary>
    private void BurstOwnedIngredients()
    {
        if (PlayerInventory.Instance == null || ingredientSpawner == null || droppedIngredientPrefab == null)
            return;

        List<IngredientType> ownedTypes = PlayerInventory.Instance.GetOwnedTypes();
        Vector2 playerPos = transform.parent != null ? (Vector2)transform.parent.position : (Vector2)transform.position;

        foreach (IngredientType type in ownedTypes)
        {
            int count = PlayerInventory.Instance.GetCount(type);
            Sprite sprite = ingredientSpawner.GetIngredientSprite(type);

            for (int i = 0; i < count; i++)
            {
                // 30°~150° 범위의 부채꼴 방향 (위쪽)
                float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                GameObject dropped = Instantiate(droppedIngredientPrefab, playerPos, Quaternion.identity);

                // 스프라이트 설정
                SpriteRenderer sr = dropped.GetComponent<SpriteRenderer>();
                if (sr != null && sprite != null)
                    sr.sprite = sprite;

                // 힘과 회전 적용
                DroppedIngredient di = dropped.GetComponent<DroppedIngredient>();
                if (di != null)
                {
                    float randomForce = burstForce + Random.Range(-1f, 1f);
                    float randomTorque = Random.Range(-burstTorque, burstTorque);
                    di.Launch(direction, randomForce, randomTorque);
                }
            }
        }
    }
}
