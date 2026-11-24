using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Fighter art: an auto-rigged imported model when available, primitive placeholder otherwise. Never participates in combat or physics.</summary>
    public sealed partial class BrawlFighterVisual : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Color> originalColors = new List<Color>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private Transform model;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform head;
        private int character;
        private float gait;

        private void BuildPrimitive(int characterIndex, Color teamColor)
        {
            ClearModel();
            character = Mathf.Clamp(characterIndex, 0, 4);
            model = Pivot("Model", transform, new Vector3(0f, 0.74f, 0f));
            if (character >= 2) { BuildExtended(teamColor); return; }

            Material team = MakeMaterial("Team", teamColor);
            Material dark = MakeMaterial("Ink", new Color(0.075f, 0.09f, 0.14f));
            Material skin = MakeMaterial("Skin", character == 0
                ? new Color(0.88f, 0.63f, 0.44f) : new Color(0.68f, 0.4f, 0.27f));
            Material white = MakeMaterial("White", new Color(0.92f, 0.95f, 0.97f));
            Material gold = MakeMaterial("Gold", new Color(1f, 0.69f, 0.22f));
            Material hair = MakeMaterial("Hair", character == 0
                ? new Color(0.2f, 0.15f, 0.16f) : new Color(0.91f, 0.23f, 0.68f));

            float shoulder = character == 0 ? 0.34f : 0.28f;
            Part("Torso", PrimitiveType.Cube, model, new Vector3(0f, 0.24f, 0f),
                character == 0 ? new Vector3(0.57f, 0.58f, 0.31f) : new Vector3(0.4f, 0.47f, 0.29f), team);
            Part("Waist", PrimitiveType.Cube, model, new Vector3(0f, -0.015f, 0f),
                new Vector3(character == 0 ? 0.42f : 0.35f, 0.19f, 0.27f), dark);

            head = Pivot("Head", model, new Vector3(0f, 0.67f, 0f));
            Part("Face", PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.4f, 0.42f, 0.36f), skin);
            Part("HairCap", PrimitiveType.Sphere, head, new Vector3(0f, 0.145f, -0.025f),
                new Vector3(0.41f, 0.18f, 0.37f), hair);
            Part("LeftEye", PrimitiveType.Cube, head, new Vector3(-0.085f, 0.02f, 0.17f),
                new Vector3(0.035f, 0.052f, 0.028f), dark);
            Part("RightEye", PrimitiveType.Cube, head, new Vector3(0.085f, 0.02f, 0.17f),
                new Vector3(0.035f, 0.052f, 0.028f), dark);
            Part("Smile", PrimitiveType.Cube, head, new Vector3(0f, -0.085f, 0.172f),
                new Vector3(0.105f, 0.023f, 0.025f), white);

            leftArm = Pivot("LeftArm", model, new Vector3(-shoulder, 0.42f, 0f));
            rightArm = Pivot("RightArm", model, new Vector3(shoulder, 0.42f, 0f));
            BuildArm(leftArm, team, skin);
            BuildArm(rightArm, team, skin);
            leftLeg = Pivot("LeftLeg", model, new Vector3(-0.12f, -0.055f, 0f));
            rightLeg = Pivot("RightLeg", model, new Vector3(0.12f, -0.055f, 0f));
            BuildLeg(leftLeg, dark, character == 0 ? dark : white, team);
            BuildLeg(rightLeg, dark, character == 0 ? dark : white, team);

            if (character == 0)
            {
                Part("Shirt", PrimitiveType.Cube, model, new Vector3(0f, 0.31f, 0.164f),
                    new Vector3(0.15f, 0.4f, 0.022f), white);
                Transform tie = Part("Tie", PrimitiveType.Cube, model, new Vector3(0f, 0.29f, 0.185f),
                    new Vector3(0.065f, 0.28f, 0.026f), gold);
                tie.localRotation = Quaternion.Euler(0f, 0f, -6f);
                Part("LapelLeft", PrimitiveType.Cube, model, new Vector3(-0.135f, 0.36f, 0.174f),
                    new Vector3(0.09f, 0.3f, 0.025f), dark).localRotation = Quaternion.Euler(0f, 0f, 19f);
                Part("LapelRight", PrimitiveType.Cube, model, new Vector3(0.135f, 0.36f, 0.174f),
                    new Vector3(0.09f, 0.3f, 0.025f), dark).localRotation = Quaternion.Euler(0f, 0f, -19f);
                Part("GavelHandle", PrimitiveType.Cylinder, rightArm, new Vector3(0f, -0.42f, 0.2f),
                    new Vector3(0.065f, 0.23f, 0.065f), dark).localRotation = Quaternion.Euler(90f, 0f, 0f);
                Part("GavelHead", PrimitiveType.Cube, rightArm, new Vector3(0f, -0.42f, 0.43f),
                    new Vector3(0.43f, 0.23f, 0.23f), gold);
                Part("Folder", PrimitiveType.Cube, leftArm, new Vector3(0f, -0.36f, 0.12f),
                    new Vector3(0.26f, 0.3f, 0.055f), white);
            }
            else
            {
                Part("HeadphonesBand", PrimitiveType.Cube, head, new Vector3(0f, 0.13f, 0f),
                    new Vector3(0.46f, 0.07f, 0.13f), team);
                Part("LeftHeadphone", PrimitiveType.Sphere, head, new Vector3(-0.21f, 0f, 0f),
                    new Vector3(0.1f, 0.17f, 0.16f), team);
                Part("RightHeadphone", PrimitiveType.Sphere, head, new Vector3(0.21f, 0f, 0f),
                    new Vector3(0.1f, 0.17f, 0.16f), team);
                Part("Ponytail", PrimitiveType.Sphere, head, new Vector3(0.12f, 0.035f, -0.22f),
                    new Vector3(0.23f, 0.37f, 0.24f), hair).localRotation = Quaternion.Euler(-28f, 0f, -16f);
                Part("JacketStripe", PrimitiveType.Cube, model, new Vector3(0f, 0.3f, 0.15f),
                    new Vector3(0.29f, 0.075f, 0.025f), white);
                Part("Phone", PrimitiveType.Cube, rightArm, new Vector3(0f, -0.33f, 0.11f),
                    new Vector3(0.14f, 0.24f, 0.055f), dark);
                Part("PhoneScreen", PrimitiveType.Cube, rightArm, new Vector3(0f, -0.33f, 0.143f),
                    new Vector3(0.11f, 0.18f, 0.012f), white);
                Transform ring = Pivot("LightRing", leftArm, new Vector3(0f, -0.34f, 0.16f));
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * Mathf.PI / 6f;
                    Part("RingSegment", PrimitiveType.Cube, ring,
                        new Vector3(Mathf.Cos(angle) * 0.15f, Mathf.Sin(angle) * 0.15f, 0f),
                        new Vector3(0.075f, 0.047f, 0.045f), white).localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + 90f);
                }
            }
        }

        /// <summary>Coach Tóxico (2), Cripto King (3) and Tío Antena (4): distinct silhouettes before any detail.</summary>
        private void BuildExtended(Color teamColor)
        {
            Material team = MakeMaterial("Team", teamColor);
            Material dark = MakeMaterial("Ink", new Color(0.075f, 0.09f, 0.14f));
            Material white = MakeMaterial("White", new Color(0.92f, 0.95f, 0.97f));
            Material gold = MakeMaterial("Gold", new Color(1f, 0.74f, 0.2f));
            Material metal = MakeMaterial("Metal", new Color(.62f, .66f, .72f));
            Material red = MakeMaterial("Signal", new Color(1f, .25f, .22f));
            Material skin = MakeMaterial("Skin", character == 2 ? new Color(.83f, .56f, .4f) : character == 3 ? new Color(.95f, .76f, .6f) : new Color(.6f, .42f, .32f));
            Material outfit = character == 4 ? MakeMaterial("Trench", new Color(.66f, .58f, .42f)) : team;

            float shoulder = character == 2 ? 0.43f : character == 3 ? 0.31f : 0.3f;
            Vector3 torso = character == 2 ? new Vector3(0.74f, 0.6f, 0.38f) : character == 3 ? new Vector3(0.5f, 0.52f, 0.32f) : new Vector3(0.46f, 0.6f, 0.32f);
            Part("Torso", PrimitiveType.Cube, model, new Vector3(0f, 0.24f, 0f), torso, outfit);
            Part("Waist", PrimitiveType.Cube, model, new Vector3(0f, -0.015f, 0f), new Vector3(torso.x * .72f, 0.19f, 0.28f), dark);

            head = Pivot("Head", model, new Vector3(0f, character == 2 ? 0.63f : 0.67f, 0f));
            Part("Face", PrimitiveType.Sphere, head, Vector3.zero, character == 2 ? new Vector3(0.36f, 0.38f, 0.34f) : new Vector3(0.4f, 0.42f, 0.36f), skin);
            Part("LeftEye", PrimitiveType.Cube, head, new Vector3(-0.085f, 0.02f, 0.17f), new Vector3(0.035f, 0.052f, 0.028f), dark);
            Part("RightEye", PrimitiveType.Cube, head, new Vector3(0.085f, 0.02f, 0.17f), new Vector3(0.035f, 0.052f, 0.028f), dark);
            Part("Mouth", PrimitiveType.Cube, head, new Vector3(0f, -0.085f, 0.172f), new Vector3(0.12f, 0.023f, 0.025f), character == 2 ? red : white);

            leftArm = Pivot("LeftArm", model, new Vector3(-shoulder, 0.42f, 0f));
            rightArm = Pivot("RightArm", model, new Vector3(shoulder, 0.42f, 0f));
            BuildArm(leftArm, character == 2 ? skin : outfit, skin);
            BuildArm(rightArm, character == 2 ? skin : outfit, skin);
            leftLeg = Pivot("LeftLeg", model, new Vector3(-0.13f, -0.055f, 0f));
            rightLeg = Pivot("RightLeg", model, new Vector3(0.13f, -0.055f, 0f));
            BuildLeg(leftLeg, character == 2 ? team : dark, character == 3 ? gold : white, team);
            BuildLeg(rightLeg, character == 2 ? team : dark, character == 3 ? gold : white, team);

            if (character == 2)
            {
                // Coach Tóxico: oversized torso, headband, whistle and absurd dumbbells.
                Part("Headband", PrimitiveType.Cube, head, new Vector3(0f, 0.1f, 0f), new Vector3(0.39f, 0.07f, 0.37f), white);
                Part("Whistle", PrimitiveType.Cube, model, new Vector3(0.08f, 0.2f, 0.2f), new Vector3(0.07f, 0.05f, 0.08f), gold);
                Part("TankStripe", PrimitiveType.Cube, model, new Vector3(0f, 0.3f, 0.192f), new Vector3(0.5f, 0.06f, 0.02f), white);
                foreach (Transform arm in new[] { leftArm, rightArm })
                {
                    Part("BarbellBar", PrimitiveType.Cylinder, arm, new Vector3(0f, -0.36f, 0.05f), new Vector3(0.05f, 0.24f, 0.05f), metal)
                        .localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Part("WeightA", PrimitiveType.Sphere, arm, new Vector3(-0.24f, -0.36f, 0.05f), Vector3.one * 0.24f, dark);
                    Part("WeightB", PrimitiveType.Sphere, arm, new Vector3(0.24f, -0.36f, 0.05f), Vector3.one * 0.24f, dark);
                }
            }
            else if (character == 3)
            {
                // Cripto King: slick hair, sunglasses, gold chain, briefcase and coin.
                Part("SlickHair", PrimitiveType.Sphere, head, new Vector3(0f, 0.15f, -0.04f), new Vector3(0.42f, 0.16f, 0.4f), gold);
                Part("Sunglasses", PrimitiveType.Cube, head, new Vector3(0f, 0.03f, 0.18f), new Vector3(0.32f, 0.07f, 0.03f), dark);
                for (int i = 0; i < 7; i++)
                {
                    float a = Mathf.Lerp(-1.1f, 1.1f, i / 6f);
                    Part("ChainLink", PrimitiveType.Cube, model, new Vector3(Mathf.Sin(a) * 0.19f, 0.43f - Mathf.Cos(a) * 0.17f, 0.17f),
                        new Vector3(0.05f, 0.05f, 0.03f), gold);
                }
                Part("Medallion", PrimitiveType.Cylinder, model, new Vector3(0f, 0.22f, 0.18f), new Vector3(0.13f, 0.012f, 0.13f), gold)
                    .localRotation = Quaternion.Euler(90f, 0f, 0f);
                Part("Briefcase", PrimitiveType.Cube, leftArm, new Vector3(0f, -0.45f, 0.06f), new Vector3(0.36f, 0.26f, 0.1f), dark);
                Part("BriefcaseLock", PrimitiveType.Cube, leftArm, new Vector3(0f, -0.38f, 0.115f), new Vector3(0.08f, 0.04f, 0.02f), gold);
                Part("Coin", PrimitiveType.Cylinder, rightArm, new Vector3(0f, -0.36f, 0.16f), new Vector3(0.17f, 0.015f, 0.17f), gold)
                    .localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                // Tío Antena: long trench coat, metal helmet with antennas, handheld rod.
                Part("CoatTail", PrimitiveType.Cube, model, new Vector3(0f, -0.2f, -0.02f), new Vector3(0.5f, 0.42f, 0.34f), outfit);
                Part("CoatPocket", PrimitiveType.Cube, model, new Vector3(-0.15f, -0.12f, 0.155f), new Vector3(0.13f, 0.11f, 0.03f), dark);
                Part("CoatPocket", PrimitiveType.Cube, model, new Vector3(0.15f, -0.12f, 0.155f), new Vector3(0.13f, 0.11f, 0.03f), dark);
                Part("Helmet", PrimitiveType.Sphere, head, new Vector3(0f, 0.09f, -0.01f), new Vector3(0.44f, 0.3f, 0.41f), metal);
                foreach (int side in new[] { -1, 1 })
                {
                    Part("Antenna", PrimitiveType.Cylinder, head, new Vector3(side * 0.13f, 0.38f, 0f), new Vector3(0.025f, 0.2f, 0.025f), metal)
                        .localRotation = Quaternion.Euler(0f, 0f, side * -18f);
                    Part("AntennaTip", PrimitiveType.Sphere, head, new Vector3(side * 0.2f, 0.58f, 0f), Vector3.one * 0.08f, red);
                }
                Part("Rod", PrimitiveType.Cylinder, rightArm, new Vector3(0f, -0.4f, 0.3f), new Vector3(0.035f, 0.3f, 0.035f), metal)
                    .localRotation = Quaternion.Euler(90f, 0f, 0f);
                Part("RodTip", PrimitiveType.Sphere, rightArm, new Vector3(0f, -0.4f, 0.62f), Vector3.one * 0.09f, team);
            }
        }

        private void AnimatePrimitive(Vector3 horizontalVelocity, bool grounded, float attack01, bool hurt, bool protectedSpawn)
        {
            if (model == null) return;
            float speed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
            float stride = Mathf.Clamp01(speed / 5f);
            gait += Time.deltaTime * Mathf.Lerp(4f, 13f, stride);
            float swing = grounded ? Mathf.Sin(gait) * 27f * stride : 0f;
            float attack = attack01 > 0f ? Mathf.Sin(Mathf.Clamp01(attack01) * Mathf.PI) : 0f;
            float bob = grounded ? Mathf.Abs(Mathf.Sin(gait)) * 0.025f * stride : 0.015f;
            model.localPosition = new Vector3(0f, 0.74f + bob, 0f);
            model.localScale = grounded ? Vector3.one : new Vector3(0.97f, 1.035f, 0.97f);
            model.localRotation = Quaternion.Euler(hurt ? -12f : stride * 7f + attack * 7f,
                attack * (character == 0 ? -16f : 24f), 0f);
            leftLeg.localRotation = Quaternion.Euler(grounded ? swing : -17f, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(grounded ? -swing : 19f, 0f, 0f);
            leftArm.localRotation = Quaternion.Euler(-swing * 0.65f - attack * 52f, 0f, -8f);
            rightArm.localRotation = Quaternion.Euler(swing * 0.65f - attack * 115f, attack * -10f, 8f);
            head.localRotation = Quaternion.Euler(0f, attack * -12f, grounded ? Mathf.Sin(Time.time * 1.8f) * 2f : 0f);

            // Reduced effects keep the protection visible as a steady tint instead of blinking.
            float protectionTint = !protectedSpawn ? 0f : BrawlFeedback.ReducedEffects ? 0.24f : 0.15f + (Mathf.Sin(Time.time * 5f) + 1f) * 0.09f;
            for (int i = 0; i < materials.Count; i++)
            {
                Color color = Color.Lerp(originalColors[i], Color.white, protectionTint);
                if (hurt) color = Color.Lerp(color, new Color(1f, 0.38f, 0.34f), 0.4f);
                materials[i].color = color;
            }
        }

        private void BuildArm(Transform parent, Material sleeve, Material skin)
        {
            Part("Sleeve", PrimitiveType.Cube, parent, new Vector3(0f, -0.15f, 0f),
                new Vector3(0.17f, 0.33f, 0.2f), sleeve);
            Part("Hand", PrimitiveType.Sphere, parent, new Vector3(0f, -0.34f, 0.015f),
                new Vector3(0.18f, 0.17f, 0.19f), skin);
        }

        private void BuildLeg(Transform parent, Material trousers, Material shoe, Material accent)
        {
            Part("Trouser", PrimitiveType.Cube, parent, new Vector3(0f, -0.215f, 0f),
                new Vector3(0.18f, 0.43f, 0.2f), trousers);
            Part("Shoe", PrimitiveType.Cube, parent, new Vector3(0f, -0.575f, 0.055f),
                new Vector3(0.215f, 0.2f, 0.33f), shoe);
            if (character == 1)
                Part("ShoeStripe", PrimitiveType.Cube, parent, new Vector3(0f, -0.57f, 0.225f),
                    new Vector3(0.17f, 0.065f, 0.012f), accent);
        }

        public const string ToonShaderName = "FUNADOS/Toon";
        private static Shader toon;
        /// <summary>The fighters' toon shader (outline included), or null when the build lacks it.</summary>
        public static Shader ToonShader
        {
            get { if (toon == null) { toon = Shader.Find(ToonShaderName); if (toon != null && !toon.isSupported) toon = null; } return toon; }
        }

        private Material MakeMaterial(string label, Color color)
        {
            Shader shader = ToonShader;
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            Material material = new Material(shader) { name = "Beta " + label, color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
            materials.Add(material);
            originalColors.Add(color);
            return material;
        }

        private static Transform Pivot(string label, Transform parent, Vector3 position)
        {
            Transform pivot = new GameObject(label).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            return pivot;
        }

        private Transform Part(string label, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                DisposeObject(collider);
            }
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderers.Add(renderer);
            return part.transform;
        }

        private void ClearModel()
        {
            if (model != null)
            {
                model.gameObject.SetActive(false);
                DisposeObject(model.gameObject);
                model = null;
            }
            foreach (Material material in materials)
                if (material != null) DisposeObject(material);
            materials.Clear();
            originalColors.Clear();
            renderers.Clear();
            gait = 0f;
            ClearRig();
        }

        private static void DisposeObject(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void OnDestroy() => ClearModel();
    }
}
