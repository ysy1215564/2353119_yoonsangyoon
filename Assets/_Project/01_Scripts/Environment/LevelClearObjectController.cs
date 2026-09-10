using GameProject.Core;
using UnityEngine;

namespace GameProject.Environment
{
    /// <summary>
    /// 스테이지 끝(드래곤 입구 등)에 배치하는 1장짜리 클리어 오브젝트 - HP/사망 같은 별도 상호작용
    /// 없이, 플레이어의 공격(콤보/점프공격/스페셜 아무거나)에 한 번이라도 맞으면 그걸로 끝
    /// (PlayerActionTestController.DealDamageToMonsters에서 RegisterHit()을 호출).
    /// StageClearController가 이 값(WasHit)과 PlayerActionTestController.IsActionLocked의
    /// true→false 전환을 같이 봐서 "마지막 타격 동작이 끝나고 IDLE로 넘어가는 순간"에 클리어
    /// 화면을 띄운다.
    ///
    /// 디버그 단축키 Ctrl+L: 스테이지 끝까지 매번 걸어가지 않고도 클리어 플로우를 바로
    /// 테스트할 수 있게 - 실제로 맞은 것과 완전히 동일하게 처리(2026-08-12, 사용자 확정).
    ///
    /// 2026-08-25: 단일 L에서 Ctrl+L로 변경. 빌드에 남기는 것 자체는 그대로다(교수님이
    /// 제출본을 채점할 때 매번 끝까지 플레이하지 않아도 되게) - 문제는 자판 위치였다. 실제
    /// 조작키인 J(공격)/K(필살기 차지) 바로 오른쪽이 L이라, 필살기를 길게 누르다 한 칸 미끄러지면
    /// 스테이지가 그 자리에서 끝나버렸다. 학생이 친구에게 보낸 빌드에서 일어나기 쉬운 사고.
    /// 같은 이유로 PlayerActionTestController의 M(즉사)도 Ctrl+M로 바꿨다. H(피격)/N(넉다운)은
    /// 애니메이션만 재생하고 게임 흐름을 건너뛰지 않아 단일 키 그대로 둔다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class LevelClearObjectController : MonoBehaviour
    {
        private bool wasHit;

        public bool WasHit => wasHit;

        public void RegisterHit()
        {
            wasHit = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L) && DebugShortcut.ModifiersHeld)
            {
                RegisterHit();
            }
        }
    }
}
