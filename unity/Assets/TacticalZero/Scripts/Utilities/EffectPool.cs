using UnityEngine;

namespace TacticalZero
{
    public sealed class EffectPool : MonoBehaviour
    {
        public static EffectPool Instance { get; private set; }

        private sealed class Item
        {
            public Transform root;
            public MeshRenderer renderer;
            public MeshFilter filter;
            public TextMesh text;
            public Vector3 velocity;
            public float remaining, size;
            public bool number;
        }

        private Item[] items;
        private int cursor;
        private Mesh mesh;
        private Mesh flashMesh, shellMesh,markMesh;
        private Material material;
        private MaterialPropertyBlock colorBlock;
        private Camera camera;
        private void Awake()
        {
            var template = Resources.Load<Material>("EffectsMaterial");
            if (!template)
            {
                Debug.LogError("Missing EffectsMaterial. Run Tactical Zero setup to create the build-retained URP effect material.", this);
                enabled = false;
                return;
            }
            Instance = this;
            items = new Item[64];
            colorBlock = new MaterialPropertyBlock();
            var sample = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh = sample.GetComponent<MeshFilter>().sharedMesh;
            flashMesh=new Mesh{name="Six-point muzzle flash"};var v=new Vector3[13];var tr=new int[36];
            for(int i=0;i<12;i++){float angle=i*Mathf.PI/6;float radius=i%2==0?.5f:.13f;v[i+1]=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);tr[i*3]=0;tr[i*3+1]=i+1;tr[i*3+2]=(i+1)%12+1;}
            flashMesh.vertices=v;flashMesh.triangles=tr;flashMesh.RecalculateNormals();
            markMesh=new Mesh{name="Irregular bullet impact mark"};var mv=new Vector3[21];var mt=new int[60];
            for(int i=0;i<20;i++){float a=i*Mathf.PI*.1f;float r=i%3==0?.5f:.37f;mv[i+1]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);mt[i*3]=0;mt[i*3+1]=i+1;mt[i*3+2]=(i+1)%20+1;}
            markMesh.vertices=mv;markMesh.triangles=mt;markMesh.RecalculateNormals();
            Destroy(sample);
            var casing=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shellMesh=casing.GetComponent<MeshFilter>().sharedMesh;Destroy(casing);
            material = new Material(template)
            {
                enableInstancing = true
            };
            for (int i = 0; i < items.Length; i++)
            {
                var root = new GameObject("Pooled Effect " + i);
                root.layer = LayerMask.NameToLayer("TZEffects");
                root.transform.SetParent(transform, false);
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var text = new GameObject("Damage").AddComponent<TextMesh>();
                text.transform.SetParent(root.transform, false);
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
                text.fontSize = 40;
                text.characterSize = .045f;
                text.anchor = TextAnchor.MiddleCenter;
                root.SetActive(false);
                items[i] = new Item
                {
                    root = root.transform,
                    renderer = renderer,
                    filter = root.GetComponent<MeshFilter>(),
                    text = text
                };
            }

            GameManager.Instance.Changed += PhaseChanged;
        }

        private void PhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Playing)
                camera = Camera.main;
            if (phase == GamePhase.Loading)
                foreach (var item in items)
                {
                    item.remaining = 0;
                    item.root.gameObject.SetActive(false);
                }
        }

        private Item Take(Vector3 point, float seconds, float size, Color color, bool number)
        {
            int limit = SaveService.Settings.quality == 0 ? 32 : items.Length;
            var item = items[cursor++ % limit];
            item.remaining = seconds;
            item.size = size;
            item.number = number;
            item.filter.sharedMesh=mesh;
            item.velocity = Vector3.zero;
            item.root.position = point;
            item.root.localScale = Vector3.one * size;
            item.root.rotation = Quaternion.identity;
            item.renderer.enabled = !number;
            item.text.gameObject.SetActive(number);
            item.root.gameObject.SetActive(true);
            colorBlock.SetColor("_BaseColor", color);
            item.renderer.SetPropertyBlock(colorBlock);
            item.text.color = color;
            return item;
        }

        public void Burst(Vector3 point, Color color, float size, float seconds)
        {
            Take(point, seconds, size, color, false);
        }
        public void MuzzleFlash(Vector3 point,Vector3 forward)
        {
            var item=Take(point,.035f,.09f,new Color(1,.76f,.28f),false);item.filter.sharedMesh=flashMesh;item.root.rotation=Quaternion.LookRotation(-forward)*Quaternion.Euler(0,0,Random.Range(0,60));
        }

        public void Tracer(Vector3 from,Vector3 to,bool rail=false)
        {
            var line=to-from;if(line.sqrMagnitude<.01f)return;
            if(!rail&&Random.value>.18f)return;
            var item=Take((from+to)*.5f,rail?.16f:.028f,1,rail?new Color(.35f,1,1):new Color(.7f,.62f,.45f),false);
            item.root.rotation=Quaternion.LookRotation(line);
            item.root.localScale=new Vector3(rail?.025f:.009f,rail?.025f:.009f,line.magnitude);
        }
        public void Impact(Vector3 at,Vector3 normal,bool enemy)
        {
            if(!enemy){var mark=Take(at+normal*.007f,18,.065f,new Color(.08f,.072f,.06f),false);mark.filter.sharedMesh=markMesh;mark.root.rotation=Quaternion.LookRotation(normal);}
            int count=SaveService.Settings.quality==0?4:7;
            for(int i=0;i<count;i++)
            {
                var item=Take(at+normal*.025f,.2f+Random.value*.2f,.025f+Random.value*.025f,enemy?new Color(.24f,.10f,.075f):new Color(.35f,.32f,.27f),false);
                item.velocity=(normal+Random.insideUnitSphere*.8f)*(1.5f+Random.value*2);
            }
        }

        public void Shell(Vector3 point, Vector3 direction)
        {
            var item = Take(point, .65f, .035f, new Color(.84f, .65f, .27f), false);
            item.velocity = direction * 1.8f + Vector3.up;
            item.filter.sharedMesh=shellMesh;
            item.root.localScale = new Vector3(.009f, .018f, .009f);
        }

        public void Number(Vector3 point, int damage, bool head)
        {
            var item = Take(point + Vector3.up * .2f, .9f, 1, head ? new Color(1, .78f, .4f) : Color.white, true);
            item.text.text = damage.ToString();
            item.velocity = Vector3.up * .45f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0)
                return;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.remaining <= 0)
                    continue;
                item.remaining -= dt;
                if (item.remaining <= 0)
                {
                    item.root.gameObject.SetActive(false);
                    continue;
                }

                item.root.position += item.velocity * dt;
                if (item.number)
                {
                    if (camera)
                        item.root.rotation = camera.transform.rotation;
                }
                else if (item.velocity.sqrMagnitude > 0)
                {
                    item.velocity += Vector3.down * 9 * dt;
                    item.root.Rotate(0, 0, 350 * dt);
                }
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance)
                GameManager.Instance.Changed -= PhaseChanged;
            if (material)
                Destroy(material);
            if(flashMesh)Destroy(flashMesh);
            if(markMesh)Destroy(markMesh);
            if (Instance == this)
                Instance = null;
        }
    }
}
