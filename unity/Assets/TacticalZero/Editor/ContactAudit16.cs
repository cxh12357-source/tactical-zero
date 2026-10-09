using UnityEditor;
using UnityEngine;
namespace TacticalZero.Editor
{
    public static class ContactAudit16
    {
        public static void Assets()
        {
            var gun=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TacticalZero/Art/BodycamFirearms15/AK47.fbx"));
            foreach(var r in gun.GetComponentsInChildren<Renderer>())if(r.name=="Bolt")Debug.Log("CONTACT_AK_BOLT "+r.bounds.center);Object.DestroyImmediate(gun);
            foreach(string id in new[]{"Desk","Chair","Vent","Door","Lamp","Lockers","Debris","Arm_L","Arm_R"})
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TacticalZero/Art/Contact16/"+id+".fbx");
                var instance=Object.Instantiate(asset);Debug.Log("CONTACT_IMPORT "+id+" rootRotation="+instance.transform.localEulerAngles+" scale="+instance.transform.localScale);
                foreach(var r in instance.GetComponentsInChildren<Renderer>())Debug.Log("CONTACT_MESH "+r.name+" enabled="+r.enabled+" center="+r.bounds.center+" size="+r.bounds.size+" rotation="+r.transform.eulerAngles);
                foreach(var t in instance.GetComponentsInChildren<Transform>())if(t.name.StartsWith("hand.")||t.name.StartsWith("digit3_2"))Debug.Log("CONTACT_BONE "+t.name+" position="+t.position);
                Object.DestroyImmediate(instance);
            }
        }
    }
}
