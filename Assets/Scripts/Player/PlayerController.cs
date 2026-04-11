using UnityEngine;

/// <summary>
/// 플레이어 이동 조작을 담당하는 스크립트
/// - A/D 키로 좌우 이동
/// - 이동 방향에 따른 스프라이트 좌우 반전
/// - X축 이동 범위 제한 (투명 벽 효과)
/// - 애니메이터 isWalking 파라미터 연동
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("플레이어 이동 속도 (단위/초)")]
    public float moveSpeed = 5f;

    [Tooltip("플레이어가 넘어갈 수 없는 X축 최대/최소 범위")]
    public float xBoundary = 5.6f;

    // 컴포넌트 참조 (자동으로 가져옴)
    private Rigidbody2D rb;           // 물리 이동에 사용
    private SpriteRenderer spriteRenderer; // 스프라이트 반전에 사용
    private Animator animator;        // 애니메이션 파라미터 제어에 사용

    void Awake()
    {
        // 같은 게임오브젝트의 컴포넌트를 자동으로 가져옴
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleMovement();
    }

    /// <summary>
    /// 매 프레임마다 A/D 입력을 처리하여 이동, 스프라이트 반전, 애니메이션을 갱신합니다.
    /// </summary>
    private void HandleMovement()
    {
        // A키: -1, D키: +1, 아무것도 없으면: 0
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // 이동 속도 계산
        Vector2 velocity = rb.linearVelocity;
        velocity.x = horizontalInput * moveSpeed;
        rb.linearVelocity = velocity;

        // X축 범위 제한 (xBoundary를 벗어나지 않도록 위치를 클램프)
        Vector2 clampedPosition = rb.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -xBoundary, xBoundary);
        rb.position = clampedPosition;

        // 스프라이트 반전: 기본 스프라이트는 오른쪽을 바라봄
        // 왼쪽으로 이동할 때만 flipX = true
        if (horizontalInput < 0f)
        {
            spriteRenderer.flipX = true;
        }
        else if (horizontalInput > 0f)
        {
            spriteRenderer.flipX = false;
        }

        // 애니메이터 파라미터 갱신: 이동 중이면 true, 정지 시 false
        bool IsWalking = horizontalInput != 0f;
        animator.SetBool("IsWalking", IsWalking);
    }
}
