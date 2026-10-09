using UnityEngine;

namespace TacticalZero
{
    // Solve the elbow and wrist after the weapon's mechanical reload pose has been evaluated.
    [DefaultExecutionOrder(220)]
    public sealed class FirstPersonArm : MonoBehaviour
    {
        public Transform Upper,Lower,Hand,Target;
        public bool Left;
        Quaternion handOffset;float upperLength,lowerLength;WeaponController owner;Camera view;
        SkinnedMeshRenderer[] skins;int[] releaseIndices;WeaponReloadAnimation choreography;
        void Start()
        {
            if(!Upper||!Lower||!Hand||!Target){enabled=false;return;}
            upperLength=Vector3.Distance(Upper.position,Lower.position);lowerLength=Vector3.Distance(Lower.position,Hand.position);
            handOffset=Quaternion.Inverse(Target.rotation)*Hand.rotation;
            owner=GetComponentInParent<WeaponController>();view=owner?owner.GetComponent<PlayerLook>().View:null;
            // World weapons and armory previews must never display a second pair of unattached arms.
            if(!owner){gameObject.SetActive(false);return;}
            choreography=GetComponentInParent<WeaponReloadAnimation>();skins=GetComponentsInChildren<SkinnedMeshRenderer>();releaseIndices=new int[skins.Length];
            for(int i=0;i<skins.Length;i++)releaseIndices[i]=skins[i].sharedMesh.GetBlendShapeIndex("ReleaseGrip");
            foreach(var r in GetComponentsInChildren<Renderer>()){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
        }
        void LateUpdate()
        {
            if(!view||!Target)return;
            Vector3 shoulder=view.transform.TransformPoint(new Vector3(Left?-.19f:.19f,-.28f,-.03f));
            Upper.position=shoulder;
            Vector3 delta=Target.position-shoulder;float distance=Mathf.Clamp(delta.magnitude,.045f,upperLength+lowerLength-.005f);Vector3 forward=delta.normalized;
            Vector3 hint=view.transform.TransformDirection(new Vector3(Left?-.8f:.8f,-1,-.15f));Vector3 bend=Vector3.ProjectOnPlane(hint,forward).normalized;
            float along=(upperLength*upperLength-lowerLength*lowerLength+distance*distance)/(2*distance);
            Vector3 elbow=shoulder+forward*along+bend*Mathf.Sqrt(Mathf.Max(0,upperLength*upperLength-along*along));
            Upper.rotation=Quaternion.FromToRotation(Lower.position-Upper.position,elbow-shoulder)*Upper.rotation;
            Lower.rotation=Quaternion.FromToRotation(Hand.position-Lower.position,Target.position-Lower.position)*Lower.rotation;
            Hand.rotation=Target.rotation*handOffset;
            if(Left&&choreography)for(int i=0;i<skins.Length;i++)if(releaseIndices[i]>=0)skins[i].SetBlendShapeWeight(releaseIndices[i],choreography.GripRelease*100);
        }
    }
}
