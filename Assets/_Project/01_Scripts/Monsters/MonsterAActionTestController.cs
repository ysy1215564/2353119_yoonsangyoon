using System.Collections;
using GameProject.Audio;
using GameProject.Core;
using UnityEngine;

namespace GameProject.Monsters
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class MonsterAActionTestController : MonoBehaviour
    {
        public enum PreviewMotion
        {
            Idle,
            Walk,
            Attack,
            Hit,
            Death
        }

        [Header("Stats")]
        [SerializeField] private int maxHp = 6;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float bodyStopDistance = 1.15f;
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private float hitInvincibleTime = 0.25f;
        [SerializeField] private float hitKnockbackDistance = 0.7f;
        [SerializeField] private int specialKnockbackSteps = 2;
        [SerializeField] private float specialKnockbackStepDelay = 0.08f;
        [SerializeField] private float deathKnockbackDistance = 0.25f;
        [SerializeField] private float deathHoldTime = 1.0f;
        [SerializeField] private float respawnDelay = 2.0f;
        [Tooltip("How far from THIS monster's own spawn point the player must be for it to notice/chase/attack at all - measured from spawn, not current position, so it can never wander further than this from home. Same idea as MonsterC's detectRange.")]
        [SerializeField] private float detectRange = 8f;
        [Tooltip("이 몬스터가 죽은 뒤 몇 번까지 다시 살아나는가 - 등장 횟수가 아니라 부활 횟수라, 실제로 나오는 횟수는 항상 이 값보다 1 크다. -1 = 무제한(기본값) / 0 = 부활 없음, 한 번 죽으면 끝(1번 등장) / 1 = 부활 1번(총 2번 등장) / 2 = 부활 2번(총 3번 등장). 씬에 놓인 몬스터마다 따로 정할 수 있고 Apply로 프리팹에 반영되지 않는다 - 난이도 조절용.")]
        [SerializeField] private int maxRespawnCount = -1;

        [Header("Ground Edge Guard")]
        [Tooltip("Never walks or gets knocked past the edge of solid ground - see MonsterGroundGuard.")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckFootOffsetY = 0.05f;
        [SerializeField] private float groundCheckDistance = 0.25f;

        [Header("Combat Boxes")]
        [SerializeField] private Vector2 hurtBoxSize = new Vector2(1.35f, 1.0f);
        [SerializeField] private Vector2 hurtBoxOffset = new Vector2(0f, 0.55f);
        [SerializeField] private Vector2 attackBoxSize = new Vector2(1.45f, 1.05f);
        [SerializeField] private Vector2 attackBoxOffset = new Vector2(1.0f, 0.65f);

        [Header("HP Display")]
        [Tooltip("HP dots float above this height (world units above the pivot). -1 = auto (uses the sprite's top edge, which includes any transparent padding in the source art). Set a value >= 0 to override once per this monster's resources so the dots sit right above the actual character instead of the art's empty margin - use the yellow gizmo sphere (select this object in Scene view) to preview without Play.")]
        [SerializeField] private float hpDisplayHeightOverride = -1f;

        [Header("Sprites")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] deathFrames;

        [Header("Frame Timing")]
        [Tooltip("Seconds per frame. Empty or non-positive entries use the motion's default timing.")]
        [SerializeField] private float[] idleFrameTimes;
        [SerializeField] private float[] walkFrameTimes;
        [SerializeField] private float[] attackFrameTimes;
        [SerializeField] private float[] hitFrameTimes;
        [SerializeField] private float[] deathFrameTimes;
        [Tooltip("1-based frame number where the attack hits the player.")]
        [SerializeField, Min(1)] private int attackDamageFrame = 3;

        [Header("SFX")]
        [SerializeField] private SfxCue attackSfx;
        [SerializeField] private SfxCue hitSfx;
        [SerializeField] private SfxCue deathSfx;
        [Tooltip("1-based frame number where the attack swing sound plays.")]
        [SerializeField, Min(1)] private int attackSwingFrame = 1;

        private SfxPlayer sfxPlayer;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D hurtCollider;
        private Transform player;
        private GameProject.Player.PlayerActionTestController playerController;
        private Coroutine routine;
        private Coroutine knockbackRoutine;
        private int currentHp;
        private int facing = -1;
        private float lastAttackTime = -999f;
        private float lastHitTime = -999f;
        private int lastAttackId = -1;
        private bool isLocked;
        private bool isDead;
        private bool previewMode;
        private Vector3 spawnPosition;
        private int respawnCount;
        private Sprite[] currentLoopFrames;
        private float loopTimer;
        private int loopIndex;
        private GUIStyle hpStyle;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;

        public void PlayPreviewMotion(PreviewMotion motion)
        {
            StopCurrentRoutine();
            previewMode = true;
            routine = StartCoroutine(PreviewRoutine(motion));
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            sfxPlayer = new SfxPlayer(transform);
            hurtCollider = GetComponent<BoxCollider2D>();
            hurtCollider.isTrigger = false;
            hurtCollider.size = hurtBoxSize;
            hurtCollider.offset = hurtBoxOffset;
            spawnPosition = transform.position;
            currentHp = maxHp;
        }

        private void Start()
        {
            FindPlayer();

            PlayLoop(idleFrames, true);
        }

        private void Update()
        {
            if (previewMode)
            {
                return;
            }

            if (player == null || playerController == null)
            {
                FindPlayer();
            }

            if (isDead)
            {
                return;
            }

            // 스테이지 클리어 등으로 월드가 얼어있는 동안 - 진행 중이던 행동 루틴(공격/피격 등)을
            // 즉시 끊고 IDLE로 고정, 새 행동 판단도 멈춘다(2026-08-12). GameplayFreezeGate는 그
            // 씬이 끝날 때까지 다시 안 풀리는 값이라 매 프레임 StopCurrentRoutine을 불러도 이미
            // 멈춘 상태면 안전하게 아무 일도 안 한다.
            if (GameplayFreezeGate.IsFrozen)
            {
                StopCurrentRoutine();
                isLocked = false;
                PlayLoop(idleFrames, true);
                UpdateLoopFrame();
                return;
            }

            if (player == null || isLocked)
            {
                return;
            }

            // Ignores the player entirely once they're outside detectRange of THIS
            // monster's own spawn point - stops the classic "every monster from every
            // earlier area eventually walks all the way to wherever the player currently
            // is" pileup, independent of how many times it has respawned.
            if (Mathf.Abs(player.position.x - spawnPosition.x) > detectRange)
            {
                PlayLoop(idleFrames);
                UpdateLoopFrame();
                return;
            }

            var delta = player.position.x - transform.position.x;
            if (Mathf.Abs(delta) > 0.05f)
            {
                facing = delta > 0f ? 1 : -1;
                FaceTarget();
            }

            if (Mathf.Abs(delta) <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                routine = StartCoroutine(AttackRoutine());
                return;
            }

            if (Mathf.Abs(delta) > bodyStopDistance)
            {
                var desiredDeltaX = Mathf.Sign(delta) * moveSpeed * Time.deltaTime;
                var allowedDeltaX = MonsterGroundGuard.ClampHorizontalMove(transform.position, desiredDeltaX, groundCheckFootOffsetY, groundCheckDistance, groundMask);
                transform.position += new Vector3(allowedDeltaX, 0f, 0f);
                PlayLoop(Mathf.Approximately(allowedDeltaX, 0f) ? idleFrames : walkFrames); // stopped at a ledge - stand instead of walking in place against it
            }
            else
            {
                PlayLoop(idleFrames);
            }

            UpdateLoopFrame();
        }

        private IEnumerator PreviewRoutine(PreviewMotion motion)
        {
            var frames = GetPreviewFrames(motion);
            if (frames == null || frames.Length == 0)
            {
                routine = null;
                yield break;
            }

            StopLoop();
            while (previewMode)
            {
                // Motion preview only cycles sprites - it doesn't go through TakeDamage()/
                // AttackRoutine() where the real gameplay SFX calls live, so the motion test
                // scene buttons would otherwise be silent even with clips wired on the prefab.
                if (motion == PreviewMotion.Hit)
                {
                    sfxPlayer.Play(hitSfx);
                }
                else if (motion == PreviewMotion.Death)
                {
                    sfxPlayer.Play(deathSfx);
                }

                for (var i = 0; i < frames.Length; i++)
                {
                    spriteRenderer.sprite = frames[i];
                    if (motion == PreviewMotion.Attack && i == GetFrameIndex(attackSwingFrame, frames.Length))
                    {
                        sfxPlayer.Play(attackSfx);
                    }

                    yield return new WaitForSeconds(GetPreviewFrameTime(motion, i));
                }
            }

            routine = null;
        }

        private Sprite[] GetPreviewFrames(PreviewMotion motion)
        {
            switch (motion)
            {
                case PreviewMotion.Walk:
                    return walkFrames;
                case PreviewMotion.Attack:
                    return attackFrames;
                case PreviewMotion.Hit:
                    return hitFrames;
                case PreviewMotion.Death:
                    return deathFrames;
                default:
                    return idleFrames;
            }
        }

        private float GetPreviewFrameTime(PreviewMotion motion, int index)
        {
            if (motion == PreviewMotion.Attack)
                return GetFrameTime(attackFrameTimes, index, 1f / 8f);
            if (motion == PreviewMotion.Hit)
                return GetFrameTime(hitFrameTimes, index, index == 0 ? 0.08f : 0.5f);
            if (motion == PreviewMotion.Death)
                return GetFrameTime(deathFrameTimes, index, 1f / 7f);
            if (motion == PreviewMotion.Idle)
                return GetFrameTime(idleFrameTimes, index, 1f / 6f);
            return GetFrameTime(walkFrameTimes, index, 1f / 6f);
        }

        private void FindPlayer()
        {
            playerController = FindObjectOfType<GameProject.Player.PlayerActionTestController>();
            player = playerController != null ? playerController.transform : null;
        }

        public void TakeDamage(int damage, int attackId, int hitDirection)
        {
            if (isDead || GameplayFreezeGate.IsFrozen || attackId == lastAttackId || Time.time - lastHitTime < hitInvincibleTime)
            {
                return;
            }

            lastAttackId = attackId;
            lastHitTime = Time.time;
            currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, damage));
            ApplyDamageKnockback(damage, hitDirection);

            if (currentHp <= 0)
            {
                sfxPlayer.Play(deathSfx);
                StopCurrentRoutine();
                routine = StartCoroutine(DeathRoutine());
                return;
            }

            sfxPlayer.Play(hitSfx);
            StopCurrentRoutine();
            routine = StartCoroutine(HitRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            isLocked = true;
            StopLoop();
            lastAttackTime = Time.time;

            for (var i = 0; i < attackFrames.Length; i++)
            {
                spriteRenderer.sprite = attackFrames[i];
                if (i == GetFrameIndex(attackSwingFrame, attackFrames.Length))
                {
                    sfxPlayer.Play(attackSfx);
                }

                if (i == GetFrameIndex(attackDamageFrame, attackFrames.Length))
                {
                    TryDamagePlayer();
                }
                yield return new WaitForSeconds(GetFrameTime(attackFrameTimes, i, 1f / 8f));
            }

            isLocked = false;
            routine = null;
            PlayLoop(idleFrames, true);
        }

        private static float GetFrameTime(float[] times, int index, float fallback)
        {
            if (times != null &&
                index >= 0 &&
                index < times.Length &&
                times[index] > 0f)
            {
                return times[index];
            }

            return fallback;
        }

        private static int GetFrameIndex(int oneBasedFrame, int frameCount)
        {
            return Mathf.Clamp(oneBasedFrame - 1, 0, Mathf.Max(0, frameCount - 1));
        }

        private IEnumerator HitRoutine()
        {
            isLocked = true;
            StopLoop();
            yield return PlayHitReaction();
            isLocked = false;
            routine = null;
            PlayLoop(idleFrames, true);
        }

        private IEnumerator DeathRoutine()
        {
            isDead = true;
            // 죽음 판정 즉시 콜라이더 해제(2026-08-17, 사용자 확정) - 죽는 모션/사라지는 타이밍은
            // 그대로 두고, 플레이어가 물리적으로 막히지 않도록 판정 순간 바로 통과 가능하게 만든다.
            hurtCollider.enabled = false;
            isLocked = true;
            StopLoop();
            yield return PlayOnce(deathFrames, deathFrameTimes, 1f / 7f);

            if (deathFrames != null && deathFrames.Length > 0)
            {
                spriteRenderer.sprite = deathFrames[deathFrames.Length - 1];
            }

            yield return new WaitForSeconds(deathHoldTime);
            spriteRenderer.enabled = false;

            if (maxRespawnCount >= 0 && respawnCount >= maxRespawnCount)
            {
                yield break; // used up its respawns - stays dead/hidden for the rest of the run
            }

            yield return new WaitForSeconds(respawnDelay);
            respawnCount++;
            Respawn();
        }

        private void Respawn()
        {
            StopCurrentRoutine();
            transform.position = spawnPosition;
            currentHp = maxHp;
            lastAttackTime = -999f;
            lastHitTime = -999f;
            lastAttackId = -1;
            isDead = false;
            isLocked = false;
            spriteRenderer.enabled = true;
            hurtCollider.enabled = true;
            PlayLoop(idleFrames, true);
        }

        private IEnumerator PlayOnce(Sprite[] frames, float[] frameTimes, float fallback)
        {
            if (frames == null || frames.Length == 0)
            {
                yield break;
            }

            for (var i = 0; i < frames.Length; i++)
            {
                spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(GetFrameTime(frameTimes, i, fallback));
            }
        }

        private IEnumerator PlayHitReaction()
        {
            if (hitFrames == null || hitFrames.Length == 0)
            {
                yield break;
            }

            for (var i = 0; i < hitFrames.Length; i++)
            {
                spriteRenderer.sprite = hitFrames[i];
                yield return new WaitForSeconds(GetFrameTime(hitFrameTimes, i, i == 0 ? 0.08f : 0.5f));
            }
        }

        private void TryDamagePlayer()
        {
            if (playerController == null)
            {
                return;
            }

            if (playerController.OverlapsHurtBox(AttackBoxCenter(), attackBoxSize))
            {
                playerController.TakeDamage(1, facing);
            }
        }

        private void FaceTarget()
        {
            // Monster A source art faces left, so visual scale is opposite of the logical attack direction.
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * -facing, transform.localScale.y, transform.localScale.z);
        }

        private void ApplyKnockback(int direction, float distance)
        {
            if (direction == 0 || distance <= 0f)
            {
                return;
            }

            // Never falls into a gap even when knocked repeatedly - stops right at the edge
            // instead (see MonsterGroundGuard). Covers normal hits, the multi-step special
            // knockback, and the death-knockback below, since they all route through here.
            var desiredDeltaX = Mathf.Sign(direction) * distance;
            var allowedDeltaX = MonsterGroundGuard.ClampHorizontalMove(transform.position, desiredDeltaX, groundCheckFootOffsetY, groundCheckDistance, groundMask);
            transform.position += new Vector3(allowedDeltaX, 0f, 0f);
        }

        private void ApplyDamageKnockback(int damage, int hitDirection)
        {
            if (currentHp <= 0)
            {
                ApplyKnockback(hitDirection, deathKnockbackDistance);
                return;
            }

            if (damage >= 3)
            {
                if (knockbackRoutine != null)
                {
                    StopCoroutine(knockbackRoutine);
                }

                knockbackRoutine = StartCoroutine(SpecialKnockbackRoutine(hitDirection));
                return;
            }

            ApplyKnockback(hitDirection, hitKnockbackDistance);
        }

        private IEnumerator SpecialKnockbackRoutine(int hitDirection)
        {
            var steps = Mathf.Max(1, specialKnockbackSteps);
            for (var i = 0; i < steps; i++)
            {
                ApplyKnockback(hitDirection, hitKnockbackDistance);
                yield return new WaitForSeconds(Mathf.Max(0f, specialKnockbackStepDelay));
            }

            knockbackRoutine = null;
        }

        private void PlayLoop(Sprite[] frames, bool forceRestart = false)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (!forceRestart && currentLoopFrames == frames)
            {
                return;
            }

            currentLoopFrames = frames;
            loopTimer = 0f;
            loopIndex = 0;
            spriteRenderer.sprite = frames[0];
        }

        private void StopLoop()
        {
            currentLoopFrames = null;
            loopTimer = 0f;
            loopIndex = 0;
        }

        private void UpdateLoopFrame()
        {
            if (currentLoopFrames == null || currentLoopFrames.Length == 0)
            {
                return;
            }

            loopTimer += Time.deltaTime;
            var delay = GetLoopFrameTime(loopIndex);
            while (loopTimer >= delay)
            {
                loopTimer -= delay;
                loopIndex = (loopIndex + 1) % currentLoopFrames.Length;
                spriteRenderer.sprite = currentLoopFrames[loopIndex];
                delay = GetLoopFrameTime(loopIndex);
            }
        }

        private float GetLoopFrameTime(int index)
        {
            if (currentLoopFrames == idleFrames)
                return GetFrameTime(idleFrameTimes, index, 1f / 6f);
            if (currentLoopFrames == walkFrames)
                return GetFrameTime(walkFrameTimes, index, 1f / 6f);
            return 1f / 6f;
        }

        private void StopCurrentRoutine()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            isLocked = false;
        }

        private Vector2 AttackBoxCenter()
        {
            // Monster A source art faces left, so the authored attack offset is mirrored opposite to logical facing.
            return (Vector2)transform.position + new Vector2(attackBoxOffset.x * -facing, attackBoxOffset.y);
        }

        // Where the HP dots float above this instance. Defaults to the sprite's rendered top edge
        // (which includes any transparent padding baked into the source art); hpDisplayHeightOverride
        // lets a specific monster's art be calibrated once so the dots sit right above the actual
        // character instead of empty margin. Falls back to GetComponent when spriteRenderer hasn't
        // been cached yet (Awake only runs in Play mode, but this is also called from the editor
        // gizmo preview while not playing).
        private Vector3 GetHpDisplayWorldPosition()
        {
            if (hpDisplayHeightOverride >= 0f)
            {
                return new Vector3(transform.position.x, transform.position.y + hpDisplayHeightOverride, transform.position.z);
            }

            var renderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            var offsetY = renderer != null ? renderer.bounds.max.y - transform.position.y + 0.15f : 1.2f;
            return new Vector3(transform.position.x, transform.position.y + offsetY, transform.position.z);
        }

        private void OnDrawGizmosSelected()
        {
            var previewFacing = Application.isPlaying ? -facing : transform.localScale.x < 0f ? -1 : 1;

            Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.35f);
            Gizmos.DrawCube((Vector2)transform.position + new Vector2(hurtBoxOffset.x * previewFacing, hurtBoxOffset.y), hurtBoxSize);

            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.35f);
            Gizmos.DrawCube((Vector2)transform.position + new Vector2(attackBoxOffset.x * previewFacing, attackBoxOffset.y), attackBoxSize);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetHpDisplayWorldPosition(), 0.08f);
        }

        // 2026-08-12: 원래 #if UNITY_EDITOR로 감싸서 에디터에서만 보이던 디버그 HP 점 표시 -
        // 사용자가 실제 빌드로 확인해보고 "지금 에디터에서 보이는 그대로가 마음에 든다, 몬스터 HP는
        // 그래픽 UI를 따로 안 만들고 이 표시를 빌드에도 그대로 넣어달라"고 확정해서 가드를 뺐다.
        // GuiScaleUtility/GUI.Label 등 여기서 쓰는 것들은 UnityEditor 의존이 전혀 없어서 빌드에서도
        // 문제없이 그대로 동작한다.
        private void OnGUI()
        {
            if (isDead || Camera.main == null)
            {
                return;
            }

            GuiScaleUtility.Begin();
            EnsureHpStyle();

            // Draw HP dots floating above this specific instance's head (world-space -> GUI
            // point) instead of a fixed screen corner - a fixed corner only works when exactly
            // one of this monster type exists, but a stage can have several.
            if (!GuiScaleUtility.TryWorldToReferencePoint(Camera.main, GetHpDisplayWorldPosition(), out var point))
            {
                return;
            }

            const float width = 160f;
            const float height = 22f;
            var hpRect = new Rect(point.x - width * 0.5f, point.y - height, width, height);
            GUI.Label(hpRect, HpDots(currentHp, maxHp), hpStyle);
        }

        private void EnsureHpStyle()
        {
            if (hpStyle != null)
            {
                return;
            }

            hpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black }
            };
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
    }
}
