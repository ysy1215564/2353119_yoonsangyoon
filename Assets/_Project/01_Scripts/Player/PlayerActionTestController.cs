using System.Collections;
using GameProject.Audio;
using GameProject.Core;
using GameProject.Environment;
using GameProject.Monsters;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameProject.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerActionTestController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float jumpForce = 9.6f;
        [SerializeField] private float dashSpeed = 14f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform groundCheck;

        [Header("Sprites")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] jumpAttackFrames;
        [SerializeField] private Sprite[] dashFrames;
        [SerializeField] private Sprite[] attack1Frames;
        [SerializeField] private Sprite[] attack2Frames;
        [SerializeField] private Sprite[] specialChargeFrames;
        [SerializeField] private Sprite[] specialFireFrames;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] knockdownFrames;
        [SerializeField] private Sprite[] deathFrames;

        [Header("Effects")]
        [SerializeField] private TestSpriteEffect attackEffect;
        [SerializeField] private TestSpriteEffect specialLaserEffect;
        [SerializeField] private TestSpriteEffect specialChargeEffect;
        [SerializeField] private TestSpriteEffect specialGroundBurstEffect;
        [SerializeField] private Sprite[] attackEffect1Frames;
        [SerializeField] private Sprite[] attackEffect2Frames;
        [SerializeField] private Sprite[] jumpAttackEffectFrames;
        [SerializeField] private Sprite[] specialChargeEffectFrames;
        [SerializeField] private Sprite[] specialLaserFrames;
        [SerializeField] private Sprite[] specialGroundBurstFrames;

        [Header("Effect Placement")]
        [SerializeField] private Vector3 attack1EffectOffset = new Vector3(2f, 3.2f, 0f);
        [SerializeField] private Vector3 attack2EffectOffset = new Vector3(1.15f, 1.7f, 0f);
        [SerializeField] private Vector3 jumpAttackEffectOffset = new Vector3(1.25f, 0.45f, 0f);
        [SerializeField] private Vector3 specialChargeEffectOffset = new Vector3(-1.34f, 0.8f, 0f);
        [SerializeField] private Vector3 specialLaserOffset = new Vector3(4.89f, 3.5f, 0f);
        [SerializeField] private Vector3 specialGroundBurstOffset = new Vector3(3.5f, 4.4f, 0f);
        [SerializeField] private Vector3 attack1EffectScale = new Vector3(1.5f, 1.5f, 1f);
        [SerializeField] private Vector3 attack2EffectScale = new Vector3(0.9f, 0.9f, 1f);
        [SerializeField] private Vector3 jumpAttackEffectScale = new Vector3(0.65f, 0.65f, 1f);
        [SerializeField] private Vector3 specialChargeEffectScale = new Vector3(0.5f, 0.5f, 1f);
        [SerializeField] private Vector3 specialLaserScale = new Vector3(1.2f, 1.2f, 1f);
        [SerializeField] private Vector3 specialGroundBurstScale = new Vector3(1.5f, 1.5f, 1f);
        [SerializeField] private int attack1EffectStartFrame = 3;
        [SerializeField] private int attack2EffectStartFrame = 2;
        [Tooltip("1-based frame number where Attack 1 hits monsters.")]
        [SerializeField, Min(1)] private int attack1DamageFrame = 3;
        [Tooltip("1-based frame number where Attack 2 hits monsters.")]
        [SerializeField, Min(1)] private int attack2DamageFrame = 2;
        [Tooltip("1-based frame number where the Special Attack hits monsters.")]
        [SerializeField, Min(1)] private int specialDamageFrame = 3;
        [Tooltip("1-based frame number where the Special laser effect starts.")]
        [SerializeField, Min(1)] private int specialLaserStartFrame = 4;
        [SerializeField] private int specialGroundBurstStartFrame = 4;
        [SerializeField] private float specialChargeEffectFps = 10f;

        [Header("Combo Movement")]
        [SerializeField] private float attack2StepForward = 1f;

        [Header("Frame Timing")]
        [SerializeField] private float[] attack1FrameTimes = { 0.1f, 0.15f, 0.12f, 0.1f };
        [SerializeField] private float[] attack2FrameTimes = { 0.1f, 0.15f, 0.1f, 0.1f };
        [SerializeField] private float[] specialFireFrameTimes =
        {
            0.07f, 0.07f, 0.07f, 0.1f, 0.2f, 0.12f,
            0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f
        };

        [Header("Special Attack")]
        // 1.5 -> 1로 변경(2026-08-17) - ActionTest.unity에서 이미 1로 튜닝되어 있던 값을 기본값으로
        // 승격. 플레이어는 프리팹이 없어서 각 씬의 Player가 이 기본값을 각자 따로 들고 있었는데,
        // BackgroundTest/Stage2_BackgroundTest 등은 이 필드를 손대지 않아 옛 기본값(1.5)이 그대로
        // 남아있었음 - PlayerStatsSyncTool로 ActionTest 기준으로 통일 가능.
        [SerializeField] private float specialMinimumChargeTime = 1f;

        [Header("SFX")]
        [SerializeField] private SfxCue attack1SwingSfx;
        [SerializeField] private SfxCue attack2SwingSfx;
        [SerializeField] private SfxCue jumpSfx;
        [SerializeField] private SfxCue dashSfx;
        [SerializeField] private SfxCue hitSfx;
        [SerializeField] private SfxCue heavyHitSfx;
        [SerializeField] private SfxCue deathSfx;
        [SerializeField] private SfxCue specialChargeLoopSfx;
        [SerializeField] private SfxCue specialFireSfx;
        [SerializeField] private SfxCue jumpAttackSfx;
        [Tooltip("1-based frame number where the Attack 1 swing sound plays.")]
        [SerializeField, Min(1)] private int attack1SwingFrame = 1;
        [Tooltip("1-based frame number where the Attack 2 swing sound plays.")]
        [SerializeField, Min(1)] private int attack2SwingFrame = 1;

        [Header("Combat")]
        [Tooltip("HP UI(PlayerHpDisplay)가 이 값을 그대로 읽어서 칸 수를 자동으로 맞춤 - 여기서만 바꾸면 됨. 화면 좌측 상단 여유를 감안해 1~10칸으로 제한.")]
        [SerializeField, Range(1, 10)] private int maxHp = 5;
        [Tooltip("좌하단에 뜨는 에디터 전용 디버그 HP 텍스트(OnGUI) - 실제 그래픽 HP UI가 붙은 씬(BackgroundTest)에서는 꺼두는 용도. ActionTest에서는 켜둬도 무방.")]
        [SerializeField] private bool showDebugHpText = true;
        [Tooltip("차징 중 뜨는 에디터 전용 디버그 스페셜 게이지(OnGUI) - 실제 그래픽 게이지가 붙은 씬(BackgroundTest)에서는 꺼두는 용도. ActionTest에서는 켜둬도 무방.")]
        [SerializeField] private bool showDebugSpecialGauge = true;
        [SerializeField] private Vector2 hurtBoxOffset = new Vector2(0f, 1.2f);
        [SerializeField] private Vector2 hurtBoxSize = new Vector2(0.65f, 1.9f);
        [SerializeField] private Vector2 attackHitBoxOffset = new Vector2(1.29f, 1.15f);
        [SerializeField] private Vector2 attackHitBoxSize = new Vector2(1.83f, 2.29f);
        [SerializeField] private Vector2 jumpAttackHitBoxOffset = new Vector2(0.8f, 0.75f);
        [SerializeField] private Vector2 jumpAttackHitBoxSize = new Vector2(2.66f, 2.75f);
        [SerializeField] private Vector2 specialHitBoxOffset = new Vector2(3.91f, 1.31f);
        [SerializeField] private Vector2 specialHitBoxSize = new Vector2(6.41f, 2.57f);
        [SerializeField] private float hitInvincibleTime = 0.6f;
        [SerializeField] private float hitKnockbackDistance = 0.55f;
        [SerializeField] private float knockdownKnockbackDistance = 0.9f;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Coroutine actionRoutine;
        private Coroutine loopRoutine;
        private Sprite[] currentLoopFrames;
        private bool isActionLocked;
        private bool isAttacking;
        private bool isCharging;
        private bool isSpecialFiring;
        private bool isDashing;
        private bool isDead;
        private bool isDeathAnimationFinished;
        private bool specialChargeFromKeyboard;
        private bool specialChargeFromMouse;
        private bool specialChargeFromTouch;
        private bool touchSpecialChargeHeld;
        private float touchHorizontalInput;
        private int attackRequestCount;
        private int jumpCount;
        private int facing = 1;
        private float specialChargeStartTime;
        private float lastJumpTime = -999f;
        private const float JumpAnimationHoldTime = 0.2f;
        private float landingFrameEndTime = -999f;
        private bool wasAirborne;
        private const float LandingFrameHoldTime = 0.14f;
        private const float SpecialChargeInputGrace = 0f;
        private int currentHp;
        private float lastHitTime = -999f;
        private int nextAttackId;
        private GUIStyle hpStyle;
        private GUIStyle specialGaugeStyle;
        private SfxPlayer sfxPlayer;
        private bool groundedCache;
        private int groundedCacheFrame = -1;

        /// <summary>Physics2D.OverlapCircle 결과를 프레임당 한 번만 계산하고 재사용(2026-08-13,
        /// 모바일 실기기 프레임 드랍 대응 - 원래는 Grounded를 부르는 곳마다 매번 새로 쿼리해서,
        /// 한 프레임 안에서 Update()/UpdateLandingState()/IsAirborneForAnimation() 등 여러 군데가
        /// 각자 부르면 같은 프레임에 물리 쿼리가 최대 5번까지 중복 실행되고 있었음). 호출 시점이
        /// Update()든, 터치 버튼 콜백 같은 다른 시점이든 상관없이 "이번 유니티 프레임에서 처음
        /// 물어본 순간" 한 번만 실제로 계산하고, 같은 프레임 안의 나머지 호출은 그 값을 그대로
        /// 재사용 - 다음 프레임이 되면(Time.frameCount 변화) 자동으로 다시 계산됨.</summary>
        private bool Grounded
        {
            get
            {
                if (groundedCacheFrame != Time.frameCount)
                {
                    groundedCacheFrame = Time.frameCount;
                    groundedCache = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, 0.12f, groundMask);
                }

                return groundedCache;
            }
        }

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public Vector2 HurtBoxSize => hurtBoxSize;

        /// <summary>true from the moment HandleDeath() runs (HP hit 0 in combat OR fell through a
        /// gap via KillByFalling() - both funnel through the same place) - DeathScreenController
        /// polls this to know when to show the death screen.</summary>
        public bool IsDead => isDead;

        /// <summary>true while any action(콤보/점프공격/스페셜 등)이 진행 중이라 새 입력을 안 받는
        /// 상태 - StageClearController가 이 값의 true→false 전환(=마지막 타격 동작이 끝나고 IDLE로
        /// 넘어가는 순간)을 감지해 클리어 화면을 띄우는 타이밍으로 쓴다(2026-08-12).</summary>
        public bool IsActionLocked => isActionLocked;

        // ---- 모바일 터치 입력 진입점 (2026-08-13) ----------------------------------------------
        //
        // 키보드/마우스 Update() 흐름은 손대지 않고, 터치 UI(MobileJoystick/MobileTouchButton/
        // MobileAttackButton)가 호출할 별도의 공개 메서드만 추가하는 방식 - 각 메서드는 해당 키보드
        // 입력이 눌렸을 때와 동일한 가드/로직을 그대로 재현해서, PC 조작감과 완전히 동일하게 동작한다.
        // 이동은 FixedUpdate()/UpdateLoopAnimation()에서 "터치 입력이 있으면 우선, 없으면 키보드"
        // 순으로 합쳐서 처리(EffectiveHorizontalInput() 참고) - 조그셔틀을 안 쓰는 동안은 키보드로도
        // 그대로 테스트 가능.

        /// <summary>조그셔틀이 매 프레임(드래그 중) 호출 - -1(왼쪽)~1(오른쪽), 데드존 처리는 조그셔틀
        /// 쪽에서 이미 끝내고 넘어옴. 손을 떼면 0으로 호출해서 정지.</summary>
        public void SetTouchMoveInput(float horizontal)
        {
            touchHorizontalInput = Mathf.Clamp(horizontal, -1f, 1f);
        }

        /// <summary>점프 버튼 탭 - Space 키와 동일한 가드(액션락/차징/스페셜 발사 중이면 무시, 2단
        /// 점프까지만).</summary>
        public void TriggerJumpInput()
        {
            if (isDead || GameplayFreezeGate.IsFrozen || Time.timeScale <= 0f)
            {
                return;
            }

            if (isActionLocked || isCharging || isSpecialFiring)
            {
                return;
            }

            if (jumpCount < 2)
            {
                Jump();
            }
        }

        /// <summary>조그셔틀 플릭(빠르게 끝까지 밀었다 놓음) - LeftShift+지상 조건과 동일.
        /// <paramref name="direction"/>이 0이 아니면(스와이프 x값) 그 방향을 즉시 바라보는 방향(facing)에
        /// 반영하고 대시한다. facing은 원래 FixedUpdate()에서 이동 입력을 보고서만 갱신되는데, 방향을
        /// 순간적으로 반대로 홱 미는 플릭 제스처는 그 갱신이 한 프레임(최대 한 물리 스텝) 늦어서 새
        /// 방향이 아니라 직전 방향으로 대시가 나가버리는 실기기 버그가 있었다(2026-08-13, Galaxy S21
        /// 실사용 확인) - 플릭 자체가 이미 알고 있는 방향을 바로 넘겨받아 쓰는 것으로 해결.</summary>
        public void TriggerDashInput(float direction = 0f)
        {
            if (isDead || GameplayFreezeGate.IsFrozen || Time.timeScale <= 0f)
            {
                return;
            }

            if (isActionLocked || isCharging || isSpecialFiring)
            {
                return;
            }

            if (!Grounded)
            {
                return;
            }

            if (Mathf.Abs(direction) > 0.01f)
            {
                facing = direction > 0f ? 1 : -1;
                transform.localScale = new Vector3(facing, 1f, 1f);
            }

            PlayDash();
        }

        /// <summary>공격 버튼을 짧게 눌렀다 뗐을 때(스페셜 차지 문턱 시간 안에 뗀 경우) - J/좌클릭과
        /// 동일한 가드+분기(콤보 큐잉/점프공격/일반공격)를 그대로 재현.</summary>
        public void TriggerAttackTap()
        {
            if (isDead || GameplayFreezeGate.IsFrozen || Time.timeScale <= 0f)
            {
                return;
            }

            if (isAttacking)
            {
                attackRequestCount = Mathf.Clamp(attackRequestCount + 1, 1, 2);
                return;
            }

            if (isActionLocked || isCharging || isSpecialFiring)
            {
                return;
            }

            if (IsAirborneForAnimation())
            {
                PlayLockedWithEffect(jumpAttackFrames, 14f, jumpAttackEffectFrames, jumpAttackEffectOffset, jumpAttackEffectScale, 1);
                return;
            }

            QueueAttack();
        }

        /// <summary>전용 차징 버튼(MobileSpecialChargeButton)을 누르는 순간 호출 - K키/우클릭과
        /// 동일하게 차징 시작. 이후 눌려있는 동안은 SetTouchSpecialChargeHeld로 계속 상태를
        /// 알려주면, Update()의 기존 차징 감시 로직이 놓는 순간을 감지해서 알아서 발사한다
        /// (키보드/마우스와 동일한 코드 경로 재사용 - 최소 충전 시간 체크 등 전부 그대로 적용됨).
        /// 2026-08-15: 원래는 공격 버튼을 일정 시간 이상 누르고 있으면(문턱 시간) 여기로 넘어오는
        /// 방식이었는데, 실기기에서 판정이 늦어 갑갑하다는 피드백으로 전용 버튼으로 분리함.</summary>
        public void TriggerSpecialChargeStart()
        {
            if (isDead || GameplayFreezeGate.IsFrozen || Time.timeScale <= 0f)
            {
                return;
            }

            // isActionLocked/isSpecialFiring도 반드시 같이 확인해야 한다 - 원래 isCharging만 봤는데,
            // 그러면 "직전 스페셜 발사 애니메이션이 아직 재생 중(isSpecialFiring=true, isCharging은
            // 이미 false)"인 타이밍에 다시 눌러도 여기를 통과해서 StartSpecialCharge()가 그 재생
            // 중이던 코루틴을 강제로 끊어버렸다 - 끊긴 코루틴은 자기 마지막에 있는
            // "isSpecialFiring = false" 정리 코드를 영영 실행 못 하게 되어, isSpecialFiring이
            // 계속 true로 고정되고 그 뒤로는 일반 공격이 전부 막혀버리는 실기기 버그였다
            // (2026-08-13, "차징 몇 번 하고 나면 일반 공격이 영영 안 나감"). 키보드/마우스 쪽 차징
            // 시작 조건도 동일하게 고쳤다(Update() 참고) - 원인이 입력 수단과 무관한 공용 로직이라.
            if (isCharging || isActionLocked || isSpecialFiring)
            {
                return;
            }

            StartSpecialCharge(false, false, true);
            touchSpecialChargeHeld = true;
        }

        /// <summary>공격 버튼이 지금 눌려있는지(차징 시작 이후) 매 프레임 알려줌 - 뗀 순간 false로
        /// 한 번 호출하면 Update()가 다음 프레임에 자동으로 발사 처리.</summary>
        public void SetTouchSpecialChargeHeld(bool held)
        {
            touchSpecialChargeHeld = held;
        }

        /// <summary>키보드(Input.GetAxisRaw)와 터치 조그셔틀 입력을 합쳐서 하나의 값으로 - 조그셔틀을
        /// 만지는 동안은 그쪽을 우선하고, 안 만지고 있으면(0에 가까우면) 키보드가 그대로 동작한다.
        /// FixedUpdate()의 실제 이동과 UpdateLoopAnimation()의 걷기/대기 판정이 항상 같은 값을
        /// 보도록 여기 한 곳에만 로직을 둔다.</summary>
        private float EffectiveHorizontalInput()
        {
            return Mathf.Abs(touchHorizontalInput) > 0.01f ? touchHorizontalInput : Input.GetAxisRaw("Horizontal");
        }

        /// <summary>true only after the death animation (deathFrames) has played all the way through
        /// its last frame - DeathScreenController waits for this (then an extra delay) before showing
        /// the death panel, instead of popping it the instant death starts. deathFrames.Length is read
        /// at runtime, so this still works if a student changes how many frames are in the death clip.</summary>
        public bool IsDeathAnimationFinished => isDeathAnimationFinished;

        /// <summary>0(충전 안 함) ~ 1(최소 충전 시간 도달, 발사 가능) - SpecialGaugeDisplay가 이 값을
        /// 그대로 Image.fillAmount에 넣어서 실제 게이지를 채운다. 최소 시간보다 오래 눌러도 1에서
        /// 멈춘다(그 이상 채워질 시각적 의미가 없어서).</summary>
        public float SpecialChargeProgress01 => isCharging
            ? Mathf.Clamp01((Time.time - specialChargeStartTime) / Mathf.Max(0.01f, specialMinimumChargeTime))
            : 0f;

        /// <summary>지금 스페셜 차징 중인지(원인 불문 - 키보드/마우스/터치 전부 포함) - 에디터
        /// 전용이 아닌 공개 프로퍼티. 원래(2026-08-13)는 공격/차징을 겸하던 MobileAttackButton이
        /// "떼는 순간이 탭인지 홀드 해제인지" 판단할 때 자기 로컬 상태 대신 이 값을 신뢰하려고
        /// 추가했던 것인데, 2026-08-15에 차징이 MobileSpecialChargeButton으로 완전히 분리되면서
        /// 그 용도는 없어졌다. 다만 여전히 "지금 차징 중인지"를 외부에 알려주는 공용 상태 조회용으로
        /// 유효해서(UI 표시 등에 쓸 수 있음) 그대로 남겨둠.</summary>
        public bool IsCharging => isCharging;

#if UNITY_EDITOR
        public bool IsSpecialFiringForTest => isSpecialFiring;
        public bool IsChargingForTest => isCharging;
        public Sprite CurrentSpriteForTest => spriteRenderer != null ? spriteRenderer.sprite : null;
#endif

        private void Awake()
        {
            CacheComponents();
            sfxPlayer = new SfxPlayer(transform);
            currentHp = maxHp;

            // 새 플레이 세션이 시작되는 시점 - 이전 씬(예: 클리어 후 재도전)에서 얼린 채로 남아있는
            // GameplayFreezeGate 상태가 이어지는 사고를 막는다. 자세한 이유는 GameplayFreezeGate 참고.
            GameplayFreezeGate.Reset();
        }

        private void CacheComponents()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            // 일시정지 메뉴가 열려 있는 동안(PauseMenuController가 Time.timeScale = 0으로 만듦) -
            // FixedUpdate는 Unity가 알아서 안 부르지만 Update()는 그대로 매 프레임 호출되므로, 입력을
            // 여기서 직접 막지 않으면 공격 등을 눌러둔 게 코루틴으로 큐잉됐다가 재개하자마자 튀어나갈
            // 수 있다(2026-08-12).
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (isDead)
            {
                return;
            }

            // 스테이지 클리어 등으로 월드가 얼어있는 동안 - 진행 중이던 동작을 즉시 끊고 IDLE
            // 포즈로 고정, 입력은 전부 무시한다(2026-08-12). InterruptActionState()가 사망 외의
            // 모든 액션 상태(콤보/차징/스페셜/대시 + 관련 SFX/이펙트)를 정리해주는 기존 메서드라
            // 그대로 재사용 - 매 프레임 불러도 이미 정리된 상태면 각 내부 호출이 알아서 아무 일도
            // 안 하니 안전하다.
            if (GameplayFreezeGate.IsFrozen)
            {
                InterruptActionState();
                rb.velocity = Vector2.zero;
                if (idleFrames != null && idleFrames.Length > 0)
                {
                    spriteRenderer.sprite = idleFrames[0];
                }
                return;
            }

            UpdateLandingState();

            if (Grounded && Mathf.Abs(rb.velocity.y) <= 0.05f && Time.time - lastJumpTime > JumpAnimationHoldTime && Time.time > landingFrameEndTime)
            {
                jumpCount = 0;
            }

            // 2026-08-25: 단일 M에서 Ctrl+M으로 변경. 아래 H/N과 달리 이 키는 게임 흐름을
            // 통째로 건너뛰는데(즉사 -> 사망 화면), 하필 조작키 J/K 바로 아래 자리라 실수로 눌리기
            // 쉬웠다. 빌드에서 빼지는 않는다 - 이유는 DebugShortcut 주석 참고.
            if (Input.GetKeyDown(KeyCode.M) && DebugShortcut.ModifiersHeld)
            {
                HandleDeath();
                return;
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                PlaySfx(hitSfx);
                PlayLocked(hitFrames, 12f);
                return;
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                PlaySfx(heavyHitSfx);
                PlayLocked(knockdownFrames, 10f);
                return;
            }

            // 모바일 터치 컨트롤(조그셔틀/버튼) 위를 눌렀을 때 - 마우스/터치 다운 자체는 UI를
            // 클릭했는지 여부와 무관하게 항상 그대로 들어오므로(Input.GetMouseButtonDown이 모름),
            // 이 값을 마우스 버튼 입력에만 걸어서 UI 위 클릭이 PC 공격/스페셜과 동시에 발동되는 걸
            // 막는다(2026-08-13 실사용 중 발견 - 조그셔틀/버튼을 누를 때마다 공격이 같이 나가던 문제).
            // 키보드는 이 문제와 무관하니 그대로 둔다.
            //
            // 추가로 2026-08-13 실기기(Galaxy S21) 테스트에서 확인된 더 근본적인 문제: 안드로이드는
            // 레거시 Input Manager가 터치를 자동으로 "마우스 버튼 0번 클릭"으로도 흉내 낸다
            // (Input.simulateMouseWithTouches, 기본 켜짐) - 그래서 pointerOverUi 판정이 아주 잠깐이라도
            // 늦으면(특히 씬 시작 직후 첫 터치, EventSystem이 그 프레임에 아직 판정을 못 끝낸 경우) UI
            // 밖 빈 화면을 눌러도 "PC 마우스 클릭"으로 인식돼 공격이 나가버렸다. 모바일 기기에는애초에
            // 진짜 마우스가 없으니 - Application.isMobilePlatform이면 이 raw 마우스 버튼 판정 자체를
            // 통째로 건너뛴다(모바일 실제 입력은 전부 MobileJoystick/MobileTouchButton/
            // MobileAttackButton이 직접 호출하는 Trigger.../SetTouch... API로만 들어옴). PC/에디터
            // 테스트는 Application.isMobilePlatform이 false라 기존 동작 그대로 유지.
            var pointerOverUi = IsPointerOverUi();
            var mouseInputAllowed = !Application.isMobilePlatform && !pointerOverUi;

            var specialKeyDown = Input.GetKeyDown(KeyCode.K);
            var specialMouseDown = mouseInputAllowed && Input.GetMouseButtonDown(1);
            // isActionLocked/isSpecialFiring도 같이 확인 - 이유는 TriggerSpecialChargeStart()의
            // 주석 참고(터치와 같은 근본 원인, 공용으로 고침).
            if (!isCharging && !isActionLocked && !isSpecialFiring && (specialKeyDown || specialMouseDown))
            {
                StartSpecialCharge(specialKeyDown, specialMouseDown);
            }

            if (isCharging)
            {
                var chargeButtonReleased =
                    (specialChargeFromKeyboard && Input.GetKeyUp(KeyCode.K)) ||
                    (specialChargeFromMouse && Input.GetMouseButtonUp(1)) ||
                    (specialChargeFromTouch && !touchSpecialChargeHeld);
                var chargeButtonHeld =
                    (specialChargeFromKeyboard && Input.GetKey(KeyCode.K)) ||
                    (specialChargeFromMouse && Input.GetMouseButton(1)) ||
                    (specialChargeFromTouch && touchSpecialChargeHeld);
                if (chargeButtonReleased || !chargeButtonHeld)
                {
                    TryFireSpecial();
                }
                return;
            }

            if (isAttacking && (Input.GetKeyDown(KeyCode.J) || (mouseInputAllowed && Input.GetMouseButtonDown(0))))
            {
                attackRequestCount = Mathf.Clamp(attackRequestCount + 1, 1, 2);
                return;
            }

            if (isActionLocked || isCharging || isSpecialFiring)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) && jumpCount < 2)
            {
                Jump();
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) && Grounded)
            {
                PlayDash();
                return;
            }

            if (Input.GetKeyDown(KeyCode.J) || (mouseInputAllowed && Input.GetMouseButtonDown(0)))
            {
                if (IsAirborneForAnimation())
                {
                    PlayLockedWithEffect(jumpAttackFrames, 14f, jumpAttackEffectFrames, jumpAttackEffectOffset, jumpAttackEffectScale, 1);
                    return;
                }

                QueueAttack();
                return;
            }

            UpdateLoopAnimation();
        }

        private void FixedUpdate()
        {
            if (isDashing)
            {
                return;
            }

            if (isDead || GameplayFreezeGate.IsFrozen || isActionLocked || isCharging || isSpecialFiring)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }

            var horizontal = EffectiveHorizontalInput();
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                facing = horizontal > 0f ? 1 : -1;
                transform.localScale = new Vector3(facing, 1f, 1f);
            }

            rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        }

        private void Jump()
        {
            PlaySfx(jumpSfx);
            jumpCount++;
            lastJumpTime = Time.time;
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            StopLoopRoutine();
            SetJumpFrame(0);
        }

        private void QueueAttack()
        {
            StopLoopRoutine();
            attackRequestCount = Mathf.Clamp(attackRequestCount + 1, 1, 2);
            if (!isAttacking)
            {
                StopActionRoutine();
                actionRoutine = StartCoroutine(AttackComboRoutine());
            }
        }

        private IEnumerator AttackComboRoutine()
        {
            isActionLocked = true;
            isAttacking = true;
            rb.velocity = new Vector2(0f, rb.velocity.y);

            yield return PlayAttackStep(1, attack1Frames, attackEffect1Frames, attack1EffectOffset, attack1EffectScale, 13f, attack1FrameTimes, attack1EffectStartFrame, attack1DamageFrame, attack1SwingFrame, attack1SwingSfx);

            if (attackRequestCount >= 2)
            {
                StepForward(attack2StepForward);
                yield return PlayAttackStep(2, attack2Frames, attackEffect2Frames, attack2EffectOffset, attack2EffectScale, 13f, attack2FrameTimes, attack2EffectStartFrame, attack2DamageFrame, attack2SwingFrame, attack2SwingSfx);
            }

            attackRequestCount = 0;
            isAttacking = false;
            isActionLocked = false;
            actionRoutine = null;
            UpdateLoopAnimation(true);
        }

        private void StepForward(float distance)
        {
            transform.position += new Vector3(facing * distance, 0f, 0f);
        }

        private IEnumerator PlayAttackStep(int step, Sprite[] frames, Sprite[] effectFrames, Vector3 effectOffset, Vector3 effectScale, float fps, float[] frameTimes, int effectStartFrame, int damageFrame, int swingFrame, SfxCue swingSfx)
        {
            var delay = 1f / fps;
            var effectFrameIndex = Mathf.Clamp(effectStartFrame - 1, 0, Mathf.Max(0, frames.Length - 1));
            var damageFrameIndex = Mathf.Clamp(damageFrame - 1, 0, Mathf.Max(0, frames.Length - 1));
            var swingFrameIndex = Mathf.Clamp(swingFrame - 1, 0, Mathf.Max(0, frames.Length - 1));
            for (var i = 0; i < frames.Length; i++)
            {
                spriteRenderer.sprite = frames[i];
                if (i == swingFrameIndex)
                {
                    PlaySfx(swingSfx);
                }

                if (i == effectFrameIndex && attackEffect != null)
                {
                    attackEffect.Play(effectFrames, 18f, effectOffset, effectScale);
                }

                if (i == damageFrameIndex)
                {
                    DealDamageToMonsters(1, attackHitBoxOffset, attackHitBoxSize);
                }
                yield return new WaitForSeconds(FrameTimeAt(frameTimes, i, delay));
            }
        }

        private void PlayDash()
        {
            PlaySfx(dashSfx);
            StopActionRoutine();
            StopLoopRoutine();
            actionRoutine = StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            isActionLocked = true;
            isDashing = true;
            var elapsed = 0f;
            var frameTime = dashDuration / Mathf.Max(1, dashFrames.Length);
            var frame = 0;

            while (elapsed < dashDuration)
            {
                rb.velocity = new Vector2(facing * dashSpeed, 0f);
                if (dashFrames.Length > 0)
                {
                    spriteRenderer.sprite = dashFrames[Mathf.Clamp(frame, 0, dashFrames.Length - 1)];
                }
                frame++;
                elapsed += frameTime;
                yield return new WaitForSeconds(frameTime);
            }

            rb.velocity = Vector2.zero;
            isDashing = false;
            isActionLocked = false;
            actionRoutine = null;
            UpdateLoopAnimation(true);
        }

        private void StartSpecialCharge(bool fromKeyboard, bool fromMouse, bool fromTouch = false)
        {
            CacheComponents();
            StopActionRoutine();
            StopLoopRoutine();
            isCharging = true;
            isActionLocked = true;
            specialChargeFromKeyboard = fromKeyboard;
            specialChargeFromMouse = fromMouse;
            specialChargeFromTouch = fromTouch;
            specialChargeStartTime = Time.time;
            rb.velocity = Vector2.zero;
            PlaySpecialChargeEffect();
            sfxPlayer.PlayLoop(specialChargeLoopSfx);
            actionRoutine = StartCoroutine(SpecialChargePoseRoutine());
        }

        private void TryFireSpecial()
        {
            if (Time.time - specialChargeStartTime < Mathf.Max(0.1f, specialMinimumChargeTime - SpecialChargeInputGrace))
            {
                CancelSpecialCharge();
                return;
            }

            FireSpecial();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            GuiScaleUtility.Begin();

            // 좌하단 디버그 스택 - HP 텍스트 + (차징 중이면) 스페셜 게이지를 같은 x/width로 나란히
            // 쌓는다. 2026-08-11: 스페셜 게이지는 원래 화면 중앙에 크게 떠 있었는데, HP 텍스트 바로
            // 위에 비슷한 크기로 옮겨서 확인용 디버그 정보 한 묶음으로 보이게 함(사용자 확정).
            const float debugRectX = 20f;
            const float debugRectWidth = 255f;
            const float debugRectHeight = 27f;
            const float debugRectGap = 4f;
            var hpRect = new Rect(debugRectX, GuiScaleUtility.ReferenceHeight - debugRectHeight - 14f, debugRectWidth, debugRectHeight);

            if (showDebugHpText)
            {
                EnsureHpStyle();
                DrawGuiRect(hpRect, Color.white); // plain placeholder backing so black HP text stays readable over any ground color
                GUI.Label(hpRect, $"PLAYER  {HpDots(currentHp, maxHp)}", hpStyle);
            }

            if (!showDebugSpecialGauge || !isCharging)
            {
                return;
            }

            var gaugeRect = new Rect(debugRectX, hpRect.y - debugRectHeight - debugRectGap, debugRectWidth, debugRectHeight);
            var elapsed = Time.time - specialChargeStartTime;
            var readyText = elapsed >= specialMinimumChargeTime ? "READY" : "CHARGING";
            DrawSpecialChargeGauge(gaugeRect, elapsed, readyText);
        }
#endif

        private void CancelSpecialCharge()
        {
            StopActionRoutine();
            StopSpecialChargeEffect();
            sfxPlayer.StopLoop();
            isCharging = false;
            isActionLocked = false;
            specialChargeFromKeyboard = false;
            specialChargeFromMouse = false;
            specialChargeFromTouch = false;
            touchSpecialChargeHeld = false;
            actionRoutine = null;
            UpdateLoopAnimation(true);
        }

        private void FireSpecial()
        {
            CacheComponents();
            StopActionRoutine();
            StopLoopRoutine();
            StopSpecialChargeEffect();
            sfxPlayer.StopLoop();
            isCharging = false;
            specialChargeFromKeyboard = false;
            specialChargeFromMouse = false;
            specialChargeFromTouch = false;
            touchSpecialChargeHeld = false;
            isSpecialFiring = true;
            isActionLocked = true;
            rb.velocity = Vector2.zero;

            if (specialFireFrames != null && specialFireFrames.Length > 0)
            {
                spriteRenderer.sprite = specialFireFrames[Mathf.Min(2, specialFireFrames.Length - 1)];
            }

            actionRoutine = StartCoroutine(SpecialFireRoutine());
        }

        public void TakeDamage(
            int damage,
            int hitDirection = 0,
            float hitReactionDurationScale = 1f,
            float knockbackScale = 1f,
            bool useHeavyHitReaction = false)
        {
            if (isDead || GameplayFreezeGate.IsFrozen || Time.time - lastHitTime < hitInvincibleTime)
            {
                return;
            }

            lastHitTime = Time.time;
            currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, damage));
            InterruptActionState();
            var isHeavyHit = useHeavyHitReaction || damage >= 2;
            ApplyKnockback(
                hitDirection,
                (isHeavyHit ? knockdownKnockbackDistance : hitKnockbackDistance) * Mathf.Max(0f, knockbackScale));

            if (currentHp <= 0)
            {
                HandleDeath();
                return;
            }

            PlaySfx(isHeavyHit ? heavyHitSfx : hitSfx);
            var hitFps = isHeavyHit ? 10f : 12f;
            PlayLocked(
                isHeavyHit ? knockdownFrames : hitFrames,
                hitFps / Mathf.Max(0.01f, hitReactionDurationScale));
        }

        /// <summary>HP 아이템(HpItemPickup)이 도착했을 때 호출 - 이미 꽉 찬 상태면 아무 일도 안 함
        /// (사용자 스펙 5번), 남은 칸보다 많은 값이 들어와도 최대치를 절대 안 넘음(스펙 6번, 예:
        /// 최대 5칸 중 4칸 찬 상태에서 3칸짜리 아이템을 먹어도 5칸까지만 채워짐) - 둘 다
        /// Mathf.Min으로 한 줄에서 자연스럽게 처리됨, 별도 분기 필요 없음.</summary>
        public void Heal(int amount)
        {
            if (isDead || amount <= 0)
            {
                return;
            }

            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }

        /// <summary>
        /// Kills the player immediately, bypassing the normal damage/HP flow - used when they
        /// fall through a gap in the ground (see FallDeathZone) rather than being hit down to
        /// 0 HP in combat.
        /// </summary>
        public void KillByFalling()
        {
            if (isDead)
            {
                return;
            }

            currentHp = 0;
            HandleDeath();
        }

        private void HandleDeath()
        {
            if (isDead)
            {
                return;
            }

            PlaySfx(deathSfx);
            isDead = true;
            isDeathAnimationFinished = false;
            rb.velocity = Vector2.zero;
            StopActionRoutine();
            StopLoopRoutine();
            isActionLocked = true;
            actionRoutine = StartCoroutine(DeathRoutine());
        }

        /// <summary>Plays deathFrames once (holding on the last frame) then flips
        /// isDeathAnimationFinished - DeathScreenController watches that flag instead of IsDead so the
        /// death panel doesn't pop up mid-animation.</summary>
        private IEnumerator DeathRoutine()
        {
            yield return PlayOnce(deathFrames, 8f);
            isDeathAnimationFinished = true;
        }

        public bool OverlapsHurtBox(Vector2 otherCenter, Vector2 otherSize)
        {
            var center = HurtBoxCenter();
            return Mathf.Abs(center.x - otherCenter.x) <= (hurtBoxSize.x + otherSize.x) * 0.5f &&
                   Mathf.Abs(center.y - otherCenter.y) <= (hurtBoxSize.y + otherSize.y) * 0.5f;
        }

        private void InterruptActionState()
        {
            StopActionRoutine();
            StopLoopRoutine();
            StopSpecialChargeEffect();
            sfxPlayer.StopLoop();
            isActionLocked = false;
            isAttacking = false;
            isCharging = false;
            isSpecialFiring = false;
            isDashing = false;
            specialChargeFromKeyboard = false;
            specialChargeFromMouse = false;
            specialChargeFromTouch = false;
            touchSpecialChargeHeld = false;
            attackRequestCount = 0;
        }

        private void PlaySpecialChargeEffect()
        {
            var chargeEffect = specialChargeEffect != null ? specialChargeEffect : specialLaserEffect;
            if (chargeEffect == null)
            {
                return;
            }

            chargeEffect.PlayLoop(specialChargeEffectFrames, specialChargeEffectFps, specialChargeEffectOffset, specialChargeEffectScale);
        }

        private void StopSpecialChargeEffect()
        {
            var chargeEffect = specialChargeEffect != null ? specialChargeEffect : specialLaserEffect;
            if (chargeEffect != null)
            {
                chargeEffect.StopAndHide();
            }
        }

        private void PlaySfx(SfxCue cue)
        {
            sfxPlayer?.Play(cue);
        }

        private void ApplyKnockback(int direction, float distance)
        {
            if (direction == 0 || distance <= 0f)
            {
                return;
            }

            transform.position += new Vector3(Mathf.Sign(direction) * distance, 0f, 0f);
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

#if UNITY_EDITOR
        public void DebugForceSpecialFireForTest()
        {
            FireSpecial();
        }

        public void DebugSimulateChargedSpecialReleaseForTest()
        {
            isCharging = true;
            isActionLocked = true;
            specialChargeStartTime = Time.time - specialMinimumChargeTime - 0.25f;
            TryFireSpecial();
        }
#endif

        private IEnumerator SpecialFireRoutine()
        {
            if (specialFireFrames == null || specialFireFrames.Length == 0)
            {
                isSpecialFiring = false;
                isActionLocked = false;
                actionRoutine = null;
                UpdateLoopAnimation(true);
                yield break;
            }

            var delay = 1f / 10f;
            var damageFrameIndex = Mathf.Clamp(specialDamageFrame - 1, 0, specialFireFrames.Length - 1);
            var laserStartFrameIndex = Mathf.Clamp(specialLaserStartFrame - 1, 0, specialFireFrames.Length - 1);
            var groundBurstIndex = Mathf.Clamp(specialGroundBurstStartFrame - 1, 0, specialFireFrames.Length - 1);
            var playedDamage = false;
            var playedLaser = false;
            var playedGroundBurst = false;

            for (var i = 0; i < specialFireFrames.Length; i++)
            {
                spriteRenderer.sprite = specialFireFrames[i];
                if (!playedDamage && i >= damageFrameIndex)
                {
                    DealDamageToMonsters(3, specialHitBoxOffset, specialHitBoxSize);
                    playedDamage = true;
                }

                if (!playedLaser && i >= laserStartFrameIndex)
                {
                    PlaySfx(specialFireSfx);
                    if (specialLaserEffect != null)
                    {
                        specialLaserEffect.Play(specialLaserFrames, 10f, specialLaserOffset, specialLaserScale);
                    }

                    playedLaser = true;
                }

                if (!playedGroundBurst && i >= groundBurstIndex)
                {
                    var groundEffect = specialGroundBurstEffect != null ? specialGroundBurstEffect : specialLaserEffect;
                    if (groundEffect != null)
                    {
                        groundEffect.Play(specialGroundBurstFrames, 10f, specialGroundBurstOffset, specialGroundBurstScale);
                    }

                    playedGroundBurst = true;
                }

                yield return new WaitForSeconds(FrameTimeAt(specialFireFrameTimes, i, delay));
            }

            isSpecialFiring = false;
            isActionLocked = false;
            actionRoutine = null;
            UpdateLoopAnimation(true);
        }

        private IEnumerator SpecialChargePoseRoutine()
        {
            var frames = specialFireFrames != null && specialFireFrames.Length > 0 ? specialFireFrames : specialChargeFrames;
            if (frames == null || frames.Length == 0)
            {
                yield break;
            }

            spriteRenderer.sprite = frames[0];
            yield return new WaitForSeconds(FrameTimeAt(specialFireFrameTimes, 0, 0.12f));

            if (isCharging)
            {
                spriteRenderer.sprite = frames[Mathf.Min(1, frames.Length - 1)];
            }

            while (isCharging)
            {
                yield return null;
            }
        }

        private void PlayLocked(Sprite[] frames, float fps, bool holdLastFrame = false)
        {
            StopActionRoutine();
            StopLoopRoutine();
            actionRoutine = StartCoroutine(PlayLockedRoutine(frames, fps, holdLastFrame));
        }

        private void PlayLockedWithEffect(Sprite[] frames, float fps, Sprite[] effectFrames, Vector3 effectOffset, Vector3 effectScale, int effectFrameIndex)
        {
            StopActionRoutine();
            StopLoopRoutine();
            actionRoutine = StartCoroutine(PlayLockedWithEffectRoutine(frames, fps, effectFrames, effectOffset, effectScale, effectFrameIndex));
        }

        private IEnumerator PlayLockedRoutine(Sprite[] frames, float fps, bool holdLastFrame)
        {
            isActionLocked = true;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            yield return PlayOnce(frames, fps);
            if (!holdLastFrame)
            {
                isActionLocked = false;
                actionRoutine = null;
                UpdateLoopAnimation(true);
            }
        }

        private IEnumerator PlayLockedWithEffectRoutine(Sprite[] frames, float fps, Sprite[] effectFrames, Vector3 effectOffset, Vector3 effectScale, int effectFrameIndex)
        {
            isActionLocked = true;
            var delay = 1f / Mathf.Max(1f, fps);
            var jumpAttackId = ++nextAttackId;
            var hasDealtJumpAttackDamage = false;

            for (var i = 0; i < frames.Length; i++)
            {
                spriteRenderer.sprite = frames[i];
                if (i == effectFrameIndex)
                {
                    PlaySfx(jumpAttackSfx);
                    if (attackEffect != null)
                    {
                        attackEffect.Play(effectFrames, 18f, effectOffset, effectScale);
                    }
                }

                if (!hasDealtJumpAttackDamage && i >= Mathf.Max(0, effectFrameIndex - 1))
                {
                    hasDealtJumpAttackDamage = DealDamageToMonsters(1, jumpAttackHitBoxOffset, jumpAttackHitBoxSize, jumpAttackId);
                }
                yield return new WaitForSeconds(delay);
            }

            isActionLocked = false;
            actionRoutine = null;
            UpdateLoopAnimation(true);
        }

        private void PlayTemporary(Sprite[] frames, float fps, float duration)
        {
            StopLoopRoutine();
            StartCoroutine(TemporaryRoutine(frames, fps, duration));
        }

        private IEnumerator TemporaryRoutine(Sprite[] frames, float fps, float duration)
        {
            var end = Time.time + duration;
            var index = 0;
            var delay = 1f / Mathf.Max(1f, fps);
            while (Time.time < end && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[index % frames.Length];
                index++;
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator PlayOnce(Sprite[] frames, float fps)
        {
            var delay = 1f / Mathf.Max(1f, fps);
            foreach (var frame in frames)
            {
                spriteRenderer.sprite = frame;
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator PlayOnceFromIndex(Sprite[] frames, float fps, int startIndex, float[] frameTimes = null)
        {
            if (frames == null || frames.Length == 0)
            {
                yield break;
            }

            var delay = 1f / Mathf.Max(1f, fps);
            for (var i = Mathf.Clamp(startIndex, 0, frames.Length - 1); i < frames.Length; i++)
            {
                spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(FrameTimeAt(frameTimes, i, delay));
            }
        }

        private static float FrameTimeAt(float[] frameTimes, int index, float fallback)
        {
            if (frameTimes == null || index < 0 || index >= frameTimes.Length || frameTimes[index] <= 0f)
            {
                return Mathf.Max(0.01f, fallback);
            }

            return Mathf.Max(0.01f, frameTimes[index]);
        }

        private IEnumerator LoopFrames(Sprite[] frames, float fps)
        {
            var delay = 1f / Mathf.Max(1f, fps);
            var index = 0;
            while (frames.Length > 0)
            {
                spriteRenderer.sprite = frames[index % frames.Length];
                index++;
                yield return new WaitForSeconds(delay);
            }
        }

        private void UpdateLoopAnimation(bool forceRestart = false)
        {
            var horizontal = Mathf.Abs(EffectiveHorizontalInput());
            var shouldShowJump = IsAirborneForAnimation() || Time.time - lastJumpTime <= JumpAnimationHoldTime || Time.time <= landingFrameEndTime;
            if (shouldShowJump)
            {
                StopLoopRoutine();
                SetJumpFrame(SelectJumpFrameIndex());
                return;
            }

            var target = horizontal > 0.01f ? walkFrames : idleFrames;
            var fps = target == walkFrames ? 10f : 6f;

            if (!forceRestart && loopRoutine != null && currentLoopFrames == target)
            {
                return;
            }

            StopLoopRoutine();
            currentLoopFrames = target;
            loopRoutine = StartCoroutine(LoopFrames(target, fps));
        }

        private void StopActionRoutine()
        {
            if (actionRoutine != null)
            {
                StopCoroutine(actionRoutine);
                actionRoutine = null;
            }
        }

        private void StopLoopRoutine()
        {
            if (loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
                loopRoutine = null;
            }
            currentLoopFrames = null;
        }

        private bool IsAirborneForAnimation()
        {
            return !Grounded || jumpCount > 0 || Mathf.Abs(rb.velocity.y) > 0.1f;
        }

        /// <summary>지금 마우스/터치 커서가 uGUI 위(모바일 조그셔틀·버튼 등)에 있는지 - PC 마우스
        /// 입력 판정에만 걸어서, 터치 UI를 조작할 때 그 클릭이 PC 공격/스페셜 시작으로도 동시에
        /// 잡히는 걸 막는다. 키보드는 이 문제와 무관해서 안 건드림.</summary>
        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void UpdateLandingState()
        {
            if (!Grounded)
            {
                wasAirborne = true;
                return;
            }

            if (wasAirborne && Mathf.Abs(rb.velocity.y) <= 0.05f && Time.time - lastJumpTime > JumpAnimationHoldTime)
            {
                landingFrameEndTime = Time.time + LandingFrameHoldTime;
                wasAirborne = false;
            }
        }

        private int SelectJumpFrameIndex()
        {
            if (jumpFrames == null || jumpFrames.Length == 0)
            {
                return -1;
            }

            if (Time.time <= landingFrameEndTime)
            {
                return jumpFrames.Length - 1;
            }

            if (Time.time - lastJumpTime <= 0.12f)
            {
                return 0;
            }

            if (rb.velocity.y > 1.2f)
            {
                return Mathf.Min(1, jumpFrames.Length - 1);
            }

            return Mathf.Min(2, jumpFrames.Length - 1);
        }

        private void SetJumpFrame(int index)
        {
            if (jumpFrames == null || jumpFrames.Length == 0 || index < 0)
            {
                return;
            }

            spriteRenderer.sprite = jumpFrames[Mathf.Clamp(index, 0, jumpFrames.Length - 1)];
        }

        private bool DealDamageToMonsters(int damage, Vector2 localOffset, Vector2 size)
        {
            return DealDamageToMonsters(damage, localOffset, size, ++nextAttackId);
        }

        private bool DealDamageToMonsters(int damage, Vector2 localOffset, Vector2 size, int attackId)
        {
            var damagedAny = false;
            var center = AttackBoxCenter(localOffset);
            var hits = Physics2D.OverlapBoxAll(center, size, 0f);
            foreach (var hit in hits)
            {
                var monster = hit.GetComponentInParent<MonsterAActionTestController>();
                if (monster != null)
                {
                    monster.TakeDamage(damage, attackId, facing);
                    damagedAny = true;
                    continue;
                }

                var monsterB = hit.GetComponentInParent<MonsterBActionTestController>();
                if (monsterB != null)
                {
                    monsterB.TakeDamage(damage, attackId, facing);
                    damagedAny = true;
                    continue;
                }

                var monsterC = hit.GetComponentInParent<MonsterCActionTestController>();
                if (monsterC != null)
                {
                    monsterC.TakeDamage(damage, attackId, facing);
                    damagedAny = true;
                    continue;
                }

                // 몬스터가 아니라 스테이지 클리어 오브젝트 - HP/사망 없이 "맞았다" 표시만 남긴다.
                // 콤보/점프공격/스페셜 등 공격 종류 상관없이 이 자리 하나로 전부 인식됨(2026-08-12).
                var levelClearObject = hit.GetComponentInParent<LevelClearObjectController>();
                if (levelClearObject != null)
                {
                    levelClearObject.RegisterHit();
                    damagedAny = true;
                    continue;
                }

                // HP 회복 상자(2026-08-14) - 몬스터/클리어 오브젝트와 마찬가지로 공격 종류 상관없이
                // 한 번만 맞으면 열림.
                var hpItemBox = hit.GetComponentInParent<HpItemBoxController>();
                if (hpItemBox != null)
                {
                    hpItemBox.RegisterHit();
                    damagedAny = true;
                }
            }

            return damagedAny;
        }

        private Vector2 AttackBoxCenter(Vector2 localOffset)
        {
            return (Vector2)transform.position + new Vector2(localOffset.x * facing, localOffset.y);
        }

        private Vector2 HurtBoxCenter()
        {
            return (Vector2)transform.position + new Vector2(hurtBoxOffset.x * facing, hurtBoxOffset.y);
        }

        private void OnDrawGizmosSelected()
        {
            var previewFacing = Application.isPlaying ? facing : transform.localScale.x < 0f ? -1 : 1;

            Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.35f);
            Gizmos.DrawCube((Vector2)transform.position + new Vector2(hurtBoxOffset.x * previewFacing, hurtBoxOffset.y), hurtBoxSize);

            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.35f);
            Gizmos.DrawCube((Vector2)transform.position + new Vector2(attackHitBoxOffset.x * previewFacing, attackHitBoxOffset.y), attackHitBoxSize);

            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.3f);
            Gizmos.DrawCube((Vector2)transform.position + new Vector2(jumpAttackHitBoxOffset.x * previewFacing, jumpAttackHitBoxOffset.y), jumpAttackHitBoxSize);

            Gizmos.color = new Color(1f, 0.95f, 0.1f, 0.25f);
            Gizmos.DrawCube((Vector2)transform.position + new Vector2(specialHitBoxOffset.x * previewFacing, specialHitBoxOffset.y), specialHitBoxSize);
        }

#if UNITY_EDITOR
        private void EnsureHpStyle()
        {
            if (hpStyle != null)
            {
                return;
            }

            hpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };

            specialGaugeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13, // 255px 폭 좌하단 스택에 맞춰 축소(예전엔 520px 폭 중앙 표시용 24pt였음)
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
        }

        private void DrawSpecialChargeGauge(Rect outer, float elapsed, string readyText)
        {
            var progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, specialMinimumChargeTime));
            var inner = new Rect(outer.x + 3f, outer.y + 3f, (outer.width - 6f) * progress, outer.height - 6f);

            DrawGuiRect(outer, new Color(0f, 0f, 0f, 0.85f));
            DrawGuiRect(new Rect(outer.x + 1.5f, outer.y + 1.5f, outer.width - 3f, outer.height - 3f), new Color(1f, 1f, 1f, 0.9f));
            DrawGuiRect(inner, progress >= 1f ? new Color(0.15f, 0.15f, 0.15f, 1f) : new Color(0.45f, 0.45f, 0.45f, 1f));

            GUI.Label(outer, $"SPECIAL {elapsed:0.0}/{specialMinimumChargeTime:0.0} {readyText}", specialGaugeStyle);
        }

        private static void DrawGuiRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static string HpDots(int current, int max)
        {
            var dots = string.Empty;
            for (var i = 0; i < max; i++)
            {
                dots += i < current ? "●" : "○";
            }

            return dots;
        }
#endif
    }
}
