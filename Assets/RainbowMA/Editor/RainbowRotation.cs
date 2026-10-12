using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace RainbowMA.Editor
{
    public static class RainbowRotation
    {
        public const string Parameter = "RainbowMA/Rotate";
        public const string SpeedParameter = "RainbowMA/RotationSpeed";
        const string Zero = "RainbowMA/StoppedVelocity";
        static readonly Regex LocomotionNames = new Regex("walk|run|strafe|locomotion|jump|hop|fall|land",RegexOptions.IgnoreCase);
        static bool IsVelocity(string name) => name=="VelocityX" || name=="VelocityY" || name=="VelocityZ" || name=="VelocityMagnitude" || name=="AngularY" || name=="MovementX" || name=="MovementZ" || name=="TrackMoveX" || name=="TrackMoveZ";

        public static void SetMatrices(Material material,GameObject avatar,Renderer renderer)
        {
            var matrix=avatar.transform.worldToLocalMatrix*renderer.localToWorldMatrix;
            for(int i=0;i<3;i++)
            {
                material.SetVector("_RainbowObjectToAvatar"+i,matrix.GetRow(i));
                material.SetVector("_RainbowAvatarToObject"+i,matrix.inverse.GetRow(i));
            }
            material.SetFloat("_RainbowRotationAngle",0);
        }

        public static void Generate(GameObject avatar,RainbowAvatar config,AnimatorController fx,Renderer[] renderers)
        {
            PatchBase(avatar,config);
            fx.AddParameter(Parameter,AnimatorControllerParameterType.Float);
            fx.AddParameter(new AnimatorControllerParameter { name=SpeedParameter,type=AnimatorControllerParameterType.Float,defaultFloat=1f/Mathf.Max(1,config.rotationMaxSpeedMultiplier) });
            fx.AddLayer("Rainbow additive visual rotation");
            var layers=fx.layers; var index=layers.Length-1; layers[index].defaultWeight=1; fx.layers=layers;
            var sm=fx.layers[index].stateMachine;
            foreach(bool on in new[]{false,true})
            {
                var clip=new AnimationClip { name=on?"Rainbow rotation ON":"Rainbow rotation OFF" };
                float duration=on?Mathf.Max(.1f,config.rotationSeconds):1f/60f;
                foreach(var renderer in renderers)
                {
                    var curve=AnimationCurve.Linear(0,0,duration,on?360:0);
                    clip.SetCurve(AnimationUtility.CalculateTransformPath(renderer.transform,avatar.transform),renderer.GetType(),"material._RainbowRotationAngle",curve);
                }
                var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=on; AnimationUtility.SetAnimationClipSettings(clip,settings);
                var state=sm.AddState(clip.name); state.motion=clip; state.writeDefaultValues=false;
                if(on) { state.speed=Mathf.Max(1,config.rotationMaxSpeedMultiplier); state.speedParameter=SpeedParameter; state.speedParameterActive=true; }
                var t=sm.AddAnyStateTransition(state); t.hasExitTime=false; t.duration=0; t.canTransitionToSelf=false;
                t.AddCondition(on?AnimatorConditionMode.Greater:AnimatorConditionMode.Less,.5f,Parameter);
                if(!on) sm.defaultState=state;
            }
            // Shader displacement must remain inside the renderer's CPU culling volume.
            var bounds=renderers[0].bounds;
            foreach(var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            float radius=bounds.extents.magnitude+Vector3.Distance(bounds.center,avatar.transform.position);
            foreach(var r in renderers)
            {
                var center=r.transform.InverseTransformPoint(avatar.transform.position);
                var scale=r.transform.lossyScale;
                var extents=new Vector3(radius/Mathf.Max(.001f,Mathf.Abs(scale.x)),radius/Mathf.Max(.001f,Mathf.Abs(scale.y)),radius/Mathf.Max(.001f,Mathf.Abs(scale.z)));
                r.localBounds=new Bounds(center,extents*2);
                if(r is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen=true;
            }
        }

        // Clone the whole controller graph, including externally stored trees. Clips remain read-only.
        public static T CloneGraph<T>(T source) where T:Object => (T)Clone(source,new Dictionary<Object,Object>());
        static Object Clone(Object source,Dictionary<Object,Object> copies)
        {
            if(!source) return null;
            if(!(source is AnimatorController || source is AnimatorStateMachine || source is AnimatorState || source is AnimatorTransitionBase || source is BlendTree || source is StateMachineBehaviour)) return source;
            if(copies.TryGetValue(source,out var existing)) return existing;
            var clone=Object.Instantiate(source); clone.name=source.name; copies[source]=clone;
            var serialized=new SerializedObject(clone); var original=new SerializedObject(source); var property=original.GetIterator();
            while(property.Next(true))
                if(property.propertyType==SerializedPropertyType.ObjectReference && property.name!="m_Script") serialized.FindProperty(property.propertyPath).objectReferenceValue=Clone(property.objectReferenceValue,copies);
            serialized.ApplyModifiedPropertiesWithoutUndo(); return clone;
        }
        static IEnumerable<AnimatorState> States(AnimatorStateMachine sm)
        {
            foreach(var state in sm.states) yield return state.state;
            foreach(var child in sm.stateMachines) foreach(var state in States(child.stateMachine)) yield return state;
        }
        static bool HasVelocity(Motion motion)
        {
            if(!(motion is BlendTree tree)) return false;
            bool twoDimensional=tree.blendType==BlendTreeType.SimpleDirectional2D || tree.blendType==BlendTreeType.FreeformDirectional2D || tree.blendType==BlendTreeType.FreeformCartesian2D;
            return (tree.blendType!=BlendTreeType.Direct && IsVelocity(tree.blendParameter)) || (twoDimensional && IsVelocity(tree.blendParameterY)) || tree.children.Any(c=>(tree.blendType==BlendTreeType.Direct && IsVelocity(c.directBlendParameter)) || HasVelocity(c.motion));
        }
        static Motion Freeze(Motion motion)
        {
            if(!(motion is BlendTree source)) return motion;
            var tree=Object.Instantiate(source); tree.name=source.name+" stationary";
            if(IsVelocity(tree.blendParameter)) tree.blendParameter=Zero;
            if(IsVelocity(tree.blendParameterY)) tree.blendParameterY=Zero;
            var children=tree.children;
            for(int i=0;i<children.Length;i++)
            {
                children[i].motion=Freeze(children[i].motion);
                if(IsVelocity(children[i].directBlendParameter)) children[i].directBlendParameter=Zero;
            }
            tree.children=children; return tree;
        }
        static Motion Wrap(Motion original,Motion stopped) => new BlendTree {
            name=original.name+" rotation locomotion gate",blendType=BlendTreeType.Simple1D,blendParameter=Parameter,useAutomaticThresholds=false,
            children=new[]{new ChildMotion { motion=original,threshold=0,timeScale=1 },new ChildMotion { motion=stopped,threshold=1,timeScale=1 }}
        };
        public static void PatchBase(GameObject avatar,RainbowAvatar config)
        {
            var descriptor=avatar.GetComponent<VRCAvatarDescriptor>();
            if(!descriptor) throw new InvalidOperationException("Rainbow MA rotation requires a VRChat Avatar Descriptor.");
            var layers=descriptor.baseAnimationLayers;
            int index=Array.FindIndex(layers,l=>l.type==VRCAvatarDescriptor.AnimLayerType.Base);
            if(index<0) throw new InvalidOperationException("Rainbow MA rotation: Avatar Descriptor has no Base layer.");
            var original=(!descriptor.customizeAnimationLayers || layers[index].isDefault || !layers[index].animatorController)
                ?AssetDatabase.LoadAssetAtPath<AnimatorController>("Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/Animation/Controllers/vrc_AvatarV3LocomotionLayer.controller")
                :layers[index].animatorController as AnimatorController;
            if(!original) throw new InvalidOperationException("Rainbow MA rotation: Base must use an AnimatorController. SDK default locomotion controller was not found.");
            var controller=CloneGraph(original); controller.name=original.name+" Rainbow rotation";
            if(!controller.parameters.Any(p=>p.name==Parameter)) controller.AddParameter(Parameter,AnimatorControllerParameterType.Float);
            if(!controller.parameters.Any(p=>p.name==Zero)) controller.AddParameter(Zero,AnimatorControllerParameterType.Float);
            var states=controller.layers.SelectMany(l=>States(l.stateMachine)).ToArray();
            var standing=states.FirstOrDefault(s=>s.name.Equals("Standing",StringComparison.OrdinalIgnoreCase) || s.name.Equals("Idle",StringComparison.OrdinalIgnoreCase));
            Motion neutral=standing && standing.motion?Freeze(standing.motion):AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/Animation/ProxyAnim/proxy_stand_still3.anim");
            if(!neutral) throw new InvalidOperationException("Rainbow MA rotation: stationary animation not found.");
            int patched=0;
            foreach(var state in states)
            {
                if(!state.motion) continue;
                bool velocity=HasVelocity(state.motion);
                bool named=LocomotionNames.IsMatch(state.name) || (config.additionalLocomotionStates??Array.Empty<string>()).Contains(state.name);
                if(!velocity && !named) continue;
                bool airborne=Regex.IsMatch(state.name,"jump|hop|fall|land",RegexOptions.IgnoreCase) || (config.additionalLocomotionStates??Array.Empty<string>()).Contains(state.name);
                state.motion=Wrap(state.motion,velocity && !airborne?Freeze(state.motion):neutral); patched++;
            }
            if(patched==0) Debug.LogWarning("Rainbow MA: custom Base locomotion states were not detected. Add their exact names to Additional Locomotion States.");
            layers[index].animatorController=controller; layers[index].isDefault=false;
            descriptor.baseAnimationLayers=layers; descriptor.customizeAnimationLayers=true;
            Debug.Log("Rainbow MA: gated "+patched+" Base movement/jump motions on the build copy.");
        }
    }
}
