using UnityEngine;

namespace TacticalZero
{
    public sealed class GrassVisualController : MonoBehaviour
    {
        private Renderer[] tufts;
        private void Awake()
        {
            tufts = GetComponentsInChildren<Renderer>();
            var blade=Resources.Load<Mesh>("Classic/Meshes/GrassBlade");
            if(blade)foreach(var tuft in tufts)if(tuft.name=="Foliage blade")
            {
                tuft.GetComponent<MeshFilter>().sharedMesh=blade;
                tuft.transform.localScale=new Vector3(.65f,tuft.transform.localScale.y,.65f);
            }
        }

        private void Start()
        {
            Apply();
            SaveService.Applied += Apply;
        }

        private void OnDestroy()
        {
            SaveService.Applied -= Apply;
        }

        private void Apply()
        {
            int quality = GameManager.Instance ? GameManager.Instance.Settings.quality : 1;
            for (int i = 0; i < tufts.Length; i++)
            {
                tufts[i].enabled = quality > 0 || i % 2 == 0;
                tufts[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
