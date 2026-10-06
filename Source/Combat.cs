using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UltraHaul.Design;
using Object=UnityEngine.Object;

namespace UltraHaul {
internal sealed class Combat {
    readonly Plugin plugin;
    readonly Dictionary<int,double> fireTimes=new Dictionary<int,double>();
    readonly Dictionary<int,double> parryTimes=new Dictionary<int,double>();
    readonly List<CoinState> coins=new List<CoinState>();
    readonly List<Shell> shells=new List<Shell>();
    readonly Dictionary<int,(float balance,double updated)> coinTimes=new Dictionary<int,(float,double)>();
    internal Combat(Plugin plugin){this.plugin=plugin;}
    internal Vector3? LatestCoin(int owner)=>coins.LastOrDefault(c=>c.Owner==owner&&!c.Used)?.Position;
    internal void Reset(){foreach(var c in coins)if(c.Visual)Object.Destroy(c.Visual);coins.Clear();shells.Clear();fireTimes.Clear();parryTimes.Clear();coinTimes.Clear();}
    internal void Request(string action,params object[] args) {
        var payload=new object[args.Length+1];payload[0]=action;Array.Copy(args,0,payload,1,args.Length);
        if(!GameManager.Multiplayer())Master(payload,0);
        else if(plugin.SessionReady)PhotonNetwork.RaiseEvent((byte)Network_Coop.EventCode,payload,new RaiseEventOptions{Receivers=ReceiverGroup.MasterClient},SendOptions.SendReliable);
    }
    internal void Receive(EventData evt) {
        if(evt.Code!=(byte)Network_Coop.EventCode || !(evt.CustomData is object[] payload)||payload.Length<1||!plugin.SessionReady)return;
        try {
            var action=payload[0] as string;
            if(action=="visual"||action=="award"||action=="toss"||action=="shell"||action=="boosted"||action=="coinEnd"||action=="shellEnd") {
                if(PhotonNetwork.InRoom && evt.Sender!=PhotonNetwork.MasterClient.ActorNumber)return;
                Output(payload);
            } else if(PhotonNetwork.IsMasterClient)Master(payload,evt.Sender);
        }catch(Exception e){plugin.Info("Rejected malformed combat event: "+e.Message);}
    }
    bool Owner(int id,int sender,out PlayerAvatar avatar) {
        avatar=null;
        if(!GameManager.Multiplayer()){avatar=plugin.Local;return avatar && id==plugin.LocalId;}
        var view=PhotonView.Find(id);if(!view || view.OwnerActorNr!=sender)return false;
        avatar=view.GetComponent<PlayerAvatar>();return avatar&&!Plugin.FieldBool(avatar,"deadSet");
    }
    bool SafeVector(Vector3 v)=>!(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));
    void Master(object[] data,int sender) {
        if(!plugin.SessionReady || !GameDirector.instance || GameDirector.instance.currentState!=GameDirector.gameState.Main)return;
        int id=(int)data[1];if(!Owner(id,sender,out var avatar))return;
        string action=(string)data[0];double now=Plugin.Clock;
        if(action=="fire") {
            int gun=(int)data[2];if(gun!=0&&gun!=1)return;
            Vector3 origin=(Vector3)data[3],dir=(Vector3)data[4];double stamp=(double)data[5];
            if(!ValidateAim(avatar,origin,dir)||double.IsNaN(stamp)||double.IsInfinity(stamp)||Math.Abs(now-stamp)>1)return;
            float cooldown=(gun==0?Weapons_Revolver.Cooldown:Weapons_Shotgun.Cooldown)/(plugin.Style(id).Boosted(now)?Style_Momentum.CombatMultiplier:1);
            if(fireTimes.TryGetValue(id,out var nextAllowed)&&now<nextAllowed-Network_Coop.RateLimit)return;
            fireTimes[id]=now+cooldown;
            if(gun==0)Revolver(id,origin,dir.normalized);
            else Broadcast("shell",id,origin,dir.normalized,now);
            plugin.Info("HOST fire "+(gun==0?"revolver":"shotgun"));
        } else if(action=="coin") {
            Vector3 pos=(Vector3)data[3],velocity=(Vector3)data[4];
            if(!SafeVector(pos)||!SafeVector(velocity)||Vector3.Distance(pos,avatar.transform.position)>Network_Coop.ShotTolerance||velocity.magnitude>Parry_Feedback.CoinUp+Parry_Feedback.CoinForward+1)return;
            if(!coinTimes.TryGetValue(id,out var budget))budget=(Parry_Feedback.CoinCapacity,now);
            float balance=Mathf.Min(Parry_Feedback.CoinCapacity,budget.balance+(float)(now-budget.updated)/Parry_Feedback.CoinRecharge);
            if(balance<1)return;coinTimes[id]=(balance-1,now);
            Broadcast("toss",id,(int)data[2],pos,velocity,now);
        } else if(action=="boost") {
            Vector3 origin=(Vector3)data[2],dir=(Vector3)data[3];
            if(!ValidateAim(avatar,origin,dir))return;
            var shell=shells.LastOrDefault(s=>s.Owner==id&&!s.Boosted&&now-s.Birth<=Weapons_Shotgun.BoostWindow&&s.Alive);
            if(shell==null)return;
            Broadcast("boosted",id,shell.Birth,dir.normalized);
        } else if(action=="parry") {
            if(parryTimes.TryGetValue(id,out var last)&&now-last<Parry_Feedback.Cooldown-Network_Coop.RateLimit)return;
            var enemy=SemiFunc.EnemyGetFromIndex((int)data[2]);
            Vector3 origin=(Vector3)data[3],dir=(Vector3)data[4];
            if(!enemy||!ValidateAim(avatar,origin,dir)||Vector3.Distance(enemy.transform.position,avatar.transform.position)>Parry_Feedback.Range)return;
            parryTimes[id]=now;
            var health=enemy.GetComponent<EnemyHealth>();if(!health||!Damage(id,health,Parry_Feedback.Damage,dir,"parry",Style_Momentum.Parry))return;
            enemy.GetComponent<EnemyStateStunned>()?.Set(Parry_Feedback.StunSeconds);
            Broadcast("visual",origin,enemy.transform.position,"parry");
        }
    }
    bool ValidateAim(PlayerAvatar avatar,Vector3 origin,Vector3 dir)=>SafeVector(origin)&&SafeVector(dir)&&dir.sqrMagnitude>0.5f&&dir.sqrMagnitude<1.5f&&Vector3.Distance(origin,avatar.transform.position)<=Network_Coop.ShotTolerance;
    void Broadcast(string action,params object[] args) {
        var payload=new object[args.Length+1];payload[0]=action;Array.Copy(args,0,payload,1,args.Length);Output(payload);
        if(GameManager.Multiplayer())PhotonNetwork.RaiseEvent((byte)Network_Coop.EventCode,payload,new RaiseEventOptions{Receivers=ReceiverGroup.Others},SendOptions.SendReliable);
    }
    void Output(object[] d) {
        string action=(string)d[0];
        if(action=="award")plugin.Award((int)d[1],(string)d[2],(float)d[3]);
        else if(action=="visual")Tracer((Vector3)d[1],(Vector3)d[2],(string)d[3]);
        else if(action=="toss") {
            var c=new CoinState{Owner=(int)d[1],Sequence=(int)d[2],Origin=(Vector3)d[3],Position=(Vector3)d[3],Velocity=(Vector3)d[4],Birth=(double)d[5]};
            c.Visual=plugin.Assets.CoinVisual();coins.Add(c);
        } else if(action=="shell") {
            shells.Add(new Shell{Owner=(int)d[1],Position=(Vector3)d[2]+(Vector3)d[3]*0.25f,Direction=(Vector3)d[3],Birth=(double)d[4],Damage=Weapons_Shotgun.Damage});
        } else if(action=="boosted") {
            int id=(int)d[1];double birth=(double)d[2];
            foreach(var shell in shells.Where(s=>s.Owner==id&&Math.Abs(s.Birth-birth)<0.001)) {shell.Boosted=true;shell.Direction=(Vector3)d[3];shell.Damage=Weapons_Shotgun.BoostDamage;}
            plugin.Info("ACTION projectile boost confirmed");
        } else if(action=="coinEnd")foreach(var coin in coins.Where(c=>c.Owner==(int)d[1]&&c.Sequence==(int)d[2]))coin.Used=true;
        else if(action=="shellEnd")foreach(var shell in shells.Where(s=>s.Owner==(int)d[1]&&Math.Abs(s.Birth-(double)d[2])<0.001))shell.Alive=false;
    }
    internal void Tick() {
        double now=Plugin.Clock;
        for(int i=coins.Count-1;i>=0;i--) {
            var c=coins[i];if(now-c.Birth>Parry_Feedback.CoinLife||c.Used){if(c.Visual)Object.Destroy(c.Visual);coins.RemoveAt(i);continue;}
            float t=(float)(now-c.Birth);c.Position=c.Origin+c.Velocity*t+Vector3.down*(4.9f*t*t);if(c.Visual){c.Visual.transform.position=c.Position;c.Visual.transform.rotation=Quaternion.Euler(360*t,0,0);}
        }
        for(int i=shells.Count-1;i>=0;i--) {
            var shell=shells[i];if(!shell.Alive || now-shell.Birth>Mathf.Min(Weapons_Shotgun.Lifetime,Weapons_Shotgun.Range/Weapons_Shotgun.Speed)){shells.RemoveAt(i);continue;}
            var start=shell.Position;var end=start+shell.Direction*Weapons_Shotgun.Speed*Time.deltaTime;
            if(!GameManager.Multiplayer()||PhotonNetwork.IsMasterClient) {
                if(TryRay(start,shell.Direction,Vector3.Distance(start,end),out var hit)) {
                    if(shell.Boosted)Blast(shell.Owner,hit.point,shell.Direction);
                    else Shotgun(shell.Owner,hit.point,shell.Direction,hit.collider);
                    Broadcast("shellEnd",shell.Owner,shell.Birth);Broadcast("visual",start,hit.point,shell.Boosted?"boost":"shotgun");
                }
            }
            shell.Position=end;
            Tracer(start,end,shell.Boosted?"boost":"shotgun");
        }
    }
    bool TryRay(Vector3 origin,Vector3 dir,float range,out RaycastHit hit) {
        foreach(var h in Physics.RaycastAll(origin,dir,range,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)) {
            if(h.collider.GetComponentInParent<PlayerAvatar>()||h.collider.GetComponentInParent<PlayerController>())continue;
            hit=h;return true;
        }
        hit=default;return false;
    }
    void Revolver(int id,Vector3 origin,Vector3 dir) {
        float range=Weapons_Revolver.Range;
        float obstruction=TryRay(origin,dir,range,out var hit)?hit.distance:range;
        var coin=coins.Where(c=>!c.Used&&Vector3.Dot(c.Position-origin,dir)>0&&Vector3.Dot(c.Position-origin,dir)<obstruction&&Vector3.Cross(c.Position-origin,dir).magnitude<Parry_Feedback.CoinHitRadius).OrderBy(c=>Vector3.Distance(c.Position,origin)).FirstOrDefault();
        if(coin!=null) {
            int chain=1;Broadcast("coinEnd",coin.Owner,coin.Sequence);Broadcast("visual",origin,coin.Position,"coin");Vector3 from=coin.Position;
            foreach(var next in coins.Where(c=>!c.Used&&Vector3.Distance(c.Position,from)<8f).Take(Parry_Feedback.CoinCapacity-1).ToArray()) {
                if(TryRay(from,(next.Position-from).normalized,Vector3.Distance(next.Position,from),out _))continue;
                Broadcast("coinEnd",next.Owner,next.Sequence);chain++;Broadcast("visual",from,next.Position,"coin");from=next.Position;
            }
            var target=Object.FindObjectsOfType<EnemyHealth>().Where(e=>e.gameObject.activeInHierarchy&&Plugin.FieldInt(e,"healthCurrent")>0&&!Plugin.FieldBool(e,"dead")).OrderBy(e=>Vector3.Distance(e.transform.position,from)).FirstOrDefault(e=>CanSee(from,e));
            if(target&&Damage(id,target,Mathf.RoundToInt(Weapons_Revolver.Damage*Mathf.Pow(Parry_Feedback.CoinMultiplier,chain)),(target.transform.position-from).normalized,"coin",Style_Momentum.Coin))Broadcast("visual",from,target.transform.position,"coin");
            Broadcast("visual",coin.Position,coin.Position+Vector3.up*0.3f,"coin");return;
        }
        var end=origin+dir*range;
        if(TryRay(origin,dir,range,out hit)) {
            end=hit.point;var health=Health(hit.collider);
            if(health)Damage(id,health,Weapons_Revolver.Damage,dir,"revolver");else Impulse(hit.collider,dir*Loot_Fragile.BulletImpulse,hit.point);
        }
        Broadcast("visual",origin,end,"revolver");
    }
    static EnemyHealth Health(Collider collider)=>collider.GetComponentInParent<EnemyHealth>()??collider.GetComponentInParent<EnemyParent>()?.GetComponentInChildren<EnemyHealth>();
    internal bool CanSee(Vector3 from,EnemyHealth health) {
        var target=health.GetComponent<Enemy>();var point=target&&target.CenterTransform?target.CenterTransform.position:health.transform.position;
        return !TryRay(from,(point-from).normalized,Vector3.Distance(from,point),out var hit)||Health(hit.collider)==health;
    }
    // ponytail: one traveling pellet packet; use independent projectiles if full-flight spread is required.
    void Shotgun(int id,Vector3 point,Vector3 dir,Collider first) {
        var hits=new Dictionary<EnemyHealth,int>();
        var direct=Health(first);if(direct)hits[direct]=1;else Impulse(first,dir*Loot_Fragile.BulletImpulse,point);
        var rotation=Quaternion.LookRotation(dir);
        for(int i=1;i<Weapons_Shotgun.Pellets;i++) {
            var spread=rotation*Quaternion.Euler(UnityEngine.Random.Range(-Weapons_Shotgun.Spread,Weapons_Shotgun.Spread),UnityEngine.Random.Range(-Weapons_Shotgun.Spread,Weapons_Shotgun.Spread),0)*Vector3.forward;
            if(!TryRay(point-dir*0.2f,spread,Weapons_Shotgun.Range,out var hit))continue;
            var h=Health(hit.collider);if(h)hits[h]=hits.TryGetValue(h,out var n)?n+1:1;else Impulse(hit.collider,spread*Loot_Fragile.BulletImpulse,hit.point);
        }
        foreach(var pair in hits)Damage(id,pair.Key,pair.Value*Weapons_Shotgun.Damage,dir,"shotgun");
    }
    void Blast(int id,Vector3 point,Vector3 direction) {
        var touched=new HashSet<EnemyHealth>();bool damaged=false;
        foreach(var collider in Physics.OverlapSphere(point,Weapons_Shotgun.BlastRadius,~0,QueryTriggerInteraction.Collide)) {
            var health=Health(collider);
            if(health&&touched.Add(health)&&CanSee(point-direction*0.1f,health))damaged|=Damage(id,health,Weapons_Shotgun.BoostDamage,direction,"boost",damaged?0:Style_Momentum.Boost);
            else if(!health)Impulse(collider,(collider.transform.position-point).normalized*Loot_Fragile.BlastImpulse,point);
        }
    }
    bool Damage(int id,EnemyHealth health,int amount,Vector3 dir,string kind,float bonus=0) {
        int before=Plugin.FieldInt(health,"healthCurrent");if(before<=0)return false;
        if(plugin.Style(id).Boosted(Plugin.Clock))amount=Mathf.RoundToInt(amount*Style_Momentum.CombatMultiplier);
        health.Hurt(amount,dir);
        int after=Plugin.FieldInt(health,"healthCurrent");if(after>=before)return false;
        Broadcast("award",id,kind,Style_Momentum.Hit+bonus);
        if(after<=0)Broadcast("award",id,"kill",Style_Momentum.Kill);
        plugin.Info("HIT "+health.GetComponentInParent<EnemyParent>()?.enemyName+" "+before+" -> "+after+" via "+kind);
        return true;
    }
    void Impulse(Collider collider,Vector3 impulse,Vector3 point) {
        if(collider.GetComponentInParent<PlayerAvatar>()||collider.GetComponentInParent<PlayerController>())return;
        if(!collider.GetComponentInParent<PhysGrabObject>())return;
        var rb=collider.attachedRigidbody;if(rb&&!rb.isKinematic)rb.AddForceAtPosition(impulse,point,ForceMode.Impulse);
    }
    static Material trailMaterial;
    static void Tracer(Vector3 from,Vector3 to,string kind) {
        if(Vector3.Distance(from,to)<0.01f)return;
        var go=new GameObject("UltraHaul "+kind+" trail");var line=go.AddComponent<LineRenderer>();
        line.positionCount=2;line.SetPosition(0,from);line.SetPosition(1,to);line.startWidth=0.025f;line.endWidth=0.01f;
        var color=kind=="coin"?new Color(1,0.8f,0.05f):kind=="boost"||kind=="parry"?Color.cyan:new Color(1,0.35f,0.1f);
        if(!trailMaterial)trailMaterial=new Material(Shader.Find("Sprites/Default"));line.sharedMaterial=trailMaterial;line.startColor=color;line.endColor=color;Object.Destroy(go,Presentation_Hud.TracerSeconds);
    }
    sealed class CoinState {internal int Owner,Sequence;internal Vector3 Origin,Velocity,Position;internal double Birth;internal bool Used;internal GameObject Visual;}
    sealed class Shell {internal int Owner,Damage;internal Vector3 Position,Direction;internal double Birth;internal bool Boosted,Alive=true;}
}
}
