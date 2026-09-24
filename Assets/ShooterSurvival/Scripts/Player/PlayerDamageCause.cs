namespace IndianOceanAssets.ShooterSurvival
{
    public enum PlayerDamageCause { None, EnemyContact, GuardShot, Crate, Cannon, Seagull, Hole, Pole, Traffic, Roadblock, Barrel, NegativeBonus, Other, EnemyProjectile, TollGate, Paddle }
    public static class PlayerDamageCauseText
    {
        public static string Label(PlayerDamageCause cause) => cause switch
        {
            PlayerDamageCause.EnemyContact=>"적과 충돌", PlayerDamageCause.GuardShot=>"경비원 탄환",
            PlayerDamageCause.Crate=>"뚱보 상자",PlayerDamageCause.Cannon=>"배의 포탄",PlayerDamageCause.Seagull=>"갈매기와 충돌",
            PlayerDamageCause.Hole=>"바닥 구멍",PlayerDamageCause.Pole=>"가로등과 충돌",PlayerDamageCause.Traffic=>"차량과 충돌",
            PlayerDamageCause.Roadblock=>"도로 장애물",PlayerDamageCause.Barrel=>"통 폭발",PlayerDamageCause.NegativeBonus=>"체력 감소 장벽",
            PlayerDamageCause.EnemyProjectile=>"적의 투척물",
            PlayerDamageCause.TollGate=>"닫힌 차단기",
            PlayerDamageCause.Paddle=>"노에 맞음",
            _=>"체력 감소"
        };
        public static string Advice(PlayerDamageCause cause) => cause switch
        {
            PlayerDamageCause.EnemyContact=>"남은 체력이 높은 적은 접촉 전에 처치하거나 피하세요.",
            PlayerDamageCause.Crate or PlayerDamageCause.GuardShot or PlayerDamageCause.Cannon or PlayerDamageCause.EnemyProjectile=>"발사 동작을 보고 일찍 옆으로 피하세요.",
            PlayerDamageCause.Traffic or PlayerDamageCause.Roadblock=>"주황색 위험 표시를 보고 빈 차로로 이동하세요.",
            PlayerDamageCause.TollGate=>"초록불이 켜지고 차단기가 올라간 통로로 이동하세요.",
            PlayerDamageCause.Paddle=>"노를 휘두르는 쪽에서 미리 떨어지세요.",
            PlayerDamageCause.Seagull=>"검은 착지 표시가 없는 쪽으로 피하세요.",
            PlayerDamageCause.Hole or PlayerDamageCause.Pole=>"앞길의 빈 공간을 먼저 확보하세요.",
            _=>"이번 판의 체력과 공격력을 보고 다음 강화를 골라보세요."
        };
    }
}
