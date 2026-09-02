using GameProject.Core;
using GameProject.Monsters;
using GameProject.Player;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

namespace GameProject.Testing
{
    public sealed class ActionTestMonsterSelector : MonoBehaviour
    {
        private enum MonsterChoice
        {
            MonsterA,
            MonsterB,
            MonsterC
        }

        [Header("Test Templates")]
        [SerializeField] private GameObject playerTemplate;
        [SerializeField] private GameObject monsterATemplate;
        [SerializeField] private GameObject monsterBTemplate;
        [SerializeField] private GameObject monsterCTemplate;

        private GameObject activePlayer;
        private GameObject activeMonster;
        private string statusText = string.Empty;

        private void Awake()
        {
            SetTemplateActive(playerTemplate, false);
            SetTemplateActive(monsterATemplate, false);
            SetTemplateActive(monsterBTemplate, false);
            SetTemplateActive(monsterCTemplate, false);
        }

        private void Start()
        {
            ResetTest(MonsterChoice.MonsterA);
        }

        private void OnGUI()
        {
            GuiScaleUtility.Begin();

            // 2026-08-11: 화면 하단 중앙은 이제 스페셜 게이지 자리라, 이 디버그 선택 버튼들을 우측으로
            // 옮기고 좀 줄임(150x38 -> 105x28). 실제 게임 리소스가 아니라 테스트용 버튼이라 부담 없이
            // 조절 가능한 부분.
            const float buttonWidth = 105f;
            const float buttonHeight = 28f;
            const float gap = 8f;
            const float rightMargin = 20f;
            float totalWidth = buttonWidth * 3f + gap * 2f;
            float startX = GuiScaleUtility.ReferenceWidth - totalWidth - rightMargin;
            float y = GuiScaleUtility.ReferenceHeight - buttonHeight - 18f;

            if (DrawMouseButton(new Rect(startX, y, buttonWidth, buttonHeight), "MONSTER A"))
                ResetTest(MonsterChoice.MonsterA);

            if (DrawMouseButton(new Rect(startX + buttonWidth + gap, y, buttonWidth, buttonHeight), "MONSTER B"))
                ResetTest(MonsterChoice.MonsterB);

            if (DrawMouseButton(new Rect(startX + (buttonWidth + gap) * 2f, y, buttonWidth, buttonHeight), "MONSTER C"))
                ResetTest(MonsterChoice.MonsterC);

            if (!string.IsNullOrEmpty(statusText))
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                GUI.Label(new Rect(startX, y - 22f, totalWidth, 18f), statusText, style);
            }
        }

        private void ResetTest(MonsterChoice choice)
        {
            DestroyActiveObject(activePlayer);
            DestroyActiveObject(activeMonster);

            activePlayer = CreateInstance(playerTemplate, "Player_ActionTest_Runtime");
            activeMonster = null;

            switch (choice)
            {
                case MonsterChoice.MonsterA:
                    activeMonster = CreateInstance(monsterATemplate, "MonsterA_ActionTest_Runtime");
                    statusText = "MONSTER A TEST";
                    break;

                case MonsterChoice.MonsterB:
                    activeMonster = CreateInstance(monsterBTemplate, "MonsterB_ActionTest_Runtime");
                    statusText = "MONSTER B TEST";
                    break;

                default:
#if UNITY_EDITOR
                    activeMonster = monsterCTemplate != null
                        ? CreateInstance(monsterCTemplate, "MonsterC_ActionTest_Runtime")
                        : CreateEditorMonsterCInstance("MonsterC_ActionTest_Runtime");
#else
                    activeMonster = CreateInstance(monsterCTemplate, "MonsterC_ActionTest_Runtime");
#endif
                    var monsterC = activeMonster != null
                        ? activeMonster.GetComponent<MonsterCActionTestController>()
                        : null;
                    if (monsterC != null && activePlayer != null)
                    {
                        monsterC.ConfigureGameplay(activePlayer.GetComponent<PlayerActionTestController>());
                    }
                    statusText = "MONSTER C TEST";
                    break;
            }
        }

        private static GameObject CreateInstance(GameObject template, string instanceName)
        {
            if (template == null)
                return null;

            GameObject instance = Instantiate(template, template.transform.position, template.transform.rotation);
            instance.name = instanceName;
            instance.transform.localScale = template.transform.localScale;
            instance.SetActive(true);
            return instance;
        }

        private static void DestroyActiveObject(GameObject target)
        {
            if (target != null)
                Destroy(target);
        }

        private static void SetTemplateActive(GameObject template, bool active)
        {
            if (template != null)
                template.SetActive(active);
        }

        private static bool DrawMouseButton(Rect rect, string label)
        {
            GUI.Box(rect, label, GUI.skin.button);

            Event current = Event.current;
            if (current.type != EventType.MouseUp || current.button != 0 || !rect.Contains(current.mousePosition))
                return false;

            current.Use();
            return true;
        }

#if UNITY_EDITOR
        private static GameObject CreateEditorMonsterCInstance(string instanceName)
        {
            const string monsterCArtRoot = "Assets/_Project/04_Art/StudentReplace/Monsters/MonsterC";

            var monster = new GameObject(
                instanceName,
                typeof(SpriteRenderer),
                typeof(BoxCollider2D),
                typeof(MonsterCActionTestController));
            monster.transform.position = new Vector3(4.8f, -0.8f, 0f);
            monster.transform.localScale = Vector3.one;

            var renderer = monster.GetComponent<SpriteRenderer>();
            var idleFrames = LoadSpritesFromFolder($"{monsterCArtRoot}/idle_frames");
            renderer.sprite = idleFrames.Length > 0 ? idleFrames[0] : null;
            renderer.sortingOrder = 8;

            var bodyCollider = monster.GetComponent<BoxCollider2D>();
            bodyCollider.size = new Vector2(1.6f, 1.6f);
            bodyCollider.offset = new Vector2(0f, 0.9f);
            bodyCollider.isTrigger = true;

            var controller = monster.GetComponent<MonsterCActionTestController>();
            var so = new SerializedObject(controller);
            SetObject(so, "spriteRenderer", renderer);
            SetSprites(so, "idleFrames", idleFrames);
            SetSprites(so, "flyFrames", LoadSpritesFromFolder($"{monsterCArtRoot}/fly_frames"));
            SetSprites(so, "attack1Frames", LoadSpritesFromFolder($"{monsterCArtRoot}/attack1_frames"));
            SetSprites(so, "attack2ChargeFrames", LoadSpritesFromFolder($"{monsterCArtRoot}/attack2_charge_frames"));
            SetSprites(so, "attack2DashFrames", LoadSpritesFromFolder($"{monsterCArtRoot}/attack2_dash_frames"));
            SetSprites(so, "hitFrames", LoadSpritesFromFolder($"{monsterCArtRoot}/hit_frames"));
            SetSprites(so, "deathFrames", LoadSpritesFromFolder($"{monsterCArtRoot}/death_frames"));
            SetSprites(so, "projectileFrames", LoadSpritesFromFolder($"{monsterCArtRoot}/Effects/Projectile"));
            so.FindProperty("controlMode").enumValueIndex = 1;
            so.FindProperty("projectileSpawnOffset").vector3Value = new Vector3(-0.9f, 1.95f, 0f);
            so.FindProperty("projectileTargetAimOffset").vector3Value = new Vector3(0f, 1.25f, 0f);
            so.FindProperty("projectileScale").vector3Value = new Vector3(0.3f, 0.3f, 1f);
            so.FindProperty("projectileSpeed").floatValue = 14f;
            so.FindProperty("attack1ProjectileFrame").intValue = 3;
            so.FindProperty("projectileAnimationFramesPerSecond").floatValue = 12f;
            so.FindProperty("projectileFloorMask").intValue = LayerMask.GetMask("Default");
            so.FindProperty("projectileColliderRadius").floatValue = 0.28f;
            so.FindProperty("maxHp").intValue = 3;
            so.FindProperty("detectRange").floatValue = 7f;
            so.FindProperty("attackRange").floatValue = 6f;
            so.FindProperty("attackCooldown").floatValue = 1.4f;
            so.FindProperty("roamArea").vector2Value = new Vector2(3.4f, 1.2f);
            so.FindProperty("roamSpeed").floatValue = 1.15f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return monster;
        }

        private static Sprite[] LoadSpritesFromFolder(string folder)
        {
            var sprites = new List<Sprite>();
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return sprites.ToArray();
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            so.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void SetSprites(SerializedObject so, string propertyName, Sprite[] sprites)
        {
            var property = so.FindProperty(propertyName);
            property.arraySize = sprites.Length;
            for (var i = 0; i < sprites.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }
#endif
    }
}
