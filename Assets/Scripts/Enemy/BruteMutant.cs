public class BruteMutant : EnemyBase
{
    protected override void Start()
    {
        maxHP = 150f;
        moveSpeed = 0.9f;
        damage = 25f;
        soulDropType = SoulType.Power;
        soulDropCount = 3;
        xpReward = 20f;

        base.Start();
    }
}
