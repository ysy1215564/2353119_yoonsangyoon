using GameProject.Core;
using GameProject.Monsters;
using UnityEngine;

namespace GameProject.Testing
{
    public sealed class MonsterCMotionTestSceneController : MonoBehaviour
    {
        [SerializeField] private MonsterCActionTestController monsterController;
        [SerializeField] private Sprite targetIdleSprite;

        private MonsterCActionTestController activeController;
        private MonsterCActionTestController.PreviewMotion selectedMotion;
        private GameObject previewFloor;
        private Transform projectileTarget;
        private Collider2D projectileTargetCollider;
        private Collider2D previewFloorCollider;

        private void Start()
        {
            var testCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (testCamera != null)
            {
                testCamera.enabled = true;
                testCamera.orthographic = true;
                testCamera.orthographicSize = 5.4f;
                testCamera.transform.position = new Vector3(0f, 0f, -10f);
                testCamera.backgroundColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            }

            CreatePreviewFloor();
            CreateProjectileTarget();

            activeController = monsterController != null
                ? monsterController
                : GetComponent<MonsterCActionTestController>();
            if (activeController == null)
            {
                activeController = FindObjectOfType<MonsterCActionTestController>();
            }
            if (activeController != null)
            {
                activeController.SetProjectileTarget(projectileTarget);
                activeController.SetAttack2CollisionTargets(
                    projectileTarget,
                    projectileTargetCollider,
                    previewFloorCollider);
            }

            Play(MonsterCActionTestController.PreviewMotion.Idle);
        }

        private void CreatePreviewFloor()
        {
            if (previewFloor != null)
            {
                return;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "MonsterCMotionTestFloorTexture"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            var floorSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            floorSprite.name = "MonsterCMotionTestFloorSprite";
            previewFloor = new GameObject("Flat Test Floor", typeof(SpriteRenderer), typeof(BoxCollider2D));
            previewFloor.transform.position = new Vector3(0f, -4.4f, 0f);
            previewFloor.transform.localScale = new Vector3(32f, 2f, 1f);

            var renderer = previewFloor.GetComponent<SpriteRenderer>();
            renderer.sprite = floorSprite;
            renderer.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            renderer.sortingOrder = -5;
            previewFloorCollider = previewFloor.GetComponent<Collider2D>();
        }

        private void CreateProjectileTarget()
        {
            if (projectileTarget != null)
            {
                return;
            }

            var target = new GameObject(
                "Projectile Target",
                typeof(SpriteRenderer),
                typeof(CapsuleCollider2D));
            target.transform.position = new Vector3(-6.2f, -3.4f, 0f);

            var renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = targetIdleSprite;
            renderer.sortingOrder = 6;

            var collider = target.GetComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.95f, 2.5f);
            collider.offset = new Vector2(0f, 1.25f);
            collider.isTrigger = true;

            projectileTarget = target.transform;
            projectileTargetCollider = collider;
        }

        private void OnGUI()
        {
            GuiScaleUtility.Begin();
            const float preferredButtonWidth = 150f;
            const float buttonHeight = 42f;
            const float gap = 10f;
            const float top = 24f;
            var motions = new[]
            {
                MonsterCActionTestController.PreviewMotion.Idle,
                MonsterCActionTestController.PreviewMotion.Move,
                MonsterCActionTestController.PreviewMotion.Attack1,
                MonsterCActionTestController.PreviewMotion.Attack2,
                MonsterCActionTestController.PreviewMotion.Hit,
                MonsterCActionTestController.PreviewMotion.Death
            };
            var labels = new[] { "IDLE", "MOVE", "ATTACK 1", "ATTACK 2", "HIT", "DEATH" };
            var buttonWidth = GuiScaleUtility.FitButtonWidth(preferredButtonWidth, motions.Length, gap);
            var totalWidth = buttonWidth * motions.Length + gap * (motions.Length - 1);
            var startX = (GuiScaleUtility.ReferenceWidth - totalWidth) * 0.5f;

            for (var i = 0; i < motions.Length; i++)
            {
                var rect = new Rect(startX + (buttonWidth + gap) * i, top, buttonWidth, buttonHeight);
                if (GUI.Button(rect, labels[i]))
                {
                    Play(motions[i]);
                }
            }

            var statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0f, top + buttonHeight + 8f, GuiScaleUtility.ReferenceWidth, 34f), selectedMotion.ToString().ToUpperInvariant(), statusStyle);
        }

        private void Play(MonsterCActionTestController.PreviewMotion motion)
        {
            selectedMotion = motion;
            if (activeController != null)
            {
                activeController.PlayPreviewMotion(motion);
            }
        }
    }
}
