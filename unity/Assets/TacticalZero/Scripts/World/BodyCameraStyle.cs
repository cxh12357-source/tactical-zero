using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TacticalZero
{
    // Exposure changes are driven by entering enclosed spaces, never by changing player movement.
    public sealed class BodyCameraStyle : MonoBehaviour
    {
        Volume volume;ColorAdjustments color;LensDistortion lens;MotionBlur motion;
        float exposure=.05f;float nextProbe;bool enclosed;
        void Start()
        {
            volume=GetComponent<Volume>();if(!volume)return;
            volume.profile.TryGet(out color);volume.profile.TryGet(out lens);volume.profile.TryGet(out motion);
        }
        void Update()
        {
            var game=GameManager.Instance;if(!game||!game.IsPlaying||!game.Player)return;
            var view=game.Player.GetComponent<PlayerLook>().View;if(!view)return;
            if(Time.time>nextProbe){nextProbe=Time.time+.25f;enclosed=Physics.Raycast(view.transform.position,Vector3.up,5,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore);}
            exposure=Mathf.Lerp(exposure,enclosed?.58f:.24f,1-Mathf.Exp(-Time.deltaTime*(enclosed?1.6f:3.5f)));
            if(color)color.postExposure.value=exposure;
            bool aiming=game.Input.Aiming;
            if(lens)lens.intensity.value=Mathf.Lerp(lens.intensity.value,aiming?-.04f:-.19f,Time.deltaTime*9);
            if(motion)motion.intensity.value=game.Settings.cameraShake&&!aiming?.10f:0;
        }
        void OnDestroy(){if(volume&&volume.HasInstantiatedProfile())Destroy(volume.profile);}
    }
}
