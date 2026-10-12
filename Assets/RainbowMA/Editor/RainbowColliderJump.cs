using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RainbowMA.Editor
{
    public static class RainbowColliderJump
    {
        public const string Parameter="RainbowMA/ColliderJump";
        public const string ObjectName="Rainbow MA Collider Jump";
        public static void Generate(GameObject avatar,RainbowAvatar config,AnimatorController fx)
        {
            var size=config.jumpColliderSize;
            if(size.x<=0 || size.y<=0 || size.z<=0) throw new InvalidOperationException("Collider Jump size must be positive.");
            if(avatar.transform.Find(ObjectName)) throw new InvalidOperationException("Rename existing object: "+ObjectName);
            var go=new GameObject(ObjectName); go.transform.SetParent(avatar.transform,false);
            go.transform.localPosition=config.jumpColliderPosition;
            var collider=go.AddComponent<BoxCollider>(); collider.center=config.jumpColliderCenter; collider.size=size; collider.isTrigger=false;
            go.SetActive(false);
            fx.AddParameter(Parameter,AnimatorControllerParameterType.Bool);
            fx.AddParameter("IsLocal",AnimatorControllerParameterType.Bool);
            fx.AddLayer("Rainbow local collider jump"); var layers=fx.layers; var index=layers.Length-1; layers[index].defaultWeight=1; fx.layers=layers;
            var sm=fx.layers[index].stateMachine;
            foreach(bool on in new[]{false,true})
            {
                var clip=new AnimationClip { name="Rainbow collider jump "+(on?"ON":"OFF") };
                clip.SetCurve(AnimationUtility.CalculateTransformPath(go.transform,avatar.transform),typeof(GameObject),"m_IsActive",AnimationCurve.Constant(0,1f/60f,on?1:0));
                var state=sm.AddState(clip.name); state.motion=clip; state.writeDefaultValues=false;
                var transition=sm.AddAnyStateTransition(state); transition.hasExitTime=false; transition.duration=0; transition.canTransitionToSelf=false;
                transition.AddCondition(on?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,Parameter);
                if(on) transition.AddCondition(AnimatorConditionMode.If,0,"IsLocal");
                else
                {
                    sm.defaultState=state;
                    var remote=sm.AddAnyStateTransition(state); remote.hasExitTime=false; remote.duration=0; remote.canTransitionToSelf=false;
                    remote.AddCondition(AnimatorConditionMode.IfNot,0,"IsLocal");
                }
            }
        }
    }
}
