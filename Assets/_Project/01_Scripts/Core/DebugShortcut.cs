using UnityEngine;

namespace GameProject.Core
{
    /// <summary>
    /// 게임 흐름을 통째로 건너뛰는 디버그 단축키(즉시 클리어 / 즉사)가 실수로 눌리지 않도록,
    /// 앞에 붙이는 조합키를 한 곳에서 정한다.
    ///
    /// <para>2026-08-25에 추가. 원래 이 기능들은 단일 키(L, M)였는데, 하필 실제 조작키인
    /// J(공격)·K(필살기 차지) 바로 옆자리라 손가락이 한 칸만 미끄러져도 스테이지가 끝나거나
    /// 캐릭터가 죽었다. 학생이 완성한 빌드를 남에게 보냈을 때 일어나기 쉬운 사고여서 조합키로
    /// 바꿨다. 빌드에서 빼지 않은 것은 의도적이다 - 교수님이 제출본을 채점할 때 매번 스테이지
    /// 끝까지 플레이하지 않아도 되게 하려는 것.</para>
    ///
    /// <para>애니메이션만 재생하고 흐름을 건너뛰지 않는 H(피격)/N(넉다운)은 단일 키 그대로다 -
    /// 학생이 자기 피격 그림을 확인할 때 자주 쓰는 키이고, 잘못 눌러도 잃을 것이 없다.</para>
    /// </summary>
    public static class DebugShortcut
    {
        /// <summary>
        /// Ctrl을 누르고 있는 상태인지. 좌우 어느 쪽 키든 인정한다.
        ///
        /// <para>2026-08-25에 조합을 두 번 바꿨다. 둘 다 실기 확인에서 걸린 것이라 기록해둔다.</para>
        ///
        /// <para>1) Ctrl+Shift → 안 됨. <b>Shift가 이 게임의 대시 키</b>여서, 디버그 키를 쓸 때마다
        /// 캐릭터가 같이 대시해버렸다.</para>
        ///
        /// <para>2) Ctrl+Alt → 반쪽만 됨. Ctrl+Alt+L은 되는데 <b>Ctrl+Alt+M이 안 들어왔다.</b>
        /// Windows에서 Ctrl+Alt는 AltGr과 같은 조합이라 글자에 따라 OS/IME 쪽에서 먹힌다.</para>
        ///
        /// <para>3) 그래서 <b>Ctrl 하나만</b>. 이 게임의 조작키는 A/D · Space · Shift · J · K와
        /// 마우스뿐이라 Ctrl은 비어 있고, 플레이 중 손이 갈 일도 없다. 조합을 늘리는 것보다
        /// <b>실제로 동작하는 것</b>이 우선이다.</para>
        /// </summary>
        public static bool ModifiersHeld =>
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }
}
