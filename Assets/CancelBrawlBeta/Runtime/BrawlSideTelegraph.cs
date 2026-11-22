using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>XY warning geometry. Caster-bound cues follow position, not the fighter's rotation.</summary>
    public sealed class BrawlSideTelegraph : MonoBehaviour
    {
        public float Remaining => Mathf.Max(0, duration - elapsed);
        private BrawlMatch match;
        private BrawlFighter caster;
        private Transform anchor;
        private float duration, elapsed, initialAttackTime;
        private bool grow;
        private float refugeLabelHeight;
        private GUIStyle refugeLabel;
        private readonly Color danger = new Color(1, .25f, .2f);

        private static BrawlSideTelegraph Create(BrawlMatch owner, Transform anchor, float seconds, string name, BrawlFighter caster = null)
        {
            var obj = new GameObject(name); obj.transform.SetParent(anchor, false);
            var cue = obj.AddComponent<BrawlSideTelegraph>();
            cue.match = owner; cue.anchor = anchor; cue.caster = caster; cue.duration = Mathf.Max(.01f, seconds);
            if (caster) cue.initialAttackTime = caster.AttackProgress * caster.CurrentAttackSpec.Duration;
            cue.Align(); return cue;
        }

        public static BrawlSideTelegraph Caster(BrawlMatch match, BrawlFighter caster, AttackSpec spec)
        {
            var cue = Create(match, caster.transform, spec.Windup + spec.Active, "Aviso lateral · " + caster.Kit.Super, caster);
            float range = spec.Range + (caster.Kit.Super == SuperStyle.GroundWave ? .4f : .3f);
            if (caster.Kit.Super == SuperStyle.SafeSweep)
            {
                float safe = BrawlFighter.SideSafeRadius;
                cue.Rectangle(-range, -safe, -spec.Height, spec.Height, cue.danger, "Peligro izquierdo");
                cue.Rectangle(safe, range, -spec.Height, spec.Height, cue.danger, "Peligro derecho");
                cue.Rectangle(-safe, safe, -spec.Height, spec.Height, Color.white, "Refugio central");
                cue.refugeLabelHeight=spec.Height+.9f;
            }
            else if (caster.Kit.Super == SuperStyle.Fan)
            {
                // The existing sweep includes a near-body margin even behind its facing cone.
                float near = Mathf.Sqrt(.2f);
                cue.Rectangle(caster.Facing.x > 0 ? -near : -range, caster.Facing.x > 0 ? range : near,
                    -spec.Height, spec.Height, cue.danger, "Alcance frontal");
            }
            else cue.Rectangle(-range, range, -spec.Height, spec.Height, cue.danger, "Alcance de onda");
            return cue;
        }

        public static BrawlSideTelegraph Zone(BrawlMatch match, Transform anchor, float radius, float height, float seconds, bool sphere)
        {
            var cue = Create(match, anchor, seconds, sphere ? "Burbuja lateral anunciada" : "Zona lateral anunciada");
            if (sphere)
            {
                var line = cue.Line("Radio de burbuja", cue.danger, 65);
                // Gameplay tests the target's chest, 0.85 m above its root.
                for (int i=0;i<=64;i++)
                {
                    float a=i*Mathf.PI*2/64;
                    line.SetPosition(i,new Vector3(Mathf.Cos(a)*(radius+.3f), Mathf.Sin(a)*(radius+.3f)-.85f,0));
                }
            }
            else cue.Rectangle(-radius-.3f,radius+.3f,-.6f,height,cue.danger,"Volumen de zona");
            return cue;
        }

        public static void Wave(BrawlMatch match, BrawlFighter caster, float radius, float height, float seconds)
        {
            var cue=Create(match,caster.transform,seconds,"Onda lateral activa",caster);
            cue.Rectangle(-radius-.4f,radius+.4f,-height,height,cue.danger,"Frente de onda"); cue.grow=true;
        }

        public static void Melee(BrawlMatch match, BrawlFighter caster, AttackSpec spec)
        {
            var cue=Create(match,caster.transform,spec.Windup+spec.Active,"Aviso de fuerte lateral",caster);
            float range=spec.Range+.3f, near=Mathf.Sqrt(.2f);
            if (caster.AttackVertical!=0)
                cue.Rectangle(-range,range,caster.AttackVertical>0 ? -.5f : -spec.Height,
                    caster.AttackVertical<0 && caster.AttackAirborne ? .5f : spec.Height,cue.danger,"Alcance de fuerte");
            else cue.Rectangle(caster.Facing.x>0 ? -near : -range,caster.Facing.x>0 ? range : near,
                -spec.Height,spec.Height,cue.danger,"Alcance de fuerte");
        }

        public static void Trajectory(BrawlMatch match, BrawlFighter caster, float length, float seconds)
        {
            var cue=Create(match,caster.transform,seconds,"Trayectoria lateral inicial",caster);
            float sign=Mathf.Sign(caster.Facing.x);
            var line=cue.Line("Dirección de token",cue.danger,5);
            line.SetPositions(new[] { new Vector3(sign*.65f,.85f,0),new Vector3(sign*length,.85f,0),
                new Vector3(sign*(length-.45f),1.1f,0),new Vector3(sign*length,.85f,0),new Vector3(sign*(length-.45f),.6f,0) });
        }

        private LineRenderer Line(string name, Color color, int count)
        {
            var obj=new GameObject(name); obj.transform.SetParent(transform,false);
            var line=obj.AddComponent<LineRenderer>(); line.sharedMaterial=BrawlFeedback.Material(color);
            line.useWorldSpace=false; line.widthMultiplier=.055f; line.positionCount=count;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; return line;
        }
        private void Rectangle(float left,float right,float bottom,float top,Color color,string name)
        {
            var line=Line(name,color,5);
            line.SetPositions(new[] { new Vector3(left,bottom,0),new Vector3(left,top,0),new Vector3(right,top,0),new Vector3(right,bottom,0),new Vector3(left,bottom,0) });
        }
        private void Align()
        {
            if (!anchor) return;
            transform.position=new Vector3(anchor.position.x,anchor.position.y,-.55f);
            transform.rotation=Quaternion.identity;
        }
        private void OnGUI()
        {
            if (refugeLabelHeight<=0 || !match || match.Phase!=MatchPhase.Playing || !Camera.main) return;
            Vector3 point=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*refugeLabelHeight);
            if (point.z<=0) return;
            if (refugeLabel==null) refugeLabel=new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold };
            float scale=Mathf.Clamp(Screen.height/720f,.8f,2f);
            refugeLabel.fontSize=Mathf.RoundToInt(15*scale); refugeLabel.normal.textColor=Color.white;
            Rect rect=new Rect(point.x-60*scale,Screen.height-point.y-12*scale,120*scale,24*scale);
            Matrix4x4 matrix=GUI.matrix; Color color=GUI.color; GUI.matrix=Matrix4x4.identity;
            GUI.color=new Color(.03f,.03f,.07f,.9f); GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=Color.white; GUI.Label(rect,"REFUGIO",refugeLabel);
            GUI.color=color; GUI.matrix=matrix;
        }
        private void LateUpdate()
        {
            if (!match || !anchor || (caster && (!caster.IsAttacking || caster.IsRespawning || caster.Eliminated))) { Destroy(gameObject); return; }
            if (match.Phase==MatchPhase.Paused) return;
            if (match.Phase!=MatchPhase.Playing) { Destroy(gameObject); return; }
            elapsed=caster ? caster.AttackProgress*caster.CurrentAttackSpec.Duration-initialAttackTime : elapsed+Time.deltaTime;
            Align();
            if (grow) transform.localScale=new Vector3(Mathf.Clamp01(elapsed/duration),1,1);
            if (elapsed>=duration) Destroy(gameObject);
        }
    }
}
