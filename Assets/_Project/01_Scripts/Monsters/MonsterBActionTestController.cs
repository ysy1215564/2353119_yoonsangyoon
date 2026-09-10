using System.Collections;
using GameProject.Audio;
using GameProject.Core;
using UnityEngine;

namespace GameProject.Monsters
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class MonsterBActionTestController : MonoBehaviour
    {
        public enum PreviewMotion
        {
            Idle,
            Walk,
            Attack1,
            Attack2,
            Hit,
            Death
        }

        [Header("Stats")]
        [SerializeField] private int maxHp = 8;
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float attackRange = 3.2f;
        [SerializeField] private float bodyStopDistance = 1.45f;
        [SerializeField] private float attackCooldown = 1.3f;
        [SerializeField] private float hitInvincibleTime = 0.25f;
        [SerializeField] private float hitKnockbackDistance = 0.55f;
        [SerializeField] private float deathHoldTime = 1f;
        [SerializeField] private float respawnDelay = 2f;
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
        [SerializeField] private Vector2 hurtBoxSize = new Vector2(1.6f, 1.4f);
        [SerializeField] private Vector2 hurtBoxOffset = new Vector2(0f, 1.35f);
        [SerializeField] private Vector2 attack1BoxSize = new Vector2(2.7f, 1.8f);
        [SerializeField] private Vector2 attack1BoxOffset = new Vector2(-1.35f, 0.8f);
        [SerializeField] private Vector2 attack2BoxSize = new Vector2(5.2f, 2.4f);
        [SerializeField] private Vector2 attack2BoxOffset = new Vector2(-2.6f, 1.2f);

        [Header("HP Display")]
        [Tooltip("HP dots float above this height (world units above the pivot). -1 = auto (uses the sprite's top edge, which includes any transparent padding in the source art). Set a value >= 0 to override once per this monster's resources so the dots sit right above the actual character instead of the art's empty margin - use the yellow gizmo sphere (select this object in Scene view) to preview without Play.")]
        [SerializeField] private float hpDisplayHeightOverride = -1f;

        [Header("Preview / Sprites")]
        // 2026-08-24: previewMotion 필드 제거. MonsterC는 Start()에서 이 값을 읽어 첫 동작을 정하지만
        // B는 어디서도 읽지 않아 CS0414(할당만 되고 쓰이지 않음) 경고만 내고 있었다 - 학생 Console에
        // 그대로 보이는 경고라 정리. 동작 선택은 MonsterB_ActionTest 씬의 화면 버튼이 PlayPreviewMotion()을
        // 직접 호출하는 방식이므로 이 필드 없이도 그대로 동작한다. PreviewMotion 열거형 자체는 계속 쓰인다.
        [SerializeField, Min(1f)] private float defaultFramesPerSecond = 8f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attack1Frames;
        [SerializeField] private Sprite[] attack2Frames;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] deathFrames;
        [Header("Frame Timing")]
        [SerializeField] private bool reverseWalkFrameOrder = true;
        [SerializeField] private float[] idleFrameTimes;
        [SerializeField] private float[] walkFrameTimes;
        [SerializeField] private float[] attack1FrameTimes;
        [SerializeField] private float[] attack2FrameTimes;
        [SerializeField] private float[] hitFrameTimes;
        [SerializeField] private float[] deathFrameTimes;

        [Header("Attack Effects")]
        [SerializeField] private SpriteRenderer attack1EffectRenderer;
        [SerializeField] private Sprite[] attack1EffectFrames;
        [SerializeField, Min(1)] private int attack1EffectStartFrame = 15;
        [SerializeField] private Vector3 attack1EffectOffset;
        [SerializeField] private Vector3 attack1EffectScale = Vector3.one;
        [Tooltip("1-based frame number where Attack 1 hits the player.")]
        [SerializeField, Min(1)] private int attack1DamageFrame = 15;
        [SerializeField] private SpriteRenderer attack2EffectRenderer;
        [SerializeField] private Sprite[] attack2EffectFrames;
        [SerializeField, Min(1)] private int attack2EffectStartFrame = 15;
        [SerializeField] private Vector3 attack2EffectOffset;
        [SerializeField] private Vector3 attack2EffectScale = Vector3.one;
        [Tooltip("1-based frame number where Attack 2 hits the player.")]
        [SerializeField, Min(1)] private int attack2DamageFrame = 15;

        [Header("SFX")]
        [SerializeField] private SfxCue attack1Sfx;
        [SerializeField] private SfxCue attack2Sfx;
        [SerializeField] private SfxCue hitSfx;
        [SerializeField] private SfxCue deathSfx;

        private SfxPlayer sfxPlayer;
        private SpriteRenderer rendererCache;
        private BoxCollider2D hurtCollider;
        private Transform player;
        private GameProject.Player.PlayerActionTestController playerController;
        private Coroutine routine;
        private int currentHp;
        private int facing = -1;
        private int lastAttackId = -1;
        private float lastAttackTime = -999f;
        private float lastHitTime = -999f;
        private bool useAttack2Next;
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
            rendererCache = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            sfxPlayer = new SfxPlayer(transform);
            hurtCollider = GetComponent<BoxCollider2D>();
            hurtCollider.isTrigger = false;
            hurtCollider.size = hurtBoxSize;
            hurtCollider.offset = hurtBoxOffset;
            spawnPosition = transform.position;
            currentHp = maxHp;
            ApplyEffectTransforms();
            PlayLoop(idleFrames, true);
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            if (previewMode)
                return;

            if (player == null || playerController == null)
                FindPlayer();

            if (isDead)
                return;

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
                return;

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

            float delta = player.position.x - transform.position.x;
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

        private void FindPlayer()
        {
            playerController = FindObjectOfType<GameProject.Player.PlayerActionTestController>();
            player = playerController != null ? playerController.transform : null;
        }

        public void TakeDamage(int damage, int attackId, int hitDirection)
        {
            if (isDead || GameplayFreezeGate.IsFrozen || attackId == lastAttackId || Time.time - lastHitTime < hitInvincibleTime)
                return;

            lastAttackId = attackId;
            lastHitTime = Time.time;
            currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, damage));

            // Never falls into a gap even when knocked repeatedly - stops right at the edge
            // instead (see MonsterGroundGuard), covering both a normal hit and the death hit.
            var desiredDeltaX = Mathf.Sign(hitDirection) * hitKnockbackDistance;
            var allowedDeltaX = MonsterGroundGuard.ClampHorizontalMove(transform.position, desiredDeltaX, groundCheckFootOffsetY, groundCheckDistance, groundMask);
            transform.position += new Vector3(allowedDeltaX, 0f, 0f);

            sfxPlayer.Play(currentHp <= 0 ? deathSfx : hitSfx);

            StopCurrentRoutine();
            routine = currentHp <= 0 ? StartCoroutine(DeathRoutine()) : StartCoroutine(HitRoutine());
        }

        private IEnumerator PreviewRoutine(PreviewMotion motion)
        {
            Sprite[] frames = GetPreviewFrames(motion);
            if (frames == null || frames.Length == 0)
                yield break;

            // Motion preview only cycles sprites - it doesn't go through TakeDamage()/
            // AttackRoutine() where the real gameplay SFX calls live, so the motion test
            // scene buttons would otherwise be silent even with clips wired on the prefab.
            var attackSfxFrame = motion == PreviewMotion.Attack1
                ? GetFrameIndex(attack1DamageFrame, frames.Length)
                : motion == PreviewMotion.Attack2
                    ? GetFrameIndex(attack2EffectStartFrame, frames.Length)
                    : -1;

            while (previewMode)
            {
                if (motion == PreviewMotion.Hit)
                {
                    sfxPlayer.Play(hitSfx);
                }
                else if (motion == PreviewMotion.Death)
                {
                    sfxPlayer.Play(deathSfx);
                }

                for (int i = 0; i < frames.Length; i++)
                {
                    rendererCache.sprite = GetPreviewSprite(motion, frames, i);
                    UpdatePreviewEffect(motion, i);
                    if (i == attackSfxFrame)
                    {
                        sfxPlayer.Play(motion == PreviewMotion.Attack2 ? attack2Sfx : attack1Sfx);
                    }

                    yield return new WaitForSeconds(GetPreviewFrameTime(motion, i));
                }
            }

            HideEffects();
            routine = null;
        }

        private Sprite[] GetPreviewFrames(PreviewMotion motion)
        {
            switch (motion)
            {
                case PreviewMotion.Walk:
                    return walkFrames;
                case PreviewMotion.Attack1:
                    return attack1Frames;
                case PreviewMotion.Attack2:
                    return attack2Frames;
                case PreviewMotion.Hit:
                    return hitFrames;
                case PreviewMotion.Death:
                    return deathFrames;
                default:
                    return idleFrames;
            }
        }

        private float GetPreviewFrameTime(PreviewMotion motion, int frameIndex)
        {
            if (motion == PreviewMotion.Attack1)
                return GetFrameTime(attack1FrameTimes, frameIndex);
            if (motion == PreviewMotion.Attack2)
                return GetFrameTime(attack2FrameTimes, frameIndex);
            if (motion == PreviewMotion.Hit)
                return GetFrameTime(hitFrameTimes, frameIndex);
            if (motion == PreviewMotion.Death)
                return GetFrameTime(deathFrameTimes, frameIndex);
            if (motion == PreviewMotion.Walk)
                return GetFrameTime(walkFrameTimes, GetWalkFrameIndex(frameIndex));
            return GetFrameTime(idleFrameTimes, frameIndex);
        }

        private void UpdatePreviewEffect(PreviewMotion motion, int frameIndex)
        {
            HideEffects();
            if (motion == PreviewMotion.Attack1)
            {
                ShowPreviewEffect(attack1EffectRenderer, attack1EffectFrames, attack1EffectStartFrame, frameIndex);
            }
            else if (motion == PreviewMotion.Attack2)
            {
                ShowPreviewEffect(attack2EffectRenderer, attack2EffectFrames, attack2EffectStartFrame, frameIndex);
            }
        }

        private static void ShowPreviewEffect(SpriteRenderer effectRenderer, Sprite[] frames, int startFrame, int frameIndex)
        {
            if (effectRenderer == null || frames == null || frames.Length == 0)
                return;

            int effectIndex = frameIndex - Mathf.Max(0, startFrame - 1);
            if (effectIndex < 0 || effectIndex >= frames.Length)
                return;

            effectRenderer.sprite = frames[effectIndex];
            effectRenderer.enabled = true;
        }

        private IEnumerator AttackRoutine()
        {
            isLocked = true;
            lastAttackTime = Time.time;
            StopLoop();

            bool hasAttack1 = attack1Frames != null && attack1Frames.Length > 0;
            bool hasAttack2 = attack2Frames != null && attack2Frames.Length > 0;
            bool useAttack2 = hasAttack2 && (!hasAttack1 || useAttack2Next);
            Sprite[] frames = useAttack2 ? attack2Frames : attack1Frames;
            if (frames == null || frames.Length == 0)
                frames = useAttack2 ? attack1Frames : attack2Frames;

            if (hasAttack1 && hasAttack2)
                useAttack2Next = !useAttack2Next;

            if (frames == null || frames.Length == 0)
            {
                isLocked = false;
                routine = null;
                PlayLoop(idleFrames, true);
                yield break;
            }

            var damageFrame = GetFrameIndex(useAttack2 ? attack2DamageFrame : attack1DamageFrame, frames.Length);
            var sfxFrame = GetFrameIndex(useAttack2 ? attack2EffectStartFrame : attack1DamageFrame, frames.Length);

            for (int i = 0; i < frames.Length; i++)
            {
                rendererCache.sprite = frames[i];
                UpdateAttackEffect(useAttack2, i);

                if (i == sfxFrame)
                {
                    sfxPlayer.Play(useAttack2 ? attack2Sfx : attack1Sfx);
                }

                if (i == damageFrame)
                {
                    TryDamagePlayer(
                        useAttack2 ? attack2BoxOffset : attack1BoxOffset,
                        useAttack2 ? attack2BoxSize : attack1BoxSize,
                        useAttack2);
                }

                yield return new WaitForSeconds(GetFrameTime(useAttack2 ? attack2FrameTimes : attack1FrameTimes, i));
            }

            HideEffects();
            isLocked = false;
            routine = null;
            PlayLoop(idleFrames, true);
        }

        private IEnumerator HitRoutine()
        {
            isLocked = true;
            StopLoop();
            if (hitFrames != null)
            {
                for (var i = 0; i < hitFrames.Length; i++)
                {
                    rendererCache.sprite = hitFrames[i];
                    yield return new WaitForSeconds(GetFrameTime(hitFrameTimes, i));
                }
            }
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
            HideEffects();
            if (deathFrames != null)
            {
                for (var i = 0; i < deathFrames.Length; i++)
                {
                    rendererCache.sprite = deathFrames[i];
                    yield return new WaitForSeconds(GetFrameTime(deathFrameTimes, i));
                }
            }
            yield return new WaitForSeconds(deathHoldTime);
            rendererCache.enabled = false;

            if (maxRespawnCount >= 0 && respawnCount >= maxRespawnCount)
            {
                routine = null;
                yield break; // used up its respawns - stays dead/hidden for the rest of the run
            }

            yield return new WaitForSeconds(respawnDelay);
            respawnCount++;
            transform.position = spawnPosition;
            currentHp = maxHp;
            lastAttackId = -1;
            lastAttackTime = -999f;
            lastHitTime = -999f;
            useAttack2Next = false;
            isDead = false;
            isLocked = false;
            rendererCache.enabled = true;
            hurtCollider.enabled = true;
            PlayLoop(idleFrames, true);
            routine = null;
        }

        private void TryDamagePlayer(Vector2 offset, Vector2 size, bool useAttack2)
        {
            if (playerController != null && playerController.OverlapsHurtBox(AttackBoxCenter(offset), size))
                playerController.TakeDamage(useAttack2 ? 2 : 1, facing, 2f, 2f, useAttack2);
        }

        private void UpdateAttackEffect(bool useAttack2, int frameIndex)
        {
            HideEffects();
            if (useAttack2)
            {
                ShowEffect(
                    attack2EffectRenderer,
                    attack2EffectFrames,
                    attack2EffectStartFrame,
                    frameIndex,
                    attack2EffectOffset,
                    attack2EffectScale);
            }
            else
            {
                ShowEffect(
                    attack1EffectRenderer,
                    attack1EffectFrames,
                    attack1EffectStartFrame,
                    frameIndex,
                    attack1EffectOffset,
                    attack1EffectScale);
            }
        }

        private Vector2 AttackBoxCenter(Vector2 offset)
        {
            // Monster B source art faces left, so mirror authored offsets against logical facing.
            return (Vector2)transform.position + new Vector2(offset.x * -facing, offset.y);
        }

        private void FaceTarget()
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * -facing, transform.localScale.y, transform.localScale.z);
        }

        private float GetFrameTime(float[] times, int index)
        {
            if (times != null && index >= 0 && index < times.Length && times[index] > 0f)
                return times[index];
            return 1f / Mathf.Max(1f, defaultFramesPerSecond);
        }

        private static int GetFrameIndex(int oneBasedFrame, int frameCount)
        {
            return Mathf.Clamp(oneBasedFrame - 1, 0, Mathf.Max(0, frameCount - 1));
        }

        private void PlayLoop(Sprite[] frames, bool forceRestart = false)
        {
            if (frames == null || frames.Length == 0 || (!forceRestart && currentLoopFrames == frames))
                return;
            currentLoopFrames = frames;
            loopTimer = 0f;
            loopIndex = 0;
            rendererCache.sprite = GetLoopSprite(loopIndex);
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
                return;
            loopTimer += Time.deltaTime;
            float delay = GetLoopFrameTime(loopIndex);
            while (loopTimer >= delay)
            {
                loopTimer -= delay;
                loopIndex = (loopIndex + 1) % currentLoopFrames.Length;
                rendererCache.sprite = GetLoopSprite(loopIndex);
                delay = GetLoopFrameTime(loopIndex);
            }
        }

        private Sprite GetPreviewSprite(PreviewMotion motion, Sprite[] frames, int index)
        {
            if (motion == PreviewMotion.Walk && reverseWalkFrameOrder)
                return frames[frames.Length - 1 - index];
            return frames[index];
        }

        private Sprite GetLoopSprite(int index)
        {
            if (currentLoopFrames == walkFrames && reverseWalkFrameOrder)
                return currentLoopFrames[currentLoopFrames.Length - 1 - index];
            return currentLoopFrames[index];
        }

        private float GetLoopFrameTime(int index)
        {
            if (currentLoopFrames == idleFrames)
                return GetFrameTime(idleFrameTimes, index);
            if (currentLoopFrames == walkFrames)
                return GetFrameTime(walkFrameTimes, GetWalkFrameIndex(index));
            return 1f / Mathf.Max(1f, defaultFramesPerSecond);
        }

        private int GetWalkFrameIndex(int index)
        {
            if (!reverseWalkFrameOrder || walkFrames == null || walkFrames.Length == 0)
                return index;
            return walkFrames.Length - 1 - index;
        }

        private void StopCurrentRoutine()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            isLocked = false;
            HideEffects();
        }

        private void ApplyEffectTransforms()
        {
            ApplyEffectTransform(attack1EffectRenderer, attack1EffectOffset, attack1EffectScale);
            ApplyEffectTransform(attack2EffectRenderer, attack2EffectOffset, attack2EffectScale);
        }

        private static void ApplyEffectTransform(SpriteRenderer effectRenderer, Vector3 offset, Vector3 scale)
        {
            if (effectRenderer == null)
                return;
            effectRenderer.transform.localPosition = offset;
            effectRenderer.transform.localScale = scale;
        }

        private void ShowEffect(
            SpriteRenderer effectRenderer,
            Sprite[] frames,
            int startFrame,
            int frameIndex,
            Vector3 offset,
            Vector3 scale)
        {
            if (effectRenderer == null || frames == null || frames.Length == 0)
                return;

            int effectIndex = frameIndex - Mathf.Max(0, startFrame - 1);
            if (effectIndex < 0 || effectIndex >= frames.Length)
                return;

            effectRenderer.transform.localPosition = offset;
            effectRenderer.transform.localScale = scale;
            effectRenderer.sprite = frames[effectIndex];
            effectRenderer.enabled = true;
        }

        private void HideEffects()
        {
            if (attack1EffectRenderer != null) attack1EffectRenderer.enabled = false;
            if (attack2EffectRenderer != null) attack2EffectRenderer.enabled = false;
        }

        // Where the HP dots float above this instance. Defaults to the sprite's rendered top edge
        // (which includes any transparent padding baked into the source art); hpDisplayHeightOverride
        // lets a specific monster's art be calibrated once so the dots sit right above the actual
        // character instead of empty margin. Falls back to GetComponent when rendererCache hasn't
        // been cached yet (Awake only runs in Play mode, but this is also called from the editor
        // gizmo preview while not playing).
        private Vector3 GetHpDisplayWorldPosition()
        {
            if (hpDisplayHeightOverride >= 0f)
            {
                return new Vector3(transform.position.x, transform.position.y + hpDisplayHeightOverride, transform.position.z);
            }

            var renderer = rendererCache != null ? rendererCache : (spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>());
            var offsetY = renderer != null ? renderer.bounds.max.y - transform.position.y + 0.15f : 1.2f;
            return new Vector3(transform.position.x, transform.position.y + offsetY, transform.position.z);
        }

        private void OnDrawGizmosSelected()
        {
            float previewFacing = Application.isPlaying ? -facing : transform.localScale.x < 0f ? -1f : 1f;

            Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.35f);
            Gizmos.DrawCube((Vector2)transform.position + hurtBoxOffset, hurtBoxSize);
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.35f);
            Gizmos.DrawCube((Vector2)transform.position + new Vector2(attack1BoxOffset.x * previewFacing, attack1BoxOffset.y), attack1BoxSize);
            Gizmos.DrawCube((Vector2)transform.position + new Vector2(attack2BoxOffset.x * previewFacing, attack2BoxOffset.y), attack2BoxSize);

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
            if (previewMode || isDead || Camera.main == null)
                return;
            GuiScaleUtility.Begin();
            if (hpStyle == null)
            {
                hpStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.black }
                };
            }

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

        private static string HpDots(int current, int max)
        {
            string dots = string.Empty;
            for (int i = 0; i < max; i++)
                dots += i < current ? "● " : "○ ";
            return dots;
        }
    }
}
