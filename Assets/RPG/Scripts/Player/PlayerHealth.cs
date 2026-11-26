using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // 캐릭터 스테이터스
    private CharacterStatus status;
    private PlayerAuto autoAttack;
    private PlayerMovement playerMove;
    private CharacterController characterController;

    // 리스폰 지점
    [SerializeField] private Transform respawnPoint;

    private float hp;
    private bool isDead;
    public bool IsDead => isDead;
    public float CurrentHP => hp;

    private void Awake()
    {
        status = GetComponent<CharacterStatus>();
        if(status != null)
        {
            hp = status.MaxHp;
        }

        autoAttack = GetComponent<PlayerAuto>();
        playerMove = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Heal(status.HpRegen * Time.deltaTime);
    }

    private void Heal(float amount)
    {
        if (IsDead == true || amount <= 0f) return;

        hp = Mathf.Min(hp + amount, status.MaxHp);
    }

    public virtual void TakeDamage(CharacterStatus attacker)
    {
        if (isDead == true || attacker == null) return;

        DamageResult result = DamageCalculator.Calculate(attacker, status);

        hp -= result.Amount;

        //DamageTextManager.Instance.Show(result, new Vector3(
        //    characterController.bounds.center.x,
        //    characterController.bounds.max.y,
        //    characterController.bounds.center.z
        //    ));

        if (hp <= 0f) Die();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        autoAttack.Reset();
        playerMove.Reset();

        Respawn();
    }

    private void Respawn()
    {
        if (respawnPoint == null) return;

        characterController.enabled = false;

        transform.position = respawnPoint.position;

        characterController.enabled = true;

        hp = status.MaxHp;
        isDead = false;
    }
}
