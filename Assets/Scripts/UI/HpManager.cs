using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HP를 관리하는 싱글톤 스크립트
/// - 하트 이미지 3개를 왼쪽(Hp_1)부터 감소
/// - 피격 시 1.5초 무적 + 깜박임 효과
///
/// [설정 안내]
/// 1. 빈 오브젝트 생성 후 이 스크립트를 부착합니다.
/// 2. heartImages 배열에 Hp_1, Hp_2, Hp_3 Image를 왼쪽부터 순서대로 할당합니다.
/// 3. playerSpriteRenderer에 플레이어의 SpriteRenderer를 할당합니다.
/// </summary>
public class HpManager : MonoBehaviour
{
    public static HpManager Instance { get; private set; }

    [Header("HP 설정")]
    [Tooltip("최대 HP (기본값: 3)")]
    public int maxHp = 3;

    [Header("하트 이미지")]
    [Tooltip("Hp_1, Hp_2, Hp_3 순서로 할당 (왼쪽부터)")]
    public Image[] heartImages;

    [Header("무적 설정")]
    [Tooltip("무적 지속 시간 (초)")]
    public float invincibilityDuration = 1.5f;

    [Tooltip("깜박임 간격 (초)")]
    public float blinkInterval = 0.1f;

    [Tooltip("플레이어의 SpriteRenderer (깜박임 효과용)")]
    public SpriteRenderer playerSpriteRenderer;

    private int currentHp;
    private bool isInvincible;
    private Coroutine invincibilityCoroutine;

    /// <summary>
    /// 현재 무적 상태인지 확인합니다.
    /// PlayerCollector에서 상한 식재료 무시 여부 판단에 사용합니다.
    /// </summary>
    public bool IsInvincible => isInvincible;

    /// <summary>
    /// HP가 변경될 때 발생하는 이벤트 (인자: 변경 후 HP)
    /// </summary>
    public event Action<int> OnHpChanged;

    /// <summary>
    /// HP가 0이 되면 발생하는 이벤트
    /// </summary>
    public event Action OnPlayerDead;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        currentHp = maxHp;
    }

    /// <summary>
    /// HP를 1 감소시킵니다. 무적 중이거나 이미 사망 상태이면 무시합니다.
    /// 피격 후 무적 상태가 시작되며, 플레이어 스프라이트가 깜박입니다.
    /// </summary>
    public void TakeDamage()
    {
        if (isInvincible || currentHp <= 0) return;

        currentHp--;
        UpdateHeartUI();
        OnHpChanged?.Invoke(currentHp);

        if (currentHp <= 0)
        {
            OnPlayerDead?.Invoke();
            return;
        }

        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);
        invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine());
    }

    /// <summary>
    /// 하트 이미지를 현재 HP에 맞게 갱신합니다.
    /// 왼쪽(인덱스 0, Hp_1)부터 사라집니다.
    /// HP=3: 모두 표시, HP=2: Hp_1 숨김, HP=1: Hp_1+Hp_2 숨김, HP=0: 모두 숨김
    /// </summary>
    private void UpdateHeartUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            heartImages[i].enabled = i >= (maxHp - currentHp);
        }
    }

    /// <summary>
    /// 무적 시간 동안 플레이어 스프라이트를 깜박이게 합니다.
    /// </summary>
    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invincibilityDuration)
        {
            visible = !visible;
            if (playerSpriteRenderer != null)
            {
                Color c = playerSpriteRenderer.color;
                c.a = visible ? 1f : 0.3f;
                playerSpriteRenderer.color = c;
            }
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        // 깜박임 종료 후 원래 상태로 복구
        if (playerSpriteRenderer != null)
        {
            Color c = playerSpriteRenderer.color;
            c.a = 1f;
            playerSpriteRenderer.color = c;
        }

        isInvincible = false;
        invincibilityCoroutine = null;
    }
}
