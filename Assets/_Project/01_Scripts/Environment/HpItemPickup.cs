using System.Collections;
using GameProject.Audio;
using GameProject.Player;
using GameProject.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameProject.Environment
{
    /// <summary>
    /// HP 회복 아이템(하트/물약 등, 2026-08-14) - HpItemBoxController가 여는 애니메이션을 끝내면
    /// BeginPickupSequence()를 호출해서 시작된다. 흐름(사용자 확정 스펙):
    /// 1) 그 자리에서 showDuration(기본 1초)만큼 보여짐(월드 스프라이트, 카메라가 움직이면 같이 움직임)
    /// 2) 화면상 현재 위치를 그대로 UI 아이콘으로 바꿔치기해서 HP UI 배경 중앙까지 flightDuration
    ///    (기본 0.45초) 동안 날아감
    /// 3) 도착하면 사라지고 PlayerActionTestController.Heal() 호출 - 이미 꽉 찬 상태면 Heal() 쪽에서
    ///    알아서 무시(사용자 스펙 5번), 남은 칸보다 많이 채우는 값이어도 최대치를 안 넘음(스펙 6번).
    ///
    /// UI로 바꿔치기하는 이유: 이 프로젝트는 카메라가 플레이어를 따라 계속 움직이는 구조라
    /// (CameraFollow2D), 월드 오브젝트를 그대로 UI 쪽 좌표로 이동시키면 날아가는 도중 카메라가
    /// 움직일 때 목표 지점(고정된 화면 위치)이 매 프레임 다시 계산돼야 해서 다루기 까다롭다.
    /// 대신 "날아가기 시작하는 바로 그 순간"의 화면 좌표를 한 번만 계산해서 UI 캔버스 위 아이콘으로
    /// 변환한 뒤부터는 순수 화면 좌표(스크린 스페이스) 안에서만 움직이므로, 그 이후 카메라가 어떻게
    /// 움직이든 전혀 영향을 안 받는다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HpItemPickup : MonoBehaviour
    {
        [Tooltip("상자가 열린 뒤 이 자리에서 가만히 보여지는 시간(초).")]
        [SerializeField, Min(0f)] private float showDuration = 1f;
        [Tooltip("HP UI까지 날아가는 데 걸리는 시간(초).")]
        [SerializeField, Min(0.05f)] private float flightDuration = 0.45f;
        [Tooltip("HP UI 방향으로 날아가기 시작하는 순간 1회 재생되는 효과음.")]
        [SerializeField] private SfxCue flySfx;

        private SpriteRenderer spriteRenderer;
        private SfxPlayer sfxPlayer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            sfxPlayer = new SfxPlayer(transform);
        }

        public void BeginPickupSequence(int healAmount)
        {
            StartCoroutine(PickupRoutine(healAmount));
        }

        private IEnumerator PickupRoutine(int healAmount)
        {
            yield return new WaitForSeconds(showDuration);

            var playerController = FindObjectOfType<PlayerActionTestController>();
            var hpDisplay = FindObjectOfType<PlayerHpDisplay>();
            var mainCamera = Camera.main;
            var canvasRect = hpDisplay != null && hpDisplay.BackgroundRect != null
                ? hpDisplay.BackgroundRect.GetComponentInParent<Canvas>()?.transform as RectTransform
                : null;

            // 뭔가(HP UI/카메라 등)를 못 찾으면 날아가는 연출 없이 즉시 회복만 하고 조용히 사라짐 -
            // 연출이 실패해서 회복 자체가 안 되는 것보다는 낫다는 판단(안전장치).
            if (playerController == null || hpDisplay == null || hpDisplay.BackgroundRect == null || mainCamera == null || canvasRect == null)
            {
                playerController?.Heal(healAmount);
                Destroy(gameObject);
                yield break;
            }

            var iconObject = new GameObject("HpItemFlyIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(canvasRect, false);
            iconObject.transform.SetAsLastSibling();

            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = spriteRenderer.sprite;
            iconImage.raycastTarget = false;
            iconImage.SetNativeSize();

            // hpDisplay.BackgroundRect.position은 그 오브젝트의 피벗 기준점(이 프로젝트에서는 좌상단)
            // 이라 "배경 이미지 중앙"이 아니었다(2026-08-14 발견 - 피벗 때문에 목표 지점이 HP UI
            // 왼쪽 위 꼭짓점으로 잡혀서 화면 좌측으로 날아가 보였음). 피벗과 무관하게 실제 사각형의
            // 시각적 중앙을 구하려면 rect.center(로컬 좌표)를 TransformPoint로 월드 좌표로 변환해야
            // 한다 - 이렇게 하면 학생이 나중에 HP UI 크기/피벗을 바꾸더라도 항상 그 배경 이미지의
            // 진짜 한가운데를 목표로 삼는다(범용적으로 동작, 특정 아이콘 위치에 의존하지 않음).
            var backgroundRect = hpDisplay.BackgroundRect;
            var backgroundCenterWorld = backgroundRect.TransformPoint(backgroundRect.rect.center);

            var startLocal = ScreenToCanvasLocal(canvasRect, mainCamera.WorldToScreenPoint(transform.position));
            var targetLocal = ScreenToCanvasLocal(canvasRect, RectTransformUtility.WorldToScreenPoint(null, backgroundCenterWorld));
            iconRect.anchoredPosition = startLocal;

            // 월드 스프라이트는 여기서부터 숨김 - 같은 화면 위치에서 방금 만든 UI 아이콘이 그대로
            // 이어받아 날아가므로 눈에는 끊김 없이 자연스럽게 이어진다.
            spriteRenderer.enabled = false;

            // 날아가기 시작하는 바로 이 순간 1회 재생(사용자 확정, 2026-08-14).
            sfxPlayer.Play(flySfx);

            var elapsed = 0f;
            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / flightDuration);
                var eased = 1f - (1f - t) * (1f - t); // ease-out - 처음엔 빠르게, 도착 직전엔 느리게
                iconRect.anchoredPosition = Vector2.Lerp(startLocal, targetLocal, eased);
                yield return null;
            }

            Destroy(iconObject);
            playerController.Heal(healAmount);

            // 효과음이 flightDuration보다 길면 도착하자마자 gameObject를 바로 지워서 소리가 중간에
            // 끊겼었다(SfxPlayer의 AudioSource가 이 오브젝트의 자식이라 같이 파괴됨, 2026-08-14
            // 발견) - 이미 재생에 쓴 시간(flightDuration)만큼을 클립 길이에서 뺀 "남은 시간"만큼만
            // 더 살려두고 나서 지운다(시각적으로는 아이콘도 이미 지워졌고 월드 스프라이트도 꺼져
            // 있어서 화면엔 아무 영향 없음 - 소리만 끝까지 재생됨).
            var remainingSfxTime = flySfx != null && flySfx.clip != null
                ? Mathf.Max(0f, flySfx.clip.length - flightDuration)
                : 0f;
            Destroy(gameObject, remainingSfxTime);
        }

        private static Vector2 ScreenToCanvasLocal(RectTransform canvasRect, Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var local);
            return local;
        }
    }
}
