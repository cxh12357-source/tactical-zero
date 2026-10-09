using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TacticalZero
{
    // Saved separately from desktop controls and from the older browser game's storage.
    public sealed class TouchLayout:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        public static bool Editing {get;private set;}
        static readonly List<TouchLayout> controls=new List<TouchLayout>();
        RectTransform rect,parent;Vector2 initialAnchor,initialPosition;MobileAction action;int pointer=int.MinValue,lastWidth,lastHeight;
        string Key=>"TZ3D.TouchLayout."+action;
        public void Configure(MobileAction value)
        {
            action=value;rect=(RectTransform)transform;parent=(RectTransform)rect.parent;
            initialAnchor=rect.anchorMin;initialPosition=rect.anchoredPosition;controls.Add(this);
        }
        void Start()
        {
            if(!PlayerPrefs.HasKey(Key))return;
            Vector2 saved;
            try{saved=JsonUtility.FromJson<Vector2>(PlayerPrefs.GetString(Key));}catch(System.ArgumentException){return;}
            if(float.IsNaN(saved.x)||float.IsInfinity(saved.x)||float.IsNaN(saved.y)||float.IsInfinity(saved.y))return;
            rect.anchorMin=rect.anchorMax=new Vector2(Mathf.Clamp01(saved.x),Mathf.Clamp01(saved.y));rect.anchoredPosition=Vector2.zero;Clamp();
        }
        public static void Begin(){GameManager.Instance.Input.Clear();Editing=true;}
        public static void End(){Editing=false;foreach(var item in controls)if(item)item.pointer=int.MinValue;GameManager.Instance.Input.Clear();}
        public static void ResetPositions()
        {
            foreach(var item in controls)if(item){PlayerPrefs.DeleteKey(item.Key);item.rect.anchorMin=item.rect.anchorMax=item.initialAnchor;item.rect.anchoredPosition=item.initialPosition;item.Clamp();}PlayerPrefs.Save();
        }
        public void OnPointerDown(PointerEventData data){if(Editing&&pointer==int.MinValue)pointer=data.pointerId;}
        void LateUpdate(){if(Screen.width!=lastWidth||Screen.height!=lastHeight){lastWidth=Screen.width;lastHeight=Screen.height;Clamp();}}
        public void OnDrag(PointerEventData data)
        {
            if(!Editing||data.pointerId!=pointer)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,data.position,data.pressEventCamera,out var point))
            {rect.anchorMin=rect.anchorMax=Vector2.zero;rect.anchoredPosition=point-parent.rect.min;Clamp();}
        }
        void Clamp()
        {
            if(parent.rect.width<1||parent.rect.height<1)return;
            Vector2 p=rect.anchoredPosition+Vector2.Scale(rect.anchorMin,parent.rect.size);
            p.x=Mathf.Clamp(p.x,rect.rect.width*.5f,parent.rect.width-rect.rect.width*.5f);
            p.y=Mathf.Clamp(p.y,rect.rect.height*.5f,parent.rect.height-rect.rect.height*.5f);
            rect.anchorMin=rect.anchorMax=new Vector2(p.x/parent.rect.width,p.y/parent.rect.height);rect.anchoredPosition=Vector2.zero;
        }
        public void OnPointerUp(PointerEventData data)
        {
            if(pointer!=data.pointerId)return;pointer=int.MinValue;if(!Editing)return;
            Clamp();PlayerPrefs.SetString(Key,JsonUtility.ToJson(rect.anchorMin));PlayerPrefs.Save();
        }
        void OnDestroy(){controls.Remove(this);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){controls.Clear();Editing=false;}
    }
}
