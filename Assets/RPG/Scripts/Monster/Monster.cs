using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public abstract class Monster : MonoBehaviour
{
    private CharacterStatus status;

    private float hp;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField, Range(0f, 1f)] private float hitFraction = 0.5f;
    [SerializeField] private DropTable dropTable;
    [SerializeField, Min(0)] private int experienceReward = 10;

    private CharacterController characterController;
    private Animator animator;
    private Transform target;
    private PlayerHealth targetHealth;
    private PlayerLevel targetLevel;

    private bool isDead;
    public bool IsDead => isDead;

    private Vector3 moveDirection;
    private float verticalSpeed = -2f;

    private bool isAttacking;
    private bool hitChecked;
    private bool canReactToHit = true;
    private bool isHitState;
    private float nextAttackTime;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Locomotion = Animator.StringToHash("Base Layer.Locomotion");
    private static readonly int Death = Animator.StringToHash("Base Layer.Death");
    private static readonly int Attack = Animator.StringToHash("Base Layer.Attack");
    private static readonly int Hit = Animator.StringToHash("Base Layer.Hit");
    private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");

    protected virtual void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        status = GetComponent<CharacterStatus>();
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

        if(isHitState == true)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (state.fullPathHash == Hit && state.normalizedTime >= 1f)
            {
                isHitState = false;
                animator.CrossFadeInFixedTime(Locomotion, 0.25f);
            }

            return;
        }

        moveDirection = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        moveDirection = moveDirection.sqrMagnitude > (status.AttackRange * status.AttackRange) ? moveDirection.normalized : Vector3.zero;

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

        characterController.Move((moveDirection * status.MoveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);

        float actualSpeed = Vector3.ProjectOnPlane(characterController.velocity, Vector3.up).magnitude;
        if (animator != null) animator.SetFloat(Speed, Mathf.Clamp01(actualSpeed / status.MoveSpeed), 0.1f, Time.deltaTime);
    }

    private void StartAttack()
    {
        isAttacking = true;
        hitChecked = false;

        animator.SetFloat(AttackSpeed, status.AttackSpeed);

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

                if (offset.sqrMagnitude <= (status.AttackRange * status.AttackRange))
                {
                    targetHealth.TakeDamage(status);
                }
            }
        }

        if(progress >= 1f)
        {
            isAttacking = false;
            canReactToHit = true;
            nextAttackTime = Time.time + status.AttackCooldown;
            animator.CrossFadeInFixedTime(Locomotion, 0.25f);
        }
    }

    public virtual void Initialize(Transform target)
    {
        this.target = target;
        targetHealth = target.GetComponent<PlayerHealth>();
        targetLevel = target.GetComponent<PlayerLevel>();

        // 초기화
        hp = status.MaxHp;
        isDead = false;
        isAttacking = false;
        hitChecked = false;
        canReactToHit = true;
        isHitState = false;
        nextAttackTime = 0f;
        moveDirection = Vector3.zero;

        characterController.enabled = true;
        animator.SetFloat(Speed, 0f);
        animator.Play(Locomotion, 0, 0f);
    }

    public virtual void TakeDamage(CharacterStatus attacker)
    {
        if (isDead == true || attacker == null) return;

        float damage = DamageCalculator.Calculate(attacker, status);

        if (damage <= 0)
            return;

        hp -= damage;

        if (hp <= 0f)
        {
            Die();

            return;
        }

        if(canReactToHit)
        {
            canReactToHit = false;
            isAttacking = false;
            isHitState = true;
            animator.CrossFadeInFixedTime(Hit, 0.25f);
        }
    }

    public virtual void Die()
    {
        if (isDead == true) return;

        isDead = true;
        characterController.enabled = false;

        if(MonsterManager.Instance != null)
            MonsterManager.Instance.UnRegister(this);

        animator.CrossFadeInFixedTime(Death, 0.05f, 0, 0);

        targetLevel.AddExperience(experienceReward);
        DropReward();
    }

    private void DropReward()
    {
        if (dropTable == null) return;

        RewardEntry reward = dropTable.Roll();
        if (reward == null) return;

        int amount = reward.RollAmount();

        ItemData item = reward.itemData;
        if (item == null) return;

        if (item.prefab != null)
        {
            Vector3 dropPosition = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            GameObject instance = Instantiate(item.prefab, dropPosition, item.prefab.transform.rotation);
            DroppedItem droppedItem = instance.AddComponent<DroppedItem>();
            droppedItem.Initialize(item, amount);
        }

        Debug.Log("드랍아이템 : " + item.itemName + " : " + amount);
    }
}
