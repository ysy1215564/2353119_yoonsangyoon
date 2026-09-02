using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameProject.UI
{
    [RequireComponent(typeof(Image))]
    public class TitleImageButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite hoverSprite;
        // 사용자 제공 아트에는 기본/오버 외에 클릭 전용 스프라이트도 있음(눌림 시 스케일만 줄이던
        // 기존 방식 대신 실제 클릭 아트를 보여줌). 없으면(예: 저장후닫기처럼 상태 이미지가 하나뿐인
        // 버튼) null로 두면 되고, 그럴 땐 기존처럼 눌림 스케일만 적용된다.
        [SerializeField] private Sprite pressedSprite;
        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private UnityEvent onClick = new UnityEvent();

        private Vector3 defaultScale;
        private bool isHovering;
        private bool isPressed;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            defaultScale = transform.localScale;
            SetNormal();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            if (!isPressed)
            {
                SetHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            if (!isPressed)
            {
                SetNormal();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            SetPressed();
            transform.localScale = defaultScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            transform.localScale = defaultScale;
            if (isHovering)
            {
                SetHover();
            }
            else
            {
                SetNormal();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onClick?.Invoke();
        }

        public void SetSprites(Sprite normal, Sprite hover, Sprite pressed = null)
        {
            normalSprite = normal;
            hoverSprite = hover;
            pressedSprite = pressed;
            SetNormal();
        }

        public void AddListener(UnityAction action)
        {
            onClick.AddListener(action);
        }

        private void SetNormal()
        {
            if (targetImage != null && normalSprite != null)
            {
                targetImage.sprite = normalSprite;
            }
        }

        private void SetHover()
        {
            if (targetImage != null && hoverSprite != null)
            {
                targetImage.sprite = hoverSprite;
            }
        }

        private void SetPressed()
        {
            if (targetImage != null && pressedSprite != null)
            {
                targetImage.sprite = pressedSprite;
            }
            else
            {
                SetHover();
            }
        }
    }
}

