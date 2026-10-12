using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

[assembly: ExportsPlugin(typeof(RainbowMA.Editor.RainbowBuild))]
namespace RainbowMA.Editor
{
    public sealed class RainbowBuild : Plugin<RainbowBuild>
    {
        public override string QualifiedName => "local.rainbow-ma";
        public override string DisplayName => "Rainbow MA";
        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).BeforePlugin("nadena.dev.modular-avatar")
                .Run("Generate rainbow materials and MA controls", ctx => {
                    var configs = ctx.AvatarRootObject.GetComponentsInChildren<RainbowAvatar>(true);
                    if (configs.Length > 1) throw new InvalidOperationException("Use one Rainbow MA prefab per avatar.");
                    if (configs.Length == 1 && configs[0].gameObject.activeInHierarchy) Generate(ctx.AvatarRootObject, configs[0]);
                    foreach (var c in configs) Object.DestroyImmediate(c);
                });
        }

        public static void Generate(GameObject avatar, RainbowAvatar config)
        {
            // Imported prefab references can be lost when the custom shader importer is registered late.
            // Resolve on the build copy; never modify the source prefab or its materials.
            config.opaqueShader=ResolveShader(config.opaqueShader,"RainbowOpaque","RainbowMA/lilToon Opaque");
            config.cutoutShader=ResolveShader(config.cutoutShader,"RainbowCutout","RainbowMA/lilToon Cutout");
            config.transparentShader=ResolveShader(config.transparentShader,"RainbowTransparent","RainbowMA/lilToon Transparent");
            config.wireShader=ResolveShader(config.wireShader,"WhiteWire","RainbowMA/White Shaded Wire");
            if (!config.opaqueShader || !config.cutoutShader || !config.transparentShader || !config.wireShader)
                throw new InvalidOperationException("Rainbow MA shader import is incomplete. Install lilToon 2.3.4 and reimport Assets/RainbowMA/Shaders. Missing: "+string.Join(", ",new[]{config.opaqueShader?null:"Opaque",config.cutoutShader?null:"Cutout",config.transparentShader?null:"Transparent",config.wireShader?null:"Wire"}.Where(s=>s!=null)));
            if (config.queuePresets == null || config.queuePresets.Length < 1 || config.queuePresets.Length > 256
                || config.queuePresets.Any(q => q < 0 || q > 5000))
                throw new InvalidOperationException("Queue presets must contain 1–256 values in 0–5000 (0 preserves the source queue).");
            var renderers = avatar.GetComponentsInChildren<Renderer>(true)
                .Where(r => !r.GetComponentsInParent<Transform>(true).Any(t => t.CompareTag("EditorOnly"))).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Rainbow MA: no mesh renderers found.");
            if(renderers.GroupBy(r => AnimationUtility.CalculateTransformPath(r.transform,avatar.transform)+"|"+r.GetType().FullName).Any(g => g.Count()>1))
                throw new InvalidOperationException("Renderer animation paths must be unique. Rename duplicate sibling objects or separate renderers.");
            var bounds = new Bounds(); bool first = true;
            foreach (var r in renderers)
            {
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = b.center + Vector3.Scale(b.extents, new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    p = avatar.transform.InverseTransformPoint(p);
                    if (first) { bounds = new Bounds(p,Vector3.zero); first=false; } else bounds.Encapsulate(p);
                }
            }
            float height = Mathf.Max(bounds.size.y, 0.01f);
            string prefix = "RainbowMA/";
            var controller = new AnimatorController { name="RainbowMA FX" };
            controller.AddParameter(prefix+"Queue",AnimatorControllerParameterType.Int);
            controller.AddParameter(prefix+"Wire",AnimatorControllerParameterType.Bool);
            controller.AddParameter(prefix+"Lights",AnimatorControllerParameterType.Bool);
            var ps = controller.parameters;
            ps[2].defaultBool=config.lightsOnByDefault; controller.parameters=ps;
            var setter = config.gameObject.AddComponent<ModularAvatarMaterialSetter>();
            var slots = new List<(Renderer renderer,int index,Material[] rainbow,Material[] wire)>();
            foreach (var renderer in renderers)
            {
                var row = (avatar.transform.worldToLocalMatrix * renderer.localToWorldMatrix).GetRow(1);
                bool excludeOutline=IsOutlineExcluded(avatar,config,renderer);
                for (int s=0;s<renderer.sharedMaterials.Length;s++)
                {
                    var source=renderer.sharedMaterials[s]; if (!source) continue;
                    var rainbow=new Material[config.queuePresets.Length];
                    var wire=new Material[rainbow.Length];
                    for(int q=0;q<rainbow.Length;q++)
                    {
                        bool transparent = source.GetTag("RenderType",false,"")=="Transparent" || source.shader.name.Contains("Transparent");
                        bool cutout = source.GetTag("RenderType",false,"")=="TransparentCutout" || source.shader.name.Contains("Cutout");
                        var shader=transparent?config.transparentShader:cutout?config.cutoutShader:config.opaqueShader;
                        var mat=new Material(source) { name=source.name+" Rainbow Q"+q, shader=shader };
                        if (!source.shader.name.Contains("lilToon") && !source.shader.name.Contains("RainbowMA"))
                        {
                            var tex=source.mainTexture; var col=source.HasProperty("_Color")?source.color:Color.white;
                            mat.SetTexture("_MainTex",tex); mat.SetColor("_Color",col);
                            mat.SetFloat("_SrcBlend",transparent?5:1); mat.SetFloat("_DstBlend",transparent?10:0);
                            mat.SetFloat("_ZWrite",transparent?0:1);
                        }
                        mat.renderQueue=config.queuePresets[q]==0?source.renderQueue:config.queuePresets[q];
                        mat.SetVector("_RainbowRowY",row); mat.SetFloat("_RainbowMinY",bounds.min.y);
                        mat.SetFloat("_RainbowHeight",height); mat.SetFloat("_RainbowEmission",config.emission);
                        mat.SetFloat("_RainbowOutlineEmission",excludeOutline?0:config.outlineEmission);
                        mat.SetFloat("_RainbowSpeed",Mathf.Max(.01f,config.rainbowSpeed));
                        mat.SetFloat("_RainbowOutlinePhase",config.outlinePhaseOffset);
                        mat.SetFloat("_OutlineWidth",excludeOutline?0:config.outlineWidth); mat.SetColor("_OutlineColor",Color.white);
                        if(excludeOutline) { mat.SetFloat("_OutlineColorMask",0); mat.SetFloat("_OutlineZWrite",0); }
                        mat.SetFloat("_OutlineEnableLighting",0); mat.SetFloat("_OutlineZTest",4);
                        mat.SetTexture("_OutlineWidthMask",null); mat.SetTexture("_OutlineTex",null);
                        rainbow[q]=mat;
                        wire[q]=new Material(config.wireShader) { name=source.name+" WhiteWire Q"+q, renderQueue=mat.renderQueue };
                        wire[q].SetVector("_RainbowRowY",row); wire[q].SetFloat("_RainbowMinY",bounds.min.y);
                        wire[q].SetFloat("_RainbowHeight",height); wire[q].SetFloat("_RainbowSpeed",Mathf.Max(.01f,config.rainbowSpeed));
                        wire[q].SetFloat("_RainbowWirePhase",config.wirePhaseOffset); wire[q].SetFloat("_RainbowWireEmission",config.wireEmission);
                    }
                    foreach(var material in rainbow.Concat(wire)) RainbowRotation.SetMatrices(material,avatar,renderer);
                    setter.Objects.Add(new MaterialSwitchObject { Object=new AvatarObjectReference(renderer.gameObject), MaterialIndex=s, Material=rainbow[0] });
                    slots.Add((renderer,s,rainbow,wire));
                }
            }
            controller.AddLayer("Rainbow materials");
            var layers=controller.layers; layers[0].defaultWeight=1; controller.layers=layers;
            var sm=controller.layers[0].stateMachine;
            for(int w=0;w<2;w++) for(int q=0;q<config.queuePresets.Length;q++)
            {
                var clip=new AnimationClip { name="Rainbow Queue "+q+" Wire "+w };
                foreach(var slot in slots)
                {
                    string path=AnimationUtility.CalculateTransformPath(slot.renderer.transform,avatar.transform);
                    AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve(path,slot.renderer.GetType(),"m_Materials.Array.data["+slot.index+"]"),
                        new[]{new ObjectReferenceKeyframe { time=0,value=w==0?slot.rainbow[q]:slot.wire[q] }});
                }
                var state=sm.AddState(clip.name); state.motion=clip; state.writeDefaultValues=false;
                var t=sm.AddAnyStateTransition(state); t.hasExitTime=false; t.duration=0; t.canTransitionToSelf=false;
                t.AddCondition(AnimatorConditionMode.Equals,q,prefix+"Queue");
                t.AddCondition(w==0?AnimatorConditionMode.IfNot:AnimatorConditionMode.If,0,prefix+"Wire");
                if(q==0 && w==0) sm.defaultState=state;
            }
            var lights=new GameObject("Rainbow Lights"); lights.transform.SetParent(config.transform,false);
            // Place along avatar height; object remains underneath the drop-in prefab.
            for(int i=0;i<7;i++)
            {
                var go=new GameObject("Spectrum "+i); go.transform.SetParent(lights.transform,false);
                var p=new Vector3(bounds.center.x,bounds.max.y-height*i/6f,bounds.center.z);
                go.transform.position=avatar.transform.TransformPoint(p);
                var light=go.AddComponent<Light>(); light.type=LightType.Point;
                light.color=Color.HSVToRGB(new[]{0f,1f/12f,1f/6f,1f/3f,.5f,2f/3f,.77f}[i],1,1); light.intensity=config.lightIntensity;
                light.range=config.lightRange; light.shadows=LightShadows.None; light.renderMode=LightRenderMode.ForcePixel;
            }
            lights.SetActive(config.lightsOnByDefault);
            controller.AddLayer("Rainbow lights"); layers=controller.layers; layers[1].defaultWeight=1; controller.layers=layers;
            var lsm=controller.layers[1].stateMachine;
            for(int on=0;on<2;on++)
            {
                var clip=new AnimationClip { name="Rainbow lights "+on };
                clip.SetCurve(AnimationUtility.CalculateTransformPath(lights.transform,avatar.transform),typeof(GameObject),"m_IsActive",AnimationCurve.Constant(0,1/60f,on));
                if(on==1)
                {
                    float duration=1f/Mathf.Max(.01f,config.rainbowSpeed);
                    for(int i=0;i<7;i++) for(int channel=0;channel<3;channel++)
                    {
                        var curve=new AnimationCurve();
                        for(int key=0;key<=72;key++)
                        {
                            var color=Color.HSVToRGB(Mathf.Repeat(i/6f-key/72f,1),1,1);
                            curve.AddKey(key/72f*duration,color[channel]);
                        }
                        for(int key=0;key<curve.length;key++) { AnimationUtility.SetKeyLeftTangentMode(curve,key,AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve,key,AnimationUtility.TangentMode.Linear); }
                        clip.SetCurve(AnimationUtility.CalculateTransformPath(lights.transform.GetChild(i),avatar.transform),typeof(Light),"m_Color."+new[]{"r","g","b"}[channel],curve);
                    }
                    var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true; AnimationUtility.SetAnimationClipSettings(clip,settings);
                }
                var state=lsm.AddState(clip.name); state.motion=clip; state.writeDefaultValues=false;
                var t=lsm.AddAnyStateTransition(state); t.duration=0; t.hasExitTime=false; t.canTransitionToSelf=false;
                t.AddCondition(on==0?AnimatorConditionMode.IfNot:AnimatorConditionMode.If,0,prefix+"Lights");
                if ((on==1)==config.lightsOnByDefault) lsm.defaultState=state;
            }
            RainbowRotation.Generate(avatar,config,controller,renderers);
            RainbowColliderJump.Generate(avatar,config,controller);
            var merge=config.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            merge.animator=controller; merge.pathMode=MergeAnimatorPathMode.Absolute; merge.matchAvatarWriteDefaults=true;
            var parameters=config.gameObject.AddComponent<ModularAvatarParameters>();
            parameters.parameters.Add(new ParameterConfig { nameOrPrefix=prefix+"Queue",syncType=ParameterSyncType.Int,saved=true,hasExplicitDefaultValue=true });
            parameters.parameters.Add(new ParameterConfig { nameOrPrefix=prefix+"Wire",syncType=ParameterSyncType.Bool,saved=true,hasExplicitDefaultValue=true });
            parameters.parameters.Add(new ParameterConfig { nameOrPrefix=prefix+"Lights",syncType=ParameterSyncType.Bool,saved=true,hasExplicitDefaultValue=true,defaultValue=config.lightsOnByDefault?1:0 });
            parameters.parameters.Add(new ParameterConfig { nameOrPrefix=prefix+"Rotate",syncType=ParameterSyncType.Float,saved=false,hasExplicitDefaultValue=true });
            parameters.parameters.Add(new ParameterConfig { nameOrPrefix=RainbowRotation.SpeedParameter,syncType=ParameterSyncType.Float,saved=true,hasExplicitDefaultValue=true,defaultValue=1f/Mathf.Max(1,config.rotationMaxSpeedMultiplier) });
            parameters.parameters.Add(new ParameterConfig { nameOrPrefix=RainbowColliderJump.Parameter,syncType=ParameterSyncType.Bool,localOnly=true,saved=false,hasExplicitDefaultValue=true });
            var queue=ScriptableObject.CreateInstance<VRCExpressionsMenu>(); queue.name="Render Queue";
            var queuePage=queue;
            for(int i=0;i<config.queuePresets.Length;i++)
            {
                if(queuePage.controls.Count==7 && config.queuePresets.Length-i>1)
                {
                    var next=ScriptableObject.CreateInstance<VRCExpressionsMenu>(); next.name="Render Queue "+(i+1);
                    queuePage.controls.Add(new VRCExpressionsMenu.Control { name="次へ",type=VRCExpressionsMenu.Control.ControlType.SubMenu,subMenu=next });
                    queuePage=next;
                }
                queuePage.controls.Add(new VRCExpressionsMenu.Control { name=config.queuePresets[i]==0?"Original Queue":"Queue "+config.queuePresets[i],type=VRCExpressionsMenu.Control.ControlType.Toggle,parameter=new VRCExpressionsMenu.Control.Parameter { name=prefix+"Queue" },value=i });
            }
            var menu=ScriptableObject.CreateInstance<VRCExpressionsMenu>(); menu.name="Rainbow MA";
            menu.controls.Add(new VRCExpressionsMenu.Control { name="Render Queue",type=VRCExpressionsMenu.Control.ControlType.SubMenu,subMenu=queue });
            menu.controls.Add(new VRCExpressionsMenu.Control { name="Gaming Rainbow Wire",type=VRCExpressionsMenu.Control.ControlType.Toggle,parameter=new VRCExpressionsMenu.Control.Parameter { name=prefix+"Wire" },value=1 });
            menu.controls.Add(new VRCExpressionsMenu.Control { name="Rainbow Lights",type=VRCExpressionsMenu.Control.ControlType.Toggle,parameter=new VRCExpressionsMenu.Control.Parameter { name=prefix+"Lights" },value=1 });
            menu.controls.Add(new VRCExpressionsMenu.Control { name="回転をON",type=VRCExpressionsMenu.Control.ControlType.Toggle,parameter=new VRCExpressionsMenu.Control.Parameter { name=prefix+"Rotate" },value=1 });
            menu.controls.Add(new VRCExpressionsMenu.Control { name="回転速度",type=VRCExpressionsMenu.Control.ControlType.RadialPuppet,subParameters=new[]{new VRCExpressionsMenu.Control.Parameter { name=RainbowRotation.SpeedParameter }} });
            menu.controls.Add(new VRCExpressionsMenu.Control { name="コライダージャンプ",type=VRCExpressionsMenu.Control.ControlType.Toggle,parameter=new VRCExpressionsMenu.Control.Parameter { name=RainbowColliderJump.Parameter },value=1 });
            var rootMenu=ScriptableObject.CreateInstance<VRCExpressionsMenu>(); rootMenu.name="Rainbow MA Installer";
            rootMenu.controls.Add(new VRCExpressionsMenu.Control { name="Rainbow MA",type=VRCExpressionsMenu.Control.ControlType.SubMenu,subMenu=menu });
            config.gameObject.AddComponent<ModularAvatarMenuInstaller>().menuToAppend=rootMenu;
            Debug.Log("Rainbow MA generated: "+slots.Count+" material slots, "+config.queuePresets.Length+" queue presets, 7 point lights.");
        }

        static Shader ResolveShader(Shader assigned,string assetName,string shaderName)
        {
            if(assigned) return assigned;
            var shader=Shader.Find(shaderName);
            if(shader) return shader;
            foreach(var extension in new[]{".lilcontainer",".shader"})
            {
                shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RainbowMA/Shaders/"+assetName+extension);
                if(shader) return shader;
            }
            foreach(var guid in AssetDatabase.FindAssets(assetName))
            {
                shader=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
                if(shader && shader.name==shaderName) return shader;
            }
            return null;
        }

        public static bool IsOutlineExcluded(GameObject avatar,RainbowAvatar config,Renderer renderer)
        {
            if(config.outlineExcludedObjects==null) return false;
            foreach(var target in config.outlineExcludedObjects)
            {
                if(!target || (target.transform!=avatar.transform && !target.transform.IsChildOf(avatar.transform))) continue;
                if(renderer.gameObject==target || (config.outlineExcludeChildren && renderer.transform.IsChildOf(target.transform))) return true;
            }
            return false;
        }
    }
}

