using GameProject.Core;
using UnityEngine;

namespace GameProject.Testing
{
    /// <summary>
    /// ActionTest 전용 디버그 버튼(2026-08-14) - HP 회복 상자는 한 번 쓰면 사라지는 게 실제 규칙
    /// (리스폰 없음, 사용자 확정)이라, 매번 다시 테스트하려고 Play를 껐다 켤 필요 없이 여기서
    /// "HP ITEM" 버튼으로 같은 자리에 다시 등장시킬 수 있게 하는 순수 테스트 편의 기능이다.
    /// **BackgroundTest에는 이 컴포넌트를 두지 않는다** - 그쪽은 실제 "한 번 쓰고 끝" 규칙 그대로,
    /// 재소환 버튼 자체가 존재하면 안 됨(사용자 확정).
    ///
    /// MONSTER A/B/C 버튼(ActionTestMonsterSelector)과 같은 스타일(OnGUI, GuiScaleUtility 기준
    /// 좌표) - 다만 그 스크립트는 플레이어/몬스터 템플릿 교체 전용이라 별개 관심사로 분리했다.
    /// </summary>
    public sealed class HpItemBoxTestRespawner : MonoBehaviour
    {
        [SerializeField] private GameObject hpItemBoxPrefab;
        [SerializeField] private string trackedInstanceName = "HpItemBox_Instance";

        private GameObject activeInstance;
        private Vector3 spawnPosition;
        private bool hasSpawnPosition;

        private void Awake()
        {
            // 씬에 이미 배치돼있는 인스턴스(에디터에서 위치 맞춰둔 것)를 찾아서 그 자리를 리스폰
            // 기준점으로 기억해둔다 - 처음 Play했을 때는 이 인스턴스를 그대로 쓰고, 버튼을 누르면
            // 그 다음부터는 같은 자리에 새로 하나씩 만든다.
            var existing = GameObject.Find(trackedInstanceName);
            if (existing != null)
            {
                spawnPosition = existing.transform.position;
                hasSpawnPosition = true;
                activeInstance = existing;
            }
        }

        private void OnGUI()
        {
            GuiScaleUtility.Begin();

            // MONSTER A/B/C 버튼 행(우측 하단) 바로 위에 한 줄 - 같은 폭/스타일, 겹치지 않게 위로.
            const float buttonWidth = 105f;
            const float buttonHeight = 28f;
            const float rightMargin = 20f;
            const float monsterRowHeight = 28f + 18f; // ActionTestMonsterSelector의 버튼 높이 + 하단 여백
            const float monsterLabelHeight = 22f; // 그 위 상태 텍스트 한 줄
            const float gapAboveMonsterRow = 10f;

            var y = GuiScaleUtility.ReferenceHeight - monsterRowHeight - monsterLabelHeight - gapAboveMonsterRow - buttonHeight;
            var rect = new Rect(GuiScaleUtility.ReferenceWidth - buttonWidth - rightMargin, y, buttonWidth, buttonHeight);

            if (DrawMouseButton(rect, "HP ITEM"))
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            if (activeInstance != null)
            {
                Destroy(activeInstance);
            }

            if (hpItemBoxPrefab == null || !hasSpawnPosition)
            {
                return;
            }

            activeInstance = Instantiate(hpItemBoxPrefab, spawnPosition, Quaternion.identity);
            activeInstance.name = trackedInstanceName;
        }

        private static bool DrawMouseButton(Rect rect, string label)
        {
            GUI.Box(rect, label, GUI.skin.button);

            var current = Event.current;
            if (current.type != EventType.MouseUp || current.button != 0 || !rect.Contains(current.mousePosition))
            {
                return false;
            }

            current.Use();
            return true;
        }
    }
}
