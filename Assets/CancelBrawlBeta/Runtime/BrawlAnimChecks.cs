using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// <c>-anim-smoke</c>: the minimum profile animation set of GDD 2.0 §11 on the current 11-bone rig, for the five kits.
    /// Static part: every state is posed, differs from rest and from its neighbour state, and every attack shows its
    /// anticipation (windup pose ≠ strike pose); one contact sheet per run. Live part: turn, braking, double-jump flip,
    /// launch tumble and respawn entry play from real ticks, are cut by hits and always end upright.
    /// </summary>
    public static class BrawlAnimChecks
    {
        [Serializable] private sealed class StateRow { public string character, state; public float fromRest; }
        [Serializable] private sealed class AttackRow { public string character, attack; public float anticipation; }
        [Serializable]
        private sealed class Report
        {
            public List<StateRow> states = new List<StateRow>();
            public List<AttackRow> attacks = new List<AttackRow>();
            public float turnMinForwardZ, turnMaxYaw, flipMaxPitch, tumbleMax, spawnMinScale;
            public bool brakeSeen, flipCutByHit, turnCutByAttack, lightHitNoTumble;
        }

        private const float Windup = .3f, Active = .55f, WindupT = .27f, StrikeT = .46f;
        private const int Cell = 160, CellHeight = 240, Layer = 29;
        private static readonly Color Backdrop = new Color(.5f, .55f, .6f);

        private struct State { public string Name; public BrawlPose Pose; public float Gait; public bool Land; }

        private static BrawlPose Attack(AttackKind kind, float t, bool air = false, int vertical = 0, SpecialStyle special = default) => new BrawlPose
        {
            Grounded = !air, AirAttack = air, VerticalSpeed = air ? 1 : 0, Attacking = true, Attack = kind, Special = special,
            AttackProgress = t, WindupEnd = Windup, ActiveEnd = Active, VerticalAttack = vertical
        };

        private static List<State> States(SpecialStyle special)
        {
            var list = new List<State>();
            void Add(string name, BrawlPose pose, float gait = 0, bool land = false) => list.Add(new State { Name = name, Pose = pose, Gait = gait, Land = land });
            Add("reposo", new BrawlPose { Grounded = true });
            Add("carrera", new BrawlPose { Grounded = true, LocalVelocity = new Vector3(0, 0, 7) }, Mathf.PI * .5f);
            Add("frenado", new BrawlPose { Grounded = true, Braking = 1 });
            Add("giro", new BrawlPose { Grounded = true, Turn = .5f });
            Add("salto", new BrawlPose { VerticalSpeed = 6 });
            Add("segundo salto", new BrawlPose { VerticalSpeed = 4, Flip = .5f });
            Add("caída", new BrawlPose { VerticalSpeed = -8 });
            Add("aterrizaje", new BrawlPose { Grounded = true }, 0, true);
            Add("golpe neutral", Attack(AttackKind.Basic, StrikeT));
            Add("golpe ascendente", Attack(AttackKind.Basic, StrikeT, false, 1));
            Add("aéreo neutral", Attack(AttackKind.Basic, StrikeT, true));
            Add("remate descendente", Attack(AttackKind.Basic, StrikeT, true, -1));
            Add("fuerte", Attack(AttackKind.Heavy, StrikeT));
            Add("especial", Attack(AttackKind.Special, StrikeT, false, 0, special));
            Add("recuperación", new BrawlPose { VerticalSpeed = 3, Recovering = true });
            Add("esquiva", new BrawlPose { Grounded = true, Dodging = true });
            Add("hitstun", new BrawlPose { Hurt = true, VerticalSpeed = 2 });
            Add("lanzamiento", new BrawlPose { Hurt = true, VerticalSpeed = 6, Tumble = 120 });
            Add("reaparición", new BrawlPose { VerticalSpeed = -1, Spawn = .8f });
            Add("carga del super", Attack(AttackKind.Super, WindupT));
            Add("liberación del super", Attack(AttackKind.Super, StrikeT));
            return list;
        }

        /// <summary>Bone rotations plus the whole-body rotation.</summary>
        private static Quaternion[] Signature(BrawlFighterVisual visual)
        {
            var result = new Quaternion[BrawlAutoRig.BoneCount + 1];
            for (int b = 0; b < BrawlAutoRig.BoneCount; b++) result[b] = visual.BoneRotation(b);
            result[BrawlAutoRig.BoneCount] = visual.transform.localRotation;
            return result;
        }

        private static float Distance(Quaternion[] a, Quaternion[] b)
        {
            float max = 0;
            for (int i = 0; i < a.Length; i++) max = Mathf.Max(max, Quaternion.Angle(a[i], b[i]));
            return max;
        }

        private static void Pose(BrawlFighterVisual visual, State state)
        {
            visual.SetGait(state.Gait);
            if (state.Land) visual.Animate(new BrawlPose { VerticalSpeed = -8 }, true);
            visual.Animate(state.Pose, true);
        }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            match.AutomatedTest = true;
            var report = new Report();
            var art = match.Settings.characterArt;

            // ── Static: poses for the five kits, contact sheet.
            int columns = States(default).Count;
            var sheet = new Texture2D(Cell * columns, CellHeight * BrawlRoster.Count, TextureFormat.RGBA32, false);
            var texture = new RenderTexture(Cell, CellHeight, 16, RenderTextureFormat.ARGB32); texture.Create();
            bool allModels = true, allPosed = true, allDistinct = true, allAnticipate = true, landSquash = true;
            var pairs = new[] { ("carrera", "frenado"), ("salto", "segundo salto"), ("hitstun", "lanzamiento"), ("golpe neutral", "aéreo neutral"),
                ("salto", "caída"), ("caída", "recuperación"), ("carga del super", "liberación del super"), ("reposo", "giro") };
            for (int c = 0; c < BrawlRoster.Count; c++)
            {
                BrawlKit kit = BrawlRoster.Get(c);
                var root = new GameObject("Animación perfil"); root.transform.position = new Vector3(0, -600, 0);
                root.transform.rotation = Quaternion.LookRotation(Vector3.right);
                var holder = new GameObject("Modelo"); holder.transform.SetParent(root.transform, false);
                var visual = holder.AddComponent<BrawlFighterVisual>();
                visual.Build(c, 0, kit.Accent, art);
                allModels &= visual.UsesModel;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) { renderer.gameObject.layer = Layer; if (renderer is SkinnedMeshRenderer s) s.updateWhenOffscreen = true; }
                var cameraObject = new GameObject("Cámara perfil"); var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 1.35f;
                camera.transform.position = root.transform.position + new Vector3(0, 1, -6);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Backdrop;
                camera.cullingMask = 1 << Layer; camera.targetTexture = texture; camera.enabled = false;

                var states = States(kit.Special);
                var signatures = new Dictionary<string, Quaternion[]>();
                for (int i = 0; i < states.Count; i++)
                {
                    Pose(visual, states[i]);
                    signatures[states[i].Name] = Signature(visual);
                    if (states[i].Land) landSquash &= visual.ModelHeightScale < .95f;
                    yield return null; // let the skinned mesh update before the shot
                    camera.Render();
                    var previous = RenderTexture.active; RenderTexture.active = texture;
                    sheet.ReadPixels(new Rect(0, 0, Cell, CellHeight), i * Cell, (BrawlRoster.Count - 1 - c) * CellHeight);
                    RenderTexture.active = previous;
                }
                Quaternion[] rest = signatures["reposo"];
                foreach (var state in states)
                {
                    if (state.Name == "reposo" || state.Land) continue;
                    float d = Distance(rest, signatures[state.Name]);
                    report.states.Add(new StateRow { character = kit.Name, state = state.Name, fromRest = d });
                    allPosed &= d >= 15;
                }
                foreach (var (a, b) in pairs) allDistinct &= Distance(signatures[a], signatures[b]) >= 15;

                // Anticipation: the windup pose reads differently from the strike.
                var attacks = new (string, BrawlPose, BrawlPose)[]
                {
                    ("golpe neutral", Attack(AttackKind.Basic, WindupT), Attack(AttackKind.Basic, StrikeT)),
                    ("golpe ascendente", Attack(AttackKind.Basic, WindupT, false, 1), Attack(AttackKind.Basic, StrikeT, false, 1)),
                    ("aéreo neutral", Attack(AttackKind.Basic, WindupT, true), Attack(AttackKind.Basic, StrikeT, true)),
                    ("remate descendente", Attack(AttackKind.Basic, WindupT, true, -1), Attack(AttackKind.Basic, StrikeT, true, -1)),
                    ("fuerte", Attack(AttackKind.Heavy, WindupT), Attack(AttackKind.Heavy, StrikeT)),
                    ("especial", Attack(AttackKind.Special, WindupT, false, 0, kit.Special), Attack(AttackKind.Special, StrikeT, false, 0, kit.Special)),
                    ("super", Attack(AttackKind.Super, WindupT), Attack(AttackKind.Super, StrikeT))
                };
                foreach (var (name, windup, strike) in attacks)
                {
                    visual.Animate(windup, true); var w = Signature(visual);
                    visual.Animate(strike, true); var s = Signature(visual);
                    float d = Distance(w, s);
                    report.attacks.Add(new AttackRow { character = kit.Name, attack = name, anticipation = d });
                    allAnticipate &= d >= 20;
                }
                UnityEngine.Object.Destroy(cameraObject); root.SetActive(false); UnityEngine.Object.Destroy(root);
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(output, "anim-profile-sheet.png"), sheet.EncodeToPNG());
            UnityEngine.Object.Destroy(sheet); texture.Release(); UnityEngine.Object.Destroy(texture);
            check(allModels, "Los cinco kits usan su modelo con rig (no el provisional)");
            check(allPosed, $"Los {columns - 2} estados de perfil se separan del reposo (≥15° en algún hueso) en los cinco kits");
            check(landSquash, "El aterrizaje aplasta el cuerpo (altura <95 %)");
            check(allDistinct, "Estados vecinos se distinguen: carrera/frenado, salto/segundo salto, hitstun/lanzamiento, golpe/aéreo, salto/caída, caída/recuperación, carga/liberación, reposo/giro");
            check(allAnticipate, "Cada ataque muestra su anticipación: la pose de carga difiere ≥20° del golpe (siete ataques, cinco kits)");
            yield return null;
            check(GameObject.Find("Animación perfil") == null, "Las poses de prueba no dejan objetos en la escena");

            // ── Live: real ticks in the training lab, P1 driven only by this test.
            match.SelectedCharacter = 0; match.ArenaIndex = 0; match.StartTraining();
            yield return new WaitForSeconds(.2f);
            var p1 = match.Fighters[0]; var dummy = match.Fighters[1];
            p1.MakeBotForTest();
            var view = p1.GetComponentInChildren<BrawlFighterVisual>();

            IEnumerator Steps(int count, BrawlCommand command)
            {
                for (int i = 0; i < count; i++) { p1.Submit(command); yield return new WaitForFixedUpdate(); }
            }

            // Turn: the body swings through the camera and settles facing the new side.
            dummy.ResetPractice(new Vector3(3, .05f, 0), 0); p1.ResetPractice(new Vector3(0, .05f, 0), 0);
            yield return Steps(4, new BrawlCommand { Move = Vector2.right });
            yield return Steps(10, default);
            report.turnMinForwardZ = 1;
            for (int i = 0; i < 14; i++)
            {
                p1.Submit(new BrawlCommand { Move = Vector2.left }); yield return new WaitForFixedUpdate();
                report.turnMinForwardZ = Mathf.Min(report.turnMinForwardZ, view.transform.forward.z);
                report.turnMaxYaw = Mathf.Max(report.turnMaxYaw, Mathf.Abs(view.BodyYaw));
            }
            yield return Steps(14, default);
            check(report.turnMaxYaw > 90 && report.turnMinForwardZ < -.6f && view.BodyYaw == 0 && Vector3.Dot(view.transform.forward, Vector3.left) > .99f,
                $"Giro de lado: el cuerpo pasa de cara a la cámara (forward.z {report.turnMinForwardZ:0.00}) y acaba mirando al nuevo lado");

            // Turning into an attack snaps instead: the body never points away from its own hitbox.
            p1.ResetPractice(new Vector3(0, .05f, 0), 0);
            yield return Steps(4, new BrawlCommand { Move = Vector2.right });
            yield return Steps(10, default);
            p1.Submit(new BrawlCommand { Move = Vector2.left, Basic = true }); yield return new WaitForFixedUpdate();
            report.turnCutByAttack = view.LastPose.Attacking && view.LastPose.Turn == 0;
            yield return Steps(30, default);
            check(report.turnCutByAttack, "Girar y atacar a la vez no hace el giro largo: el golpe sale ya mirando a su lado");

            // Braking: running then releasing plays the brake, which ends on its own.
            p1.ResetPractice(new Vector3(-4, .05f, 0), 0);
            yield return Steps(5, default);
            yield return Steps(24, new BrawlCommand { Move = Vector2.right });
            for (int i = 0; i < 4; i++) { p1.Submit(default); yield return new WaitForFixedUpdate(); report.brakeSeen |= view.LastPose.Braking > 0; }
            yield return Steps(20, default);
            check(report.brakeSeen && view.LastPose.Braking == 0, "Frenado al soltar la carrera, y se apaga solo");

            // Double jump: a full forward flip that ends upright on landing.
            p1.ResetPractice(new Vector3(0, .05f, 0), 0);
            yield return Steps(5, default);
            p1.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate();
            yield return Steps(12, default);
            p1.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate();
            for (int i = 0; i < 30; i++) { yield return new WaitForFixedUpdate(); report.flipMaxPitch = Mathf.Max(report.flipMaxPitch, Mathf.Abs(view.BodyPitch)); }
            float until = Time.time + 3; while (!p1.Grounded && Time.time < until) yield return new WaitForFixedUpdate();
            yield return Steps(3, default);
            check(report.flipMaxPitch > 150 && p1.Grounded && view.BodyPitch == 0 && Quaternion.Angle(view.transform.localRotation, Quaternion.identity) < 1,
                $"Segundo salto con voltereta completa (pasa por {report.flipMaxPitch:0}°) y cae derecho");

            // A hit cuts the flip at once.
            p1.ResetPractice(new Vector3(0, .05f, 0), 0);
            yield return Steps(5, default);
            p1.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate();
            yield return Steps(12, default);
            p1.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate();
            yield return Steps(4, default);
            bool flipping = view.LastPose.Flip > 0;
            p1.ReceiveHit(new AttackSpec(AttackKind.Basic, 2, 0, .1f, .1f, 2, 1, 1, 1), Vector3.right, dummy);
            yield return Steps(4, default); // past the 35 ms hit-stop, which freezes the pose
            report.flipCutByHit = flipping && view.LastPose.Flip == 0 && view.LastPose.Hurt;
            check(report.flipCutByHit, "Un golpe corta la voltereta al instante");
            until = Time.time + 3; while (!p1.Grounded && Time.time < until) yield return new WaitForFixedUpdate();

            // A light hit flinches; a strong one tumbles and still lands upright.
            dummy.ResetPractice(new Vector3(-2, .05f, 0), 0);
            yield return new WaitForFixedUpdate();
            dummy.ReceiveHit(new AttackSpec(AttackKind.Basic, 2, 0, .1f, .1f, 2, 1, 1, 1), Vector3.right, p1);
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            var dummyView = dummy.GetComponentInChildren<BrawlFighterVisual>();
            report.lightHitNoTumble = dummyView.LastPose.Hurt && dummyView.LastPose.Tumble == 0;
            check(report.lightHitNoTumble, "Un golpe flojo es hitstun (encogido), sin voltereta");
            yield return new WaitForSeconds(.6f);
            dummy.ResetPractice(new Vector3(-3, .05f, 0), 60);
            yield return new WaitForFixedUpdate();
            dummy.ReceiveHit(new AttackSpec(AttackKind.Heavy, 14, 0, .1f, .1f, 2, 16, 9, 1), new Vector3(1, .4f, 0).normalized, p1);
            until = Time.time + 4;
            while (Time.time < until && !dummy.IsRespawning)
            {
                yield return new WaitForFixedUpdate();
                report.tumbleMax = Mathf.Max(report.tumbleMax, dummyView.LastPose.Tumble);
                if (report.tumbleMax > 0 && dummy.Grounded && dummyView.LastPose.Tumble == 0) break;
            }
            yield return new WaitForSeconds(.2f);
            check(report.tumbleMax > 90 && !dummy.IsRespawning && dummyView.BodyPitch == 0 && Quaternion.Angle(dummyView.transform.localRotation, Quaternion.identity) < 1,
                $"Un golpe fuerte lanza dando vueltas ({report.tumbleMax:0}°) y el luchador termina derecho");

            // Respawn entry: grows into place and settles at full size.
            p1.ResetPractice(new Vector3(0, .05f, 0), 0);
            yield return Steps(3, default);
            p1.LoseStock();
            until = Time.time + 5; while (p1.IsRespawning && Time.time < until) yield return new WaitForFixedUpdate();
            report.spawnMinScale = 1;
            for (int i = 0; i < 6; i++) { yield return new WaitForFixedUpdate(); report.spawnMinScale = Mathf.Min(report.spawnMinScale, view.transform.localScale.y); }
            bool entering = view.LastPose.Spawn > 0 || report.spawnMinScale < .95f;
            yield return new WaitForSeconds(.7f);
            check(entering && report.spawnMinScale < .95f && Mathf.Abs(view.transform.localScale.y - 1) < .001f,
                $"Reaparición: entra desde el {report.spawnMinScale * 100:0} % de tamaño y acaba en el 100 %");

            match.ReturnToMenu(); yield return null;
            File.WriteAllText(Path.Combine(output, "anim-smoke.json"), JsonUtility.ToJson(report, true));
        }
    }
}
