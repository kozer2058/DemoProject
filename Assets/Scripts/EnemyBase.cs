using System.Collections;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public float maxHP = 50f;
    public float currentHP;
    public float moveSpeed = 1f;
    public float damage = 20f;
    public float coreAttackInterval = 2f;

    [Header("Soul Drop")]
    public SoulType soulDropType = SoulType.Speed;
    public int soulDropCount = 1;
    public GameObject soulPrefab;
    public float xpReward = 5f;

    [Header("Visual Animation")]
    public Sprite[] walkSprites;
    public Sprite[] deathSprites;
    public float walkAnimationFps = 6f;
    public float deathFrameDuration = 0.14f;

    [Header("Detection Settings")]
    public float detectionRadius = 5f; // Bán kính phát hiện mục tiêu
    public LayerMask targetLayer;      // Layer chứa Player và Công trình
    public float scanInterval = 0.2f;   // Thời gian giãn cách giữa các lần quét (giúp mượt game)

    protected Transform targetTransform;

    private float baseMoveSpeed;
    private Coroutine slowRoutine;
    private Coroutine burnRoutine;
    private Coroutine knockbackRoutine;
    private bool isDead;
    private EnergyCore attackedCore;
    private float nextCoreAttackTime;
    private SpriteRenderer visualRenderer;
    private float walkAnimationTime;
    private Rigidbody2D physicsBody;
    private MapBounds2D mapBounds;
    private float nextScanTime;

    protected bool IsDead => isDead;

    private PlayerStats attackedPlayer; // <-- THÊM DÒNG NÀY: Lưu trữ Player đang va chạm
    private float nextAttackTime;       // <-- ĐỔI TÊN: Dùng chung thời gian hồi đòn cho cả Core và Player

    protected virtual void Start()
    {
        currentHP = maxHP;
        baseMoveSpeed = moveSpeed;
        visualRenderer = GetComponent<SpriteRenderer>();
        physicsBody = GetComponent<Rigidbody2D>();
        mapBounds = FindFirstObjectByType<MapBounds2D>();

        if (physicsBody != null)
        {
            physicsBody.constraints |= RigidbodyConstraints2D.FreezeRotation;
            physicsBody.linearVelocity = Vector2.zero;
            physicsBody.angularVelocity = 0f;
        }

        IgnoreCrowdCollisions();

        // Mặc định lúc xuất hiện, tìm Core/Player ở xa trước
        FindDefaultTarget();
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        StopPhysicsDrift();
        UpdateWalkAnimation();

        // Chủ động quét mục tiêu xung quanh theo chu kỳ thời gian
        if (Time.time >= nextScanTime)
        {
            ScanForTargets();
            nextScanTime = Time.time + scanInterval;
        }

        if (attackedCore != null)
        {
            TryAttackTargets();
            ClampToMapBounds();
            return;
        }

        MoveTowardTarget();
        ClampToMapBounds();
    }

    protected virtual void MoveTowardTarget()
    {
        if (targetTransform == null || attackedCore != null)
            return;

        Vector2 dir = (targetTransform.position - transform.position).normalized;
        transform.Translate(dir * moveSpeed * Time.deltaTime);
    }

    // Cơ chế quét mục tiêu bằng Code thay thế cho Trigger Zone
    private void ScanForTargets()
    {
        // Quét tất cả các Collider nằm trong vòng tròn bán kính detectionRadius thuộc targetLayer
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, targetLayer);

        if (hitColliders.Length == 0)
        {
            // Nếu không có ai ở gần, quay lại đuổi theo mục tiêu mặc định từ xa
            if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
            {
                FindDefaultTarget();
            }
            return;
        }

        Transform closestTarget = null;
        float minDistance = float.MaxValue;

        foreach (var col in hitColliders)
        {
            // Lọc đúng Tag mong muốn
            if (col.CompareTag("Player") || col.CompareTag("EnergyCore"))
            {
                float distance = Vector2.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTarget = col.transform;
                }
            }
        }

        // Nếu tìm thấy đối tượng gần nhất ở trong tầm, đổi mục tiêu sang đối tượng đó
        if (closestTarget != null)
        {
            targetTransform = closestTarget;
        }
    }

    private void FindDefaultTarget()
    {
        GameObject core = GameObject.FindWithTag("EnergyCore");
        if (core != null)
        {
            targetTransform = core.transform;
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            targetTransform = player.transform;
    }

    public virtual void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHP -= amount;
        StartCoroutine(DamageFlash());

        if (currentHP <= 0f)
            Die();
    }

    public void ApplyBurn(float damagePerSecond, float duration)
    {
        if (burnRoutine != null)
            StopCoroutine(burnRoutine);

        burnRoutine = StartCoroutine(BurnRoutine(damagePerSecond, duration));
    }

    public void ApplySlow(float multiplier, float duration)
    {
        if (slowRoutine == null)
            baseMoveSpeed = moveSpeed;

        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(SlowRoutine(multiplier, duration));
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction.normalized, force));
    }

    public void ApplyDifficulty(float healthMultiplier, float damageMultiplier, float speedMultiplier)
    {
        maxHP *= healthMultiplier;
        currentHP = maxHP;
        damage *= damageMultiplier;
        moveSpeed *= speedMultiplier;
        baseMoveSpeed = moveSpeed;
        xpReward *= Mathf.Lerp(1f, healthMultiplier, 0.5f);
    }

    public void ConfigureElite(string enemyName, float healthMultiplier, float damageMultiplier,
        float speedMultiplier, float scaleMultiplier, Color color, int extraSoulDrops)
    {
        gameObject.name = enemyName;
        ApplyDifficulty(healthMultiplier, damageMultiplier, speedMultiplier);
        transform.localScale *= scaleMultiplier;
        soulDropCount += extraSoulDrops;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = color;
    }

    protected virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;

        for (int i = 0; i < soulDropCount; i++)
        {
            Vector3 offset = (Vector3)Random.insideUnitCircle * 0.8f;
            Vector3 spawnPos = transform.position + offset;

            if (soulPrefab != null)
            {
                GameObject soul = Instantiate(soulPrefab, spawnPos, Quaternion.identity);
                SoulPickup pickup = soul.GetComponent<SoulPickup>();
                if (pickup != null)
                    pickup.soulType = soulDropType;
            }
        }

        GameObject player = GameObject.FindWithTag("Player");
        PlayerStats stats = player == null ? null : player.GetComponent<PlayerStats>();
        if (stats != null)
            stats.GainXP(xpReward);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.RegisterEnemyKilled(xpReward);

        foreach (Collider2D collider in GetComponents<Collider2D>())
            collider.enabled = false;

        StartCoroutine(DeathAnimationRoutine());
    }

    void UpdateWalkAnimation()
    {
        if (visualRenderer == null || walkSprites == null || walkSprites.Length == 0)
            return;

        walkAnimationTime += Time.deltaTime;
        int frame = Mathf.FloorToInt(walkAnimationTime * Mathf.Max(1f, walkAnimationFps)) % walkSprites.Length;
        if (walkSprites[frame] != null)
            visualRenderer.sprite = walkSprites[frame];
    }

    IEnumerator DeathAnimationRoutine()
    {
        if (visualRenderer != null && deathSprites != null && deathSprites.Length > 0)
        {
            foreach (Sprite frame in deathSprites)
            {
                if (frame != null)
                    visualRenderer.sprite = frame;

                yield return new WaitForSeconds(Mathf.Max(0.04f, deathFrameDuration));
            }
        }
        else
        {
            float elapsed = 0f;
            const float duration = 0.3f;
            Vector3 startScale = transform.localScale;
            Color startColor = visualRenderer != null ? visualRenderer.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, progress);

                if (visualRenderer != null)
                {
                    Color color = startColor;
                    color.a = 1f - progress;
                    visualRenderer.color = color;
                }

                yield return null;
            }
        }

        Destroy(gameObject);
    }

    IEnumerator BurnRoutine(float damagePerSecond, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && currentHP > 0f)
        {
            TakeDamage(damagePerSecond * 0.5f);
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        burnRoutine = null;
    }

    IEnumerator SlowRoutine(float multiplier, float duration)
    {
        moveSpeed = baseMoveSpeed * Mathf.Clamp(multiplier, 0.1f, 1f);
        yield return new WaitForSeconds(duration);
        moveSpeed = baseMoveSpeed;
        slowRoutine = null;
    }

    IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        float timer = 0f;
        const float duration = 0.12f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.Translate(direction * force * Time.deltaTime, Space.World);
            ClampToMapBounds();
            yield return null;
        }

        knockbackRoutine = null;
    }

    protected void ClampToMapBounds()
    {
        mapBounds ??= FindFirstObjectByType<MapBounds2D>();
        if (mapBounds == null)
            return;

        Vector2 clamped = mapBounds.ClampPoint(transform.position, mapBounds.enemyPadding);
        transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
    }

    void StopPhysicsDrift()
    {
        if (physicsBody == null)
            return;

        physicsBody.linearVelocity = Vector2.zero;
        physicsBody.angularVelocity = 0f;
    }

    void IgnoreCrowdCollisions()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int playerLayer = LayerMask.NameToLayer("Player");

        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);

        if (enemyLayer >= 0 && playerLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyLayer, playerLayer, true);
    }

    IEnumerator DamageFlash()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            yield break;

        Color original = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        sr.color = original;
    }

void OnCollisionStay2D(Collision2D collision)
    {
        // 1. Xử lý va chạm với EnergyCore (Giữ nguyên logic cũ của bạn)
        if (collision.collider.CompareTag("EnergyCore"))
        {
            EnergyCore core = collision.collider.GetComponent<EnergyCore>();
            if (core != null)
            {
                attackedCore = core;
            }
        }

        // 2. THÊM MỚI: Xử lý va chạm với Player
        if (collision.collider.CompareTag("Player"))
        {
            PlayerStats player = collision.collider.GetComponent<PlayerStats>();
            if (player != null)
            {
                attackedPlayer = player;
            }
        }

        // Thực hiện tấn công bất kỳ mục tiêu nào đang bám dính
        TryAttackTargets();
    }

 void OnCollisionExit2D(Collision2D collision)
    {
        // Rời khỏi EnergyCore
        if (collision.collider.CompareTag("EnergyCore"))
        {
            EnergyCore core = collision.collider.GetComponent<EnergyCore>();
            if (core != null && core == attackedCore)
                attackedCore = null;
        }

        // THÊM MỚI: Rời khỏi Player
        if (collision.collider.CompareTag("Player"))
        {
            PlayerStats player = collision.collider.GetComponent<PlayerStats>();
            if (player != null && player == attackedPlayer)
                attackedPlayer = null;
        }
    }

    void TryAttackTargets()
    {
        if (Time.time < nextAttackTime)
            return;

        bool attackedSomething = false;

        // Nếu đang chạm Core -> Gây sát thương cho Core
        if (attackedCore != null)
        {
            attackedCore.TakeDamage(damage);
            attackedSomething = true;
        }
        
        // Nếu đang chạm Player -> Gây sát thương cho Player (dùng hàm TakeDamage có sẵn của bạn)
        else if (attackedPlayer != null)
        {
            attackedPlayer.TakeDamage(damage);
            attackedSomething = true;
        }

        // Nếu có tấn công (Core hoặc Player), bắt đầu tính thời gian hồi đòn tiếp theo
        if (attackedSomething)
        {
            nextAttackTime = Time.time + Mathf.Max(0.1f, coreAttackInterval);
        }
    }

    // Vẽ vòng tròn đỏ trong Editor giúp dễ căn chỉnh bán kính tầm nhìn
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}