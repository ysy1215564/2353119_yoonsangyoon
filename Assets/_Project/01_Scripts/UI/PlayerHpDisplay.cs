using GameProject.Player;
using UnityEngine;
using UnityEngine.UI;

namespace GameProject.UI
{
    /// <summary>
    /// Pip 방식(Pip-based) 주인공 HP 표시 - PlayerActionTestController.MaxHp/CurrentHp를 그대로
    /// 따라가는 Dot(켜짐/꺼짐) 한 줄. 칸 수는 이 컴포넌트가 아니라 PlayerActionTestController 쪽
    /// maxHp 하나만 진짜 값(single source of truth)이라, 둘이 따로 놀 일이 없다.
    ///
    /// 설계 의도(2026-08-11 확정):
    ///  - dotGroup 자신의 Rect Transform(위치/크기)만 학생이 자유롭게 조절 대상 - hp_background.png
    ///    디자인이 바뀌어도 이 그룹 하나만 옮기면 맞출 수 있게.
    ///  - Dot 사이 간격(DotSpacing)은 코드에 고정값으로 박아서 인스펙터에 아예 안 보이게 함 -
    ///    hp_background.png 우측 페이드아웃 폭에 맞춰 디자인된 간격이라 실수로 못 건드리게.
    ///  - [ExecuteAlways]라서 Play 모드가 아니어도(에디터에서 maxHp를 바꾸는 즉시) Dot이 다시
    ///    배치됨 - 별도 "Refresh" 버튼 없이 항상 최신 상태로 보임.
    /// </summary>
    [ExecuteAlways]
    public class PlayerHpDisplay : MonoBehaviour
    {
        private const float DotSpacing = 40f; // hp_dot 35px + 여백 - 그룹 내부 고정값(인스펙터 비노출)
        private const int FallbackMaxHp = 5; // playerController가 아직 안 붙었을 때(에디터 미리보기용)

        [SerializeField] private RectTransform dotGroup;
        [SerializeField] private RectTransform background;
        [SerializeField] private Sprite dotOnSprite;
        [SerializeField] private Sprite dotOffSprite;
        [SerializeField] private PlayerActionTestController playerController;

        private Image[] dots = new Image[0];
        private int builtForMaxHp = -1;

        /// <summary>hp_background.png의 Rect Transform - HP 아이템이 날아가는 목표 지점으로
        /// HpItemPickup이 참조한다(2026-08-14). "배경 이미지 중앙"이 곧 이 RectTransform의
        /// position이라 별도 좌표 계산 없이 그대로 쓸 수 있음.</summary>
        public RectTransform BackgroundRect => background;

        private void OnEnable()
        {
            RebuildIfNeeded();
        }

        // OnValidate()에서는 일부러 Rebuild를 안 부른다 - Unity가 OnValidate 안에서
        // DestroyImmediate 호출을 안전하지 않다고 명시적으로 경고하기 때문. [ExecuteAlways] 덕분에
        // Update()가 Edit 모드에서도 매 프레임 돌아서, 필드를 바꾸면 다음 틱에 바로 재배치된다 -
        // 체감상 딜레이는 없음.

        private void Update()
        {
            ForceRefresh();
        }

        /// <summary>
        /// 매 프레임 Update()가 부르는 것과 같은 갱신 로직을 즉시(동기적으로) 실행 - 에디터 도구가
        /// Max Hp 같은 값을 스크립트로 바꾼 직후, Update()가 다음 틱까지 도는 걸 기다리지 않고 바로
        /// 칸 수/채움 상태를 맞춰서 그 상태 그대로 씬을 저장할 수 있게 public으로 열어둠.
        /// </summary>
        public void ForceRefresh()
        {
            RefreshPlayerControllerReference();
            RebuildIfNeeded();
            Refresh(DisplayedCurrentHp());
        }

        /// <summary>
        /// ActionTest 씬은 Play 모드에서 "Player_ActionTest_Runtime" 같은 별도 런타임 복제본을
        /// 새로 만들고 원본(Player_ActionTest)은 비활성화하는 구조라(2026-08-11 확인) - 에디터에서
        /// 미리 연결해둔 playerController가 그 비활성화된 원본을 계속 가리키면 전투를 겪는 진짜
        /// 인스턴스와 완전히 딴 데를 보게 된다(칸 수는 맞아도 체력 변화가 전혀 안 보이던 원인).
        /// 지금 가리키는 대상이 없거나 비활성화됐으면, 활성 상태인 진짜 플레이어를 다시 찾는다
        /// (FindObjectOfType 기본값은 활성 오브젝트만 찾음 - 비활성 원본은 자동으로 걸러짐).
        /// </summary>
        private void RefreshPlayerControllerReference()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (playerController != null && playerController.isActiveAndEnabled)
            {
                return;
            }

            var found = Object.FindObjectOfType<PlayerActionTestController>();
            if (found != playerController)
            {
                playerController = found;
                builtForMaxHp = -1; // 새 인스턴스 기준으로 칸 수부터 다시 맞추도록 강제 재생성
            }
        }

        private void RebuildIfNeeded()
        {
            var targetMaxHp = playerController != null ? Mathf.Max(1, playerController.MaxHp) : FallbackMaxHp;
            if (targetMaxHp == builtForMaxHp)
            {
                return;
            }

            Rebuild(targetMaxHp);
        }

        /// <summary>
        /// PlayerActionTestController.currentHp는 [SerializeField]가 아니라서(런타임 전투 상태라
        /// 저장할 이유가 없음) Edit 모드에서는 항상 0으로 읽힌다 - 씬 빌더 도구가 Edit 모드에서
        /// 이 컴포넌트를 만들 때 그 값을 그대로 썼더니 "전부 빈 칸"이 씬 파일에 그대로 저장되는
        /// 사고가 있었다(2026-08-11). Play 모드가 아닐 때는 실제 값 대신 MaxHp(꽉 찬 상태)를
        /// 미리보기로 보여준다.
        /// </summary>
        private int DisplayedCurrentHp()
        {
            if (playerController == null)
            {
                return FallbackMaxHp;
            }

            return Application.isPlaying ? playerController.CurrentHp : playerController.MaxHp;
        }

        private void Rebuild(int maxHp)
        {
            if (dotGroup == null)
            {
                return;
            }

            ClearDots();

            var dotSize = dotOnSprite != null
                ? new Vector2(dotOnSprite.rect.width, dotOnSprite.rect.height)
                : new Vector2(35f, 35f);

            dots = new Image[maxHp];
            for (var i = 0; i < maxHp; i++)
            {
                var dotObject = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image));
                dotObject.transform.SetParent(dotGroup, false);

                var rect = dotObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = dotSize;
                rect.anchoredPosition = new Vector2(dotSize.x * 0.5f + i * DotSpacing, 0f);

                var image = dotObject.GetComponent<Image>();
                image.raycastTarget = false;
                dots[i] = image;
            }

            builtForMaxHp = maxHp;
            Refresh(DisplayedCurrentHp());
        }

        private void ClearDots()
        {
            if (dotGroup == null)
            {
                return;
            }

            for (var i = dotGroup.childCount - 1; i >= 0; i--)
            {
                var child = dotGroup.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void Refresh(int currentHp)
        {
            if (dots == null)
            {
                return;
            }

            for (var i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null)
                {
                    continue;
                }

                dots[i].sprite = i < currentHp ? dotOnSprite : dotOffSprite;
            }
        }
    }
}
