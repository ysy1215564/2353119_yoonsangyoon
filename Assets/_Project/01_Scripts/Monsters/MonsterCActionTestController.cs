using System.Collections;
using GameProject.Audio;
using GameProject.Core;
using GameProject.Player;
using UnityEngine;

namespace GameProject.Monsters
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MonsterCActionTestController : MonoBehaviour
    {
        public enum PreviewMotion
        {
            Idle,
            Move,
            Attack1,
            Attack2,
            Hit,
            Death
        }

        private enum ControlMode
        {
            MotionPreview,
            Gameplay
        }

        [Header("Preview / Sprites")]
        [SerializeField] private ControlMode controlMode = ControlMode.MotionPreview;
        [SerializeField] private PreviewMotion previewMotion = PreviewMotion.Idle;
        [SerializeField, Min(1f)] private float defaultFramesPerSecond = 8f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] flyFrames;
        [SerializeField] private Sprite[] attack1Frames;
        [SerializeField] private Sprite[] attack2ChargeFrames;
        [SerializeField] private Sprite[] attack2DashFrames;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] deathFrames;

        [Header("Attack 1 Projectile")]
        [SerializeField] private Sprite[] projectileFrames;
        [SerializeField] private Transform projectileTarget;
        [SerializeField] private Vector3 projectileSpawnOffset = new Vector3(-0.9f, 1.95f, 0f);
        [SerializeField] private Vector3 projectileTargetAimOffset = new Vector3(0f, 1.25f, 0f);
        [SerializeField] private Vector3 projectileScale = new Vector3(0.3f, 0.3f, 1f);
        [SerializeField, Min(0.1f)] private float projectileSpeed = 14f;
        [SerializeField, Min(1)] private int attack1ProjectileFrame = 3;
        [SerializeField, Min(0.01f)] private float projectileAnimationFramesPerSecond = 12f;
        [SerializeField] private LayerMask projectileFloorMask = 1;
        [SerializeField, Min(0.01f)] private float projectileColliderRadius = 0.28f;

        [Header("Attack 2 Dash")]
        [SerializeField] private Transform attack2Target;
        [SerializeField] private Vector3 attack2TargetAimOffset = new Vector3(0f, 1.25f, 0f);
        [SerializeField, Min(0.1f)] private float attack2DashSpeed = 12f;
        [SerializeField, Min(0.1f)] private float attack2ReturnSpeed = 10f;
        [SerializeField, Min(0.1f)] private float attack2MaxDashSeconds = 1.5f;
        [SerializeField] private Collider2D attack2TargetCollider;
        [SerializeField] private Collider2D attack2FloorCollider;

        [Header("Gameplay AI")]
        [SerializeField] private PlayerActionTestController playerController;
        [SerializeField, Min(1)] private int maxHp = 3;
        [SerializeField] private float detectRange = 7f;
        [SerializeField] private float attackRange = 6f;
        [SerializeField] private float attackCooldown = 1.4f;
        [SerializeField] private Vector2 roamArea = new Vector2(3.4f, 1.2f);
        [SerializeField] private float roamSpeed = 1.15f;
        [SerializeField] private float idleHoverMinSeconds = 0.6f;
        [SerializeField] private float idleHoverMaxSeconds = 1.4f;
        [SerializeField] private float roamMinSeconds = 1.0f;
        [SerializeField] private float roamMaxSeconds = 2.2f;
        [SerializeField] private float hitInvincibleTime = 0.25f;
        [SerializeField] private float deathHoldTime = 0.8f;
        [SerializeField] private float respawnDelay = 2f;
        [Tooltip("이 몬스터가 죽은 뒤 몇 번까지 다시 살아나는가 - 등장 횟수가 아니라 부활 횟수라, 실제로 나오는 횟수는 항상 이 값보다 1 크다. -1 = 무제한(기본값) / 0 = 부활 없음, 한 번 죽으면 끝(1번 등장) / 1 = 부활 1번(총 2번 등장) / 2 = 부활 2번(총 3번 등장). 씬에 놓인 몬스터마다 따로 정할 수 있고 Apply로 프리팹에 반영되지 않는다 - 난이도 조절용.")]
        [SerializeField] private int maxRespawnCount = -1;

        [Header("HP Display")]
        [Tooltip("HP dots float above this height (world units above the pivot). -1 = auto (uses the sprite's top edge, which includes any transparent padding in the source art). Set a value >= 0 to override once per this monster's resources so the dots sit right above the actual character instead of the art's empty margin - use the yellow gizmo sphere (select this object in Scene view) to preview without Play.")]
        [SerializeField] private float hpDisplayHeightOverride = -1f;

        [Header("Frame Timing")]
        [SerializeField] private float[] idleFrameTimes;
        [SerializeField] private float[] flyFrameTimes;
        [SerializeField] private float[] attack1FrameTimes;
        [SerializeField] private float[] attack2ChargeFrameTimes;
        [SerializeField] private float[] attack2DashFrameTimes;
        [SerializeField] private float[] hitFrameTimes;
        [SerializeField] private float[] deathFrameTimes;

        [Header("SFX")]
        [SerializeField] private SfxCue attack1Sfx;
        [SerializeField] private SfxCue attack2Sfx;
        [SerializeField] private SfxCue hitSfx;
        [SerializeField] private SfxCue deathSfx;

        private SfxPlayer sfxPlayer;
        private SpriteRenderer rendererCache;
        private Collider2D bodyCollider;
        private Coroutine routine;
        private bool attack1ProjectileFired;
        private bool attack2InProgress;
        private bool attack2ReturnPending;
        private bool isDead;
        private bool worldFreezeHandled;
        private int currentHp;
        private int lastAttackId = -1;
        private float lastHitTime = -999f;
        private float lastAttackTime = -999f;
        private int facing = -1;
        private Vector3 gameplayOrigin;
        private int respawnCount;
        private GameObject activeProjectile;
        private Vector3 attack2Origin;
        private int attackPatternIndex;
        private GUIStyle hpStyle; // 디버그 HP 점 표시용(OnGUI) - 2026-08-12부터 빌드에도 포함(아래 OnGUI 참고)

        public void SetProjectileTarget(Transform target)
        {
            projectileTarget = target;
        }

        public void SetAttack2CollisionTargets(Transform target, Collider2D targetCollider, Collider2D floorCollider)
        {
            attack2Target = target;
            attack2TargetCollider = targetCollider;
            attack2FloorCollider = floorCollider;
        }

        public void PlayPreviewMotion(PreviewMotion motion)
        {
            previewMotion = motion;
            attack1ProjectileFired = false;
            StopCurrentRoutine();
            routine = StartCoroutine(PreviewRoutine(motion));
        }

        private void Awake()
        {
            rendererCache = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            sfxPlayer = new SfxPlayer(transform);
            bodyCollider = GetComponent<Collider2D>();
            if (bodyCollider != null)
            {
                bodyCollider.isTrigger = true;
            }

            if (idleFrames != null && idleFrames.Length > 0)
            {
                rendererCache.sprite = idleFrames[0];
            }
        }

        private void Start()
        {
            currentHp = maxHp;
            if (controlMode == ControlMode.Gameplay)
            {
                gameplayOrigin = transform.position;
                FindPlayer();
                routine = StartCoroutine(GameplayRoutine());
                return;
            }

            PlayPreviewMotion(previewMotion);
        }

        /// <summary>
        /// MonsterA/B와 달리 MonsterC는 행동을 전부 코루틴 체인(GameplayRoutine이 Attack1/Attack2/
        /// Roam/IdleHover 등을 yield로 물고 도는 구조)으로 돌려서 매 프레임 폴링하는 Update()가
        /// 원래 없었다 - 스테이지 클리어로 월드가 얼어붙는 순간(2026-08-12)을 잡아내기 위해서만
        /// 추가. GameplayFreezeGate는 그 씬이 끝날 때까지 다시 안 풀리는 값이라, 얼어붙는 그 프레임
        /// 딱 한 번만 지금 돌고 있는 코루틴 체인을 통째로 끊고(StopCurrentRoutine - 어느 하위
        /// 루틴에 있었든 그 지점에서 즉시 멈춤) IDLE 프레임으로 고정하면 그걸로 끝, 이후 다시 확인할
        /// 필요가 없다(worldFreezeHandled로 1회만 처리).
        /// </summary>
        private void Update()
        {
            if (controlMode != ControlMode.Gameplay || isDead || worldFreezeHandled || !GameplayFreezeGate.IsFrozen)
            {
                return;
            }

            worldFreezeHandled = true;
            StopCurrentRoutine();
            PlayLoopFrame(idleFrames, 0);
        }

        public void ConfigureGameplay(PlayerActionTestController player)
        {
            controlMode = ControlMode.Gameplay;
            playerController = player;
            if (playerController != null)
            {
                SetProjectileTarget(playerController.transform);
                attack2Target = playerController.transform;
            }
        }

        public void TakeDamage(int damage, int attackId, int hitDirection)
        {
            if (controlMode != ControlMode.Gameplay
                || isDead
                || GameplayFreezeGate.IsFrozen
                || attackId == lastAttackId
                || Time.time - lastHitTime < hitInvincibleTime)
            {
                return;
            }

            lastAttackId = attackId;
            lastHitTime = Time.time;
            currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, damage));
            StopCurrentRoutine();

            if (currentHp <= 0)
            {
                sfxPlayer.Play(deathSfx);
                routine = StartCoroutine(DeathRoutine());
                return;
            }

            sfxPlayer.Play(hitSfx);
            routine = StartCoroutine(HitRoutine());
        }

        private IEnumerator GameplayRoutine()
        {
            PlayLoopFrame(idleFrames, 0);

            while (!isDead)
            {
                FindPlayer();
                FacePlayer();
                if (CanAttackPlayer())
                {
                    var useAttack2 = attackPatternIndex == 2;
                    attackPatternIndex = (attackPatternIndex + 1) % 3;
                    yield return useAttack2 ? Attack2GameplayRoutine() : Attack1GameplayRoutine();
                    continue;
                }

                if (Random.value < 0.35f)
                {
                    yield return IdleHoverRoutine(Random.Range(idleHoverMinSeconds, idleHoverMaxSeconds));
                }
                else
                {
                    yield return RoamRoutine(Random.Range(roamMinSeconds, roamMaxSeconds));
                }
            }
        }

        private bool CanAttackPlayer()
        {
            if (playerController == null
                || activeProjectile != null
                || attack2InProgress
                || Time.time - lastAttackTime < attackCooldown)
            {
                return false;
            }

            var distance = Vector2.Distance(transform.position, playerController.transform.position);
            return distance <= detectRange && distance <= attackRange;
        }

        private IEnumerator Attack1GameplayRoutine()
        {
            FacePlayer();
            previewMotion = PreviewMotion.Attack1;
            attack1ProjectileFired = false;
            lastAttackTime = Time.time;
            yield return PlayFrames(attack1Frames, attack1FrameTimes);
            previewMotion = PreviewMotion.Idle;
        }

        private IEnumerator Attack2GameplayRoutine()
        {
            FacePlayer();
            previewMotion = PreviewMotion.Attack2;
            lastAttackTime = Time.time;
            yield return PlayAttack2Routine();
            previewMotion = PreviewMotion.Idle;
        }

        private IEnumerator IdleHoverRoutine(float duration)
        {
            var elapsed = 0f;
            var frameIndex = 0;
            var frameTimer = 0f;

            while (elapsed < duration && !CanAttackPlayer())
            {
                elapsed += Time.deltaTime;
                FacePlayer();
                frameTimer += Time.deltaTime;
                AnimateLoop(idleFrames, idleFrameTimes, ref frameIndex, ref frameTimer);
                yield return null;
            }
        }

        private IEnumerator RoamRoutine(float duration)
        {
            var target = RandomRoamPoint();
            var elapsed = 0f;
            var frameIndex = 0;
            var frameTimer = 0f;

            while (elapsed < duration && !CanAttackPlayer())
            {
                elapsed += Time.deltaTime;
                FaceMoveTarget(target);
                transform.position = Vector3.MoveTowards(transform.position, target, roamSpeed * Time.deltaTime);
                AnimateLoop(flyFrames, flyFrameTimes, ref frameIndex, ref frameTimer);

                if ((transform.position - target).sqrMagnitude < 0.04f)
                {
                    target = RandomRoamPoint();
                }

                yield return null;
            }
        }

        private Vector3 RandomRoamPoint()
        {
            return gameplayOrigin + new Vector3(
                Random.Range(-roamArea.x, roamArea.x),
                Random.Range(-roamArea.y, roamArea.y),
                0f);
        }

        private IEnumerator HitRoutine()
        {
            previewMotion = PreviewMotion.Hit;
            yield return PlayFrames(hitFrames, hitFrameTimes);
            previewMotion = PreviewMotion.Idle;

            if (attack2ReturnPending)
            {
                attack2ReturnPending = false;
                yield return ReturnToOrigin(attack2Origin);
            }

            routine = StartCoroutine(GameplayRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            isDead = true;
            // 죽음 판정 즉시 콜라이더 해제(2026-08-17, 사용자 확정) - 죽는 모션/사라지는 타이밍은
            // 그대로 두고, 판정 순간 바로 콜라이더를 끈다(A/B와 동일 관례로 통일 - 이 콜라이더 자체는
            // 원래 트리거라 플레이어를 물리적으로 막지는 않았지만, 죽은 몬스터가 계속 히트/트리거
            // 판정에 걸려있는 창을 없애기 위함).
            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }
            attack2ReturnPending = false;
            previewMotion = PreviewMotion.Death;
            yield return PlayFrames(deathFrames, deathFrameTimes);
            yield return new WaitForSeconds(deathHoldTime);

            rendererCache.enabled = false;

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
            transform.position = gameplayOrigin;
            currentHp = maxHp;
            lastAttackId = -1;
            lastHitTime = -999f;
            lastAttackTime = -999f;
            attack1ProjectileFired = false;
            attack2InProgress = false;
            attack2ReturnPending = false;
            attackPatternIndex = 0;
            isDead = false;

            rendererCache.enabled = true;
            if (bodyCollider != null)
            {
                bodyCollider.enabled = true;
            }

            previewMotion = PreviewMotion.Idle;
            routine = StartCoroutine(GameplayRoutine());
        }

        private void AnimateLoop(Sprite[] frames, float[] frameTimes, ref int frameIndex, ref float frameTimer)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            PlayLoopFrame(frames, frameIndex);
            frameTimer += Time.deltaTime;
            if (frameTimer >= GetFrameTime(frameTimes, frameIndex))
            {
                frameTimer = 0f;
                frameIndex = (frameIndex + 1) % frames.Length;
            }
        }

        private void PlayLoopFrame(Sprite[] frames, int frameIndex)
        {
            if (frames != null && frames.Length > 0)
            {
                rendererCache.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
            }
        }

        private void FindPlayer()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerActionTestController>();
            }

            if (playerController != null)
            {
                SetProjectileTarget(playerController.transform);
                attack2Target = playerController.transform;
            }
        }

        private void FacePlayer()
        {
            if (playerController == null)
            {
                return;
            }

            var delta = playerController.transform.position.x - transform.position.x;
            if (Mathf.Abs(delta) > 0.05f)
            {
                SetFacing(delta > 0f ? 1 : -1);
            }
        }

        private void FaceMoveTarget(Vector3 target)
        {
            var delta = target.x - transform.position.x;
            if (Mathf.Abs(delta) > 0.05f)
            {
                SetFacing(delta > 0f ? 1 : -1);
            }
        }

        private void SetFacing(int direction)
        {
            facing = direction >= 0 ? 1 : -1;
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x) * -facing,
                transform.localScale.y,
                transform.localScale.z);
        }

        private IEnumerator PreviewRoutine(PreviewMotion motion)
        {
            while (true)
            {
                if (motion == PreviewMotion.Attack2)
                {
                    yield return PlayAttack2Routine();
                    motion = PreviewMotion.Idle;
                    previewMotion = PreviewMotion.Idle;
                }

                // Attack1/Attack2 already play their SFX further down the call chain
                // (TryFireAttack1Projectile / PlayAttack2Routine), but Hit/Death preview just
                // cycles sprites via PlayFrames() with nothing else listening, so they'd
                // otherwise stay silent even with clips wired on the prefab.
                if (motion == PreviewMotion.Hit)
                {
                    sfxPlayer.Play(hitSfx);
                }
                else if (motion == PreviewMotion.Death)
                {
                    sfxPlayer.Play(deathSfx);
                }

                yield return PlayFrames(GetPreviewFrames(motion), GetPreviewFrameTimes(motion));
            }
        }

        private IEnumerator PlayAttack2Routine()
        {
            attack2Origin = transform.position;
            attack2InProgress = true;
            yield return PlayFrames(attack2ChargeFrames, attack2ChargeFrameTimes);

            var direction = GetAttack2DashDirection();
            FaceDashDirection(direction);
            sfxPlayer.Play(attack2Sfx);
            yield return PlayAttack2Dash(direction);
            yield return ReturnToOrigin(attack2Origin);
            attack2InProgress = false;
        }

        private void FaceDashDirection(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) > 0.001f)
            {
                SetFacing(direction.x > 0f ? 1 : -1);
            }
        }

        private Vector3 GetAttack2DashDirection()
        {
            if (attack2Target == null)
            {
                return Vector3.left;
            }

            var targetPosition = GetTargetBottomCenter(attack2Target, attack2TargetAimOffset);
            var dashOrigin = bodyCollider != null ? bodyCollider.bounds.center : transform.position;
            var direction = targetPosition - dashOrigin;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.left;
            }

            return direction.normalized;
        }

        private static Vector3 GetTargetCenter(Transform target, Vector3 fallbackAimOffset)
        {
            var targetCollider = target.GetComponentInChildren<Collider2D>();
            if (targetCollider != null)
            {
                return targetCollider.bounds.center;
            }

            var targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
            if (targetRenderer != null && targetRenderer.sprite != null)
            {
                return targetRenderer.bounds.center;
            }

            return target.TransformPoint(fallbackAimOffset);
        }

        private static Vector3 GetTargetBottomCenter(Transform target, Vector3 fallbackAimOffset)
        {
            var targetCollider = target.GetComponentInChildren<Collider2D>();
            if (targetCollider != null)
            {
                var bounds = targetCollider.bounds;
                return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }

            return GetTargetCenter(target, fallbackAimOffset);
        }

        private IEnumerator PlayAttack2Dash(Vector3 direction)
        {
            var elapsed = 0f;
            var frameIndex = 0;
            var frameTimer = 0f;
            ApplyAttack2DashFrame(frameIndex);

            while (elapsed < attack2MaxDashSeconds)
            {
                var deltaTime = Time.deltaTime;
                elapsed += deltaTime;
                frameTimer += deltaTime;
                transform.position += direction * attack2DashSpeed * deltaTime;

                var frameDuration = GetFrameTime(attack2DashFrameTimes, frameIndex);
                if (frameTimer >= frameDuration)
                {
                    frameTimer -= frameDuration;
                    frameIndex = GetNextDashFrameIndex(frameIndex);
                    ApplyAttack2DashFrame(frameIndex);
                }

                if (HasAttack2DashCollision())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private int GetNextDashFrameIndex(int currentIndex)
        {
            if (attack2DashFrames == null || attack2DashFrames.Length == 0)
            {
                return 0;
            }

            return (currentIndex + 1) % attack2DashFrames.Length;
        }

        private void ApplyAttack2DashFrame(int frameIndex)
        {
            if (attack2DashFrames == null || attack2DashFrames.Length == 0)
            {
                return;
            }

            rendererCache.sprite = attack2DashFrames[Mathf.Clamp(frameIndex, 0, attack2DashFrames.Length - 1)];
        }

        private bool HasAttack2DashCollision()
        {
            if (bodyCollider == null)
            {
                return false;
            }

            if (controlMode == ControlMode.Gameplay)
            {
                return HasAttack2DashCollisionGameplay();
            }

            return IsTouchingCollider(attack2TargetCollider) || IsTouchingCollider(attack2FloorCollider);
        }

        private bool IsTouchingCollider(Collider2D other)
        {
            return other != null && bodyCollider.bounds.Intersects(other.bounds);
        }

        private bool HasAttack2DashCollisionGameplay()
        {
            var bounds = bodyCollider.bounds;

            if (playerController != null && playerController.OverlapsHurtBox(bounds.center, bounds.size))
            {
                DealAttack2DamageToPlayer();
                return true;
            }

            return HasAttack2FloorCollisionGameplay(bounds);
        }

        private bool HasAttack2FloorCollisionGameplay(Bounds bounds)
        {
            var hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f, projectileFloorMask);
            foreach (var hit in hits)
            {
                if (hit == null || hit == bodyCollider)
                {
                    continue;
                }

                if (playerController != null && hit.transform.IsChildOf(playerController.transform))
                {
                    // The player's physical collider is not the floor. Missed hits should
                    // keep flying to the captured target point instead of treating the
                    // player's body as an obstacle.
                    continue;
                }

                return true;
            }

            return false;
        }

        private void DealAttack2DamageToPlayer()
        {
            if (playerController == null)
            {
                return;
            }

            var hitDirection = playerController.transform.position.x >= transform.position.x ? 1 : -1;
            playerController.TakeDamage(2, hitDirection, 2f, 2f, true);
        }

        private IEnumerator ReturnToOrigin(Vector3 origin)
        {
            while ((transform.position - origin).sqrMagnitude > 0.0004f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    origin,
                    attack2ReturnSpeed * Time.deltaTime);
                yield return PlayFramesForOneFrame(flyFrames, flyFrameTimes);
            }

            transform.position = origin;
        }

        private IEnumerator PlayFramesForOneFrame(Sprite[] frames, float[] frameTimes)
        {
            if (frames == null || frames.Length == 0)
            {
                yield return null;
                yield break;
            }

            var frameIndex = Mathf.FloorToInt(Time.time / GetFrameTime(frameTimes, 0)) % frames.Length;
            rendererCache.sprite = frames[frameIndex];
            yield return null;
        }

        private IEnumerator PlayFrames(Sprite[] frames, float[] frameTimes)
        {
            if (frames == null || frames.Length == 0)
            {
                yield return null;
                yield break;
            }

            for (var i = 0; i < frames.Length; i++)
            {
                rendererCache.sprite = frames[i];
                TryFireAttack1Projectile(i);
                yield return new WaitForSeconds(GetFrameTime(frameTimes, i));
            }
        }

        private void TryFireAttack1Projectile(int frameIndex)
        {
            if (previewMotion != PreviewMotion.Attack1
                || attack1ProjectileFired
                || activeProjectile != null
                || frameIndex != attack1ProjectileFrame - 1
                || projectileTarget == null
                || projectileFrames == null
                || projectileFrames.Length == 0)
            {
                return;
            }

            attack1ProjectileFired = true;
            sfxPlayer.Play(attack1Sfx);

            var spawnPosition = transform.TransformPoint(projectileSpawnOffset);
            var targetPosition = GetTargetCenter(projectileTarget, projectileTargetAimOffset);
            var direction = targetPosition - spawnPosition;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector3.left;
            }

            activeProjectile = new GameObject(
                "MonsterC_Projectile",
                typeof(SpriteRenderer),
                typeof(Rigidbody2D),
                typeof(CircleCollider2D),
                typeof(MonsterCProjectile));
            var projectile = activeProjectile;
            projectile.transform.position = spawnPosition;
            projectile.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            projectile.transform.localScale = projectileScale;

            var projectileRenderer = projectile.GetComponent<SpriteRenderer>();
            projectileRenderer.sortingOrder = rendererCache != null ? rendererCache.sortingOrder + 2 : 10;

            var projectileCollider = projectile.GetComponent<CircleCollider2D>();
            projectileCollider.radius = projectileColliderRadius;

            projectile.GetComponent<MonsterCProjectile>().Initialize(
                projectileFrames,
                direction,
                projectileSpeed,
                projectileTarget,
                projectileFloorMask,
                projectileAnimationFramesPerSecond,
                controlMode == ControlMode.Gameplay ? playerController : null,
                controlMode == ControlMode.Gameplay ? 1 : 0,
                bodyCollider);
        }

        private Sprite[] GetPreviewFrames(PreviewMotion motion)
        {
            switch (motion)
            {
                case PreviewMotion.Move:
                    return flyFrames;
                case PreviewMotion.Attack1:
                    return attack1Frames;
                case PreviewMotion.Hit:
                    return hitFrames;
                case PreviewMotion.Death:
                    return deathFrames;
                default:
                    return idleFrames;
            }
        }

        private float[] GetPreviewFrameTimes(PreviewMotion motion)
        {
            switch (motion)
            {
                case PreviewMotion.Move:
                    return flyFrameTimes;
                case PreviewMotion.Attack1:
                    return attack1FrameTimes;
                case PreviewMotion.Hit:
                    return hitFrameTimes;
                case PreviewMotion.Death:
                    return deathFrameTimes;
                default:
                    return idleFrameTimes;
            }
        }

        private float GetFrameTime(float[] times, int index)
        {
            if (times != null && index >= 0 && index < times.Length && times[index] > 0f)
            {
                return times[index];
            }

            return 1f / Mathf.Max(1f, defaultFramesPerSecond);
        }

        private void OnDrawGizmosSelected()
        {
            var sourceCollider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            if (sourceCollider != null)
            {
                Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.35f);
                Gizmos.DrawCube(sourceCollider.bounds.center, sourceCollider.bounds.size);
            }

            var origin = transform.position;
            Gizmos.color = new Color(0.2f, 1f, 0.25f, 0.25f);
            Gizmos.DrawWireSphere(origin, detectRange);

            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(origin, attackRange);

            var spawnPosition = transform.TransformPoint(projectileSpawnOffset);
            Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.85f);
            Gizmos.DrawSphere(spawnPosition, 0.08f);

            Gizmos.color = new Color(0.9f, 0.15f, 1f, 0.45f);
            Gizmos.DrawWireSphere(spawnPosition, ProjectileColliderWorldRadius());

            if (activeProjectile != null)
            {
                var projectileCollider = activeProjectile.GetComponent<CircleCollider2D>();
                if (projectileCollider != null)
                {
                    Gizmos.color = new Color(0.9f, 0.15f, 1f, 0.35f);
                    Gizmos.DrawWireSphere(
                        projectileCollider.bounds.center,
                        Mathf.Max(projectileCollider.bounds.extents.x, projectileCollider.bounds.extents.y));
                }
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetHpDisplayWorldPosition(), 0.08f);
        }

        private float ProjectileColliderWorldRadius()
        {
            return projectileColliderRadius * Mathf.Max(
                Mathf.Abs(projectileScale.x),
                Mathf.Abs(projectileScale.y),
                0.01f);
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

        private void StopCurrentRoutine()
        {
            if (routine == null)
            {
                return;
            }

            StopCoroutine(routine);
            routine = null;

            if (attack2InProgress)
            {
                attack2InProgress = false;
                attack2ReturnPending = true;
            }
        }

        // 2026-08-12: 원래 #if UNITY_EDITOR로 감싸서 에디터에서만 보이던 디버그 HP 점 표시 -
        // 사용자가 실제 빌드로 확인해보고 "지금 에디터에서 보이는 그대로가 마음에 든다, 몬스터 HP는
        // 그래픽 UI를 따로 안 만들고 이 표시를 빌드에도 그대로 넣어달라"고 확정해서 가드를 뺐다.
        // GuiScaleUtility/GUI.Label 등 여기서 쓰는 것들은 UnityEditor 의존이 전혀 없어서 빌드에서도
        // 문제없이 그대로 동작한다.
        private void OnGUI()
        {
            if (controlMode != ControlMode.Gameplay || isDead || Camera.main == null)
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
