using UnityEngine;

public class TurretShooter : MonoBehaviour
{
    [Header("Stats")]
    public float range = 8f;
    public float fireRate = 0.65f;
    public float bulletSpeed = 10f;
    public float bulletDamage = 20f;
    public bool isActive = false;
    public TurretAttackMode attackMode = TurretAttackMode.Bullet;

    [Header("References")]
    public GameObject bulletPrefab;
    public Transform rotatingPart;
    public Transform muzzlePoint;
    public SpriteRenderer turretRenderer;

    [Header("Visuals")]
    public Sprite idleSprite;
    public Sprite[] idleSprites;
    public Sprite[] firingSprites;
    public float idleAnimationFps = 3f;
    public float muzzleOffset = 0.45f;
    public float fireFrameTime = 0.05f;
    public Sprite projectileSprite;
    public Color projectileColor = Color.white;
    public float projectileScale = 0.45f;

    [Header("Projectile Effects")]
    public int pierceCount = 0;
    public float knockbackForce = 0f;
    public float burnDamagePerSecond = 0f;
    public float burnDuration = 0f;
    public float slowMultiplier = 1f;
    public float slowDuration = 0f;

    private float nextFireTime;
    private float fireAnimationTimer;
    private float idleAnimationTimer;
    private Sprite fallbackIdleSprite;

    void Awake()
    {
        AutoWireReferences();
        ApplyIdleSprite();
    }

    void Update()
    {
        UpdateVisualAnimation();

        if (!isActive)
            return;

        EnemyBase target = FindNearestEnemy();
        if (target == null)
            return;

        if (attackMode != TurretAttackMode.SonicPulse)
            AimAt(target.transform.position);

        if (Time.time < nextFireTime)
            return;

        Shoot(target);
        nextFireTime = Time.time + fireRate;
    }

    public void Activate()
    {
        isActive = true;
    }

    public void ConfigureVisuals(SpriteRenderer renderer, Sprite idle, Sprite[] fireSprites)
    {
        ConfigureVisuals(renderer, idle != null ? new[] { idle } : null, fireSprites);
    }

    public void ConfigureVisuals(SpriteRenderer renderer, Sprite[] newIdleSprites, Sprite[] fireSprites)
    {
        if (renderer != null)
        {
            turretRenderer = renderer;
            rotatingPart = renderer.transform;
        }

        if (newIdleSprites != null && newIdleSprites.Length > 0)
        {
            idleSprites = newIdleSprites;
            idleSprite = newIdleSprites[0];
        }

        if (fireSprites != null && fireSprites.Length > 0)
            firingSprites = fireSprites;

        AutoWireReferences();
        ApplyIdleSprite();
    }

    public void ConfigureAttack(TurretAttackMode mode, float newRange, float newFireRate, float newDamage, float newBulletSpeed,
        Sprite newProjectileSprite, Color newProjectileColor, float newProjectileScale, int newPierceCount,
        float newKnockbackForce, float newBurnDamagePerSecond, float newBurnDuration, float newSlowMultiplier, float newSlowDuration)
    {
        attackMode = mode;
        range = newRange;
        fireRate = Mathf.Max(0.08f, newFireRate);
        bulletDamage = newDamage;
        bulletSpeed = newBulletSpeed;
        projectileSprite = newProjectileSprite;
        projectileColor = newProjectileColor;
        projectileScale = Mathf.Max(0.05f, newProjectileScale);
        pierceCount = Mathf.Max(0, newPierceCount);
        knockbackForce = Mathf.Max(0f, newKnockbackForce);
        burnDamagePerSecond = Mathf.Max(0f, newBurnDamagePerSecond);
        burnDuration = Mathf.Max(0f, newBurnDuration);
        slowMultiplier = Mathf.Clamp(newSlowMultiplier, 0.1f, 1f);
        slowDuration = Mathf.Max(0f, newSlowDuration);
    }

    void AutoWireReferences()
    {
        if (rotatingPart == null)
        {
            Transform body = transform.Find("Body");
            rotatingPart = body != null ? body : transform;
        }

        if (turretRenderer == null && rotatingPart != null)
            turretRenderer = rotatingPart.GetComponent<SpriteRenderer>();

        if (turretRenderer == null)
            turretRenderer = GetComponent<SpriteRenderer>();

        if (muzzlePoint == null && rotatingPart != null)
            muzzlePoint = rotatingPart.Find("MuzzlePoint");

        if (turretRenderer != null && fallbackIdleSprite == null)
            fallbackIdleSprite = turretRenderer.sprite;
    }

    EnemyBase FindNearestEnemy()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase nearest = null;
        float nearestSqrDistance = range * range;

        foreach (EnemyBase enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    void AimAt(Vector3 targetPosition)
    {
        if (rotatingPart == null)
            return;

        Vector2 direction = targetPosition - rotatingPart.position;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        rotatingPart.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Shoot(EnemyBase target)
    {
        if (attackMode == TurretAttackMode.SonicPulse)
        {
            SonicPulse();
            StartFireAnimation();
            return;
        }

        EnsureBulletPrefab();
        if (bulletPrefab == null)
            return;

        Vector3 origin = GetMuzzlePosition();
        Vector2 direction = (target.transform.position - origin).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.Euler(0f, 0f, angle));

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.Configure(bulletDamage, range, 1f, 1f, pierceCount, knockbackForce,
                burnDamagePerSecond, burnDuration, slowMultiplier, slowDuration);

        SpriteRenderer bulletRenderer = bullet.GetComponent<SpriteRenderer>();
        if (bulletRenderer != null)
        {
            if (projectileSprite != null)
                bulletRenderer.sprite = projectileSprite;

            bulletRenderer.color = projectileColor;
            bulletRenderer.transform.localScale = Vector3.one * projectileScale;
        }

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * bulletSpeed;

        Destroy(bullet, 3f);
        StartFireAnimation();
    }

    void SonicPulse()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (Collider2D hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null)
                continue;

            enemy.TakeDamage(bulletDamage);

            if (slowMultiplier < 1f && slowDuration > 0f)
                enemy.ApplySlow(slowMultiplier, slowDuration);

            Vector2 away = (enemy.transform.position - transform.position).normalized;
            if (knockbackForce > 0f)
                enemy.ApplyKnockback(away, knockbackForce);
        }

        StartCoroutine(PulseFlash());
    }

    System.Collections.IEnumerator PulseFlash()
    {
        if (rotatingPart == null)
            yield break;

        Vector3 originalScale = rotatingPart.localScale;
        rotatingPart.localScale = originalScale * 1.25f;
        yield return new WaitForSeconds(0.08f);
        rotatingPart.localScale = originalScale;
    }

    void EnsureBulletPrefab()
    {
        if (bulletPrefab != null)
            return;

        SoulGun soulGun = FindFirstObjectByType<SoulGun>();
        if (soulGun != null)
            bulletPrefab = soulGun.bulletPrefab;
    }

    Vector3 GetMuzzlePosition()
    {
        if (muzzlePoint != null)
            return muzzlePoint.position;

        if (rotatingPart != null)
            return rotatingPart.position + rotatingPart.up * muzzleOffset;

        return transform.position;
    }

    void StartFireAnimation()
    {
        if (turretRenderer == null)
            return;

        if (firingSprites == null || firingSprites.Length == 0)
        {
            if (attackMode != TurretAttackMode.SonicPulse)
                StartCoroutine(PulseFlash());
            return;
        }

        fireAnimationTimer = fireFrameTime * firingSprites.Length;
        turretRenderer.sprite = firingSprites[0];
    }

    void UpdateVisualAnimation()
    {
        if (turretRenderer == null)
            return;

        if (fireAnimationTimer > 0f)
        {
            fireAnimationTimer -= Time.deltaTime;
            if (fireAnimationTimer <= 0f)
            {
                ApplyIdleSprite();
                return;
            }

            if (firingSprites == null || firingSprites.Length == 0)
                return;

            float elapsed = (fireFrameTime * firingSprites.Length) - fireAnimationTimer;
            int fireFrame = Mathf.Clamp(Mathf.FloorToInt(elapsed / fireFrameTime), 0, firingSprites.Length - 1);
            turretRenderer.sprite = firingSprites[fireFrame];
            return;
        }

        if (idleSprites == null || idleSprites.Length == 0)
            return;

        idleAnimationTimer += Time.deltaTime;
        int idleFrame = Mathf.FloorToInt(idleAnimationTimer * Mathf.Max(1f, idleAnimationFps)) % idleSprites.Length;
        if (idleSprites[idleFrame] != null)
            turretRenderer.sprite = idleSprites[idleFrame];
    }

    void ApplyIdleSprite()
    {
        if (turretRenderer == null)
            return;

        if (idleSprites != null && idleSprites.Length > 0 && idleSprites[0] != null)
            turretRenderer.sprite = idleSprites[0];
        else if (idleSprite != null)
            turretRenderer.sprite = idleSprite;
        else if (fallbackIdleSprite != null)
            turretRenderer.sprite = fallbackIdleSprite;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}

public enum TurretAttackMode { Bullet, SonicPulse }
