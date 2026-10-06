using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UltraHaul.Design;
using Object=UnityEngine.Object;

namespace UltraHaul {
internal sealed class Assets {
    readonly Plugin plugin;
    internal bool Ready;
    AssetBundle models,textures,sounds;
    internal Mesh V1,Revolver,Cylinder,Shotgun,Coin;
    internal GameObject V1Prefab;
    GameObject revolverPrefab,shotgunPrefab;
    internal AnimationClip[] V1Clips;
    AnimationClip[] revolverClips,shotgunClips;
    AudioClip revolverSound,shotgunSound,coinSound;
    Texture2D revolverTexture,shotgunTexture;
    readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    internal Assets(Plugin plugin){this.plugin=plugin;}
    internal IEnumerator Load(string configured) {
        string game=FindGame(configured);
        if(game==null){plugin.Status="ULTRAKILL is required. Install your Steam copy before Play.";plugin.Info(plugin.Status);yield break;}
        var folder=Path.Combine(game,"ULTRAKILL_Data","StreamingAssets","aa","StandaloneWindows64","assets_assets_assets");
        var request=AssetBundle.LoadFromFileAsync(Path.Combine(folder,"models.bundle"));yield return request;models=request.assetBundle;
        if(!models){plugin.Status="Could not load ULTRAKILL models for this game version.";yield break;}
        var textureRequest=AssetBundle.LoadFromFileAsync(Path.Combine(folder,"textures.bundle"));yield return textureRequest;textures=textureRequest.assetBundle;
        var meshRequest=models.LoadAssetWithSubAssetsAsync<Mesh>(Assets_V1.Key);yield return meshRequest;V1=meshRequest.allAssets.OfType<Mesh>().FirstOrDefault(m=>m.name==Assets_V1.MeshName);
        V1Prefab=models.LoadAsset<GameObject>(Assets_V1.Key);
        V1Clips=models.LoadAssetWithSubAssets<AnimationClip>(Assets_V1.Key);
        var revolverRequest=models.LoadAssetWithSubAssetsAsync<Mesh>(Assets_Revolver.Key);yield return revolverRequest;
        Revolver=revolverRequest.allAssets.OfType<Mesh>().FirstOrDefault(m=>m.name==Assets_Revolver.MeshName);Cylinder=revolverRequest.allAssets.OfType<Mesh>().FirstOrDefault(m=>m.name=="Revolver_Cylinder");
        revolverPrefab=models.LoadAsset<GameObject>(Assets_Revolver.Key);revolverClips=models.LoadAssetWithSubAssets<AnimationClip>(Assets_Revolver.Key);
        var shotgunRequest=models.LoadAssetWithSubAssetsAsync<Mesh>(Assets_Shotgun.Key);yield return shotgunRequest;Shotgun=shotgunRequest.allAssets.OfType<Mesh>().FirstOrDefault(m=>m.name==Assets_Shotgun.MeshName);
        shotgunPrefab=models.LoadAsset<GameObject>(Assets_Shotgun.Key);shotgunClips=models.LoadAssetWithSubAssets<AnimationClip>(Assets_Shotgun.Key);
        var coinRequest=models.LoadAssetWithSubAssetsAsync<Mesh>(Assets_Coin.Key);yield return coinRequest;Coin=coinRequest.allAssets.OfType<Mesh>().FirstOrDefault(m=>m.name==Assets_Coin.MeshName);
        if(textures){revolverTexture=textures.LoadAsset<Texture2D>(Assets_Revolver.TextureKey);shotgunTexture=textures.LoadAsset<Texture2D>(Assets_Shotgun.TextureKey);}
        var soundRequest=AssetBundle.LoadFromFileAsync(Path.Combine(folder,"sounds.bundle"));yield return soundRequest;sounds=soundRequest.assetBundle;
        if(sounds){revolverSound=sounds.LoadAsset<AudioClip>(Audio_Revolver.Key);shotgunSound=sounds.LoadAsset<AudioClip>(Audio_Shotgun.Key);coinSound=sounds.LoadAsset<AudioClip>(Audio_Coin.Key);}
        Ready=V1&&Revolver&&Shotgun&&Coin&&V1Prefab&&revolverPrefab&&shotgunPrefab&&revolverTexture&&shotgunTexture&&revolverSound&&shotgunSound&&coinSound
            &&V1Clips.Any(c=>c.name==Assets_V1.IdleClip)&&V1Clips.Any(c=>c.name==Assets_V1.WalkClip)
            &&revolverClips.Any(c=>c.name==Assets_Revolver.IdleClip)&&revolverClips.Any(c=>c.name==Assets_Revolver.FireClip)
            &&shotgunClips.Any(c=>c.name==Assets_Shotgun.IdleClip)&&shotgunClips.Any(c=>c.name==Assets_Shotgun.FireClip);
        plugin.Info("ASSETS textures="+(bool)revolverTexture+","+(bool)shotgunTexture+" sounds="+(bool)revolverSound+","+(bool)shotgunSound+","+(bool)coinSound);
        plugin.Status=Ready?"V1 + revolver + shotgun loaded from your ULTRAKILL copy.":"ULTRAKILL mesh mapping failed; release is not compatible with this version yet.";
        plugin.Info(plugin.Status+" V1="+(V1?V1.vertexCount:0)+" Revolver="+(Revolver?Revolver.vertexCount:0)+" Shotgun="+(Shotgun?Shotgun.vertexCount:0));
    }
    static string FindGame(string configured) {
        var candidates=new List<string>();
        var supplied=Environment.GetEnvironmentVariable(Lifecycle_Bootstrap.AssetEnv);if(!string.IsNullOrEmpty(supplied))candidates.Add(supplied);
        if(!string.IsNullOrEmpty(configured))candidates.Add(configured);
        var primary=Directory.GetParent(Application.dataPath)?.FullName;
        if(primary!=null)candidates.Add(Path.Combine(Directory.GetParent(primary).FullName,"ULTRAKILL"));
        var roots=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try {
            var registry=Type.GetType("Microsoft.Win32.Registry, mscorlib");
            var user=registry?.GetField("CurrentUser")?.GetValue(null);
            var key=user?.GetType().GetMethod("OpenSubKey",new[]{typeof(string)})?.Invoke(user,new object[]{@"Software\Valve\Steam"});
            var steam=key?.GetType().GetMethod("GetValue",new[]{typeof(string)})?.Invoke(key,new object[]{"SteamPath"}) as string;
            if(steam!=null)roots.Add(steam);
        }catch{}
        roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam"));
        foreach(var root in roots.ToArray()) {
            string manifest=Path.Combine(root,"steamapps","libraryfolders.vdf");
            if(File.Exists(manifest))foreach(Match m in Regex.Matches(File.ReadAllText(manifest),"\"path\"\\s+\"([^\"]+)\""))roots.Add(m.Groups[1].Value.Replace(@"\\",@"\"));
        }
        foreach(var root in roots)candidates.Add(Path.Combine(root,"steamapps","common","ULTRAKILL"));
        return candidates.FirstOrDefault(path=>File.Exists(Path.Combine(path,"ULTRAKILL.exe"))&&Directory.Exists(Path.Combine(path,"ULTRAKILL_Data")));
    }
    static Material Material(Color color,Texture texture=null) {
        var shader=(texture?Shader.Find("Unlit/Texture"):Shader.Find("Sprites/Default"))??Shader.Find("Standard");
        var mat=new Material(shader);mat.color=color;if(texture)mat.mainTexture=texture;return mat;
    }
    Material Cached(string key,Color color,Texture texture=null){if(!materials.TryGetValue(key,out var mat)||!mat)materials[key]=mat=Material(color,texture);return mat;}
    static GameObject MeshObject(string name,Mesh mesh,Material material,Transform parent) {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;return g;
    }
    internal GameObject CoinVisual(){var g=MeshObject("UltraHaul ULTRAKILL coin",Coin,Cached("coin",new Color(1,0.75f,0.1f)),null);float scale=Assets_Coin.Height/Mathf.Max(Coin.bounds.size.x,Mathf.Max(Coin.bounds.size.y,Coin.bounds.size.z));g.transform.localScale=Vector3.one*scale;return g;}
    internal GameObject Gun(int gun,Transform parent) {
        var root=new GameObject("UltraHaul viewmodel");root.transform.SetParent(parent,false);
        var prefab=gun==0?revolverPrefab:shotgunPrefab;
        if(prefab) {
            var model=Object.Instantiate(prefab,root.transform,false);model.name="ULTRAKILL native weapon rig";
            foreach(var animator in model.GetComponentsInChildren<Animator>())animator.enabled=false;
            var clips=gun==0?revolverClips:shotgunClips;
            clips.FirstOrDefault(c=>c.name==(gun==0?Assets_Revolver.IdleClip:Assets_Shotgun.IdleClip))?.SampleAnimation(model,0);
            foreach(var r in model.GetComponentsInChildren<Renderer>()) {
                bool arm=r.name.ToLowerInvariant().Contains("arm");
                var mat=Cached(arm?"arm":"gun"+gun,arm?ArmColor(gun==0?Assets_Revolver.ArmColor:Assets_Shotgun.ArmColor):Color.white,arm?null:gun==0?revolverTexture:shotgunTexture);
                r.sharedMaterials=Enumerable.Repeat(mat,Math.Max(1,r.sharedMaterials.Length)).ToArray();
            }
            Normalize(model,gun==0?Assets_Revolver.Height:Assets_Shotgun.Height,true);
            var animation=model.AddComponent<WeaponPose>();animation.Idle=clips.FirstOrDefault(c=>c.name==(gun==0?Assets_Revolver.IdleClip:Assets_Shotgun.IdleClip));animation.Fire=clips.FirstOrDefault(c=>c.name==(gun==0?Assets_Revolver.FireClip:Assets_Shotgun.FireClip));animation.CaptureImportTransforms();
            plugin.Info("Native weapon rig + idle/fire animations loaded.");return root;
        }
        var mesh=gun==0?Revolver:Shotgun;var material=Cached("gun"+gun,Color.white,gun==0?revolverTexture:shotgunTexture);
        var child=MeshObject(gun==0?"Marksman":"Shotgun",mesh,material,root.transform);
        float size=Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
        float scale=(gun==0?Assets_Revolver.Height:Assets_Shotgun.Height)/size;
        child.transform.localScale=Vector3.one*scale;child.transform.localRotation=Quaternion.Euler(gun==0?new Vector3(Assets_Revolver.RotateX,Assets_Revolver.RotateY,Assets_Revolver.RotateZ):new Vector3(Assets_Shotgun.RotateX,Assets_Shotgun.RotateY,Assets_Shotgun.RotateZ));
        child.transform.localPosition=-(child.transform.localRotation*mesh.bounds.center)*scale;
        if(gun==0&&Cylinder){var cyl=MeshObject("Cylinder",Cylinder,material,root.transform);cyl.transform.localScale=child.transform.localScale;cyl.transform.localRotation=child.transform.localRotation;cyl.transform.localPosition=child.transform.localPosition;}
        plugin.Info("Viewmodel created from local ULTRAKILL mesh: "+mesh.name);
        return root;
    }
    static Color ArmColor(string value){var c=value.Split(',').Select(v=>float.Parse(v,System.Globalization.CultureInfo.InvariantCulture)).ToArray();return new Color(c[0],c[1],c[2]);}
    internal void Dress(PlayerAvatar avatar) {
        var visuals=avatar.playerAvatarVisuals;var parent=visuals.meshParent?visuals.meshParent.transform:visuals.transform;
        var original=parent.GetComponentsInChildren<Renderer>(true);
        GameObject body;
        if(V1Prefab) {
            body=Object.Instantiate(V1Prefab,parent,false);body.name="UltraHaul V1";
            foreach(var animator in body.GetComponentsInChildren<Animator>())animator.enabled=false;
            V1Clips.FirstOrDefault(c=>c.name==Assets_V1.IdleClip)?.SampleAnimation(body,0);
            foreach(var renderer in body.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(Cached("v1",Color.white),renderer.sharedMaterials.Length).ToArray();
            body.transform.localScale=Vector3.one;
            Normalize(body,Assets_V1.Height,false);
        } else {
            body=MeshObject("UltraHaul V1",V1,Cached("v1",new Color(0.25f,0.6f,1)),parent);
            body.transform.localRotation=Quaternion.Euler(Assets_V1.RotateX,Assets_V1.RotateY,Assets_V1.RotateZ);
            body.transform.localScale=Vector3.one*(Assets_V1.Height/V1.bounds.size.z);
        }
        var controller=body.AddComponent<V1Body>();controller.Avatar=avatar;controller.Original=original;controller.Visible=original.Select(r=>r.enabled).ToArray();foreach(var r in original)r.enabled=false;controller.Idle=V1Clips.FirstOrDefault(c=>c.name==Assets_V1.IdleClip);controller.Run=V1Clips.FirstOrDefault(c=>c.name==Assets_V1.WalkClip);controller.CaptureImportTransforms();
        plugin.Info("V1 body attached to native player; imported prefab="+(bool)V1Prefab);
    }
    static void Normalize(GameObject model,float size,bool center) {
        var bounds=new Bounds();bool any=false;
        foreach(var renderer in model.GetComponentsInChildren<Renderer>()) {
            if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
            var b=renderer.bounds;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2) {
                var point=model.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
            }
        }
        if(!any)return;
        float scale=size/(center?Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)):bounds.size.y);
        model.transform.localScale=Vector3.one*scale;
        model.transform.localPosition=-(center?bounds.center:new Vector3(0,bounds.min.y,0))*scale;
    }
    internal void Sound(string kind) {
        var clip=kind=="revolver"?revolverSound:kind=="shotgun"?shotgunSound:coinSound;
        if(!clip||!plugin.View)return;
        var go=new GameObject("UltraHaul local weapon sound");go.transform.SetParent(plugin.View.transform,false);var audio=go.AddComponent<AudioSource>();audio.spatialBlend=0;audio.volume=kind=="revolver"?Audio_Revolver.Volume:kind=="shotgun"?Audio_Shotgun.Volume:Audio_Coin.Volume;audio.clip=clip;audio.Play();Object.Destroy(go,clip.length+0.1f);
    }
}
public class ImportedPose:MonoBehaviour {
    Transform[] nodes;Vector3[] scales;Vector3 offset;Quaternion rotation;
    internal void CaptureImportTransforms(){nodes=GetComponentsInChildren<Transform>(true);scales=nodes.Select(t=>t.localScale).ToArray();offset=transform.localPosition;rotation=transform.localRotation;}
    protected void Sample(AnimationClip clip,float time){if(!clip)return;clip.SampleAnimation(gameObject,time);for(int i=0;i<nodes.Length;i++)nodes[i].localScale=scales[i];transform.localPosition=offset;transform.localRotation=rotation;}
}
public sealed class WeaponPose:ImportedPose {
    internal AnimationClip Idle,Fire;
    void LateUpdate(){if(!Plugin.Instance)return;double elapsed=Plugin.Clock-Plugin.Instance.LastShot;var clip=Fire&&elapsed<Fire.length?Fire:Idle;if(clip)Sample(clip,clip==Fire?(float)elapsed:Time.time%Mathf.Max(0.01f,clip.length));}
}
public sealed class V1Body:ImportedPose {
    internal PlayerAvatar Avatar;
    internal Renderer[] Original;internal bool[] Visible;
    internal AnimationClip Idle,Run;
    Vector3 previous;
    void Start(){previous=transform.position;}
    void LateUpdate(){if(!Avatar)return;float speed=Vector3.Distance(transform.position,previous)/Mathf.Max(0.001f,Time.deltaTime);previous=transform.position;var clip=speed>1?Run:Idle;if(clip)Sample(clip,Time.time%Mathf.Max(0.01f,clip.length));bool ready=Plugin.Instance&&Plugin.Instance.SessionReady;bool local=Plugin.FieldBool(Avatar,"isLocal");for(int i=0;i<Original.Length;i++)if(Original[i])Original[i].enabled=!ready&&Visible[i];foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=ready&&!local&&!Plugin.FieldBool(Avatar,"isDisabled");}
}
}
