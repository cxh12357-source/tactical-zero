using UnityEngine;
using UnityEngine.UI;

namespace TacticalZero
{
    // Resolution-independent, original silhouettes; no text glyphs or emoji fallbacks.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SupplyIcon : MaskableGraphic
    {
        string kind="ammo",id="";Color accent=Color.white;VertexHelper mesh;
        public string Kind=>kind;
        public string ItemId=>id;
        public override Texture mainTexture=>Texture2D.whiteTexture;
        public void Set(string type,string item,Color tint)
        {
            raycastTarget=false;
            if(kind==type&&id==item&&accent==tint)return;
            kind=type;id=item;accent=tint;raycastTarget=false;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;
            if(kind=="weapon")Gun();
            else if(kind=="equipment")Gear();
            else if(kind=="heal"||kind=="med") {Box(25,25,50,50,accent);Box(42,31,16,38,Color.white);Box(31,42,38,16,Color.white);}
            else if(kind=="ammo"||kind=="arms")for(int i=0;i<3;i++){float x=22+i*21;Box(x,28,13,37,accent);Poly(accent,new Vector2(x,65),new Vector2(x+6.5f,78),new Vector2(x+13,65));Box(x-1,23,15,4,Color.white);}
            else if(kind=="tech"||kind=="circuit"||kind=="intel")
            {
                Box(30,30,40,40,accent);Box(39,39,22,22,new Color(.08f,.14f,.18f));
                for(int i=0;i<4;i++){float p=34+i*10;Box(p,19,3,10,accent);Box(p,71,3,10,accent);Box(19,p,10,3,accent);Box(71,p,10,3,accent);}
            }
            else {Box(23,23,54,49,accent);Box(37,72,26,9,accent);Box(45,23,10,49,new Color(.08f,.14f,.18f));Box(22,46,56,7,Color.white);}
        }
        void Gun()
        {
            var dark=new Color(.08f,.14f,.18f);
            bool pistol=id=="USP",sniper=id=="AWP"||id=="DMR",rail=id=="RAIL",launcher=id=="NOVA",smg=id=="MP6"||id=="VECTOR";
            if(pistol)
            {
                Box(20,53,61,18,accent);Box(72,57,13,9,accent);Poly(accent,new Vector2(25,53),new Vector2(43,53),new Vector2(35,26),new Vector2(19,26));
                Box(43,41,20,4,accent);Box(59,44,4,11,accent);Box(27,66,17,3,dark);return;
            }
            Box(25,47,42,16,accent);Box(65,52,sniper?30:25,6,accent);
            Poly(accent,new Vector2(26,60),new Vector2(7,64),new Vector2(7,44),new Vector2(25,51));
            Poly(accent,new Vector2(33,49),new Vector2(42,49),new Vector2(37,31),new Vector2(28,31));
            if(id=="Shotgun")Box(54,44,25,7,Color.white);
            else if(id=="RPK")Disc(52,35,12,accent);
            else if(id=="AK47")Poly(accent,new Vector2(48,48),new Vector2(58,48),new Vector2(63,27),new Vector2(53,23),new Vector2(49,35));
            else Box(smg?41:48,29,9,19,accent);
            if(sniper){Box(36,69,24,6,Color.white);Box(43,62,3,7,accent);Box(56,62,3,7,accent);Disc(60,72,5,Color.white);}
            else{Box(30,63,4,6,Color.white);Box(62,63,3,5,Color.white);}
            if(id=="MP6")Box(78,49,18,12,Color.white);
            if(id=="VECTOR")Box(33,35,17,16,Color.white);
            if(launcher){Box(48,42,39,28,accent);Disc(85,56,14,accent);Disc(85,56,7,dark);}
            if(rail)for(int i=0;i<4;i++)Box(56+i*9,45,4,20,Color.white);
            if(id=="CYCLONE"){for(int i=0;i<3;i++)Box(60,46+i*7,33,3,Color.white);Box(86,44,4,23,accent);}
            for(int i=0;i<3;i++)Box(49+i*6,54,3,5,dark);
        }
        void Gear()
        {
            var dark=new Color(.08f,.14f,.18f);
            if(id=="Armor")
            {
                Poly(accent,new Vector2(35,83),new Vector2(65,83),new Vector2(80,66),new Vector2(73,27),new Vector2(50,15),new Vector2(27,27),new Vector2(20,66));
                Box(36,53,28,5,dark);Box(36,41,28,5,dark);Box(47,68,6,8,Color.white);
            }
            else if(id=="Shield")
            {
                Poly(accent,new Vector2(50,88),new Vector2(81,73),new Vector2(77,38),new Vector2(50,12),new Vector2(23,38),new Vector2(19,73));
                Poly(dark,new Vector2(50,79),new Vector2(72,68),new Vector2(68,43),new Vector2(50,24),new Vector2(32,43),new Vector2(28,68));
                Poly(Color.white,new Vector2(53,70),new Vector2(37,47),new Vector2(48,47),new Vector2(44,32),new Vector2(64,56),new Vector2(53,56));
            }
            else
            {
                Poly(accent,new Vector2(34,29),new Vector2(25,38),new Vector2(60,73),new Vector2(73,60),new Vector2(38,25));
                Poly(Color.white,new Vector2(57,78),new Vector2(61,82),new Vector2(82,61),new Vector2(78,57));
                Poly(Color.white,new Vector2(20,16),new Vector2(17,19),new Vector2(31,36),new Vector2(36,31));
                Poly(dark,new Vector2(38,42),new Vector2(42,46),new Vector2(52,36),new Vector2(48,32));
                Poly(dark,new Vector2(48,52),new Vector2(52,56),new Vector2(62,46),new Vector2(58,42));
            }
        }
        void Box(float x,float y,float w,float h,Color c)=>Poly(c,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));
        void Disc(float x,float y,float radius,Color c)
        {
            var points=new Vector2[20];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length;points[i]=new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius);}Poly(c,points);
        }
        void Poly(Color c,params Vector2[] points)
        {
            var rect=rectTransform.rect;float size=Mathf.Min(rect.width,rect.height)/100;int start=mesh.currentVertCount;
            foreach(var p in points)mesh.AddVert(rect.center+(p-Vector2.one*50)*size,c*color,Vector2.one*.5f);
            for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(start,start+i+1,start+i);
        }
    }
}
