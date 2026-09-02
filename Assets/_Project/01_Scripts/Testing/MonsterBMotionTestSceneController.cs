using GameProject.Core;
using GameProject.Monsters;
using UnityEngine;

namespace GameProject.Testing
{
    public sealed class MonsterBMotionTestSceneController : MonoBehaviour
    {
        [SerializeField] private MonsterBActionTestController monsterController;

        private MonsterBActionTestController activeController;
        private MonsterBActionTestController.PreviewMotion selectedMotion;
        private GameObject previewFloor;

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

            activeController = monsterController != null
                ? monsterController
                : GetComponent<MonsterBActionTestController>();
            if (activeController == null)
                activeController = FindObjectOfType<MonsterBActionTestController>();
            Play(MonsterBActionTestController.PreviewMotion.Idle);
        }

        private void CreatePreviewFloor()
        {
            if (previewFloor != null)
                return;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "MonsterBMotionTestFloorTexture"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            var floorSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            floorSprite.name = "MonsterBMotionTestFloorSprite";
            previewFloor = new GameObject("Flat Test Floor", typeof(SpriteRenderer), typeof(BoxCollider2D));
            previewFloor.transform.position = new Vector3(0f, -4.4f, 0f);
            previewFloor.transform.localScale = new Vector3(32f, 2f, 1f);

            var renderer = previewFloor.GetComponent<SpriteRenderer>();
            renderer.sprite = floorSprite;
            renderer.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            renderer.sortingOrder = -5;
        }

        private void OnGUI()
        {
            GuiScaleUtility.Begin();
            const float preferredButtonWidth = 170f;
            const float buttonHeight = 42f;
            const float gap = 10f;
            const float top = 24f;
            var motions = new[]
            {
                MonsterBActionTestController.PreviewMotion.Idle,
                MonsterBActionTestController.PreviewMotion.Walk,
                MonsterBActionTestController.PreviewMotion.Attack1,
                MonsterBActionTestController.PreviewMotion.Attack2,
                MonsterBActionTestController.PreviewMotion.Hit,
                MonsterBActionTestController.PreviewMotion.Death
            };
            var labels = new[] { "IDLE", "WALK", "ATTACK 1", "ATTACK 2", "HIT", "DEATH" };
            var buttonWidth = GuiScaleUtility.FitButtonWidth(preferredButtonWidth, motions.Length, gap);
            var totalWidth = buttonWidth * motions.Length + gap * (motions.Length - 1);
            var startX = (GuiScaleUtility.ReferenceWidth - totalWidth) * 0.5f;

            for (var i = 0; i < motions.Length; i++)
            {
                var rect = new Rect(startX + (buttonWidth + gap) * i, top, buttonWidth, buttonHeight);
                if (GUI.Button(rect, labels[i]))
                    Play(motions[i]);
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

        private void Play(MonsterBActionTestController.PreviewMotion motion)
        {
            selectedMotion = motion;
            if (activeController != null)
                activeController.PlayPreviewMotion(motion);
        }

    }
}
