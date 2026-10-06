using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.InputSystem;
using UltraHaul.Design;
using Object = UnityEngine.Object;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace UltraHaul {
[BepInPlugin("local.ultrahaul", "UltraHaul", "0.1.0")]
public sealed class Plugin : BaseUnityPlugin {
    internal static Plugin Instance;
    internal readonly Dictionary<int, StyleState> Styles = new Dictionary<int, StyleState>();
    internal Combat Combat;
    internal Assets Assets;
    internal bool Equipped;
    internal int Weapon;
    internal float Dash = Movement_V1.DashCapacity;
    internal float ParryUntil;
    internal double LastShot = -100;
    internal string Status = "Loading your ULTRAKILL assets…";
    internal InputAction Fire, Coin, Parry, Equip, Switch;
    private float nextScan, dashUntil, parryCooldown, nextFire;
    private Vector3 dashDirection;
    private bool slamming, handshake, priorActive, priorSlide, networkSubscribed;
    private float coins = Parry_Feedback.CoinCapacity;
    private int identity, coinSequence;
    private PlayerController configured;
    private float originalWalk,originalSprint,originalCrouch,originalJump,originalGravity,originalSlideTime,originalSlideDecay;
    private GameObject viewModel;
    private float recoil;
    private readonly HashSet<PlayerAvatar> dressed = new HashSet<PlayerAvatar>();
    private readonly List<InputAction> actions = new List<InputAction>();
    private Harmony harmony;
    private ConfigEntry<string> sourceGame;
    private float lastTelemetry;
    private float peakSpeed,minVertical;
    private EnemyHealth testEnemy;
    internal static bool NetworkActive=>GameManager.instance&&GameManager.Multiplayer();
    internal static double Clock => NetworkActive&&PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.timeAsDouble;
    internal PlayerAvatar Local => PlayerController.instance ? PlayerController.instance.playerAvatar?.GetComponent<PlayerAvatar>() : null;
    internal Camera View {
        get {
            var p=PlayerController.instance;
            var camera=p && p.cameraGameObjectLocal ? p.cameraGameObjectLocal.GetComponentInChildren<Camera>() : null;
            return camera ? camera : Camera.main;
        }
    }
    internal bool SessionReady {
        get {
            if (!GameManager.instance || !GameManager.Multiplayer()) return true;
            return PhotonNetwork.InRoom && PhotonNetwork.PlayerList.All(p => p.CustomProperties.TryGetValue(Network_Coop.HandshakeKey,out var v) && v is string protocol && protocol==Network_Coop.Protocol);
        }
    }
    internal bool Playing => Assets!=null && Assets.Ready && SessionReady && PlayerController.instance && configured==PlayerController.instance && Local && !FieldBool(Local,"deadSet") && !FieldBool(Local,"isDisabled") && GameDirector.instance && GameDirector.instance.currentState==GameDirector.gameState.Main && !GameDirector.instance.DisableInput && (SemiFunc.RunIsLevel()||SemiFunc.RunIsTutorial()||SemiFunc.RunIsLobby()) && Cursor.lockState==CursorLockMode.Locked;
    internal static bool FieldBool(object o,string name) => o!=null && (bool)(AccessTools.Field(o.GetType(),name)?.GetValue(o) ?? false);
    internal static int FieldInt(object o,string name) => (int)(AccessTools.Field(o.GetType(),name)?.GetValue(o) ?? 0);
    static IEnumerable<EnemyHealth> LivingEnemies()=>Object.FindObjectsOfType<EnemyHealth>().Where(e=>e.gameObject.activeInHierarchy&&FieldInt(e,"healthCurrent")>0&&!FieldBool(e,"dead"));
    internal int LocalId => Local && Local.photonView ? Local.photonView.ViewID : 0;
    internal StyleState Style(int id) { if(!Styles.TryGetValue(id,out var s)) Styles[id]=s=new StyleState();return s; }
    internal new void Info(string text) => Logger.LogInfo(text);
    void Awake() {
        Instance=this; Application.runInBackground=true;
        if(Lifecycle_Bootstrap.HideManager)gameObject.hideFlags=HideFlags.HideAndDontSave;
        sourceGame=Config.Bind("Assets","ULTRAKILLFolder","","Optional override; normally discovered from Steam libraries without asking the player.");
        Fire=Bind("Fire",Controls_Fire.Path);Coin=Bind("Coin",Controls_Coin.Path);Parry=Bind("Parry",Controls_Parry.Path);Equip=Bind("DrawHolster",Controls_Equip.Path);Switch=Bind("SwitchWeapon",Controls_Switch.Path);
        Combat=new Combat(this);Assets=new Assets(this);
        harmony=new Harmony("local.ultrahaul");harmony.PatchAll();
        StartCoroutine(Assets.Load(sourceGame.Value));
        Info("UltraHaul 0.1.0 loaded. Native fragile loot, solo + private co-op; protocol "+Network_Coop.Protocol);
    }
    InputAction Bind(string name,string path) {
        var binding=Config.Bind("Controls",name,path,"Unity Input System binding path.");
        var action=new InputAction("UltraHaul."+name,InputActionType.Button,binding.Value);action.Enable();actions.Add(action);return action;
    }
    void OnDestroy() {
        if(networkSubscribed)PhotonNetwork.NetworkingClient.EventReceived-=Combat.Receive;
        foreach(var a in actions){a.Disable();a.Dispose();}
        harmony?.UnpatchSelf();
    }
    void Update() {
        if(NetworkActive&&!networkSubscribed){PhotonNetwork.NetworkingClient.EventReceived+=Combat.Receive;networkSubscribed=true;}
        if(NetworkActive&&PhotonNetwork.InRoom && Assets.Ready && !handshake) {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable{{Network_Coop.HandshakeKey,Network_Coop.Protocol}});handshake=true;
            Info("Advertised UltraHaul protocol in native lobby.");
        }
        if(!NetworkActive||!PhotonNetwork.InRoom)handshake=false;
        if(!SessionReady && configured)RestoreController();
        Configure();
        foreach(var s in Styles.Values)s.Tick(Clock);
        Combat.Tick();
        if(Time.unscaledTime>=nextScan) {nextScan=Time.unscaledTime+Presentation_Hud.ScanSeconds;DressPlayers();}
        bool active=Playing;
        if(!active) {
            if(viewModel)viewModel.SetActive(false);
            if(priorActive && Local && FieldBool(Local,"deadSet")){Styles.Clear();Dash=Movement_V1.DashCapacity;Equipped=false;}
            priorActive=false;DevBridge();return;
        }
        if(identity!=LocalId) {identity=LocalId;ResetRun();}
        priorActive=true;
        Dash=Mathf.Min(Movement_V1.DashCapacity,Dash+Time.deltaTime*Movement_V1.DashRecharge);
        coins=Mathf.Min(Parry_Feedback.CoinCapacity,coins+Time.deltaTime/Parry_Feedback.CoinRecharge);
        if(Equip.WasPressedThisFrame()){Equipped=!Equipped;RebuildView();}
        if(Switch.WasPressedThisFrame() && Equipped){Weapon=1-Weapon;RebuildView();}
        var p=PlayerController.instance;
        if(SemiFunc.InputDown(InputKey.Sprint) && Dash>=1f) {
            var input=SemiFunc.InputMovement();dashDirection=p.transform.TransformDirection(new Vector3(input.x,0,input.y));
            if(dashDirection.sqrMagnitude<0.1f)dashDirection=p.transform.forward;
            Dash-=1;dashUntil=Time.time+Movement_V1.DashSeconds;Info("ACTION dash; charges="+Dash.ToString("F2"));
        }
        if(SemiFunc.InputDown(InputKey.Crouch)) {
            if(!p.CollisionController.Grounded){slamming=true;Info("ACTION slam");}
            else {
                var move=SemiFunc.InputMovement();
                if(move.sqrMagnitude>0.1f){AccessTools.Field(typeof(PlayerController),"CanSlide").SetValue(p,true);SlideVector(p,new Vector3(move.x,0,move.y));}
            }
        }
        if(Equipped && Fire.IsPressed())FireCurrent();
        if(Equipped && Weapon==0 && Coin.WasPressedThisFrame() && coins>=1) {
            var camera=View;coins-=1;
            Assets.Sound("coin");
            Combat.Request("coin",LocalId,++coinSequence,camera.transform.position+camera.transform.forward*0.6f,camera.transform.forward*Parry_Feedback.CoinForward+Vector3.up*Parry_Feedback.CoinUp,Clock);
        }
        if(Parry.WasPressedThisFrame())ArmParry();
        if(viewModel) {
            viewModel.SetActive(Equipped);recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*0.6f);
            viewModel.transform.localPosition=new Vector3(Presentation_Hud.ViewX,Presentation_Hud.ViewY+Mathf.Sin(Time.time*9)*Mathf.Min(p.rb.velocity.magnitude*0.001f,0.008f),Presentation_Hud.ViewZ-recoil);
        }
        DevBridge();
    }
    void ResetRun(){Styles.Clear();Combat.Reset();Dash=Movement_V1.DashCapacity;coins=Parry_Feedback.CoinCapacity;Equipped=false;Weapon=0;LastShot=-100;nextFire=parryCooldown=ParryUntil=dashUntil=0;slamming=priorSlide=false;if(viewModel)Destroy(viewModel);}
    internal void FireCurrent() {
        var camera=View;if(!Playing||!Equipped||!camera||Time.time<nextFire)return;
        nextFire=Time.time+(Weapon==0?Weapons_Revolver.Cooldown:Weapons_Shotgun.Cooldown)/(Style(LocalId).Boosted(Clock)?Style_Momentum.CombatMultiplier:1f);
        LastShot=Clock;recoil=Presentation_Hud.Recoil;Assets.Sound(Weapon==0?"revolver":"shotgun");
        Combat.Request("fire",LocalId,Weapon,camera.transform.position,camera.transform.forward,Clock);
    }
    void ArmParry() {
        if(!Playing||Time.time<parryCooldown)return;
        ParryUntil=Time.time+Parry_Feedback.Window;parryCooldown=Time.time+Parry_Feedback.Cooldown;
        if(Equipped&&Weapon==1&&Clock-LastShot<=Weapons_Shotgun.BoostWindow)Combat.Request("boost",LocalId,View.transform.position,View.transform.forward,Clock);
        Info("ACTION parry armed");
    }
    void FixedUpdate() {
        if(!Playing)return;var p=PlayerController.instance;
        peakSpeed=Mathf.Max(peakSpeed,new Vector2(p.rb.velocity.x,p.rb.velocity.z).magnitude);minVertical=Mathf.Min(minVertical,p.rb.velocity.y);
        if(Time.time<dashUntil)p.MoveForce(dashDirection,Movement_V1.DashSpeed*(p.physGrabActive?Movement_V1.CarryMultiplier:1f),Time.fixedDeltaTime*2);
        if(slamming) {
            p.CollisionController.ResetFalling();
            if(p.CollisionController.Grounded)slamming=false;
            else p.rb.velocity=new Vector3(p.rb.velocity.x,-Movement_V1.SlamSpeed,p.rb.velocity.z);
        }
    }
    internal void Configure() {
        var p=PlayerController.instance;
        if(!p || !p.rb || configured==p || !Assets.Ready||!SessionReady||!GameDirector.instance||GameDirector.instance.currentState!=GameDirector.gameState.Main)return;
        if((float)AccessTools.Field(typeof(PlayerController),"playerOriginalMoveSpeed").GetValue(p)<=0)return;
        ResetRun();configured=p;originalWalk=p.MoveSpeed;originalSprint=p.SprintSpeed;originalCrouch=p.CrouchSpeed;originalJump=p.JumpForce;originalGravity=p.CustomGravity;originalSlideTime=p.SlideTime;originalSlideDecay=p.SlideDecay;
        p.MoveSpeed=Movement_V1.Walk;p.SprintSpeed=Movement_V1.Sprint;p.CrouchSpeed=Movement_V1.Crouch;p.JumpForce=Movement_V1.Jump;p.CustomGravity=Movement_V1.Gravity;
        SetNativeSpeeds(p,p.MoveSpeed,p.SprintSpeed,p.CrouchSpeed,p.CustomGravity);
        p.SlideTime=Movement_V1.SlideSeconds;p.SlideDecay=Movement_V1.SlideDecay;
        Info("Configured native controller: walk="+p.MoveSpeed+" sprint="+p.SprintSpeed+" jump="+p.JumpForce);
    }
    static void SetNativeSpeeds(PlayerController p,float walk,float sprint,float crouch,float gravity){AccessTools.Field(typeof(PlayerController),"playerOriginalMoveSpeed").SetValue(p,walk);AccessTools.Field(typeof(PlayerController),"playerOriginalSprintSpeed").SetValue(p,sprint);AccessTools.Field(typeof(PlayerController),"playerOriginalCrouchSpeed").SetValue(p,crouch);AccessTools.Field(typeof(PlayerController),"playerOriginalCustomGravity").SetValue(p,gravity);}
    void RestoreController(){configured.MoveSpeed=originalWalk;configured.SprintSpeed=originalSprint;configured.CrouchSpeed=originalCrouch;configured.JumpForce=originalJump;configured.CustomGravity=originalGravity;configured.SlideTime=originalSlideTime;configured.SlideDecay=originalSlideDecay;SetNativeSpeeds(configured,originalWalk,originalSprint,originalCrouch,originalGravity);configured=null;ResetRun();}
    internal void NativeSlide(PlayerController p) {
        if(p!=configured||!Playing)return;
        if(p.Sliding&&!priorSlide) {
            var direction=(Vector3)AccessTools.Field(typeof(PlayerController),"SlideDirection").GetValue(p);
            SlideVector(p,direction);
            Info("ACTION native slide");
        }
        priorSlide=p.Sliding;
    }
    static void SlideVector(PlayerController p,Vector3 direction){direction=direction.normalized*Mathf.Max(0,Movement_V1.SlideSpeed-p.CrouchSpeed);AccessTools.Field(typeof(PlayerController),"SlideDirection").SetValue(p,direction);AccessTools.Field(typeof(PlayerController),"SlideDirectionCurrent").SetValue(p,direction);}
    void DressPlayers() {
        if(!Assets.Ready || !GameDirector.instance||!SessionReady)return;
        foreach(var avatar in SemiFunc.PlayerGetList()) {
            if(!avatar || !avatar.playerAvatarVisuals || dressed.Contains(avatar))continue;
            Assets.Dress(avatar);dressed.Add(avatar);
        }
        dressed.RemoveWhere(p=>!p);
    }
    void RebuildView() {
        if(viewModel)Destroy(viewModel);var camera=View;if(!camera||!Assets.Ready||!Equipped)return;
        viewModel=Assets.Gun(Weapon,camera.transform);
    }
    internal void Award(int playerId,string kind,float points) {
        float actual=Style(playerId).Award(kind,points,Clock);
        if(playerId==LocalId){Dash=Mathf.Min(Movement_V1.DashCapacity,Dash+actual*Style_Momentum.EnergyPerPoint);Info("STYLE "+kind+" +"+actual.ToString("F0"));}
    }
    internal bool TryParry(PlayerHealth target,int enemyIndex) {
        if(Environment.GetEnvironmentVariable("ULTRAHAUL_DEV")=="1"&&Time.time<=ParryUntil)Info("PARRY CHECK playing="+Playing+" local="+(target==Local?.playerHealth)+" index="+enemyIndex);
        if(!Playing||Time.time>ParryUntil||enemyIndex<0||target!=Local.playerHealth)return false;
        var enemy=SemiFunc.EnemyGetFromIndex(enemyIndex);
        if(Environment.GetEnvironmentVariable("ULTRAHAUL_DEV")=="1"&&enemy)Info("PARRY GEOMETRY distance="+Vector3.Distance(enemy.transform.position,Local.transform.position)+" dot="+Vector3.Dot(View.transform.forward,(enemy.transform.position-View.transform.position).normalized));
        if(!enemy||Vector3.Distance(enemy.transform.position,Local.transform.position)>Parry_Feedback.Range)return false;
        var camera=View;if(!camera||Vector3.Dot(camera.transform.forward,(enemy.transform.position-camera.transform.position).normalized)<0.1f)return false;
        ParryUntil=0;Combat.Request("parry",LocalId,enemyIndex,camera.transform.position,camera.transform.forward,Clock);return true;
    }
    void OnGUI() {
        if(!Assets.Ready){GUI.Box(new Rect(24,24,560,60),Presentation_Hud.Title+"\n"+Status);return;}
        if(SemiFunc.IsMainMenu()){GUI.Box(new Rect(24,24,420,65),Presentation_Hud.Title+" // 0.1.0 DEVELOPMENT\nSolo + private co-op • ULTRAKILL assets loaded");return;}
        if(!Local)return;
        var matrix=GUI.matrix;float scale=Screen.height/Presentation_Hud.ReferenceHeight;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        float x=Presentation_Hud.HudX,y=Presentation_Hud.HudY;
        var rect=new Rect(x,y,Presentation_Hud.HudWidth,Presentation_Hud.HudHeight);
        GUI.backgroundColor=new Color(0.04f,0.07f,0.09f,0.9f);GUI.Box(rect,"");GUI.color=Color.white;
        var s=Style(LocalId);var heading=new GUIStyle(GUI.skin.label){fontSize=Presentation_Hud.FontSize,fontStyle=FontStyle.Bold};heading.normal.textColor=new Color(0.1f,0.9f,1f);
        var label=new GUIStyle(GUI.skin.label){fontSize=18};
        GUI.Label(new Rect(x+14,y+7,400,35),Presentation_Hud.Title+"  //  "+(!SessionReady?"WAITING FOR MODS":GameManager.Multiplayer()?"CO-OP":"SOLO"),heading);
        GUI.Label(new Rect(x+14,y+44,390,26),Equipped?(Weapon==0?"MARKSMAN REVOLVER":"SHOTGUN • "+KeyName(Parry)+" TO BOOST"):"LOOT MODE • "+KeyName(Equip)+" TO DRAW GUN",label);
        GUI.Label(new Rect(x+14,y+71,390,26),"DASH "+new string('■',Mathf.FloorToInt(Dash))+new string('□',Mathf.Max(0,3-Mathf.FloorToInt(Dash)))+"   STYLE "+s.Rank+"  "+s.Score.ToString("F0"),label);
        GUI.Label(new Rect(x+14,y+98,390,26),s.Boosted(Clock)?"MOMENTUM • COMBAT BOOST ACTIVE":"PARRY + VARIETY = MORE MOMENTUM",label);
        GUI.Label(new Rect(x+14,y+129,390,55),KeyName(Equip)+" draw/holster  "+KeyName(Switch)+" swap  "+KeyName(Parry)+" parry\n"+KeyName(InputManager.instance.GetAction(InputKey.Sprint))+" dash  "+KeyName(InputManager.instance.GetAction(InputKey.Crouch))+" slide/slam",label);
        GUI.matrix=matrix;
        if(Equipped && Playing){GUI.color=new Color(1,0.75f,0.15f);GUI.Box(new Rect(Screen.width/2-3,Screen.height/2-3,6,6),"");GUI.color=Color.white;}
    }
    static string KeyName(InputAction action)=>action==null?"?":InputControlPath.ToHumanReadableString(action.bindings[0].effectivePath,InputControlPath.HumanReadableStringOptions.OmitDevice);
    void TestPosition(Vector3 point) {
        var p=PlayerController.instance;
        if(FieldBool(Local,"isTumbling"))((PlayerTumble)AccessTools.Field(typeof(PlayerAvatar),"tumble").GetValue(Local)).TumbleSet(false,false);
        p.transform.position=point;p.rb.position=point;p.rb.velocity=Vector3.zero;
        p.CollisionController.transform.position=p.CollisionController.FollowTarget.position+p.CollisionController.Offset;
        AccessTools.Field(typeof(PlayerCollisionController),"fallLastY").SetValue(p.CollisionController,p.CollisionController.transform.position.y);
        p.CollisionController.ResetFalling();
    }
    void DevBridge() {
        if(Environment.GetEnvironmentVariable("ULTRAHAUL_DEV")!="1")return;
        var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UltraHaul","dev");Directory.CreateDirectory(dir);
        var slot=Environment.GetEnvironmentVariable(Presentation_Hud.TestSlotEnv);if(!string.IsNullOrEmpty(slot)&&System.Text.RegularExpressions.Regex.IsMatch(slot,"^[a-z0-9_-]{1,32}$")){dir=Path.Combine(dir,slot);Directory.CreateDirectory(dir);}
        var command=Path.Combine(dir,"command.txt");
        if(File.Exists(command)) {
            var text=File.ReadAllText(command).Trim();File.Delete(command);
            try {
                if(text=="tutorial" && MenuPageMain.instance)MenuPageMain.instance.ButtonEventTutorial();
                else if(text=="menu"){if(NetworkActive)PhotonNetwork.Disconnect();StartCoroutine(RunManager.instance.LeaveToMainMenu());}
                else if(text=="host"){SemiFunc.MainMenuSetMultiplayer();SemiFunc.MenuActionHostGame();}
                else if(text=="solo" && MenuPageMain.instance)MenuPageMain.instance.ButtonEventSinglePlayer();
                else if(text=="new")Object.FindObjectOfType<MenuPageSaves>()?.OnNewGame();
                else if(text=="equip"){Equipped=true;RebuildView();}
                else if(text=="shotgun"){Weapon=1;Equipped=true;RebuildView();}
                else if(text=="revolver"){Weapon=0;Equipped=true;RebuildView();}
                else if(text=="holster"){Equipped=false;RebuildView();}
                else if(text=="dash" && Playing){dashDirection=PlayerController.instance.transform.forward;dashUntil=Time.time+Movement_V1.DashSeconds;Dash=Mathf.Max(0,Dash-1);Info("DEV dash");}
                else if(text=="fire")FireCurrent();
                else if(text=="parry")ArmParry();
                else if(text=="measure"){peakSpeed=0;minVertical=0;}
                else if(text=="slam drop"&&Playing){var controller=PlayerController.instance;controller.rb.position+=Vector3.up*Presentation_Hud.TestDropHeight;controller.rb.velocity=Vector3.zero;controller.CollisionController.GroundedDisableTimer=0.2f;}
                else if(text=="freeze enemies")foreach(var enemy in Object.FindObjectsOfType<EnemyStateStunned>())enemy.Set(30);
                else if(text=="movement point"&&Playing) {
                    bool placed=false;
                    foreach(var point in Object.FindObjectsOfType<LevelPoint>().OrderBy(n=>Vector3.Distance(n.transform.position,Local.transform.position))) {
                        if(Physics.Raycast(point.transform.position+Vector3.up,Vector3.up,Presentation_Hud.TestHeadroom,~0,QueryTriggerInteraction.Ignore))continue;
                        foreach(var direction in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left}) {
                            if(Physics.Raycast(point.transform.position+Vector3.up*1.3f,direction,8,~0,QueryTriggerInteraction.Ignore))continue;
                            TestPosition(point.transform.position+Vector3.up*0.7f);CameraAim.Instance.SetPlayerAim(Quaternion.LookRotation(direction),true);placed=true;break;
                        }
                        if(placed)break;
                    }
                    Info("DEV movement test point="+placed);
                }
                else if(text=="session") {
                    var lobby=(Steamworks.Data.Lobby)AccessTools.Field(typeof(SteamManager),"currentLobby").GetValue(SteamManager.instance);
                    Info("SESSION room="+PhotonNetwork.InRoom+" master="+PhotonNetwork.IsMasterClient+" count="+(PhotonNetwork.InRoom?PhotonNetwork.CurrentRoom.PlayerCount:0)+" lobby="+lobby.Id);
                }
                else if(text=="start")Object.FindObjectOfType<MenuPageLobby>()?.ButtonStart();
                else if(text=="coin shot" && Playing)StartCoroutine(CheckCoin());
                else if(text=="boost shot" && Playing)StartCoroutine(CheckBoost());
                else if(text=="parry test" && Playing)StartCoroutine(CheckParry());
                else if((text=="near enemy"||text=="near hunter") && Playing) {
                    var enemies=LivingEnemies().Where(e=>text!="near hunter"||e.GetComponentInParent<EnemyParent>()?.enemyName==Presentation_Hud.TestEnemy).OrderByDescending(e=>FieldInt(e,"healthCurrent"));
                    bool placed=false;
                    foreach(var e in enemies) {
                        var point=Object.FindObjectsOfType<LevelPoint>().Where(n=>Vector3.Distance(n.transform.position,e.transform.position)>=3&&Vector3.Distance(n.transform.position,e.transform.position)<=8&&Combat.CanSee(n.transform.position+Vector3.up*1.4f,e)).OrderBy(n=>Vector3.Distance(n.transform.position,e.transform.position)).FirstOrDefault();
                        if(point){testEnemy=e;TestPosition(point.transform.position+Vector3.up*0.7f);Info("DEV at native level point with line of sight to "+e.GetComponentInParent<EnemyParent>()?.enemyName);placed=true;break;}
                    }
                    if(!placed)Info("DEV no reachable enemy test point found");
                }
                else if(text=="ray"&&Playing) {
                    Info("RAY camera="+View.name+" origin="+View.transform.position+" forward="+View.transform.forward+" aim="+CameraAim.Instance.transform.forward);
                    if(testEnemy){var enemy=testEnemy.GetComponent<Enemy>();Info("TARGET "+testEnemy.GetComponentInParent<EnemyParent>()?.enemyName+" root="+testEnemy.transform.position+" center="+(enemy&&enemy.CenterTransform?enemy.CenterTransform.position:testEnemy.transform.position));}
                    foreach(var hit in Physics.RaycastAll(View.transform.position,View.transform.forward,Weapons_Revolver.Range,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))Info("RAY HIT "+hit.collider.name+" distance="+hit.distance+" trigger="+hit.collider.isTrigger+" enemy="+hit.collider.GetComponentInParent<EnemyParent>()?.enemyName+" player="+(bool)hit.collider.GetComponentInParent<PlayerAvatar>());
                }
                else if(text=="inspect") {
                    foreach(var c in Object.FindObjectsOfType<Camera>())Info("CAM "+c.name+" enabled="+c.enabled+" mask="+c.cullingMask+" near="+c.nearClipPlane+" selected="+(c==View));
                    if(viewModel)foreach(var r in viewModel.GetComponentsInChildren<Renderer>(true))Info("GUN "+r.name+" active="+r.gameObject.activeInHierarchy+" enabled="+r.enabled+" layer="+r.gameObject.layer+" pos="+r.transform.position+" bounds="+r.bounds+" scale="+r.transform.lossyScale);
                    foreach(var key in new[]{InputKey.Sprint,InputKey.Crouch,InputKey.Jump})Info("BIND "+key+" "+InputManager.instance.GetAction(key)?.bindings[0].effectivePath);
                }
                else if(text.StartsWith("native ")) {
                    var words=text.Substring(7).Split(' ');
                    var commands=(Dictionary<string,DebugCommandHandler.ChatCommand>)AccessTools.Field(typeof(DebugCommandHandler),"_commands").GetValue(DebugCommandHandler.instance);
                    if(commands.TryGetValue(words[0],out var native))native.Execute(true,words.Skip(1).ToArray());
                }
                else if(text=="aim enemy" && Playing) {
                    var visible=LivingEnemies().Where(e=>Combat.CanSee(View.transform.position,e));
                    var enemy=testEnemy&&visible.Contains(testEnemy)?testEnemy:visible.OrderBy(e=>Vector3.Distance(e.transform.position,Local.transform.position)).FirstOrDefault();
                    if(enemy){testEnemy=enemy;var e=enemy.GetComponent<Enemy>();var point=e&&e.CenterTransform?e.CenterTransform.position:enemy.transform.position;CameraAim.Instance.SetPlayerAim(Quaternion.LookRotation(point-View.transform.position),true);Info("DEV aim "+enemy.GetComponentInParent<EnemyParent>()?.enemyName+" at "+point+" from "+View.transform.position);}
                }
                else if(text=="names")Info("ENEMY SETUPS "+string.Join(", ",EnemyDirector.instance.enemiesDifficulty1.Concat(EnemyDirector.instance.enemiesDifficulty2).Concat(EnemyDirector.instance.enemiesDifficulty3).Select(e=>e.name)));
                else if(text=="entities") {
                    foreach(var e in Object.FindObjectsOfType<EnemyHealth>(true))Info("ENEMY "+e.GetComponentInParent<EnemyParent>()?.enemyName+" active="+e.gameObject.activeInHierarchy+" health="+FieldInt(e,"healthCurrent")+" pos="+e.transform.position);
                    foreach(var v in Object.FindObjectsOfType<PhysGrabObject>().Take(10))Info("LOOT "+v.name+" pos="+v.transform.position);
                }
                Info("DEV command "+text);
            }catch(Exception e){Logger.LogError("DEV "+e.Message);}
        }
        if(Time.unscaledTime-lastTelemetry<0.5f)return;lastTelemetry=Time.unscaledTime;
        var p=PlayerController.instance;var camera=View;
        var telemetry=new Telemetry{assets=Assets.Ready,status=Status,playing=Playing,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,equipped=Equipped,weapon=Weapon,dash=Dash,style=Style(LocalId).Score,sessionReady=SessionReady,players=NetworkActive&&PhotonNetwork.InRoom?PhotonNetwork.CurrentRoom.PlayerCount:1,velocity=p&&p.rb?p.rb.velocity:Vector3.zero,position=p?p.transform.position:Vector3.zero,cameraForward=camera?camera.transform.forward:Vector3.zero,sprintHeld=InputManager.instance&&InputManager.instance.GetAction(InputKey.Sprint).IsPressed(),sprintAllowed=InputManager.instance&&SemiFunc.InputHold(InputKey.Sprint),grounded=p&&p.CollisionController.Grounded,sliding=p&&p.Sliding,tumbling=Local&&FieldBool(Local,"isTumbling"),peakSpeed=peakSpeed,minVertical=minVertical,boosted=Style(LocalId).Boosted(Clock),godMode=Local&&FieldBool(Local.playerHealth,"godMode"),health=Local?FieldInt(Local.playerHealth,"health"):0,enemies=Object.FindObjectsOfType<EnemyHealth>().Select(e=>new Entity{name=e.name,health=FieldInt(e,"healthCurrent"),position=e.transform.position}).ToArray()};
        File.WriteAllText(Path.Combine(dir,"state.json"),JsonUtility.ToJson(telemetry,true));
    }
    IEnumerator CheckCoin() {
        Weapon=0;Equipped=true;RebuildView();
        Combat.Request("coin",LocalId,++coinSequence,View.transform.position+View.transform.forward*0.6f,View.transform.forward*Parry_Feedback.CoinForward+Vector3.up*Parry_Feedback.CoinUp,Clock);
        yield return new WaitForSeconds(0.2f);
        var point=Combat.LatestCoin(LocalId);if(!point.HasValue)yield break;
        CameraAim.Instance.SetPlayerAim(Quaternion.LookRotation(point.Value-View.transform.position),true);yield return null;FireCurrent();
    }
    IEnumerator CheckBoost(){Weapon=1;Equipped=true;RebuildView();while(Time.time<nextFire)yield return null;FireCurrent();ArmParry();}
    IEnumerator CheckParry() {
        var enemy=LivingEnemies().Where(e=>e.GetComponentInParent<EnemyParent>()?.enemyName==Presentation_Hud.TestEnemy).OrderBy(e=>Vector3.Distance(e.transform.position,Local.transform.position)).Select(e=>e.GetComponent<Enemy>()).FirstOrDefault();
        if(!enemy)yield break;
        TestPosition(enemy.transform.position+Vector3.back*1.8f+Vector3.up*0.7f);
        yield return new WaitForSeconds(0.5f);
        CameraAim.Instance.SetPlayerAim(Quaternion.LookRotation(enemy.transform.position+Vector3.up-View.transform.position),true);
        yield return null;
        int before=FieldInt(Local.playerHealth,"health");Dash=0;ArmParry();Local.playerHealth.Hurt(5,false,SemiFunc.EnemyGetIndex(enemy));
        Info("CHECK timed native incoming hit: health "+before+" -> "+FieldInt(Local.playerHealth,"health")+" dash="+Dash);
        yield return new WaitForSeconds(0.35f);before=FieldInt(Local.playerHealth,"health");Local.playerHealth.Hurt(5,false,SemiFunc.EnemyGetIndex(enemy));
        Info("CHECK expired window: health "+before+" -> "+FieldInt(Local.playerHealth,"health"));
    }
    [Serializable] class Telemetry { public bool assets,playing,equipped,sessionReady,sprintHeld,sprintAllowed,grounded,sliding,tumbling,boosted,godMode;public string status,scene;public int weapon,players,health;public float dash,style,peakSpeed,minVertical;public Vector3 velocity,position,cameraForward;public Entity[] enemies; }
    [Serializable] class Entity {public string name;public int health;public Vector3 position;}
}
[HarmonyPatch(typeof(PlayerHealth),nameof(PlayerHealth.Hurt))]
static class ParryPatch { static bool Prefix(PlayerHealth __instance,int enemyIndex)=>!Plugin.Instance || !Plugin.Instance.TryParry(__instance,enemyIndex); }
[HarmonyPatch(typeof(PlayerController),"FixedUpdate")]
static class SlidePatch {static void Postfix(PlayerController __instance){if(Plugin.Instance)Plugin.Instance.NativeSlide(__instance);}}
[HarmonyPatch(typeof(InputManager),nameof(InputManager.KeyHold))]
static class HoldPatch {
    static bool Prefix(InputKey key,ref bool __result){if(Plugin.Instance&&Plugin.Instance.Equipped&&Plugin.Instance.Playing&&(key==InputKey.Grab||key==InputKey.Rotate)){__result=false;return false;}return true;}
}
[HarmonyPatch(typeof(InputManager),nameof(InputManager.KeyDown))]
static class DownPatch {
    static bool Prefix(InputKey key,ref bool __result){if(Plugin.Instance&&Plugin.Instance.Equipped&&Plugin.Instance.Playing&&(key==InputKey.Grab||key==InputKey.Rotate)){__result=false;return false;}return true;}
}
internal sealed class StyleState {
    internal float Score;
    private string lastKind="";
    private double lastAward,lastTick,boostUntil;
    internal bool Boosted(double now)=>now<boostUntil;
    internal string Rank=>Score>=500?"SSS":Score>=400?"SS":Score>=300?"S":Score>=200?"A":Score>=100?"B":Score>=40?"C":"D";
    internal void Tick(double now){if(lastTick>0&&now-lastAward>Style_Momentum.Grace)Score=Mathf.Max(0,Score-(float)Math.Min(1,now-lastTick)*Style_Momentum.Decay);lastTick=now;}
    internal float Award(string kind,float points,double now){Tick(now);float actual=points*(kind==lastKind?Style_Momentum.StaleMultiplier:Style_Momentum.VarietyMultiplier);Score=Mathf.Min(Style_Momentum.MaxScore,Score+actual);lastKind=kind;lastAward=now;if(Score>=Style_Momentum.BoostThreshold)boostUntil=now+Style_Momentum.BoostSeconds;return actual;}
}
}


