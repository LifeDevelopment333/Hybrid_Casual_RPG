using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public abstract class Monster : MonoBehaviour
{
    [SerializeField] private float HP = 100f;
    private float hp;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 360f;

    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 0.9f;
    [SerializeField, Range(0f, 1f)] private float hitFraction = 0.5f;

    private CharacterController characterController;
    private Animator animator;
    private Transform target;
    private PlayerHealth targetHealth;

    private bool isDead;
    public bool IsDead => isDead;

    private Vector3 moveDirection;
    private float verticalSpeed = -2f;

    private bool isAttacking;
    private bool hitChecked;
    private float nextAttackTime;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Locomotion = Animator.StringToHash("Base Layer.Locomotion");
    private static readonly int Death = Animator.StringToHash("Base Layer.Death");
    private static readonly int Attack = Animator.StringToHash("Base Layer.Attack");

    protected virtual void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Update()
    {
        if(isDead == true)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if(state.fullPathHash == Death && state.normalizedTime >= 1f)
            {
                gameObject.SetActive(false);
            }

            return;
        }
        if (target == null) return;

        moveDirection = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        moveDirection = moveDirection.sqrMagnitude > (attackRange * attackRange) ? moveDirection.normalized : Vector3.zero;

        if(isAttacking)
        {
            UpdateAttack();
            return;
        }
        else if(moveDirection == Vector3.zero)
        {
            if(Time.time >= nextAttackTime)
                StartAttack();
        }

        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection), turnSpeed * Time.deltaTime);

        characterController.Move((moveDirection * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);

        float actualSpeed = Vector3.ProjectOnPlane(characterController.velocity, Vector3.up).magnitude;
        if (animator != null) animator.SetFloat(Speed, Mathf.Clamp01(actualSpeed / moveSpeed), 0.1f, Time.deltaTime);
    }

    private void StartAttack()
    {
        isAttacking = true;
        hitChecked = false;
        animator.CrossFadeInFixedTime(Attack, 0.05f, 0);
    }

    private void UpdateAttack()
    {
        AnimatorStateInfo state = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);

        if (state.fullPathHash != Attack) return;

        float progress = state.normalizedTime;

        if(hitChecked == false && progress >= hitFraction)
        {
            hitChecked = true;

            if (target != null && targetHealth.IsDead == false)
            {
                Vector3 offset = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);

                if (offset.sqrMagnitude <= (attackRange * attackRange))
                {
                    targetHealth.TakeDamage(attackDamage);
                }
            }
        }

        if(progress >= 1f)
        {
            isAttacking = false;
            nextAttackTime = Time.time + attackCooldown;
            animator.CrossFadeInFixedTime(Locomotion, 0.25f);
        }
    }

    public virtual void Initialize(Transform target)
    {
        this.target = target;
        targetHealth = target.GetComponent<PlayerHealth>();

        // 초기화
        hp = HP;
        isDead = false;
        isAttacking = false;
        hitChecked = false;
        nextAttackTime = 0f;
        moveDirection = Vector3.zero;

        characterController.enabled = true;
        animator.SetFloat(Speed, 0f);
        animator.Play(Locomotion, 0, 0f);
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead == true || damage <= 0f) return;

        hp -= damage;

        if (hp <= 0f) Die();
    }



    public virtual void Die()
    {
        if (isDead == true) return;

        isDead = true;
        characterController.enabled = false;

        if(MonsterManager.Instance != null)
            MonsterManager.Instance.UnRegister(this);

        animator.CrossFadeInFixedTime(Death, 0.05f, 0, 0);

        // 임시
        //gameObject.SetActive(false);
    }
}
