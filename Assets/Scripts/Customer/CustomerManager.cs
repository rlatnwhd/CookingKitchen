using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 손님 슬롯 전체를 관리하는 스크립트
///
/// [설정 방법]
/// 1. 씬에 빈 GameObject를 손님이 서 있을 위치마다 배치하고 slotTransforms에 좌→우 순서로 할당
///    → 각 GameObject의 Z값으로 배경 레이어 사이를 조정하세요
///      (예: 뒷 배경 Z=1.0, 앞 배경 Z=0.1 → 슬롯 오브젝트 Z=0.5)
/// 2. Customer 컴포넌트가 붙은 손님 프리팹을 customerPrefab에 할당
/// 3. 손님 스프라이트들을 customerSprites 배열에 할당
/// 4. RecipeQueueManager 인스펙터의 customerManager 필드에 이 컴포넌트를 할당
///
/// [동작 로직]
/// - 레시피 카드 등장 → 왼쪽(인덱스 0)부터 빈 슬롯을 찾아 손님 생성
/// - 레시피 완성 → 연결된 손님이 아래로 퇴장 후 슬롯 해제
/// - 슬롯 위치는 고정 (손님이 퇴장해도 자리 이동 없음)
/// - 빈 자리는 다음 손님이 왼쪽 우선으로 채움
/// </summary>
public class CustomerManager : MonoBehaviour
{
    [Header("슬롯 위치 설정")]
    [Tooltip("손님이 서 있을 위치의 Transform. 좌→우 순서로 할당하세요.\n" +
             "각 오브젝트의 Z값으로 배경 사이 레이어를 설정합니다 (뒷배경~앞배경 사이 Z)")]
    public Transform[] slotTransforms;

    [Header("손님 프리팹")]
    [Tooltip("Customer 컴포넌트가 붙어있는 손님 프리팹")]
    public GameObject customerPrefab;

    [Header("손님 스프라이트")]
    [Tooltip("손님 생성 시 이 목록에서 랜덤으로 스프라이트를 선택합니다")]
    public Sprite[] customerSprites;

    // 슬롯별 현재 손님 (null = 빈 슬롯)
    private Customer[] slots;

    // RecipeUI → Customer 빠른 조회 테이블
    private Dictionary<RecipeUI, Customer> cardToCustomer = new Dictionary<RecipeUI, Customer>();

    void Awake()
    {
        int count = slotTransforms != null ? slotTransforms.Length : 0;
        slots = new Customer[count];
    }

    // ─────────────────── RecipeQueueManager 연동 ───────────────────

    /// <summary>
    /// 레시피 카드가 화면에 등장할 때 RecipeQueueManager에서 호출합니다.
    /// 왼쪽부터 빈 슬롯을 찾아 손님을 생성하고 등장 애니메이션을 시작합니다.
    /// </summary>
    public void OnRecipeSpawned(RecipeUI card)
    {
        if (card == null || customerPrefab == null) return;
        if (slotTransforms == null || slotTransforms.Length == 0) return;

        int slotIndex = FindEmptySlot();
        if (slotIndex < 0)
        {
            Debug.Log("[CustomerManager] 빈 슬롯이 없어 손님을 생성하지 않음");
            return;
        }

        // 손님 프리팹 생성
        GameObject obj = Instantiate(customerPrefab);
        Customer customer = obj.GetComponent<Customer>();
        if (customer == null)
        {
            Destroy(obj);
            Debug.LogError("[CustomerManager] customerPrefab에 Customer 컴포넌트가 없습니다!");
            return;
        }

        // 랜덤 스프라이트 선택
        Sprite sprite = null;
        if (customerSprites != null && customerSprites.Length > 0)
            sprite = customerSprites[UnityEngine.Random.Range(0, customerSprites.Length)];

        // 초기화 및 등장 애니메이션 시작
        Vector3 worldPos = slotTransforms[slotIndex].position;
        customer.Initialize(slotIndex, worldPos, card, sprite);

        slots[slotIndex] = customer;
        cardToCustomer[card] = customer;
    }

    /// <summary>
    /// 레시피가 완성될 때 RecipeQueueManager에서 호출합니다.
    /// 연결된 손님을 퇴장 애니메이션과 함께 제거하고 슬롯을 해제합니다.
    /// </summary>
    public void OnRecipeCompleted(RecipeUI card)
    {
        if (card == null) return;
        if (!cardToCustomer.TryGetValue(card, out Customer customer))
            return;

        // 매핑에서 먼저 제거
        cardToCustomer.Remove(card);

        if (customer == null) return;

        int idx = customer.SlotIndex;

        // 슬롯 즉시 해제 → 새 손님이 바로 이 슬롯을 사용할 수 있음
        if (idx >= 0 && idx < slots.Length && slots[idx] == customer)
            slots[idx] = null;

        // 퇴장 애니메이션 (완료 후 자동 Destroy)
        customer.Disappear();
    }

    /// <summary>
    /// 모든 손님을 즉시 제거합니다. 라운드 전환 등 전체 리셋 시 호출합니다.
    /// </summary>
    public void ClearAll()
    {
        if (slots != null)
        {
            foreach (var customer in slots)
            {
                if (customer != null)
                    Destroy(customer.gameObject);
            }
            for (int i = 0; i < slots.Length; i++)
                slots[i] = null;
        }
        cardToCustomer.Clear();
    }

    // ─────────────────── 내부 유틸 ───────────────────

    /// <summary>
    /// 왼쪽(인덱스 0)부터 탐색해 가장 앞의 빈 슬롯 인덱스를 반환합니다.
    /// 모든 슬롯이 차 있으면 -1 반환.
    /// </summary>
    private int FindEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return i;
        }
        return -1;
    }
}
