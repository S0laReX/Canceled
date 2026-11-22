using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class BrawlFighter : MonoBehaviour
    {
        public BrawlKit Kit { get; private set; }
        public string DisplayName => Kit.Name;
        public int PlayerIndex { get; private set; }
        public int CharacterIndex { get; private set; }
        public int SkinIndex { get; private set; }
        /// <summary>Look in use ("Provisional" when the kit has no imported model).</summary>
        public string SkinName => visual != null ? visual.SkinName : "Provisional";
        public Color PlayerColor { get; private set; }
        public float Damage { get; private set; }
        public int Stocks { get; private set; }
        public float Meter { get; private set; }
        public int Place { get; internal set; }
        public bool IsBot { get; private set; }
        public bool IsRespawning { get; private set; }
        public bool Eliminated => Stocks <= 0;
        public bool Grounded { get; private set; }
        public bool RecoveryAvailable => !recoveryUsed;
        public bool Invulnerable => protection > 0 || dodgeRemaining > .09f;
        public float RespawnRemaining { get; private set; }
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public Vector3 Velocity => velocity;
        public float TotalDamageDealt { get; private set; }
        /// <summary>Expulsions credited to this fighter (last valid attacker inside the attribution window).</summary>
        public int KOs { get; private set; }
        /// <summary>Lives lost without a rival hit inside the attribution window.</summary>
        public int SelfDestructs { get; private set; }
        public BrawlFighter LastAttacker { get; private set; }
        public int JumpsUsed => jumps;
        public bool IsAttacking => attacking;
        public AttackKind CurrentAttack => attack.Kind;
        public float AttackProgress => attacking ? Mathf.Clamp01(attackTime / attack.Duration) : 0;
        public BrawlTrap ActiveTrap => activeTrap;
        public AttackSpec CurrentAttackSpec => attack;
        public bool AttackIsActive => attacking && attackTime >= attack.Windup && attackTime <= attack.Windup + attack.Active;
        public int AttackVertical { get; private set; }
        public bool AttackAirborne { get; private set; }
        public int ComboHits => match != null && match.SimTime - comboHitClock < 1.1f ? comboHitCount : 0;
        private float comboHitClock, hitPause, dropRemaining, basicBuffer, heavyBuffer, jumpBuffer;
        private int comboHitCount, comboTarget = -1;
        private bool attackConnected;

        /// <summary>Directional 2.5D move in progress (data from BrawlSettings.sideMoves), if any.</summary>
        public SideMove? CurrentSideMove { get; private set; }
        private MoveBox sideMoveBox;

        /// <summary>
        /// Side-view hit box of the current melee attack in local X/Y (SYS-042–048). Directional moves use their own
        /// data box; kit moves derive one that reproduces the historical sweep. Shared by the hit check and CombatLab.
        /// </summary>
        public bool TryGetSideHitbox(out MoveBox box, out bool safeSectors)
        {
            box = default; safeSectors = false;
            if (!match.IsSideView || !TryGetMeleeVolume(out _, out float height, out float minDot, out safeSectors)) return false;
            box = CurrentSideMove.HasValue ? sideMoveBox : BrawlMoveData.SweepBox(attack.Range, -height, height, bothSides: minDot <= -.99f);
            return true;
        }

        public Vector2 Root2D => new Vector2(transform.position.x, transform.position.y);
        public float FacingSign => Facing.x < 0 ? -1 : 1;

        // The debug view and the actual sweep share these values.
        public bool TryGetMeleeVolume(out float range, out float height, out float minDot, out bool safeSectors)
        {
            range = attack.Range + .3f; height = attack.Height; safeSectors = false; minDot = 0;
            if (!attacking) return false;
            if (match.IsSideView && AttackVertical != 0 && (attack.Kind == AttackKind.Basic || attack.Kind == AttackKind.Heavy)) { minDot = -1; return true; }
            if (attack.Kind == AttackKind.Basic) { minDot = Kit.BasicCone; return true; }
            if (attack.Kind == AttackKind.Heavy) { minDot = .25f; return true; }
            if (attack.Kind == AttackKind.Special && Kit.Special == SpecialStyle.Dash) { minDot = .2f; return true; }
            if (attack.Kind == AttackKind.Super && Kit.Super == SuperStyle.Fan) { minDot = .26f; return true; }
            if (attack.Kind == AttackKind.Super && Kit.Super == SuperStyle.SafeSweep) { minDot = -1; safeSectors = true; return true; }
            return false;
        }

        private BrawlMatch match;
        private BrawlSettings settings;
        private CharacterController controller;
        private BrawlFighterVisual visual;
        private Transform marker;
        private LineRenderer facingLine;
        private Vector3 velocity, dashDirection;
        private float stun, protection, dodgeRemaining, dodgeCooldown, attackTime, comboWindow, botThink, recoveryTimer, hoverTimer, lastHitClock;
        private int jumps, combo;
        private bool recoveryUsed, airDodgeUsed, attacking, activeStarted, chainCharged;
        private AttackSpec attack;
        private BrawlAttackToken attackToken;
        private BrawlCommand command;
        private BrawlTrap activeTrap;

        public void Configure(BrawlMatch owner, BrawlSettings tuning, int index, int character, int skin, bool bot, Color color)
        {
            match = owner; settings = tuning; PlayerIndex = index; IsBot = bot; PlayerColor = color;
            CharacterIndex = ((character % BrawlRoster.Count) + BrawlRoster.Count) % BrawlRoster.Count;
            Kit = BrawlRoster.Get(CharacterIndex);
            SkinIndex = BrawlRoster.WrapSkin(CharacterIndex, skin);
            Stocks = settings.startingStocks;
            controller = GetComponent<CharacterController>();
            controller.height = 1.7f; controller.radius = .36f; controller.center = Vector3.up * .85f;
            controller.stepOffset = .22f; controller.slopeLimit = 50; controller.skinWidth = .035f;
            gameObject.layer = 8;
            var model = new GameObject("Modelo"); model.transform.SetParent(transform, false);
            visual = model.AddComponent<BrawlFighterVisual>(); visual.Build(CharacterIndex, SkinIndex, color, settings.characterArt);
            var markerObject = new GameObject("Proyección al suelo");
            marker = markerObject.transform; marker.SetParent(transform, false);
            var ring = markerObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 32; ring.widthMultiplier = .055f;
            ring.material = BrawlFeedback.Material(color);
            for (int i = 0; i < 32; i++) { float a = i * Mathf.PI / 16; ring.SetPosition(i, new Vector3(Mathf.Cos(a) * .56f, .02f, Mathf.Sin(a) * .56f)); }
            var arrow = new GameObject("Orientación del ataque"); arrow.transform.SetParent(transform, false);
            facingLine = arrow.AddComponent<LineRenderer>(); facingLine.material = ring.material;
            facingLine.widthMultiplier = .07f; facingLine.positionCount = 5;
            RespawnAt(transform.position, false);
        }

        public void Submit(BrawlCommand input)
        {
            command.Move = input.Move; command.Aim = input.Aim; command.FastFall = input.FastFall;
            command.Jump |= input.Jump; command.Basic |= input.Basic; command.Heavy |= input.Heavy;
            command.Special |= input.Special; command.Recovery |= input.Recovery;
            command.Dodge |= input.Dodge; command.Super |= input.Super;
        }

        public void Tick(float dt)
        {
            if (Eliminated) return;
            if (match.IsSideView)
            {
                if (command.Basic) basicBuffer = .14f;
                if (command.Heavy) heavyBuffer = .14f;
                if (command.Jump) jumpBuffer = .12f;
                command.Basic = basicBuffer > 0; command.Heavy = heavyBuffer > 0; command.Jump = jumpBuffer > 0;
                basicBuffer -= dt; heavyBuffer -= dt; jumpBuffer -= dt; dropRemaining -= dt;
                if (hitPause > 0) { hitPause -= dt; return; }
            }
            if (IsRespawning)
            {
                RespawnRemaining -= dt;
                if (RespawnRemaining <= 0) RespawnAt(match.SafeSpawnFor(this), true);
                command = default; return;
            }
            if (IsBot && !match.AutomatedTest) command = BotCommand(dt);
            protection = Mathf.Max(0, protection - dt); stun = Mathf.Max(0, stun - dt);
            dodgeCooldown = Mathf.Max(0, dodgeCooldown - dt); dodgeRemaining = Mathf.Max(0, dodgeRemaining - dt);
            recoveryTimer = Mathf.Max(0, recoveryTimer - dt); hoverTimer = Mathf.Max(0, hoverTimer - dt);
            comboWindow -= dt;
            if (comboWindow <= 0 && !attacking) { combo = 0; chainCharged = false; }
            Vector3 move = match.CameraDirection(command.Move);
            Vector3 aim = match.CameraDirection(command.Aim);
            if (!attacking && stun <= 0 && dodgeRemaining <= 0)
            {
                if (aim.sqrMagnitude > .08f) Facing = aim.normalized;
                else if (move.sqrMagnitude > .05f) Facing = move.normalized;
            }
            if (match.IsSideView && attacking && attackConnected && attack.Kind == AttackKind.Basic && command.Jump && stun <= 0 && jumps < 2)
                attacking = false; // Confirmed basic can be followed by a jump, never a whiff cancel.
            if (stun <= 0 && !attacking)
            {
                if (match.IsSideView && Grounded && transform.position.y > 1 && command.Move.y < -.5f && command.Jump)
                { dropRemaining = .24f; command.Jump = false; jumpBuffer = 0; }
                if (command.Jump && jumps < 2 && dodgeRemaining <= 0)
                { velocity.y = settings.jumpSpeed * (jumps == 1 ? .94f : 1); jumps++; jumpBuffer = 0; Grounded = false; hoverTimer = 0; BrawlFeedback.Sound("jump"); }
                if (command.Recovery && !Grounded && !recoveryUsed) Recover(move);
                if (command.Dodge && dodgeCooldown <= 0 && (Grounded || !airDodgeUsed))
                {
                    dodgeCooldown = settings.dodgeCooldown; dodgeRemaining = .23f;
                    dashDirection = move.sqrMagnitude > .1f ? move.normalized : Facing;
                    if (!Grounded) { airDodgeUsed = true; velocity.y *= .35f; }
                    BrawlFeedback.Burst(transform.position + Vector3.up * .5f, PlayerColor, 4);
                }
                if (dodgeRemaining <= 0)
                {
                    if (command.Super && Meter >= 100) BeginAttack(AttackKind.Super);
                    else if (command.Special) BeginAttack(AttackKind.Special);
                    else if (command.Heavy) BeginAttack(AttackKind.Heavy);
                    else if (command.Basic) BeginAttack(AttackKind.Basic);
                }
            }
            bool lunging = false;
            if (attacking) lunging = AdvanceAttack(dt);
            if (Grounded && velocity.y < 0) velocity.y = -2;
            float gravity = settings.gravity * ((command.FastFall || (match.IsSideView && command.Move.y < -.5f)) && !Grounded ? 2.1f : 1) * (hoverTimer > 0 ? .25f : 1);
            velocity.y = Mathf.Max(-32, velocity.y - gravity * dt);
            Vector3 controlled = move * settings.moveSpeed * (Grounded ? 1 : settings.airControl);
            if (recoveryTimer > 0) controlled *= Kit.RecoveryAirControl;
            if (stun > 0) controlled *= .17f;
            if (attacking) controlled *= attack.Kind == AttackKind.Super ? 0 : .38f;
            if (lunging) controlled = Facing * Kit.LungeSpeed;
            if (dodgeRemaining > 0) controlled = dashDirection * settings.dodgeSpeed;
            if (match.IsSideView)
            {
                velocity.z = 0; controlled.z = 0;
                foreach (var platform in match.Arena.OneWayPlatforms)
                    if (platform) Physics.IgnoreCollision(controller, platform, velocity.y > 0 || transform.position.y < platform.bounds.max.y - .13f || dropRemaining > 0);
            }
            CollisionFlags flags = controller.Move((controlled + velocity) * dt);
            Grounded = (flags & CollisionFlags.Below) != 0 && velocity.y <= 0;
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0) velocity.y = 0;
            if (Grounded) { jumps = 0; recoveryUsed = false; airDodgeUsed = false; recoveryTimer = 0; hoverTimer = 0; }
            else if (jumps == 0) jumps = 1;
            float drag = Mathf.Exp(-(Grounded ? 4.3f : 1.9f) * dt);
            velocity.x *= drag; velocity.z *= drag;
            transform.rotation = Quaternion.LookRotation(Facing);
            visual.Animate(new BrawlPose
            {
                LocalVelocity = transform.InverseTransformDirection(controlled + new Vector3(velocity.x, 0, velocity.z)),
                VerticalSpeed = velocity.y, Grounded = Grounded, Hurt = stun > 0, Protected = protection > 0,
                Dodging = dodgeRemaining > 0, Recovering = recoveryTimer > 0 && !Grounded,
                Attacking = attacking, Attack = attack.Kind, Special = Kit.Special, Combo = combo,
                AttackProgress = AttackProgress,
                WindupEnd = attack.Duration > 0 ? attack.Windup / attack.Duration : 0,
                ActiveEnd = attack.Duration > 0 ? (attack.Windup + attack.Active) / attack.Duration : 0,
                VerticalAttack = AttackVertical, AirAttack = AttackAirborne
            });
            UpdateMarker();
            command.Jump = command.Basic = command.Heavy = command.Special = command.Recovery = command.Dodge = command.Super = false;
        }

        /// <summary>One air recovery per airborne phase; its shape comes from the kit and needs a direction.</summary>
        private void Recover(Vector3 move)
        {
            recoveryUsed = true;
            Vector3 direction = move.sqrMagnitude > .04f ? move.normalized : Facing;
            velocity = direction * Kit.RecoveryHorizontal + Vector3.up * settings.recoverySpeed * Kit.RecoveryVertical;
            recoveryTimer = .6f; hoverTimer = Kit.RecoveryHover;
            BrawlFeedback.Burst(transform.position + Vector3.up * .5f, PlayerColor, 9); BrawlFeedback.Sound("jump");
        }

        public bool OutsideArena
        {
            get
            {
                if (IsRespawning || Eliminated) return false;
                Vector3 p = transform.position; BrawlArena arena = match.Arena;
                return p.y < arena.LowerLimit || p.y > arena.UpperLimit || Mathf.Abs(p.x) > arena.OuterLimits.x || Mathf.Abs(p.z) > arena.OuterLimits.y;
            }
        }

        private void UpdateMarker()
        {
            Vector3 origin = transform.position + Vector3.up * .1f;
            bool onFloor = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 30, 1, QueryTriggerInteraction.Ignore);
            marker.gameObject.SetActive(onFloor);
            if (onFloor) marker.position = hit.point + Vector3.up * .035f;
            Vector3 floor = onFloor ? hit.point : transform.position;
            floor.y += .07f;
            facingLine.SetPosition(0, floor); facingLine.SetPosition(1, floor + Facing * 1.25f);
            facingLine.SetPosition(2, floor + Facing * .95f + transform.right * .2f);
            facingLine.SetPosition(3, floor + Facing * 1.25f);
            facingLine.SetPosition(4, floor + Facing * .95f - transform.right * .2f);
        }

        private void BeginAttack(AttackKind kind)
        {
            if (kind == AttackKind.Basic)
            {
                if (comboWindow > 0) combo = (combo + 1) % 3;
                else { combo = 0; chainCharged = false; }
            }
            else { combo = 0; chainCharged = false; }
            protection = 0; attack = settings.Attack(kind, Kit, combo);
            AttackAirborne = !Grounded;
            float vertical = Mathf.Abs(command.Move.y) > .5f ? command.Move.y : command.Aim.y;
            AttackVertical = match.IsSideView && (kind == AttackKind.Basic || kind == AttackKind.Heavy) && Mathf.Abs(vertical) > .5f ? (vertical > 0 ? 1 : -1) : 0;
            attackConnected = false; basicBuffer = heavyBuffer = 0;
            CurrentSideMove = null;
            if (match.IsSideView && (kind == AttackKind.Basic || kind == AttackKind.Heavy))
            {
                // Directional moves come from editable data (SYS-011) instead of literals.
                if (AttackVertical > 0) CurrentSideMove = kind == AttackKind.Basic ? SideMove.UpBasic : SideMove.UpHeavy;
                else if (AttackAirborne && AttackVertical < 0) CurrentSideMove = kind == AttackKind.Basic ? SideMove.AirDownBasic : SideMove.AirDownHeavy;
                else if (AttackAirborne && kind == AttackKind.Basic) CurrentSideMove = SideMove.AirNeutral;
                if (CurrentSideMove.HasValue)
                {
                    var def = settings.SideMoveDef(CurrentSideMove.Value);
                    attack = def.ToSpec(kind); sideMoveBox = def.hitbox;
                }
            }
            if (kind == AttackKind.Basic || kind == AttackKind.Heavy) AssistFacing(attack.Range);
            attackTime = 0; attacking = true; activeStarted = false; attackToken = new BrawlAttackToken(this, attack);
            Vector3 position = transform.position;
            if (match.IsSideView && AttackVertical != 0 && (kind == AttackKind.Basic || kind == AttackKind.Heavy))
                BrawlFeedback.Burst(position + Vector3.up * (AttackVertical > 0 ? 1.6f : .25f), PlayerColor, 5);
            if (kind == AttackKind.Heavy)
            {
                if (match.IsSideView) BrawlSideTelegraph.Melee(match,this,attack);
                else BrawlFeedback.Telegraph(position, Facing, attack.Range, attack.Windup + attack.Active, PlayerColor);
            }
            if (kind == AttackKind.Special && Kit.Special == SpecialStyle.BounceToken)
            {
                if (match.IsSideView) BrawlSideTelegraph.Trajectory(match,this,6,attack.Windup+.2f);
                else BrawlFeedback.Telegraph(position, Facing, 6, attack.Windup + .2f, PlayerColor);
            }
            if (kind != AttackKind.Super) return;
            Meter = 0;
            BrawlFeedback.Sound("warning");
            match.Haptic(PlayerIndex, .4f, .6f, .3f);
            switch (Kit.Super)
            {
                case SuperStyle.Fan:
                    if (match.IsSideView) BrawlSideTelegraph.Caster(match,this,attack);
                    else BrawlFeedback.Warning(position, attack.Range, attack.Height, attack.Windup, PlayerColor, Facing, 150); break;
                case SuperStyle.Zone:
                {
                    Vector3 centre = position + Facing * 3.6f;
                    Vector3 floor = BrawlFeedback.FloorBelow(centre + Vector3.up);
                    BrawlZone.Create(match, attackToken, floor.y > position.y - 2.5f ? floor : centre, attack.Range, attack.Height, attack.Windup, false);
                    break;
                }
                case SuperStyle.GroundWave:
                    if (match.IsSideView) BrawlSideTelegraph.Caster(match,this,attack);
                    else BrawlFeedback.Warning(position, attack.Range, attack.Height, attack.Windup + attack.Active, PlayerColor, Vector3.zero, 360); break;
                case SuperStyle.Bubble:
                    BrawlZone.Create(match, attackToken, position + Facing * 4.2f + Vector3.up, attack.Range, attack.Height, attack.Windup, true); break;
                case SuperStyle.SafeSweep:
                    if (match.IsSideView)
                    {
                        BrawlSideTelegraph.Caster(match,this,attack);
                    }
                    else BrawlFeedback.Warning(position, attack.Range, attack.Height, attack.Windup + attack.Active, PlayerColor, Facing, 360, SafeHalfAngle); break;
            }
        }

        private const float SafeHalfAngle = 35;
        public const float SideSafeRadius = 1.4f;

        /// <summary>Soft aim help: small correction toward a visible rival inside a short cone. Never through walls.</summary>
        private void AssistFacing(float range)
        {
            BrawlFighter best = null; float bestDot = .906f; Vector3 bestDir = Facing;
            foreach (var target in match.Fighters)
            {
                if (target == this || target.Eliminated || target.IsRespawning) continue;
                Vector3 difference = target.transform.position - transform.position;
                Vector3 flat = Vector3.ProjectOnPlane(difference, Vector3.up);
                if (flat.magnitude > range + .6f || flat.sqrMagnitude < .01f || Mathf.Abs(difference.y) > 1.6f) continue;
                float dot = Vector3.Dot(Facing, flat.normalized);
                if (dot < bestDot) continue;
                if (Physics.Linecast(transform.position + Vector3.up * .85f, target.transform.position + Vector3.up * .85f, 1, QueryTriggerInteraction.Ignore)) continue;
                best = target; bestDot = dot; bestDir = flat.normalized;
            }
            if (best != null) Facing = bestDir;
        }

        /// <returns>True while a lunge (Coach's dash) drives horizontal motion.</returns>
        private bool AdvanceAttack(float dt)
        {
            attackTime += dt;
            bool active = attackTime >= attack.Windup && attackTime <= attack.Windup + attack.Active;
            bool lunging = false;
            if (active)
            {
                if (!activeStarted) { activeStarted = true; OnActiveStart(); }
                switch (attack.Kind)
                {
                    case AttackKind.Basic:
                    case AttackKind.Heavy: SweepCurrent(); break;
                    case AttackKind.Special:
                        if (Kit.Special == SpecialStyle.Dash) { lunging = true; SweepCurrent(); }
                        break;
                    case AttackKind.Super:
                        if (Kit.Super == SuperStyle.Fan) SweepCurrent();
                        else if (Kit.Super == SuperStyle.GroundWave) GroundWave();
                        else if (Kit.Super == SuperStyle.SafeSweep) SweepCurrent();
                        break;
                }
            }
            if (attackTime >= attack.Duration) { attacking = false; comboWindow = attack.Kind == AttackKind.Basic ? .5f : 0; }
            return lunging;
        }

        private void OnActiveStart()
        {
            Vector3 muzzle = transform.position + Vector3.up * .85f + Facing * .65f;
            if (attack.Kind == AttackKind.Special)
            {
                switch (Kit.Special)
                {
                    case SpecialStyle.Plate:
                        BrawlProjectile.Create(match, attackToken, muzzle, Facing, PlayerColor, "Decreto", new Vector3(.8f, .7f, .18f)); break;
                    case SpecialStyle.TagBurst:
                        for (int i = 0; i < 3; i++)
                                BrawlProjectile.Create(match, attackToken, muzzle, (match.IsSideView ? Quaternion.AngleAxis((i - 1) * 12, Vector3.forward) : Quaternion.Euler(0, (i - 1) * 16, 0)) * Facing, PlayerColor,
                                "Etiqueta", new Vector3(.33f, .33f, .25f), 13, .9f);
                        break;
                    case SpecialStyle.BounceToken:
                        BrawlProjectile.Create(match, attackToken, muzzle, Facing, PlayerColor, "Token", new Vector3(.5f, .05f, .5f), 11, 2.2f, 3); break;
                    case SpecialStyle.Emitter:
                        if (activeTrap) Destroy(activeTrap.gameObject);
                        activeTrap = BrawlTrap.Create(match, attackToken, BrawlFeedback.FloorBelow(transform.position + Facing * 1.6f + Vector3.up));
                        BrawlFeedback.Sound("trap"); break;
                    case SpecialStyle.Dash:
                        BrawlFeedback.Burst(transform.position + Vector3.up * .5f, PlayerColor, 6); BrawlFeedback.Sound("swing"); break;
                }
                return;
            }
            bool super = attack.Kind == AttackKind.Super;
            if (super && Kit.Super == SuperStyle.GroundWave)
            {
                if (match.IsSideView) BrawlSideTelegraph.Wave(match,this,attack.Range,attack.Height,attack.Active);
                else BrawlFeedback.Pulse(transform.position, attack.Range, attack.Active, PlayerColor);
            }
            if (super && (Kit.Super == SuperStyle.Zone || Kit.Super == SuperStyle.Bubble)) return; // resolved by the zone
            BrawlFeedback.Burst(transform.position + Vector3.up * .9f + Facing * (super ? .5f : 1), PlayerColor, super ? 18 : 5);
            BrawlFeedback.Sound(super ? "super" : "swing");
            if (super) match.Shake(.3f);
        }

        /// <summary>Volumetric melee/area check: horizontal reach, vertical reach, facing cone and line of sight.</summary>
        private void SweepCurrent()
        {
            if (match.IsSideView) { SweepSideBox(); return; }
            if (!TryGetMeleeVolume(out float range, out float height, out float minDot, out bool safeSectors)) return;
            foreach (var target in match.Fighters)
            {
                if (target == this || target.Eliminated || target.IsRespawning) continue;
                Vector3 difference = target.transform.position - transform.position;
                Vector3 flat = Vector3.ProjectOnPlane(difference, Vector3.up);
                if (match.IsSideView && AttackVertical > 0 && difference.y < -.5f) continue;
                if (match.IsSideView && AttackVertical < 0 && AttackAirborne && difference.y > .5f) continue;
                if (flat.magnitude > range || Mathf.Abs(difference.y) > height) continue;
                if (flat.sqrMagnitude > .2f && Vector3.Dot(Facing, flat.normalized) < minDot) continue;
                if (safeSectors && (match.IsSideView ? Mathf.Abs(difference.x)<=SideSafeRadius : flat.sqrMagnitude > .2f && Mathf.Abs(Vector3.Angle(Facing, flat) - 90) < SafeHalfAngle)) continue;
                if (Physics.Linecast(transform.position + Vector3.up * .85f, target.transform.position + Vector3.up * .85f, 1, QueryTriggerInteraction.Ignore)) continue;
                attackToken.Hit(target, safeSectors && flat.sqrMagnitude > .01f ? flat.normalized : Facing);
            }
        }

        /// <summary>Side view: hit box against each rival's hurtbox in X/Y, mirrored by facing (SYS-042–048).</summary>
        private void SweepSideBox()
        {
            if (!TryGetSideHitbox(out MoveBox box, out bool safeSectors)) return;
            Vector2 root = Root2D; float facing = FacingSign;
            foreach (var target in match.Fighters)
            {
                if (target == this || target.Eliminated || target.IsRespawning) continue;
                Vector2 other = target.Root2D;
                if (!BrawlMoveData.Overlaps(box, root, facing, other)) continue;
                // Tío Antena's super keeps its central refuge (±1.4 m) inside the box.
                if (safeSectors && Mathf.Abs(other.x - root.x) <= SideSafeRadius) continue;
                if (Physics.Linecast(transform.position + Vector3.up * .85f, target.transform.position + Vector3.up * .85f, 1, QueryTriggerInteraction.Ignore)) continue;
                Vector3 away = new Vector3(other.x - root.x, 0, 0);
                attackToken.Hit(target, safeSectors && away.sqrMagnitude > .0001f ? away.normalized : Facing);
            }
        }

        /// <summary>Current shockwave ring shared by the hit check and the lab visualizer.</summary>
        public bool TryGetWaveVolume(out float radius, out float height)
        {
            radius = 0; height = attack.Height;
            if (!attacking || attack.Kind != AttackKind.Super || Kit.Super != SuperStyle.GroundWave) return false;
            radius = attack.Range * Mathf.Clamp01((attackTime - attack.Windup) / attack.Active) + WaveTolerance;
            return true;
        }
        private const float WaveTolerance = .4f;

        /// <summary>Coach's shockwave: travels outward and only hits grounded fighters within the height tolerance.</summary>
        private void GroundWave()
        {
            float radius = attack.Range * Mathf.Clamp01((attackTime - attack.Windup) / attack.Active);
            foreach (var target in match.Fighters)
            {
                if (target == this || target.Eliminated || target.IsRespawning || !target.Grounded) continue;
                Vector3 difference = target.transform.position - transform.position;
                Vector3 flat = Vector3.ProjectOnPlane(difference, Vector3.up);
                if (flat.magnitude > radius + WaveTolerance || Mathf.Abs(difference.y) > attack.Height) continue;
                attackToken.Hit(target, flat.sqrMagnitude > .01f ? flat.normalized : Facing);
            }
        }

        internal void Connected(AttackKind kind, float dealt)
        {
            attackConnected = true;
            if (match.IsSideView) hitPause = .035f;
            float before = Meter;
            if (kind == AttackKind.Basic && !chainCharged) { Meter = Mathf.Min(100, Meter + 8); chainCharged = true; }
            if (kind == AttackKind.Special) Meter = Mathf.Min(100, Meter + 12);
            if (before < 100 && Meter >= 100) BrawlFeedback.Sound("ready");
        }

        internal void Dealt(float amount) => TotalDamageDealt += amount;

        internal BrawlMatch Match => match;
        /// <summary>Can be hit right now (not eliminated, respawning or protected).</summary>
        public bool Hittable => !Eliminated && !IsRespawning && !Invulnerable;

        /// <summary>
        /// Immediate hit outside a simulation step. Inside a step, <see cref="BrawlAttackToken"/> queues hits in the
        /// match instead, and they arrive here together through <see cref="ApplyHits"/> (SYS-045).
        /// </summary>
        public bool ReceiveHit(AttackSpec spec, Vector3 direction, BrawlFighter source)
        {
            if (!Hittable) return false;
            ApplyHits(new List<PendingHit> { new PendingHit(this, source, spec, direction) });
            return true;
        }

        /// <summary>Applies every hit this fighter received in one step at once; the result does not depend on list order.</summary>
        internal void ApplyHits(List<PendingHit> hits)
        {
            if (hits.Count == 0 || Eliminated || IsRespawning) return;
            var result = CombineHits(Damage, Kit.Resistance, settings, hits, match.IsSideView);
            foreach (var hit in hits)
            {
                if (hit.Source == null || hit.Source == this) continue;
                hit.Source.Dealt(hit.Spec.Damage);
                if (!match.IsSideView) continue;
                // Combo counter and hit-stop (side view): every attacker of the step is credited the same way.
                var attacker = hit.Source;
                attacker.comboHitCount = attacker.comboTarget == PlayerIndex && match.SimTime - attacker.comboHitClock < 1.1f ? attacker.comboHitCount + 1 : 1;
                attacker.comboTarget = PlayerIndex; attacker.comboHitClock = match.SimTime;
                hitPause = attacker.hitPause = .035f;
            }
            Damage = result.Damage;
            if (result.Attacker != null) { LastAttacker = result.Attacker; lastHitClock = match.SimTime; }
            velocity = result.Velocity;
            stun = result.Stun; Grounded = false; hoverTimer = 0;
            attacking = false; comboWindow = 0; dodgeRemaining = 0;
            BrawlFeedback.Burst(transform.position + Vector3.up * .9f, PlayerColor, 9 + 3 * (hits.Count - 1));
            BrawlFeedback.Sound("hit");
            float strength = Mathf.Clamp01(result.Push / Mathf.Max(1, settings.maxKnockback));
            match.Haptic(PlayerIndex, .35f + .55f * strength, .2f + .6f * strength, .18f + .2f * strength);
        }

        public struct HitOutcome { public float Damage; public Vector3 Velocity; public float Push; public float Stun; public BrawlFighter Attacker; }

        /// <summary>
        /// Order-independent combination: total damage first, then each hit's push uses the resulting damage,
        /// horizontal pushes add up (clamped), lift and stun take the strongest. One hit equals the beta formula.
        /// Attribution goes to the strongest hit; ties go to the attacker with the lower player number.
        /// </summary>
        public static HitOutcome CombineHits(float damageBefore, float resistance, BrawlSettings settings, IList<PendingHit> hits, bool sideView = false)
        {
            float damage = damageBefore;
            foreach (var hit in hits) damage += hit.Spec.Damage;
            Vector3 horizontal = Vector3.zero; float lift = 0, stun = 0, strongest = -1; bool hasLift = false; BrawlFighter attacker = null;
            foreach (var hit in hits)
            {
                float push = Mathf.Min(settings.maxKnockback, (hit.Spec.Push + damage * settings.knockbackPerDamage) / resistance);
                Vector3 direction = hit.Direction; direction.y = 0;
                // Side view: knockback stays in X/Y; the horizontal part only keeps its left/right sign.
                if (sideView) direction = Vector3.right * Mathf.Sign(direction.x == 0 ? (hit.Source ? hit.Source.Facing.x : 1) : direction.x);
                horizontal += direction.normalized * push;
                // Strongest vertical launch wins with its sign, so a downward finisher (negative lift) still spikes.
                float hitLift = Mathf.Min(16, hit.Spec.Lift + damage * .018f);
                if (!hasLift || Mathf.Abs(hitLift) > Mathf.Abs(lift)) { lift = hitLift; hasLift = true; }
                stun = Mathf.Max(stun, Mathf.Min(.48f, .13f + push * .009f));
                if (hit.Source == null || hit.Source == hit.Target) continue;
                float weight = hit.Spec.Damage * 1000 + push;
                if (weight > strongest + .0001f || (Mathf.Abs(weight - strongest) <= .0001f && attacker != null && hit.Source.PlayerIndex < attacker.PlayerIndex))
                { strongest = weight; attacker = hit.Source; }
            }
            horizontal = Vector3.ClampMagnitude(horizontal, settings.maxKnockback);
            return new HitOutcome { Damage = damage, Velocity = horizontal + Vector3.up * lift, Push = horizontal.magnitude, Stun = stun, Attacker = attacker };
        }

        /// <returns>The fighter credited with this expulsion, or null for a self-destruct.</returns>
        internal BrawlFighter LoseStock()
        {
            BrawlFighter credited = LastAttacker != null && match.SimTime - lastHitClock <= settings.attributionSeconds ? LastAttacker : null;
            if (credited != null) credited.KOs++; else SelfDestructs++;
            LastAttacker = null;
            Stocks--; Damage = 0; Meter = 0; command = default; velocity = Vector3.zero;
            attacking = false; protection = 0; stun = 0; chainCharged = false; comboWindow = 0; hoverTimer = 0; recoveryTimer = 0;
            if (activeTrap) Destroy(activeTrap.gameObject);
            BrawlFeedback.Burst(transform.position + Vector3.up, PlayerColor, 20); BrawlFeedback.Sound("ko");
            match.Haptic(PlayerIndex, .95f, .9f, .5f);
            controller.enabled = false;
            transform.position = match.SafeSpawnFor(this);
            if (Eliminated) gameObject.SetActive(false);
            else { IsRespawning = true; RespawnRemaining = settings.respawnSeconds; visual.gameObject.SetActive(false); marker.gameObject.SetActive(false); facingLine.enabled = false; }
            return credited;
        }

        public void RespawnAt(Vector3 position, bool grantProtection)
        {
            if (match.IsSideView) position.z = 0;
            controller.enabled = false; transform.position = position; controller.enabled = true;
            IsRespawning = false; velocity = Vector3.zero; jumps = 0; recoveryUsed = false; airDodgeUsed = false;
            dodgeRemaining = 0; dodgeCooldown = 0; stun = 0; Grounded = false; hoverTimer = 0; recoveryTimer = 0;
            protection = grantProtection ? settings.protectionSeconds : 0;
            if (match.IsSideView)
            {
                Facing = transform.position.x > 0 ? Vector3.left : Vector3.right;
                float nearest = float.PositiveInfinity;
                foreach (var rival in match.Fighters)
                {
                    if (!rival || rival == this || rival.Eliminated) continue;
                    float delta = rival.transform.position.x - transform.position.x;
                    if (Mathf.Abs(delta) < .01f || Mathf.Abs(delta) >= nearest) continue;
                    nearest = Mathf.Abs(delta); Facing = delta > 0 ? Vector3.right : Vector3.left;
                }
                basicBuffer = heavyBuffer = jumpBuffer = hitPause = dropRemaining = 0;
                comboHitCount = 0; comboTarget = -1;
            }
            if (visual != null) visual.gameObject.SetActive(true);
            if (facingLine != null) facingLine.enabled = true;
        }

        /// <summary>Test hook used only by the standalone smoke probe.</summary>
        internal void SetMeterForTest(float value) => Meter = Mathf.Clamp(value, 0, 100);

        internal void SetPracticeDamage(float value) => Damage = Mathf.Clamp(value, 0, 300);

        internal void ResetPractice(Vector3 position, float damage)
        {
            if (activeTrap) Destroy(activeTrap.gameObject);
            attacking = false; comboWindow = 0; combo = 0; chainCharged = false;
            attackToken = null; activeStarted = false; LastAttacker = null;
            Damage = damage; Meter = 0; command = default;
            RespawnAt(position, false);
        }

        // ───────────────────────── Practice AI ─────────────────────────
        private BrawlCommand BotCommand(float dt)
        {
            var result = new BrawlCommand();
            Vector3 position = transform.position;
            Vector3 safe = match.Arena.SafePoint(position);
            bool overVoid = !Physics.Raycast(position + Vector3.up * .3f, Vector3.down, 25, 1, QueryTriggerInteraction.Ignore);
            if (!Grounded && overVoid)
            {
                // Return to the court: double jump first, then the kit recovery while falling.
                Vector3 home = Vector3.ProjectOnPlane(safe - position, Vector3.up).normalized;
                result.Move = match.WorldToCamera(home); result.Aim = result.Move;
                if (jumps < 2 && velocity.y < 1) result.Jump = true;
                else if (!recoveryUsed && velocity.y < 0) result.Recovery = true;
                return result;
            }

            BrawlFighter target = null; float closest = float.MaxValue;
            foreach (var candidate in match.Fighters)
            {
                if (candidate == this || candidate.Eliminated || candidate.IsRespawning) continue;
                float dist = (candidate.transform.position - position).sqrMagnitude;
                if (dist < closest) { closest = dist; target = candidate; }
            }
            if (target == null) return result;
            Vector3 delta = target.transform.position - position;
            Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
            float distance = flat.magnitude;
            Vector3 desire = distance > .01f ? flat / distance : Facing;

            if (Grounded && !FloorAt(position + desire * 1.2f, position.y, out _))
            {
                bool landing = false;
                foreach (float reach in new[] { 2.5f, 3.5f, 4.5f })
                    if (FloorAt(position + desire * reach, position.y, out float top) && top <= position.y + 1.6f) { landing = true; break; }
                if (landing && distance > 2.5f) result.Jump = true;
                else
                {
                    // No safe landing: route through the spawn point nearest to the target instead of walking off.
                    Vector3 waypoint = match.Arena.SafePoint(target.transform.position);
                    Vector3 route = Vector3.ProjectOnPlane(waypoint - position, Vector3.up);
                    desire = route.sqrMagnitude > 1f && FloorAt(position + route.normalized * 1.2f, position.y, out _) ? route.normalized
                        : Vector3.ProjectOnPlane(safe - position, Vector3.up).normalized;
                }
            }
            else if (!Grounded && velocity.y < -2 && jumps < 2 && !FloorAt(position, position.y, out _)) result.Jump = true;

            if (distance > 1.55f) result.Move = match.WorldToCamera(desire);
            result.Aim = match.WorldToCamera(distance > .01f ? flat / distance : Facing);
            botThink -= dt;
            if (botThink <= 0)
            {
                botThink = Random.Range(.28f, .65f);
                bool level = Mathf.Abs(delta.y) < 1.3f;
                if (Meter >= 100 && level && distance < Kit.SuperRange * .8f + (Kit.Super == SuperStyle.Bubble || Kit.Super == SuperStyle.Zone ? 2 : 0)) result.Super = true;
                else if (distance < Kit.BasicRange + .4f && level)
                { result.Heavy = target.Damage > 65 || Random.value < .3f; result.Basic = !result.Heavy; }
                else if (level && distance >= Kit.SpecialPreferredRange.x && distance <= Kit.SpecialPreferredRange.y &&
                         (Kit.Special != SpecialStyle.Emitter || activeTrap == null)) result.Special = true;
                if (delta.y > .8f && distance < 5 && jumps < 2) result.Jump = true;
                if (distance < 3 && target.attacking && target.attack.Kind != AttackKind.Basic && Random.value < .45f) result.Dodge = true;
            }
            if (match.IsSideView && distance < 2.5f && Mathf.Abs(delta.y) > .6f)
                result.Move.y = delta.y > 0 ? 1 : (!Grounded ? -1 : 0);
            return result;
        }

        private static bool FloorAt(Vector3 point, float referenceY, out float top)
        {
            top = 0;
            if (!Physics.Raycast(new Vector3(point.x, referenceY + 2.5f, point.z), Vector3.down, out RaycastHit hit, 6.5f, 1, QueryTriggerInteraction.Ignore)) return false;
            top = hit.point.y; return true;
        }

        public void ClearInput() => command = default;
    }

    /// <summary>One activation of an attack: at most one hit per target, and at most one meter charge.</summary>
    public sealed class BrawlAttackToken
    {
        public readonly BrawlFighter Source;
        public readonly AttackSpec Spec;
        private readonly HashSet<int> hits = new HashSet<int>();
        private bool charged;
        public BrawlAttackToken(BrawlFighter source, AttackSpec spec) { Source = source; Spec = spec; }
        public bool Hit(BrawlFighter target, Vector3 direction)
        {
            if (target == Source || hits.Contains(target.PlayerIndex)) return false;
            BrawlMatch match = target.Match;
            // Inside a simulation step every hit is queued and checked against the state at the start of the step.
            if (match != null && match.CollectingHits)
            {
                if (!match.QueueHit(new PendingHit(target, Source, Spec, direction))) return false;
            }
            else if (!target.ReceiveHit(Spec, direction, Source)) return false;
            hits.Add(target.PlayerIndex);
            if (!charged) { Source.Connected(Spec.Kind, Spec.Damage); charged = true; }
            return true;
        }
    }

    public readonly struct PendingHit
    {
        public readonly BrawlFighter Target, Source; public readonly AttackSpec Spec; public readonly Vector3 Direction;
        public PendingHit(BrawlFighter target, BrawlFighter source, AttackSpec spec, Vector3 direction)
        { Target = target; Source = source; Spec = spec; Direction = direction; }
    }
}
