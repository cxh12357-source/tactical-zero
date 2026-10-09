using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TacticalZero.Editor
{
    public static class BodycamArchitecture15
    {
        public static void Build(Transform root,string id,Material concrete,Material steel)
        {
            if(id=="outpost")return;
            float width=id=="docks"?17:24,depth=id=="docks"?14:22,height=id=="ruins"?5.5f:6.5f;
            Vector3 center=id=="docks"?new Vector3(2,0,-1):Vector3.zero;
            GameObject Part(string n,Vector3 at,Vector3 size,Material m,bool solid=false)
            {
                var o=ClassicArt.Part(n,root,center+at,size,m);o.layer=LayerMask.NameToLayer("TZWorld");GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.BatchingStatic);if(solid)o.AddComponent<BoxCollider>();return o;
            }
            // The hall roof leaves continuous skylight slots and retains all existing ground routes.
            for(int end=-1;end<=1;end+=2)
            {
                Part("Warehouse eave girder",new Vector3(end*width*.5f,height,0),new Vector3(.18f,.32f,depth+.8f),steel);
                for(int k=-1;k<=1;k++)
                {
                    // Footings sit above existing perimeter cover; thin structural posts stay off authored route centers.
                    Part("Flanged structural column",new Vector3(end*width*.5f,height*.5f,k*depth*.5f),new Vector3(.18f,height,.18f),steel,true);
                    Part("Column base flange",new Vector3(end*width*.5f,.05f,k*depth*.5f),new Vector3(.42f,.10f,.42f),steel);
                    for(int bolt=-1;bolt<=1;bolt+=2)Part("Foundation anchor bolt",new Vector3(end*width*.5f+bolt*.13f,.12f,k*depth*.5f+.13f),new Vector3(.025f,.05f,.025f),steel);
                }
            }
            for(int k=0;k<5;k++)
            {
                float z=(k/4f-.5f)*depth;
                Part("Overhead truss lower chord",new Vector3(0,height,z),new Vector3(width,.13f,.12f),steel);
                Part("Overhead truss upper chord",new Vector3(0,height+.55f,z),new Vector3(width,.11f,.11f),steel);
                for(int j=0;j<8;j++)
                {var b=Part("Triangular truss web",new Vector3((j/7f-.5f)*(width-2),height+.26f,z),new Vector3(2.0f,.075f,.075f),steel);b.transform.localRotation=Quaternion.Euler(0,0,j%2==0?16:-16);}
            }
            for(int side=-1;side<=1;side+=2)
            {
                if(id=="ruins"&&side==1)continue;
                Part("Weathered roof deck",new Vector3(side*width*.30f,height+.65f,0),new Vector3(width*.39f,.16f,depth+.6f),id=="ruins"?concrete:steel,true);
                for(int i=0;i<12;i++)Part("Standing roof seam",new Vector3(side*width*.30f,height+.74f,(i/11f-.5f)*depth),new Vector3(width*.39f,.03f,.04f),steel);
            }
            if(id!="ruins")for(int side=-1;side<=1;side+=2)
            {
                var lamp=new GameObject("Hall practical lighting").AddComponent<Light>();lamp.transform.SetParent(root,false);lamp.transform.localPosition=center+new Vector3(side*6,height-.5f,0);lamp.type=LightType.Point;lamp.range=13;lamp.intensity=3.0f;lamp.color=new Color(.82f,.89f,1);lamp.shadows=LightShadows.None;
                Part("Suspended strip light",new Vector3(side*6,height-.3f,0),new Vector3(1.4f,.08f,.22f),ClassicArt.White);
            }
        }
    }
}
