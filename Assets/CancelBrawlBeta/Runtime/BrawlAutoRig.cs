using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Turns a static, A-pose humanoid mesh (Meshy export) into a skinned mesh with an 11-bone skeleton.
    /// Joints are estimated from the silhouette and weights from distance to each limb, so any similar
    /// model works without manual rigging. Results are cached per model; the source mesh needs Read/Write.
    /// </summary>
    public static class BrawlAutoRig
    {
        public const int Hips = 0, Chest = 1, Head = 2, UpperArmL = 3, ForearmL = 4, UpperArmR = 5, ForearmR = 6,
            ThighL = 7, ShinL = 8, ThighR = 9, ShinR = 10, BoneCount = 11;
        public static readonly int[] Parents = { -1, Hips, Chest, Chest, UpperArmL, Chest, UpperArmR, Hips, ThighL, Hips, ThighR };
        public static readonly string[] BoneNames = { "Hips", "Chest", "Head", "UpperArm.L", "Forearm.L", "UpperArm.R", "Forearm.R", "Thigh.L", "Shin.L", "Thigh.R", "Shin.R" };

        /// <summary>Character height in metres after normalisation; feet rest on y = 0, facing +Z.</summary>
        public const float TargetHeight = 1.78f;

        public sealed class Rig
        {
            public Mesh Mesh;
            /// <summary>Joint positions in rig space (feet at origin).</summary>
            public Vector3[] Joints;
            public Bounds Bounds;
        }

        private static readonly Dictionary<GameObject, Rig> Cache = new Dictionary<GameObject, Rig>();

        public static Rig Get(GameObject model)
        {
            if (model == null) return null;
            if (Cache.TryGetValue(model, out Rig cached) && cached.Mesh != null) return cached;
            Rig rig = Build(model);
            if (rig != null) Cache[model] = rig;
            return rig;
        }

        private static Rig Build(GameObject model)
        {
            // Bake every mesh of the prefab into one model-space vertex list.
            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh source = filter.sharedMesh;
                if (source == null) continue;
                if (!source.isReadable) { Debug.LogWarning("BrawlAutoRig: activa Read/Write en " + model.name); return null; }
                // Full import transform (axis conversion, unit scale) without the prefab's own position.
                Matrix4x4 m = Matrix4x4.Translate(-model.transform.position) * filter.transform.localToWorldMatrix;
                int offset = verts.Count;
                Vector3[] v = source.vertices; Vector3[] n = source.normals; Vector2[] uv = source.uv;
                for (int i = 0; i < v.Length; i++)
                {
                    verts.Add(m.MultiplyPoint3x4(v[i]));
                    normals.Add(n.Length == v.Length ? m.MultiplyVector(n[i]).normalized : Vector3.up);
                    uvs.Add(uv.Length == v.Length ? uv[i] : Vector2.zero);
                }
                for (int s = 0; s < source.subMeshCount; s++)
                    foreach (int t in source.GetTriangles(s)) tris.Add(t + offset);
            }
            if (verts.Count == 0) return null;

            // Normalise: feet on the floor, centred, fixed height.
            Vector3 min = verts[0], max = verts[0];
            foreach (Vector3 p in verts) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            float scale = TargetHeight / Mathf.Max(.0001f, max.y - min.y);
            Vector3 shift = new Vector3(-(min.x + max.x) * .5f, -min.y, -(min.z + max.z) * .5f);
            for (int i = 0; i < verts.Count; i++) verts[i] = (verts[i] + shift) * scale;
            float H = TargetHeight;

            Vector3[] joints = EstimateJoints(verts, H, out float armReach);
            var weights = new BoneWeight[verts.Count];
            var w = new float[BoneCount];
            for (int i = 0; i < verts.Count; i++)
            {
                Weigh(verts[i], joints, H, armReach, w);
                weights[i] = Pack(w);
            }

            var mesh = new Mesh { name = model.name + " (auto-rig)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.boneWeights = weights;
            var bind = new Matrix4x4[BoneCount];
            for (int b = 0; b < BoneCount; b++) bind[b] = Matrix4x4.Translate(-joints[b]);
            mesh.bindposes = bind;
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            mesh.UploadMeshData(false);
            return new Rig { Mesh = mesh, Joints = joints, Bounds = mesh.bounds };
        }

        /// <summary>Joint layout from the silhouette: hands are the outermost mass at mid height, the crotch is where the leg gap closes.</summary>
        private static Vector3[] EstimateJoints(List<Vector3> verts, float H, out float armReach)
        {
            // Hands: outermost vertices per side between 30 % and 55 % of the height.
            float maxL = 0, maxR = 0;
            foreach (Vector3 p in verts)
            {
                float h = p.y / H;
                if (h < .3f || h > .55f) continue;
                if (p.x < 0) maxL = Mathf.Max(maxL, -p.x); else maxR = Mathf.Max(maxR, p.x);
            }
            Vector3 handL = Vector3.zero, handR = Vector3.zero; int nL = 0, nR = 0;
            float outerShoulder = 0;
            foreach (Vector3 p in verts)
            {
                float h = p.y / H;
                if (h >= .64f && h <= .68f) outerShoulder = Mathf.Max(outerShoulder, Mathf.Abs(p.x));
                if (h < .3f || h > .55f) continue;
                if (p.x < -maxL * .8f) { handL += p; nL++; }
                else if (p.x > maxR * .8f) { handR += p; nR++; }
            }
            handL = nL > 0 ? handL / nL : new Vector3(-.3f * H, .45f * H, 0);
            handR = nR > 0 ? handR / nR : new Vector3(.3f * H, .45f * H, 0);

            float shoulderY = .7f * H;
            float shoulderX = Mathf.Lerp((Mathf.Abs(handL.x) + Mathf.Abs(handR.x)) * .5f * .62f, outerShoulder * .78f, .5f);
            Vector3 shoulderL = new Vector3(-shoulderX, shoulderY, Mathf.Lerp(handL.z, 0, .5f));
            Vector3 shoulderR = new Vector3(shoulderX, shoulderY, Mathf.Lerp(handR.z, 0, .5f));
            // Wrist sits a bit above the hand's centre of mass.
            Vector3 wristL = Vector3.Lerp(handL, shoulderL, .08f), wristR = Vector3.Lerp(handR, shoulderR, .08f);
            Vector3 elbowL = Vector3.Lerp(shoulderL, wristL, .5f), elbowR = Vector3.Lerp(shoulderR, wristR, .5f);
            armReach = (Vector3.Distance(shoulderL, handL) + Vector3.Distance(shoulderR, handR)) * .5f;

            // Crotch: lowest height where non-arm geometry crosses the centre line.
            float crotch = .45f;
            float centre = .025f * H;
            const int Bands = 60;
            var occupied = new bool[Bands];
            float legX = 0; int legN = 0;
            foreach (Vector3 p in verts)
            {
                float h = p.y / H;
                if (h > .2f && h < .62f && Mathf.Abs(p.x) < centre && !NearArm(p, shoulderL, wristL, shoulderR, wristR, H))
                    occupied[Mathf.Clamp((int)((h - .2f) / .42f * Bands), 0, Bands - 1)] = true;
                if (h > .12f && h < .24f) { legX += Mathf.Abs(p.x); legN++; }
            }
            for (int b = 0; b < Bands; b++)
                if (occupied[b] && (b + 1 >= Bands || occupied[b + 1])) { crotch = .2f + (b + .5f) / Bands * .42f; break; }
            crotch = Mathf.Clamp(crotch, .36f, .55f);
            legX = legN > 0 ? Mathf.Clamp(legX / legN, .04f * H, .12f * H) : .07f * H;

            var j = new Vector3[BoneCount];
            j[Hips] = new Vector3(0, (crotch + .05f) * H, 0);
            j[Chest] = new Vector3(0, .6f * H, 0);
            j[Head] = new Vector3(0, .755f * H, 0);
            j[UpperArmL] = shoulderL; j[ForearmL] = elbowL;
            j[UpperArmR] = shoulderR; j[ForearmR] = elbowR;
            j[ThighL] = new Vector3(-legX, (crotch + .02f) * H, 0);
            j[ThighR] = new Vector3(legX, (crotch + .02f) * H, 0);
            j[ShinL] = new Vector3(-legX, crotch * .56f * H, 0);
            j[ShinR] = new Vector3(legX, crotch * .56f * H, 0);
            return j;
        }

        private static bool NearArm(Vector3 p, Vector3 sL, Vector3 wL, Vector3 sR, Vector3 wR, float H)
        {
            Vector3 s = p.x < 0 ? sL : sR, wr = p.x < 0 ? wL : wR;
            return SegmentDistance(p, s, wr, out _) < .05f * H;
        }

        private static void Weigh(Vector3 p, Vector3[] j, float H, float armReach, float[] w)
        {
            System.Array.Clear(w, 0, w.Length);
            bool left = p.x < 0;
            Vector3 shoulder = j[left ? UpperArmL : UpperArmR];
            Vector3 elbow = j[left ? ForearmL : ForearmR];
            Vector3 wrist = shoulder + (elbow - shoulder) * 2f;
            float h = p.y / H;

            // ── Arms: closeness to the shoulder→wrist line; the outside of the line is always arm.
            float arm = 0, along = 0;
            if (p.y < shoulder.y + .045f * H && p.y > wrist.y - .24f * H)
            {
                float d = SegmentDistance(p, shoulder, wrist, out along);
                bool outside = Mathf.Abs(p.x) > Mathf.Abs(Mathf.Lerp(shoulder.x, wrist.x, Mathf.Clamp01(along)));
                float radius = Mathf.Lerp(.06f, .04f, Mathf.Clamp01(along)) * H;
                arm = outside ? 1 - Smooth(radius * 1.4f, radius * 2.4f, d) : 1 - Smooth(radius * .75f, radius * 1.25f, d);
                arm *= Smooth(-.08f, .16f, along); // blends into the chest at the shoulder
                // Hand and fingers: a ball past the wrist, only on the outer side of the hand.
                Vector3 hand = wrist + (wrist - elbow).normalized * .06f * H;
                float handDistance = Vector3.Distance(p, hand);
                if (along > .7f && Mathf.Abs(p.x) > Mathf.Abs(hand.x) - .075f * H)
                {
                    float handWeight = 1 - Smooth(.1f * H, .125f * H, handDistance);
                    if (handWeight > arm) { arm = handWeight; along = 1; }
                }
                // Fingertips hanging below the wrist and outside it can only be hand.
                if (p.y < wrist.y && Mathf.Abs(p.x) > Mathf.Abs(wrist.x) - .03f * H && Mathf.Abs(p.z - wrist.z) < .14f * H)
                { arm = 1; along = 1; }
            }
            if (arm > 0)
            {
                float fore = Smooth(.4f, .6f, along);
                w[left ? UpperArmL : UpperArmR] += arm * (1 - fore);
                w[left ? ForearmL : ForearmR] += arm * fore;
            }
            float body = 1 - arm;
            if (body <= 0) return;

            // ── Legs below the crotch, split by side with a soft centre seam.
            float hipY = j[Hips].y - .05f * H;
            float leg = 1 - Smooth(hipY - .01f * H, hipY + .07f * H, p.y);
            if (leg > 0)
            {
                float sideL = 1 - Smooth(-.015f * H, .015f * H, p.x);
                float shin = 1 - Smooth(j[ShinL].y - .035f * H, j[ShinL].y + .035f * H, p.y);
                float share = body * leg;
                w[ThighL] += share * sideL * (1 - shin); w[ShinL] += share * sideL * shin;
                w[ThighR] += share * (1 - sideL) * (1 - shin); w[ShinR] += share * (1 - sideL) * shin;
            }
            float trunk = body * (1 - leg);
            if (trunk <= 0) return;

            // ── Trunk: hips → chest → head.
            float head = Smooth(.735f, .78f, h);
            float chest = Smooth(hipY / H + .06f, .6f, h);
            w[Head] += trunk * head;
            w[Chest] += trunk * (1 - head) * chest;
            w[Hips] += trunk * (1 - head) * (1 - chest);
        }

        private static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, x));

        private static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b, out float t)
        {
            Vector3 ab = b - a;
            t = Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude);
            return Vector3.Distance(p, a + ab * Mathf.Clamp01(t));
        }

        private static BoneWeight Pack(float[] w)
        {
            // Keep the four strongest influences.
            int a = -1, b = -1, c = -1, d = -1;
            for (int i = 0; i < w.Length; i++)
            {
                if (a < 0 || w[i] > w[a]) { d = c; c = b; b = a; a = i; }
                else if (b < 0 || w[i] > w[b]) { d = c; c = b; b = i; }
                else if (c < 0 || w[i] > w[c]) { d = c; c = i; }
                else if (d < 0 || w[i] > w[d]) d = i;
            }
            float wa = w[a], wb = b >= 0 ? w[b] : 0, wc = c >= 0 ? w[c] : 0, wd = d >= 0 ? w[d] : 0;
            float sum = wa + wb + wc + wd;
            if (sum <= 1e-5f) return new BoneWeight { boneIndex0 = Chest, weight0 = 1 };
            return new BoneWeight
            {
                boneIndex0 = a, weight0 = wa / sum, boneIndex1 = Mathf.Max(0, b), weight1 = wb / sum,
                boneIndex2 = Mathf.Max(0, c), weight2 = wc / sum, boneIndex3 = Mathf.Max(0, d), weight3 = wd / sum
            };
        }
    }
}
