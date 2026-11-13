using UnityEngine;

public class PlayerAuto : MonoBehaviour
{
    [Header("옵션")]
    [SerializeField] private float detectionRadius = 18f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField, Range(0f, 1f)] float hitFraction = 0.45f;
    [SerializeField] private float attackCooldown = 0.15f;
    [SerializeField] private float attackDamage = 50f;
    [SerializeField] private float turnSpeed = 360f;

    [Header("참조")]
    [SerializeField] private Animator animator;

    private Monster target;

    private float nextAttackTime;
    private bool hitChecked = false;
    private bool isAttacking = false;

    private void Awake()
    {
        if(animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private Monster FindTarget()
    {
        Monster nearestMonster = null;
        float nearestDistance = detectionRadius * detectionRadius;
        
        foreach(Monster monster in MonsterManager.Instance.Monsters)
        {
            if (monster == null) continue;

            Vector3 offset = Vector3.ProjectOnPlane(monster.transform.position - transform.position, Vector3.up);
            float distance = offset.sqrMagnitude;

            if(distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestMonster = monster;
            }
        }

        return nearestMonster;
    }

    public Vector3 AutoMovement(bool isHandle)
    {
        if (isHandle) { Cancle(); return Vector3.zero; }

        if(isAttacking)
        {
            UpdateAttack();
            return Vector3.zero;
        }

        // 탐지 사거리
        bool isOutOfRange = true;
        if(target != null && target.IsDead == false)
        {
            isOutOfRange = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up).sqrMagnitude > (detectionRadius * detectionRadius);
        }

        if(target == null || target.IsDead || isOutOfRange)
        {
            target = FindTarget();
        }

        if (target == null)
            return Vector3.zero;

        Vector3 offset = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);

        if (offset.sqrMagnitude > (attackRange * attackRange))
            return offset.normalized;

        // 공격사거리 각도내에 부드럽게 위치하도록 회전
        if(offset.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(offset, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            // 각도 10도 이내
            if(Quaternion.Angle(transform.rotation, targetRotation) > 10)
            {
                return Vector3.zero;
            }
        }

        // 공격사거리 안
        // 공격 시도
        if(Time.time >= nextAttackTime)
            StartAttack(offset);

        return Vector3.zero;
    }

    private void StartAttack(Vector3 offset)
    {
        isAttacking = true;
        hitChecked = false;

        transform.rotation = Quaternion.LookRotation(offset, Vector3.up);

        animator.CrossFadeInFixedTime("Base Layer.AutoAttack", 0.05f, 0, 0f);
    }

    private void UpdateAttack()
    {
        AnimatorStateInfo state = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsTag("BasicAttack") == false)
            return;

        float progress = state.normalizedTime;

        if(hitChecked == false && progress >= hitFraction)
        {
            hitChecked = true;

            if (target != null && target.IsDead == false)
            {
                Vector3 offset = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);

                if (offset.sqrMagnitude <= (attackRange * attackRange))
                {
                    target.TakeDamage(attackDamage);
                }
            }
        }

        if (progress >= 1f)
            EndAttack();
    }

    private void EndAttack()
    {
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;

        animator.CrossFadeInFixedTime("Base Layer.Locomotion", 0.08f);
    }

    private void Cancle()
    {
        if(isAttacking)
            EndAttack();
    }
}
