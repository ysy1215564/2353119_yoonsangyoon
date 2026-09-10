namespace GameProject.Core
{
    /// <summary>
    /// 범용 "월드 얼리기" 신호 - 게임플레이 오브젝트(플레이어, 몬스터A/B/C)가 매 프레임 이 값을
    /// 읽고, true면 조작/AI/피격 판정을 멈추고 그 자리에서 IDLE 포즈로 고정한다.
    /// Time.timeScale과 달리 UI 애니메이션, 코루틴의 실제 시간 흐름, 파티클(WeatherFX 등)은 전혀
    /// 건드리지 않는다 - 이 값을 직접 확인하는 스크립트만 영향을 받는다.
    ///
    /// 2026-08-12 스테이지 클리어 화면(StageClearController)에서 처음 사용 - "클리어 BGM이 나오는
    /// 동안 몬스터/플레이어는 IDLE로 멈춰 보이되, 눈 내리는 연출 같은 건 계속 움직여야 한다"는
    /// 요구사항 때문에 Time.timeScale=0 대신 이 방식을 선택했다(사용자 확정). 이름/구조를 스테이지
    /// 클리어 전용이 아니라 범용으로 잡아둔 이유는, 나중에 일시정지 메뉴를 만들 때도 같은 게이트를
    /// 재사용할 수 있을 것으로 보여서(사용자 확정).
    ///
    /// static이라 씬을 넘어가도 값이 그대로 남는다 - 각 캐릭터 컨트롤러(Player/MonsterA/B/C)의
    /// Awake()에서 항상 Reset()을 호출해, 재도전/재시작했는데 처음부터 얼어있는 사고를 막는다.
    /// </summary>
    public static class GameplayFreezeGate
    {
        public static bool IsFrozen { get; private set; }

        public static void Freeze()
        {
            IsFrozen = true;
        }

        /// <summary>새 플레이 세션이 시작되는 시점(플레이어 Awake 등)에 항상 호출 - 이전 씬/이전
        /// 플레이에서 얼린 채로 남아있는 상태가 새 세션까지 이어지는 걸 막는다.</summary>
        public static void Reset()
        {
            IsFrozen = false;
        }
    }
}
