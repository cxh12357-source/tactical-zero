using UnityEngine;
namespace TacticalZero
{
    public sealed class BuildingSign:MonoBehaviour
    {
        static Material world;
        public static Material WorldMaterial
        {
            get {if(!world){world=new Material(Shader.Find("TacticalZero/WorldLabel"));Font.textureRebuilt+=Refresh;}world.mainTexture=UIFactory.Font.material.mainTexture;return world;}
        }
        static void Refresh(Font font){if(world&&font==UIFactory.Font)world.mainTexture=font.material.mainTexture;}
        void Awake(){var label=GetComponent<TextMesh>();label.font=UIFactory.Font;GetComponent<MeshRenderer>().sharedMaterial=WorldMaterial;}
    }
}
