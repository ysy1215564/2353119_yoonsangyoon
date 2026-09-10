using System.Collections;
using UnityEngine;

namespace GameProject.Environment
{
    /// <summary>
    /// 바닥에 배치하는 HP 회복 상자(2026-08-14) - 플레이어 공격(콤보/점프공격/스페셜 아무거나)에
    /// 한 번 맞으면 그걸로 끝(몬스터처럼 HP가 따로 있어 여러 번 때려야 하는 방식 아님, 사용자 확정)
    /// - PlayerActionTestController.DealDamageToMonsters에서 RegisterHit()을 호출한다
    /// (LevelClearObjectController와 동일한 패턴).
    ///
    /// 콜리더는 트리거로 둬서 플레이어/몬스터가 그냥 통과하지만(충돌 없음, 사용자 확정), 공격
    /// 판정(Physics2D.OverlapBoxAll)에는 트리거 여부와 상관없이 걸리므로 "타격은 가능"이라는
    /// 요구사항과 둘 다 만족한다.
    ///
    /// 리스폰 없음(사용자 확정) - 한 번 쓰면 그 자리에서 영구히 사라진다.
    ///
    /// HP 아이템(<see cref="HpItemPickup"/>)은 이 오브젝트의 자식으로, 여는 애니메이션이 시작되는
    /// 순간부터 이미 그 자리에 존재한다 - 상자보다 낮은 Sorting Order로 "뒤에 배치"돼서, 상자
    /// 프레임이 진행되며 생기는 빈틈 사이로 자연스럽게 보이기 시작한다(별도의 "짠 등장" 연출 코드
    /// 없이 스프라이트 레이어링만으로 사용자가 원한 "열리는 순간부터 뒤에 있는 게 보임" 효과를 얻음,
    /// 아트가 실제로 열림/파열 프레임이라 이 방식이 자연스럽게 맞아떨어짐).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HpItemBoxController : MonoBehaviour
    {
        [Header("Frames")]
        [Tooltip("여는/부서지는 프레임 - 04_Art/StudentReplace/Items/HpBox_frames 폴더 스캔 결과, 개수 자유.")]
        [SerializeField] private Sprite[] openFrames = new Sprite[0];
        [SerializeField, Min(1f)] private float fps = 8f;

        [Header("Item")]
        [SerializeField] private HpItemPickup item;
        [Tooltip("이 상자가 채워주는 HP 칸 수 - 기본 1, 배치한 상자마다 개별 조절 가능.")]
        [SerializeField, Min(1)] private int healAmount = 1;

        private SpriteRenderer spriteRenderer;
        private Collider2D hitCollider;
        private bool triggered;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            hitCollider = GetComponent<Collider2D>();
            if (openFrames != null && openFrames.Length > 0)
            {
                spriteRenderer.sprite = openFrames[0];
            }
        }

        /// <summary>PlayerActionTestController.DealDamageToMonsters가 호출 - 두 번째 히트부터는
        /// 이미 열리는 중이라 무시(콤보 연타 한 번에 여러 번 안 열림).</summary>
        public void RegisterHit()
        {
            if (triggered)
            {
                return;
            }

            triggered = true;
            if (hitCollider != null)
            {
                hitCollider.enabled = false; // 재타격 자체를 원천 차단 - 애니메이션 도중 판정에서도 안전
            }

            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            var delay = 1f / Mathf.Max(1f, fps);
            if (openFrames != null)
            {
                foreach (var frame in openFrames)
                {
                    spriteRenderer.sprite = frame;
                    yield return new WaitForSeconds(delay);
                }
            }

            spriteRenderer.enabled = false; // 상자 자체는 이제 안 보임 - 아이템만 남는다.

            if (item != null)
            {
                item.BeginPickupSequence(healAmount);
            }

            Destroy(gameObject, 0.1f); // 아이템(별개 오브젝트)에는 영향 없음, 빈 상자 껍데기만 정리.
        }
    }
}
