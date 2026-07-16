using UnityEngine;
using System;

// Lưu máu, kinh nghiệm, cấp độ và các thay đổi chỉ số của người chơi.
public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP;

    [Header("Experience")]
    public float xpToNextLevel = 70f;
    [SerializeField] private float currentXP = 0f;
    [SerializeField] private int level = 1;

    // Các thuộc tính chỉ đọc để HUD lấy dữ liệu mà không sửa trực tiếp biến private.
    public float CurrentHP => currentHP;
    public float CurrentXP => currentXP;
    public int Level => level;
    public float HealthPercent => maxHP <= 0f ? 0f : currentHP / maxHP;
    public float XPPercent => xpToNextLevel <= 0f ? 0f : currentXP / xpToNextLevel;

    // Sự kiện thông báo cho LevelUpUpgradeUI mỗi khi người chơi tăng cấp.
    public event Action<int> LeveledUp;

    // Bắt đầu màn chơi với đầy máu.
    void Start()
    {
        currentHP = maxHP;
    }

    // Trừ máu nhưng không cho giá trị giảm xuống dưới 0.
    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(currentHP - amount, 0f);
        Debug.Log($"Player HP {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            Die();
    }

    // Cộng XP và cho phép lên nhiều cấp nếu nhận một lượng XP rất lớn.
    public void GainXP(float amount)
    {
        currentXP += amount;

        while (currentXP >= xpToNextLevel && xpToNextLevel > 0f)
            LevelUp();
    }

    // Trừ lượng XP đã dùng, tăng cấp và tăng yêu cầu XP của cấp kế tiếp 20%.
    void LevelUp()
    {
        currentXP -= xpToNextLevel;
        level++;
        xpToNextLevel *= 1.2f;
        Debug.Log($"Level up: {level}");
        LeveledUp?.Invoke(level);
    }

    // Nâng máu tối đa và đồng thời hồi đúng lượng máu vừa tăng.
    public void UpgradeMaxHealth(float amount)
    {
        maxHP += amount;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    // Hồi máu nhưng không vượt quá maxHP.
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    // Tạm dừng game khi người chơi hết máu.
    void Die()
    {
        Debug.Log("Game over: player died.");

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.LoseGame();
        else
            Time.timeScale = 0f;
    }
}
