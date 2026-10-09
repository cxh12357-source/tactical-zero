using UnityEngine;

namespace TacticalZero
{
    public sealed class ClassicPlayerPresentation : MonoBehaviour
    {
        Camera view;GameObject body;bool deathStarted;Vector3 deathPosition;Quaternion deathRotation;
        void Start()
        {
            view=GetComponent<PlayerLook>().View;
            view.clearFlags=RenderSettings.skybox?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;view.backgroundColor=RenderSettings.fogColor;
            body=Instantiate(Resources.Load<GameObject>("Classic/Operators/"+"assault"),transform);
            foreach(var renderer in body.GetComponentsInChildren<Renderer>())
            {
                bool leg=renderer.transform.IsChildOf(body.transform.Find("LeftLeg"))||renderer.transform.IsChildOf(body.transform.Find("RightLeg"));
                if(!leg)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
        public void ResetForRespawn()
        {
            deathStarted=false;
            GetComponent<PlayerLook>().ResetView();
            var mount=view.transform.Find("Weapon mount"); if(mount)mount.gameObject.SetActive(true);
            if(!body)return;
            foreach(var renderer in body.GetComponentsInChildren<Renderer>())
            {
                bool leg=renderer.transform.IsChildOf(body.transform.Find("LeftLeg"))||renderer.transform.IsChildOf(body.transform.Find("RightLeg"));
                renderer.shadowCastingMode=leg?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
        void LateUpdate()
        {
            if(!GameManager.Instance||GameManager.Instance.Phase!=GamePhase.Defeat)return;
            if(!deathStarted)
            {
                deathStarted=true;var damage=GetComponent<PlayerHealth>().LastDamage;
                Vector3 origin=transform.position+Vector3.up*1.5f,target=origin-transform.forward*3+Vector3.up*1.2f;
                if(Physics.Linecast(origin,target,out var hit,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore))target=hit.point+(origin-target).normalized*.25f;
                deathPosition=target;deathRotation=Quaternion.LookRotation((damage.Origin+Vector3.up*.4f-target).normalized);
                var mount=view.transform.Find("Weapon mount");if(mount)mount.gameObject.SetActive(false);
                foreach(var renderer in body.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            }
            view.transform.position=Vector3.Lerp(view.transform.position,deathPosition,Time.unscaledDeltaTime*2.5f);
            view.transform.rotation=Quaternion.Slerp(view.transform.rotation,deathRotation,Time.unscaledDeltaTime*2.5f);
        }
    }
}
