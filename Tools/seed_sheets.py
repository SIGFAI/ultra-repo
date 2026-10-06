"""One-time authoring of initial design sheets. Subsequent changes edit JSON first."""
import json
from pathlib import Path

root=Path(__file__).resolve().parents[1]
def sheet(name, columns, rows, evidence):
    p=root/'Sheets'/f'{name}.json';p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(json.dumps({'sheet':name,'columns':columns,'evidence':evidence,'rows':rows},indent=2)+'\n')

sheet('movement',{'id':'string','walk':'float','sprint':'float','crouch':'float','jump':'float','gravity':'float','dashSpeed':'float','dashSeconds':'float','dashCapacity':'float','dashRecharge':'float','slideSpeed':'float','slideDecay':'float','slamSpeed':'float','carryMultiplier':'float'},[
 {'id':'v1','walk':7.0,'sprint':10.0,'crouch':3.0,'jump':10.0,'gravity':20.0,'dashSpeed':23.0,'dashSeconds':0.16,'dashCapacity':3.0,'dashRecharge':0.65,'slideSpeed':16.0,'slideDecay':0.5,'slamSpeed':27.0,'carryMultiplier':0.75}],
 'Design: approved dash/slide/slam; hooks verified in PlayerController.Start, FixedUpdate, MoveForce, CrouchOverride and CollisionController.Grounded. Tunable values are initial design choices, not runtime test results.')
sheet('weapons',{'id':'string','asset':'ref:assets','damage':'int','pellets':'int','cooldown':'float','range':'float','spread':'float','speed':'float','lifetime':'float','boostWindow':'float','boostDamage':'int','blastRadius':'float'},[
 {'id':'revolver','asset':'revolver','damage':24,'pellets':1,'cooldown':0.38,'range':80.0,'spread':0.0,'speed':0.0,'lifetime':0.0,'boostWindow':0.0,'boostDamage':0,'blastRadius':0.0},
 {'id':'shotgun','asset':'shotgun','damage':8,'pellets':8,'cooldown':0.95,'range':40.0,'spread':4.0,'speed':42.0,'lifetime':1.2,'boostWindow':0.22,'boostDamage':60,'blastRadius':2.5}],
 'Design: user chose revolver/shotgun, coin ricochets/projectile boosts. EnemyHealth.Hurt requires host/singleplayer. Unlimited ammunition follows ULTRAKILL weapon behavior; reload/clip is deliberately not a touched system.')
sheet('assets',{'id':'string','bundle':'string','key':'string','meshName':'string','textureKey':'string','height':'float','rotateX':'float','rotateY':'float','rotateZ':'float'},[
 {'id':'v1','bundle':'models','key':'438442920efb1e24fa03e81636cfc9d4','meshName':'v1_mdl','textureKey':'none','height':1.8,'rotateX':-90.0,'rotateY':180.0,'rotateZ':0.0},
 {'id':'revolver','bundle':'models','key':'0dbb5142230adc84292865064219a5d3','meshName':'Revolver_Body','textureKey':'647dea7a4421fff41bbcd2f00080eb39','height':0.42,'rotateX':0.0,'rotateY':0.0,'rotateZ':0.0},
 {'id':'shotgun','bundle':'models','key':'e1e497dbbcf47df49a1b3d23fd1b4994','meshName':'Shotgun_New','textureKey':'20c9f6d019cb72f49b178539f13f5377','height':0.5,'rotateX':90.0,'rotateY':0.0,'rotateZ':0.0}],
 'Verified by UnityPy against installed ULTRAKILL asset bundle containers/mesh names. Assets are loaded at runtime from local game; no extracted assets shipped. V1 uses native vertex colors with blue material.')
sheet('style',{'id':'string','maxScore':'float','decay':'float','grace':'float','hit':'float','kill':'float','coin':'float','parry':'float','boost':'float','varietyMultiplier':'float','staleMultiplier':'float','energyPerPoint':'float','boostThreshold':'float','boostSeconds':'float','combatMultiplier':'float'},[
 {'id':'momentum','maxScore':600.0,'decay':40.0,'grace':3.0,'hit':12.0,'kill':85.0,'coin':55.0,'parry':100.0,'boost':75.0,'varietyMultiplier':1.25,'staleMultiplier':0.35,'energyPerPoint':0.007,'boostThreshold':160.0,'boostSeconds':3.0,'combatMultiplier':1.2}],
 'Design: user chose combat momentum, not money or cargo shields. Host acknowledges successful hits before style is rewarded; style is local per player and resets on death/scene change.')
sheet('parry',{'id':'string','window':'float','cooldown':'float','range':'float','damage':'int','stunSeconds':'float','coinCapacity':'int','coinRecharge':'float','coinLife':'float','coinUp':'float','coinForward':'float','coinMultiplier':'float','coinHitRadius':'float'},[
 {'id':'feedback','window':0.22,'cooldown':0.55,'range':3.0,'damage':35,'stunSeconds':0.6,'coinCapacity':4,'coinRecharge':3.0,'coinLife':2.0,'coinUp':6.0,'coinForward':3.0,'coinMultiplier':2.0,'coinHitRadius':0.22}],
 'Design: timed incoming enemy parries and projectile boosts. Hooks verified: PlayerHealth.Hurt enemyIndex, SemiFunc.EnemyGetFromIndex, EnemyHealth.Hurt, EnemyStateStunned.Set. No fall-damage immunity or generic invincibility.')
sheet('controls',{'id':'string','mode':'string','path':'string','action':'string'},[
 {'id':'dash','mode':'game','path':'Sprint','action':'dash'},
 {'id':'slide','mode':'game','path':'Crouch','action':'slide'},
 {'id':'fire','mode':'custom','path':'<Mouse>/leftButton','action':'fire'},
 {'id':'coin','mode':'custom','path':'<Mouse>/rightButton','action':'coin'},
 {'id':'parry','mode':'custom','path':'<Keyboard>/f','action':'parry'},
 {'id':'equip','mode':'custom','path':'<Keyboard>/q','action':'equip'},
 {'id':'switch','mode':'custom','path':'<Keyboard>/r','action':'switch'}],
 'Game keys use InputManager saved remaps (Sprint/Crouch); custom InputActions are configurable BepInEx entries. Q toggles guns/grabber to preserve R.E.P.O. original mouse grabbing/rotation and inventory keys. All input disabled in menus/death/chat.')
sheet('network',{'id':'string','eventCode':'int','maxPlayers':'int','protocol':'string','transport':'string','handshakeKey':'string','shotTolerance':'float','rateLimit':'float','joinArg':'string'},[
 {'id':'coop','eventCode':181,'maxPlayers':6,'protocol':'UH-1','transport':'native Steam + Photon PUN relay','handshakeKey':'UltraHaulProtocol','shotTolerance':6.0,'rateLimit':0.1,'joinArg':'+connect_lobby'}],
 'Verified: installed game ships Photon PUN2; EnemyHealth is host-authoritative, SteamManager.Start reads +connect_lobby, native game supports 6. Require all room players to advertise protocol before gameplay activates. Join integration stays unpublished until two-client verification.')
sheet('loot',{'id':'string','damagePolicy':'string','valuePolicy':'string','grabPolicy':'string','bulletImpulse':'float','blastImpulse':'float'},[
 {'id':'fragile','damagePolicy':'native','valuePolicy':'native','grabPolicy':'native when holstered','bulletImpulse':1.5,'blastImpulse':2.0}],
 'Design: preserve fragile cargo. Never patch valuable durability, price or extraction. Bullet/blast impacts add bounded native rigidbody impulses; original impact damage remains authoritative.')
sheet('presentation',{'id':'string','title':'string','hudWidth':'int','hudHeight':'int','scanSeconds':'float','tracerSeconds':'float','viewX':'float','viewY':'float','viewZ':'float','recoil':'float','testBridge':'string'},[
 {'id':'hud','title':'ULTRAHAUL // DEVELOPMENT','hudWidth':330,'hudHeight':165,'scanSeconds':2.0,'tracerSeconds':0.12,'viewX':0.22,'viewY':-0.24,'viewZ':0.55,'recoil':0.08,'testBridge':'{localappdata}/UltraHaul/dev'}],
 'Design: game-only HUD showing weapon, dash charges, style rank, controls and handshake state. Camera sourced from PlayerController.cameraGameObjectLocal, not a separate gameplay scene. Optional local file bridge only with ULTRAHAUL_DEV=1; excluded from release activation.')
sheet('hooks',{'id':'string','type':'string','member':'string','phase':'string','system':'string','source':'string'},[
 {'id':'movement','type':'PlayerController','member':'Start','phase':'postfix','system':'movement','source':'PlayerController.cs'},
 {'id':'parry','type':'PlayerHealth','member':'Hurt','phase':'prefix','system':'parry','source':'PlayerHealth.cs'},
 {'id':'damage','type':'EnemyHealth','member':'Hurt','phase':'host call','system':'weapons','source':'EnemyHealth.cs'},
 {'id':'input','type':'InputManager','member':'GetAction','phase':'read saved bindings','system':'controls','source':'InputManager.cs'},
 {'id':'avatars','type':'SemiFunc','member':'PlayerGetList','phase':'read on main thread','system':'assets','source':'SemiFunc.cs'},
 {'id':'session','type':'SteamManager','member':'Start','phase':'native join arg','system':'network','source':'SteamManager.cs'}],
 'Every named hook inspected in installed game using ILSpy 9.1; no game source is shipped. Runtime verification is recorded separately in test-results.json and never inferred from this preflight.')
print('Authored 10 initial sheets.')
