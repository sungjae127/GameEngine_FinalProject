using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private int attackDamage = 20;

    private Collider2D hitboxCollider;
    private HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>();

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        // 활성화될 때 히트 목록 초기화
        hitEnemies.Clear();

        // 한 프레임 대기 후 겹쳐있는 적들을 감지
        StartCoroutine(CheckOverlappingEnemies());
    }

    private IEnumerator CheckOverlappingEnemies()
    {
        yield return null; // 한 프레임 대기

        if (hitboxCollider != null)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                hitboxCollider.bounds.center,
                hitboxCollider.bounds.size,
                0f
            );

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Enemy") && !hitEnemies.Contains(hit))
                {
                    DealDamage(hit);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적을 때렸을 때 (이미 맞은 적은 제외)
        if (other.CompareTag("Enemy") && !hitEnemies.Contains(other))
        {
            DealDamage(other);
        }
    }

    private void DealDamage(Collider2D other)
    {
        // 이미 맞은 적은 다시 맞지 않음
        hitEnemies.Add(other);

        Debug.Log("Hit Enemy: " + other.name);

        // 적에게 데미지 전달
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage);
        }
    }
}
