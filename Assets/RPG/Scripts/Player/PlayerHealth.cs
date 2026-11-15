using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float hp = 100;

    private bool isDead;
    public bool IsDead => isDead;

    public virtual void TakeDamage(float damage)
    {
        if (isDead == true || damage <= 0f) return;

        hp -= damage;

        if (hp <= 0f) Die();
    }

    private void Die()
    {
        Debug.Log("플레이어 사망");
    }
}
