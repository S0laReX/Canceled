using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Everything the art needs to know about a fighter this frame. Read-only for the visual.</summary>
    public struct BrawlPose
    {
        /// <summary>Planar velocity in the fighter's local space (+Z forward).</summary>
        public Vector3 LocalVelocity;
        public float VerticalSpeed;
        public bool Grounded, Hurt, Protected, Dodging, Recovering, Attacking;
        public AttackKind Attack;
        public SpecialStyle Special;
        public int Combo;
        /// <summary>0–1 over the whole attack; WindupEnd/ActiveEnd mark where the hit starts and ends.</summary>
        public float AttackProgress, WindupEnd, ActiveEnd;
        public int VerticalAttack;
        public bool AirAttack;
    }

    public sealed partial class BrawlFighterVisual
    {
        private Transform[] bones;
        private Vector3[] restPositions;
        private SkinnedMeshRenderer skinned;
        private readonly Vector3[] target = new Vector3[BrawlAutoRig.BoneCount];
        private Vector3 hipsOffset, hipsOffsetCurrent;
        private float airBlend, landSquash;
        private bool wasGrounded = true, instantPose;

        /// <summary>Name of the look in use ("Provisional" for primitives).</summary>
        public string SkinName { get; private set; } = "Provisional";
        public bool UsesModel => skinned != null;

        public void Build(int characterIndex, int skin, Color teamColor, BrawlCharacterArt art)
        {
            ClearModel();
            character = Mathf.Clamp(characterIndex, 0, BrawlRoster.Count - 1);
            BrawlKit kit = BrawlRoster.Get(character);
            BrawlCharacterArt.Entry entry = art != null && kit.Skins.Length > 0
                ? art.Find(kit.Skins[BrawlRoster.WrapSkin(character, skin)]) : null;
            BrawlAutoRig.Rig rig = entry != null ? BrawlAutoRig.Get(entry.model) : null;
            if (rig == null) { BuildPrimitive(characterIndex, teamColor); SkinName = "Provisional"; return; }
            SkinName = BrawlRoster.SkinName(character, skin);

            model = Pivot("Model", transform, Vector3.zero);
            bones = new Transform[BrawlAutoRig.BoneCount];
            restPositions = new Vector3[BrawlAutoRig.BoneCount];
            for (int b = 0; b < BrawlAutoRig.BoneCount; b++)
            {
                int parent = BrawlAutoRig.Parents[b];
                Vector3 local = rig.Joints[b] - (parent >= 0 ? rig.Joints[parent] : Vector3.zero);
                bones[b] = Pivot(BrawlAutoRig.BoneNames[b], parent >= 0 ? bones[parent] : model, local);
                restPositions[b] = local;
            }

            var meshObject = new GameObject("Mesh " + SkinName);
            meshObject.transform.SetParent(model, false);
            skinned = meshObject.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = rig.Mesh;
            skinned.bones = bones;
            skinned.rootBone = bones[BrawlAutoRig.Hips];
            // Bounds relative to the hips, padded for raised arms and lunges.
            Bounds local0 = rig.Bounds; local0.center -= rig.Joints[BrawlAutoRig.Hips]; local0.Expand(1.4f);
            skinned.localBounds = local0;
            skinned.updateWhenOffscreen = false;

            // GDD §11: one toon material with its outline for every fighter; URP Lit only if the toon shader is missing.
            Shader shader = BrawlFighterVisual.ToonShader;
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = "Fighter " + SkinName, color = Color.white };
            if (entry.albedo != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", entry.albedo);
                material.mainTexture = entry.albedo;
            }
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .28f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            materials.Add(material); originalColors.Add(Color.white);
            skinned.sharedMaterial = material;
            renderers.Add(skinned);
            wasGrounded = true; airBlend = 0; landSquash = 0; hipsOffsetCurrent = Vector3.zero;
        }

        /// <summary>Kept for callers without art: primitive placeholder.</summary>
        public void Build(int characterIndex, Color teamColor) => Build(characterIndex, 0, teamColor, null);

        public void Animate(BrawlPose pose) => Animate(pose, false);

        /// <summary><paramref name="instant"/> snaps to the target pose (previews and tests).</summary>
        public void Animate(BrawlPose pose, bool instant)
        {
            if (model == null) return;
            if (skinned == null)
            {
                Vector3 world = transform.TransformDirection(pose.LocalVelocity);
                AnimatePrimitive(world, pose.Grounded, pose.Attacking ? Mathf.Max(.001f, pose.AttackProgress) : 0, pose.Hurt, pose.Protected);
                return;
            }
            float dt = instant ? 1f / 60 : Mathf.Min(Time.deltaTime, .05f);
            instantPose = instant;
            ComposePose(pose, dt);
            float rate = pose.Attacking || pose.Hurt || pose.Dodging ? 30f : 15f;
            float k = instant ? 1 : 1 - Mathf.Exp(-rate * dt);
            for (int b = 0; b < bones.Length; b++)
                bones[b].localRotation = Quaternion.Slerp(bones[b].localRotation, Quaternion.Euler(target[b]), k);
            hipsOffsetCurrent = Vector3.Lerp(hipsOffsetCurrent, hipsOffset, k);
            bones[BrawlAutoRig.Hips].localPosition = restPositions[BrawlAutoRig.Hips] + hipsOffsetCurrent;
            float squash = landSquash * .11f;
            float stretch = pose.Grounded ? 0 : Mathf.Clamp(Mathf.Abs(pose.VerticalSpeed) * .004f, 0, .04f);
            model.localScale = new Vector3(1 + squash * .5f - stretch * .5f, 1 - squash + stretch, 1 + squash * .5f - stretch * .5f);
            Tint(pose.Hurt, pose.Protected);
        }

        /// <summary>Gait phase override for previews (radians).</summary>
        public void SetGait(float phase) => gait = phase;

        private void ComposePose(BrawlPose p, float dt)
        {
            System.Array.Clear(target, 0, target.Length);
            hipsOffset = Vector3.zero;
            float time = Time.time;
            if (p.Grounded && !wasGrounded && dt < .5f) landSquash = 1;
            wasGrounded = p.Grounded;
            landSquash = Mathf.MoveTowards(landSquash, 0, dt * 6.5f);
            airBlend = instantPose ? (p.Grounded ? 0 : 1) : Mathf.MoveTowards(airBlend, p.Grounded ? 0 : 1, dt * 7f);

            // ── Locomotion (ground).
            Vector3 v = p.LocalVelocity; v.y = 0;
            float speed = v.magnitude, stride = Mathf.Clamp01(speed / 7f);
            if (!instantPose) gait += dt * Mathf.Lerp(4.5f, 12.5f, stride) * (p.Grounded ? 1 : .3f);
            float s = Mathf.Sin(gait), c = Mathf.Cos(gait), breathe = Mathf.Sin(time * 2.3f);
            float back = speed > .1f && v.z < -.2f ? -1 : 1; // backpedal reverses the swing
            float ground = 1 - airBlend;

            Set(BrawlAutoRig.ThighL, -s * 44 * stride * back, 0, 0);
            Set(BrawlAutoRig.ThighR, s * 44 * stride * back, 0, 0);
            Set(BrawlAutoRig.ShinL, stride * (8 + 55 * Mathf.Max(0, c * back)), 0, 0);
            Set(BrawlAutoRig.ShinR, stride * (8 + 55 * Mathf.Max(0, -c * back)), 0, 0);
            Set(BrawlAutoRig.UpperArmL, s * 38 * stride * back, 0, -3 - breathe * 1.5f);
            Set(BrawlAutoRig.UpperArmR, -s * 38 * stride * back, 0, 3 + breathe * 1.5f);
            Set(BrawlAutoRig.ForearmL, -12 - 32 * stride, 0, 0);
            Set(BrawlAutoRig.ForearmR, -12 - 32 * stride, 0, 0);
            Set(BrawlAutoRig.Chest, 7 * stride + breathe * 1.3f, s * 8 * stride, -v.x * 1.4f);
            Set(BrawlAutoRig.Hips, 2 * stride, -s * 6 * stride, v.x * .8f);
            Set(BrawlAutoRig.Head, -4 * stride, Mathf.Sin(time * .7f) * 6 * (1 - stride), 0);
            hipsOffset.y = (-Mathf.Abs(s) * .055f + .025f) * stride * ground - landSquash * .09f;

            // ── Air: knees tucked while rising, limbs open while falling.
            if (airBlend > 0)
            {
                float rising = Mathf.Clamp01(p.VerticalSpeed / 6f), falling = Mathf.Clamp01(-p.VerticalSpeed / 8f);
                float kick = Mathf.Sin(time * 9f) * 6 * falling;
                Blend(BrawlAutoRig.ThighL, new Vector3(Mathf.Lerp(-58, -18, falling) + kick, 0, -4), airBlend);
                Blend(BrawlAutoRig.ShinL, new Vector3(Mathf.Lerp(78, 25, falling), 0, 0), airBlend);
                Blend(BrawlAutoRig.ThighR, new Vector3(Mathf.Lerp(-14, -4, falling) - kick, 0, 4), airBlend);
                Blend(BrawlAutoRig.ShinR, new Vector3(Mathf.Lerp(40, 18, falling), 0, 0), airBlend);
                float lift = Mathf.Lerp(45, 72, falling) + rising * 10;
                Blend(BrawlAutoRig.UpperArmL, new Vector3(-22, 0, -lift), airBlend);
                Blend(BrawlAutoRig.UpperArmR, new Vector3(-22, 0, lift), airBlend);
                Blend(BrawlAutoRig.ForearmL, new Vector3(-30, 0, 0), airBlend);
                Blend(BrawlAutoRig.ForearmR, new Vector3(-30, 0, 0), airBlend);
                Blend(BrawlAutoRig.Chest, new Vector3(Mathf.Lerp(10, -6, falling), 0, 0), airBlend);
                Blend(BrawlAutoRig.Head, new Vector3(Mathf.Lerp(-6, 8, falling), 0, 0), airBlend);
            }

            if (p.Recovering)
            {
                // Arms thrown up, legs together: reads as "reaching back to the stage".
                Set(BrawlAutoRig.UpperArmL, -25, 0, -130); Set(BrawlAutoRig.UpperArmR, -25, 0, 130);
                Set(BrawlAutoRig.ForearmL, -10, 0, 0); Set(BrawlAutoRig.ForearmR, -10, 0, 0);
                Set(BrawlAutoRig.ThighL, -8, 0, 2); Set(BrawlAutoRig.ThighR, -8, 0, -2);
                Set(BrawlAutoRig.ShinL, 20, 0, 0); Set(BrawlAutoRig.ShinR, 20, 0, 0);
                Set(BrawlAutoRig.Chest, -10, 0, 0); Set(BrawlAutoRig.Head, -18, 0, 0);
            }

            if (p.Dodging)
            {
                Set(BrawlAutoRig.Chest, 32, 0, -v.x * 2f); Set(BrawlAutoRig.Head, -14, 0, 0);
                Set(BrawlAutoRig.ThighL, -72, 0, -6); Set(BrawlAutoRig.ThighR, -40, 0, 6);
                Set(BrawlAutoRig.ShinL, 95, 0, 0); Set(BrawlAutoRig.ShinR, 80, 0, 0);
                Set(BrawlAutoRig.UpperArmL, -55, 0, -20); Set(BrawlAutoRig.UpperArmR, -55, 0, 20);
                Set(BrawlAutoRig.ForearmL, -70, 0, 0); Set(BrawlAutoRig.ForearmR, -70, 0, 0);
                hipsOffset.y = -.26f;
            }

            if (p.Attacking) ComposeAttack(p);
            if (p.Attacking && p.VerticalAttack != 0 && (p.Attack == AttackKind.Basic || p.Attack == AttackKind.Heavy))
            {
                float strike = Mathf.Sin(p.AttackProgress * Mathf.PI);
                Set(BrawlAutoRig.UpperArmR, p.VerticalAttack > 0 ? -155 * strike : 25, 0, 15);
                Set(BrawlAutoRig.ForearmR, -25, 0, 0);
                Set(BrawlAutoRig.Chest, p.VerticalAttack > 0 ? -15 : 35, 0, 0);
                if (p.AirAttack && p.VerticalAttack < 0) { Set(BrawlAutoRig.ThighR, 40, 0, 0); Set(BrawlAutoRig.ShinR, 0, 0, 0); }
            }

            if (p.Hurt)
            {
                float shake = Mathf.Sin(time * 40f) * 4;
                Set(BrawlAutoRig.Chest, -24, shake, 0); Set(BrawlAutoRig.Head, -18, 0, shake);
                Set(BrawlAutoRig.UpperArmL, -35, 0, -68); Set(BrawlAutoRig.UpperArmR, -35, 0, 68);
                Set(BrawlAutoRig.ForearmL, -45, 0, 0); Set(BrawlAutoRig.ForearmR, -45, 0, 0);
                Set(BrawlAutoRig.ThighL, -28, 0, -6); Set(BrawlAutoRig.ThighR, 10, 0, 6);
                Set(BrawlAutoRig.ShinL, 40, 0, 0); Set(BrawlAutoRig.ShinR, 25, 0, 0);
            }
        }

        private void ComposeAttack(BrawlPose p)
        {
            float t = Mathf.Clamp01(p.AttackProgress);
            float we = Mathf.Clamp(p.WindupEnd, .02f, .95f), ae = Mathf.Clamp(p.ActiveEnd, we + .01f, .99f);
            float strike = Ease(Mathf.Clamp01((t - we) / Mathf.Max(.01f, ae - we) * 1.6f));
            float settle = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - ae) / Mathf.Max(.01f, 1 - ae)));
            float windup = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / we));
            float hold = 1 - settle * .85f; // the strike pose melts back into locomotion during recovery

            switch (p.Attack)
            {
                case AttackKind.Basic:
                    if (p.Combo == 2)
                    {
                        // Finisher: big spinning hook with a step in.
                        Pose(BrawlAutoRig.Chest, new Vector3(6, 40, 0), new Vector3(16, -48, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.UpperArmR, new Vector3(20, 0, 55), new Vector3(-80, -30, 70), windup, strike, hold);
                        Pose(BrawlAutoRig.ForearmR, new Vector3(-60, 0, 0), new Vector3(-20, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.UpperArmL, new Vector3(-30, 0, -20), new Vector3(15, 0, -40), windup, strike, hold);
                        Pose(BrawlAutoRig.ForearmL, new Vector3(-90, 0, 0), new Vector3(-60, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.ThighL, new Vector3(-10, 0, 0), new Vector3(-42, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.ShinL, new Vector3(15, 0, 0), new Vector3(45, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.ThighR, new Vector3(10, 0, 0), new Vector3(22, 0, 0), windup, strike, hold);
                    }
                    else
                    {
                        // Alternating jabs: right on 0, left on 1; the other arm guards.
                        bool right = p.Combo == 0;
                        int punch = right ? BrawlAutoRig.UpperArmR : BrawlAutoRig.UpperArmL;
                        int punchFore = right ? BrawlAutoRig.ForearmR : BrawlAutoRig.ForearmL;
                        int guard = right ? BrawlAutoRig.UpperArmL : BrawlAutoRig.UpperArmR;
                        int guardFore = right ? BrawlAutoRig.ForearmL : BrawlAutoRig.ForearmR;
                        float side = right ? 1 : -1;
                        Pose(punch, new Vector3(25, 0, side * 22), new Vector3(-92, side * -12, side * 8), windup, strike, hold);
                        Pose(punchFore, new Vector3(-110, 0, 0), new Vector3(-4, 0, 0), windup, strike, hold);
                        Pose(guard, new Vector3(-45, 0, -side * 12), new Vector3(-38, 0, -side * 18), windup, strike, hold);
                        Pose(guardFore, new Vector3(-105, 0, 0), new Vector3(-110, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.Chest, new Vector3(4, side * 18, 0), new Vector3(10, side * -24, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.Head, new Vector3(0, side * -10, 0), new Vector3(0, side * 14, 0), windup, strike, hold);
                    }
                    break;

                case AttackKind.Heavy:
                    // Overhead wind-up, slam down with a lunge.
                    Pose(BrawlAutoRig.UpperArmL, new Vector3(-150, 0, -15), new Vector3(-58, 0, -6), windup, strike, hold);
                    Pose(BrawlAutoRig.UpperArmR, new Vector3(-150, 0, 15), new Vector3(-58, 0, 6), windup, strike, hold);
                    Pose(BrawlAutoRig.ForearmL, new Vector3(-50, 0, 0), new Vector3(-8, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.ForearmR, new Vector3(-50, 0, 0), new Vector3(-8, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.Chest, new Vector3(-16, 0, 0), new Vector3(30, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.Head, new Vector3(-10, 0, 0), new Vector3(-12, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.ThighL, new Vector3(-8, 0, 0), new Vector3(-50, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.ShinL, new Vector3(12, 0, 0), new Vector3(60, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.ThighR, new Vector3(6, 0, 0), new Vector3(28, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.ShinR, new Vector3(8, 0, 0), new Vector3(18, 0, 0), windup, strike, hold);
                    hipsOffset.y += Mathf.Lerp(0, -.14f, strike) * hold;
                    break;

                case AttackKind.Special:
                    if (p.Special == SpecialStyle.Dash)
                    {
                        // Shoulder charge.
                        Pose(BrawlAutoRig.Chest, new Vector3(20, 25, 0), new Vector3(38, -20, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.UpperArmR, new Vector3(-10, 0, 30), new Vector3(-70, 0, 40), windup, strike, hold);
                        Pose(BrawlAutoRig.ForearmR, new Vector3(-90, 0, 0), new Vector3(-95, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.UpperArmL, new Vector3(30, 0, -10), new Vector3(40, 0, -15), windup, strike, hold);
                        Pose(BrawlAutoRig.ThighL, new Vector3(-30, 0, 0), new Vector3(-60, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.ShinL, new Vector3(40, 0, 0), new Vector3(50, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.ThighR, new Vector3(20, 0, 0), new Vector3(35, 0, 0), windup, strike, hold);
                        hipsOffset.y += -.08f * windup * hold;
                    }
                    else if (p.Special == SpecialStyle.Emitter)
                    {
                        // Crouch and plant the device in front.
                        Pose(BrawlAutoRig.Chest, new Vector3(10, 0, 0), new Vector3(42, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.UpperArmR, new Vector3(-30, 0, 10), new Vector3(-62, 0, 4), windup, strike, hold);
                        Pose(BrawlAutoRig.UpperArmL, new Vector3(-20, 0, -10), new Vector3(-50, 0, -4), windup, strike, hold);
                        Pose(BrawlAutoRig.ThighL, new Vector3(-30, 0, 0), new Vector3(-75, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.ThighR, new Vector3(-20, 0, 0), new Vector3(-55, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.ShinL, new Vector3(40, 0, 0), new Vector3(100, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.ShinR, new Vector3(30, 0, 0), new Vector3(85, 0, 0), windup, strike, hold);
                        hipsOffset.y += Mathf.Lerp(-.08f * windup, -.3f, strike) * hold;
                    }
                    else
                    {
                        // Throw / cast: right arm (both for the tag burst) snaps forward.
                        bool both = p.Special == SpecialStyle.TagBurst;
                        Pose(BrawlAutoRig.UpperArmR, new Vector3(40, 0, 30), new Vector3(-95, 0, 6), windup, strike, hold);
                        Pose(BrawlAutoRig.ForearmR, new Vector3(-80, 0, 0), new Vector3(-5, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.UpperArmL, both ? new Vector3(40, 0, -30) : new Vector3(-30, 0, -25),
                            both ? new Vector3(-95, 0, -6) : new Vector3(20, 0, -30), windup, strike, hold);
                        Pose(BrawlAutoRig.ForearmL, new Vector3(-80, 0, 0), both ? new Vector3(-5, 0, 0) : new Vector3(-40, 0, 0), windup, strike, hold);
                        Pose(BrawlAutoRig.Chest, new Vector3(-6, both ? 0 : 28, 0), new Vector3(14, both ? 0 : -22, 0), windup, strike, hold);
                    }
                    break;

                case AttackKind.Super:
                    // Gather (crouch, arms crossed) then explode outwards.
                    Pose(BrawlAutoRig.UpperArmL, new Vector3(-75, 0, 40), new Vector3(-35, 0, -115), windup, strike, hold);
                    Pose(BrawlAutoRig.UpperArmR, new Vector3(-75, 0, -40), new Vector3(-35, 0, 115), windup, strike, hold);
                    Pose(BrawlAutoRig.ForearmL, new Vector3(-110, 0, 0), new Vector3(-10, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.ForearmR, new Vector3(-110, 0, 0), new Vector3(-10, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.Chest, new Vector3(26, 0, 0), new Vector3(-20, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.Head, new Vector3(18, 0, 0), new Vector3(-22, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.ThighL, new Vector3(-55, 0, -10), new Vector3(-8, 0, -14), windup, strike, hold);
                    Pose(BrawlAutoRig.ThighR, new Vector3(-55, 0, 10), new Vector3(-8, 0, 14), windup, strike, hold);
                    Pose(BrawlAutoRig.ShinL, new Vector3(90, 0, 0), new Vector3(8, 0, 0), windup, strike, hold);
                    Pose(BrawlAutoRig.ShinR, new Vector3(90, 0, 0), new Vector3(8, 0, 0), windup, strike, hold);
                    hipsOffset.y += Mathf.Lerp(-.24f * windup, .06f, strike) * hold;
                    // Trembling charge-up, skipped with reduced effects.
                    if (strike <= 0 && !BrawlFeedback.ReducedEffects)
                        target[BrawlAutoRig.Chest].y += Mathf.Sin(Time.time * 55f) * 3 * windup;
                    break;
            }
        }

        /// <summary>Windup pose → strike pose, then blended back toward locomotion by <paramref name="hold"/>.</summary>
        private void Pose(int bone, Vector3 windupPose, Vector3 strikePose, float windup, float strike, float hold)
        {
            Vector3 pose = Vector3.Lerp(windupPose * windup, strikePose, strike);
            target[bone] = Vector3.Lerp(target[bone], pose, hold);
        }

        private void Set(int bone, float x, float y, float z) => target[bone] = new Vector3(x, y, z);
        private void Blend(int bone, Vector3 value, float amount) => target[bone] = Vector3.Lerp(target[bone], value, amount);
        private static float Ease(float x) => 1 - (1 - x) * (1 - x) * (1 - x);

        private void Tint(bool hurt, bool protectedSpawn)
        {
            float protectionTint = !protectedSpawn ? 0f : BrawlFeedback.ReducedEffects ? 0.24f : 0.15f + (Mathf.Sin(Time.time * 5f) + 1f) * 0.09f;
            for (int i = 0; i < materials.Count; i++)
            {
                Color color = Color.Lerp(originalColors[i], Color.white, protectionTint);
                if (protectedSpawn) color = Color.Lerp(originalColors[i], new Color(1.6f, 1.6f, 1.6f), protectionTint);
                if (hurt) color = Color.Lerp(color, new Color(1f, 0.38f, 0.34f), 0.45f);
                materials[i].color = color;
            }
        }

        private void ClearRig()
        {
            bones = null; restPositions = null; skinned = null;
        }
    }
}
