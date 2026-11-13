using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public abstract class Monster : MonoBehaviour
{
    [SerializeField] private float hp = 100f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float attackRange = 1.5f;

    private CharacterController characterController;
    private Animator animator;
    protected Transform target;

    public bool IsDead => isDead;
    protected bool isDead;
    private Vector3 moveDirection;
    private float verticalSpeed = -2f;

    private static readonly int Speed = Animator.StringToHash("Speed");

    protected virtual void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Update()
    {
        if (isDead == true || target == null) return;

        moveDirection = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        moveDirection = moveDirection.sqrMagnitude > (attackRange * attackRange) ? moveDirection.normalized : Vector3.zero;

        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection), turnSpeed * Time.deltaTime);

        characterController.Move((moveDirection * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);

        float actualSpeed = Vector3.ProjectOnPlane(characterController.velocity, Vector3.up).magnitude;
        if (animator != null) animator.SetFloat(Speed, Mathf.Clamp01(actualSpeed / moveSpeed), 0.1f, Time.deltaTime);
    }

    public virtual void Initialize(Transform target)
    {
        this.target = target;
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

        if(MonsterManager.Instance != null)
            MonsterManager.Instance.UnRegister(this);

        // юс╫ц
        gameObject.SetActive(false);
    }
}
