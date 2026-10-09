using UnityEngine;
using UnityEngine.UI;

namespace TacticalZero
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpticReticle : MaskableGraphic
    {
        string weaponId=""; VertexHelper mesh;
        public override Texture mainTexture=>Texture2D.whiteTexture;
        public void SetWeapon(string id){raycastTarget=false;if(id==weaponId)return;weaponId=id;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;var profile=WeaponSight.For(weaponId);var rect=rectTransform.rect;
            float r=Mathf.Min(rect.width,rect.height)*.385f;
            Color ink=new Color(.025f,.033f,.03f,.95f),red=new Color(1,.15f,.07f,.95f),cyan=new Color(.22f,.94f,.88f,.9f);
            if(profile.Magnified)
            {
                Ring(r+16,Mathf.Max(rect.width,rect.height)*1.5f,new Color(.005f,.009f,.014f,1));
                Ring(r,r+16,new Color(.045f,.055f,.065f,1));Ring(r-3,r,new Color(.24f,.3f,.32f,.9f));
                Ring(r-14,r-3,new Color(.15f,.38f,.44f,.12f));
                Color c=profile.Style==SightStyle.Digital?cyan:ink;
                if(profile.Style==SightStyle.Digital)
                {
                    Line(new Vector2(-20,0),new Vector2(-7,0),2,c);Line(new Vector2(7,0),new Vector2(20,0),2,c);
                    Line(new Vector2(0,-20),new Vector2(0,-7),2,c);Line(new Vector2(0,7),new Vector2(0,20),2,c);
                    for(int i=1;i<6;i++){float y=-i*r*.13f;Line(new Vector2(-r*.16f,y),new Vector2(r*.16f,y),1,c);}
                    Ring(2,3,c);
                }
                else
                {
                    Line(new Vector2(-r,0),new Vector2(r,0),1.2f,c);Line(new Vector2(0,-r),new Vector2(0,r),1.2f,c);
                    for(int i=1;i<=5;i++)
                    {
                        float d=i*r*.13f;
                        if(weaponId=="AWP")foreach(int sign in new[]{-1,1}){Dot(new Vector2(sign*d,0),2,c);Dot(new Vector2(0,sign*d),2,c);}
                        else Line(new Vector2(-6-i*2,-d),new Vector2(6+i*2,-d),1.4f,c);
                    }
                    Line(new Vector2(-r,0),new Vector2(-r*.64f,0),4,c);Line(new Vector2(r*.64f,0),new Vector2(r,0),4,c);
                    Line(new Vector2(0,-r),new Vector2(0,-r*.7f),4,c);Dot(Vector2.zero,1.5f,red);
                }
            }
            else if(profile.Style!=SightStyle.Iron)
            {
                Dot(Vector2.zero,1.7f,red);
                if(profile.Style==SightStyle.Holographic){Ring(14,15,red);Line(new Vector2(0,-21),new Vector2(0,-16),1,red);}
            }
        }
        void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
        {
            int n=mesh.currentVertCount;foreach(var p in new[]{a,b,c,d})mesh.AddVert(p,tint,new Vector2(.5f,.5f));
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        void Line(Vector2 a,Vector2 b,float width,Color tint){var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Quad(a-normal,a+normal,b+normal,b-normal,tint);}
        void Ring(float inner,float outer,Color tint)
        {
            for(int i=0;i<128;i++){float a=i*Mathf.PI/64,b=(i+1)*Mathf.PI/64;var x=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var y=new Vector2(Mathf.Cos(b),Mathf.Sin(b));Quad(x*inner,y*inner,y*outer,x*outer,tint);}
        }
        void Dot(Vector2 p,float radius,Color tint){for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Quad(p,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,p,tint);}}
    }
    [DefaultExecutionOrder(210)]
    public sealed class WeaponOpticHUD : MonoBehaviour
    {
        GameManager game;RectTransform root;OpticReticle reticle;Text caption;
        public void Build(Transform parent)
        {
            game=GameManager.Ensure();root=UIFactory.Fill("Weapon optic",parent);root.SetAsFirstSibling();
            reticle=UIFactory.Fill("Optic aperture and reticle",root).gameObject.AddComponent<OpticReticle>();
            caption=UIFactory.Text("Optic magnification",root,"",new Vector2(.5f,.5f),new Vector2(0,-215),new Vector2(300,25),15,TextAnchor.MiddleCenter);
            caption.color=new Color(.55f,.7f,.69f);root.gameObject.SetActive(false);
        }
        void LateUpdate()
        {
            var gun=game&&game.Player?game.Player.GetComponent<WeaponController>():null;
            bool show=gun&&gun.SightEngaged;root.gameObject.SetActive(show);if(!show)return;
            var profile=WeaponSight.For(gun.Current.Id);reticle.SetWeapon(gun.Current.Id);caption.text=profile.Magnified?profile.Label:"";
            caption.rectTransform.anchoredPosition=new Vector2(0,-Mathf.Min(root.rect.width,root.rect.height)*.3f);
        }
    }
}
