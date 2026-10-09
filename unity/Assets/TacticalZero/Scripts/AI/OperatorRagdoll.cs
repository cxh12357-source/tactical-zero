using System.Collections.Generic;
using UnityEngine;

namespace TacticalZero
{
    public sealed class OperatorRagdoll : MonoBehaviour
    {
        readonly List<Rigidbody> bodies=new List<Rigidbody>();float sleepAt;
        public void Fall(Vector3 origin)
        {
            var bones=new Dictionary<string,Transform>();foreach(var t in GetComponentsInChildren<Transform>())bones[t.name]=t;
            string[] names={"pelvis","chest","head","upper_arm.L","forearm.L","upper_arm.R","forearm.R","thigh.L","shin.L","thigh.R","shin.R"};
            string[] ends={"spine","neck",null,"forearm.L","hand.L","forearm.R","hand.R","shin.L","foot.L","shin.R","foot.R"};
            string[] parents={null,"pelvis","chest","chest","upper_arm.L","chest","upper_arm.R","pelvis","thigh.L","pelvis","thigh.R"};
            var byName=new Dictionary<string,Rigidbody>();var colliders=new List<Collider>();
            for(int i=0;i<names.Length;i++)
            {
                if(!bones.TryGetValue(names[i],out var bone))continue;
                var body=bone.gameObject.AddComponent<Rigidbody>();body.mass=i<2?15:i==2?5:4;body.linearDamping=.25f;body.angularDamping=1.5f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.maxAngularVelocity=8;
                bone.gameObject.layer=LayerMask.NameToLayer("TZEffects");bodies.Add(body);byName[names[i]]=body;
                Vector3 end=ends[i]!=null&&bones.TryGetValue(ends[i],out var tip)?tip.position:bone.position+transform.up*(i==2?.16f:.22f);
                var capsule=bone.gameObject.AddComponent<CapsuleCollider>();var local=bone.InverseTransformVector(end-bone.position);int axis=Mathf.Abs(local.y)>=Mathf.Abs(local.x)&&Mathf.Abs(local.y)>=Mathf.Abs(local.z)?1:Mathf.Abs(local.x)>Mathf.Abs(local.z)?0:2;
                capsule.direction=axis;float scale=bone.lossyScale.x;capsule.height=Mathf.Max(.1f,Vector3.Distance(bone.position,end))/scale;capsule.radius=(i<2?.14f:i==2?.10f:i>6?.075f:.052f)/scale;capsule.center=bone.InverseTransformPoint((bone.position+end)*.5f);colliders.Add(capsule);
                if(parents[i]!=null&&byName.TryGetValue(parents[i],out var connected))
                {
                    var joint=bone.gameObject.AddComponent<CharacterJoint>();joint.connectedBody=connected;joint.enableProjection=true;joint.projectionDistance=.035f;
                    joint.lowTwistLimit=new SoftJointLimit{limit=-25};joint.highTwistLimit=new SoftJointLimit{limit=25};joint.swing1Limit=new SoftJointLimit{limit=i==2?30:45};joint.swing2Limit=new SoftJointLimit{limit=25};
                }
            }
            var weapon=GetComponentInChildren<WeaponReloadAnimation>();
            if(weapon)
            {
                var renderers=weapon.GetComponentsInChildren<Renderer>();Bounds bounds=new Bounds(weapon.transform.position,Vector3.zero);bool first=true;
                foreach(var renderer in renderers)if(renderer.enabled){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
                if(!first)
                {
                    var gun=weapon.gameObject.AddComponent<Rigidbody>();gun.mass=3.5f;gun.angularDamping=1.5f;gun.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;bodies.Add(gun);weapon.gameObject.layer=LayerMask.NameToLayer("TZEffects");
                    var collider=weapon.gameObject.AddComponent<BoxCollider>();collider.center=weapon.transform.InverseTransformPoint(bounds.center);var scale=weapon.transform.lossyScale;collider.size=new Vector3(bounds.size.x/Mathf.Abs(scale.x),bounds.size.y/Mathf.Abs(scale.y),bounds.size.z/Mathf.Abs(scale.z));colliders.Add(collider);
                }
            }
            // Corpses collide with architecture, never block living actors or each other internally.
            var actors=new List<Collider>();foreach(var enemy in EnemyHealth.Active)actors.AddRange(enemy.GetComponentsInChildren<Collider>());
            if(GameManager.Instance&&GameManager.Instance.Player)actors.AddRange(GameManager.Instance.Player.GetComponentsInChildren<Collider>());
            foreach(var a in colliders){foreach(var b in colliders)if(a!=b)Physics.IgnoreCollision(a,b);foreach(var b in actors)if(a!=b)Physics.IgnoreCollision(a,b);}
            foreach(var b in bodies)b.AddForce(((transform.position-origin).normalized+Vector3.up*.1f)*1.5f,ForceMode.VelocityChange);
            sleepAt=Time.time+6;
        }
        void FixedUpdate(){if(Time.time<sleepAt)return;foreach(var b in bodies)if(b)b.isKinematic=true;enabled=false;}
    }
}
