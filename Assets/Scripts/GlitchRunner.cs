using UnityEngine;

public class GlitchRunner : EnemyBase
{
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float flickerTimer;

    protected override void Start()
    {
        maxHP = 25f;
        moveSpeed = 2.6f;
        damage = 5f;
        soulDropType = SoulType.Speed;
        soulDropCount = 1;
        xpReward = 3f;

        base.Start();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
            spriteRenderer.enabled = true;
            spriteRenderer.color = baseColor;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead)
            return;

        UpdateVisualFlicker();
    }

    void UpdateVisualFlicker()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.enabled = true;

        if (flickerTimer > 0f)
        {
            flickerTimer -= Time.deltaTime;
            float pulse = Mathf.PingPong(Time.time * 28f, 1f);
            spriteRenderer.color = Color.Lerp(baseColor, new Color(0.72f, 0.95f, 1f, 0.85f), pulse);
            return;
        }

        spriteRenderer.color = baseColor;

        if (Random.value < 0.006f)
            flickerTimer = 0.08f;
    }
}
