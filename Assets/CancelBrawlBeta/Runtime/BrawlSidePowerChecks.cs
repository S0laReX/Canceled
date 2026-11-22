using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Opt-in integration cases for power counterplay and XY warnings.</summary>
    public static class BrawlSidePowerChecks
    {
        private static IEnumerator Setup(BrawlMatch match,int character,float targetX,float targetY=0)
        {
            match.SelectedCharacter=character; match.ArenaIndex=0; match.StartTraining();
            match.Fighters[0].ResetPractice(new Vector3(0,.05f,0),0);
            match.Fighters[1].ResetPractice(new Vector3(targetX,targetY+.05f,0),0);
            yield return new WaitForSeconds(.2f);
        }
        private static void Super(BrawlMatch match)
        {
            match.Fighters[0].SetMeterForTest(100);
            match.Fighters[0].Submit(new BrawlCommand {Super=true,Aim=Vector2.right});
        }
        private static bool Planar(BrawlSideTelegraph cue)
        {
            if (!cue) return false;
            bool nonFlat=false;
            foreach(var line in cue.GetComponentsInChildren<LineRenderer>())
            {
                float bottom=float.PositiveInfinity,top=float.NegativeInfinity;
                for(int i=0;i<line.positionCount;i++)
                {
                    Vector3 world=line.transform.TransformPoint(line.GetPosition(i));
                    if(Mathf.Abs(world.z+.55f)>.002f) return false;
                    bottom=Mathf.Min(bottom,world.y); top=Mathf.Max(top,world.y);
                }
                nonFlat|=top-bottom>.2f;
            }
            return nonFlat;
        }
        public static IEnumerator Run(BrawlMatch match,Action<bool,string> check,string output)
        {
            match.AutomatedTest=true;
            for(int kit=0;kit<BrawlRoster.Count;kit++)
            {
                yield return Setup(match,kit,kit==1 ? 3.6f : 3);
                Super(match); yield return new WaitForSeconds(.12f);
                var cue=UnityEngine.Object.FindAnyObjectByType<BrawlSideTelegraph>();
                check(Planar(cue),BrawlRoster.Get(kit).Name+": super anuncia alcance y altura en XY");
                if(kit==4)
                {
                    var safe=cue ? cue.transform.Find("Refugio central") : null;
                    var line=safe ? safe.GetComponent<LineRenderer>() : null;
                    bool correct=line && Mathf.Abs(line.GetPosition(0).x+BrawlFighter.SideSafeRadius)<.001f &&
                        Mathf.Abs(line.GetPosition(2).x-BrawlFighter.SideSafeRadius)<.001f;
                    check(correct,"Tío Antena: refugio blanco coincide con el límite de protección central");
                    yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output,"tio-warning.png"));
                }
                if(kit==1 || kit==3)
                {
                    yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output,kit==1 ? "lola-warning.png" : "cripto-warning.png"));
                }
                yield return new WaitForSeconds(BrawlRoster.Get(kit).SuperWindup+BrawlRoster.Get(kit).SuperActive+.15f);
                check(match.Fighters[1].Damage==match.Settings.Attack(AttackKind.Super,kit,0).Damage && match.Fighters[0].Meter==0,
                    BrawlRoster.Get(kit).Name+": super conecta una vez y consume barra en 2.5D");
            }
            foreach(float x in new[] {-.8f,.8f,-BrawlFighter.SideSafeRadius,BrawlFighter.SideSafeRadius})
            {
                yield return Setup(match,4,x); Super(match); yield return new WaitForSeconds(1.2f);
                check(match.Fighters[1].Damage==0,"Tío Antena: centro accesible protege al rival en X="+x);
            }
            yield return Setup(match,4,-3); Super(match); yield return new WaitForSeconds(1.2f);
            check(match.Fighters[1].Damage==match.Settings.Attack(AttackKind.Super,4,0).Damage,"Tío Antena: zona izquierda peligrosa también golpea");
            yield return Setup(match,4,7.6f); Super(match); yield return new WaitForSeconds(1.2f);
            check(match.Fighters[1].Damage==0,"Tío Antena: salir del alcance horizontal evita el super");
            yield return Setup(match,4,4.8f,2.6f); Super(match); yield return new WaitForSeconds(1.2f);
            check(match.Fighters[1].Damage==0 && match.Fighters[1].Grounded,"Tío Antena: plataforma por encima del alcance vertical es segura");
            yield return Setup(match,4,3); Super(match);
            match.Fighters[1].Submit(new BrawlCommand {Move=Vector2.left}); yield return new WaitForSeconds(.3f);
            match.Fighters[1].ClearInput(); yield return new WaitForSeconds(.95f);
            check(Mathf.Abs(match.Fighters[1].transform.position.x)<BrawlFighter.SideSafeRadius && match.Fighters[1].Damage==0,
                "Tío Antena: caminar al centro durante el aviso permite escapar realmente");
            yield return Setup(match,4,3); Super(match); yield return new WaitForSeconds(.15f);
            var pausedCue=match.Fighters[0].GetComponentInChildren<BrawlSideTelegraph>();
            float remaining=pausedCue ? pausedCue.Remaining : -1;
            match.TogglePause(); yield return new WaitForSeconds(.3f);
            check(pausedCue && pausedCue.Remaining==remaining && match.Fighters[1].Damage==0,"Pausa congela aviso y resolución del super");
            match.TogglePause(); yield return new WaitForSeconds(1.2f);
            check(match.Fighters[1].Damage>0,"Reanudar conserva y resuelve el super anunciado");
            yield return Setup(match,4,3); Super(match); yield return new WaitForSeconds(.15f);
            var interrupt=new AttackSpec(AttackKind.Basic,1,0,.1f,.1f,2,1,1,1);
            match.Fighters[0].ReceiveHit(interrupt,Vector3.left,match.Fighters[1]); yield return new WaitForSeconds(.1f);
            check(match.Fighters[0].GetComponentInChildren<BrawlSideTelegraph>()==null,"Interrumpir preparación elimina el aviso del super cancelado");
            yield return new WaitForSeconds(1.1f);
            check(match.Fighters[1].Damage==0,"Super interrumpido no produce daño fantasma");
            bool reduced=BrawlFeedback.ReducedEffects;
            yield return Setup(match,4,.8f); BrawlFeedback.ReducedEffects=true; Super(match); yield return new WaitForSeconds(.1f);
            check(match.Fighters[0].GetComponentInChildren<BrawlSideTelegraph>()!=null,"Efectos reducidos conservan el aviso esencial y el refugio");
            BrawlFeedback.ReducedEffects=reduced;
            match.ReturnToMenu(); yield return null;
            check(UnityEngine.Object.FindObjectsByType<BrawlSideTelegraph>().Length==0,"Volver al menú elimina todos los avisos laterales");
            for(int kit=2;kit<BrawlRoster.Count;kit++)
            {
                yield return Setup(match,kit,kit==4 ? 1.6f : 3);
                match.Fighters[0].Submit(new BrawlCommand {Special=true,Aim=Vector2.right}); yield return new WaitForSeconds(.9f);
                check(match.Fighters[1].Damage>0 && Mathf.Abs(match.Fighters[1].Velocity.z)<.001f,
                    BrawlRoster.Get(kit).Name+": especial conservado conecta sobre el plano lateral");
            }
            foreach(int kit in new[] {1,2})
            {
                yield return Setup(match,kit,4.8f,2.6f); Super(match); yield return new WaitForSeconds(1.5f);
                check(match.Fighters[1].Damage==0 && match.Fighters[1].Grounded,
                    BrawlRoster.Get(kit).Name+": altura fuera del aviso evita el impacto");
            }
            yield return Setup(match,3,6.5f);
            match.Fighters[0].ResetPractice(new Vector3(-3,.05f,0),0); yield return new WaitForSeconds(.1f);
            Super(match); yield return new WaitForSeconds(1.5f);
            check(match.Fighters[1].Damage==0,"Cripto King: rival fuera del círculo anunciado no recibe daño");
            yield return Setup(match,0,2);
            match.Fighters[0].Submit(new BrawlCommand {Heavy=true,Aim=Vector2.left}); yield return new WaitForSeconds(.1f);
            check(Planar(match.Fighters[0].GetComponentInChildren<BrawlSideTelegraph>()),"Ataque fuerte orientado a la izquierda anuncia altura en XY");
            match.ReturnToMenu(); yield return null;
        }
    }
}
