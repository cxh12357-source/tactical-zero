using UnityEngine;
using UnityEngine.UI;
namespace TacticalZero
{
    public sealed class OperationHUD : MonoBehaviour
    {
        RectTransform root,banner;Text title,detail,task,waypoint,timer;bool wasClear;float clearAt;
        public void Build(Transform parent)
        {
            root=UIFactory.Fill("Operation HUD",parent);
            banner=UIFactory.Rect("Clear announcement",root,new Vector2(.5f,.5f),new Vector2(0,168),new Vector2(620,82));UIFactory.Panel(banner,new Color(.025f,.12f,.10f,.92f));
            title=UIFactory.Text("Clear title",banner,"敌人已全部清除",new Vector2(.5f,.5f),new Vector2(0,13),new Vector2(600,40),30,TextAnchor.MiddleCenter);title.color=new Color(.4f,1,.7f);
            detail=UIFactory.Text("Clear instruction",banner,"",new Vector2(.5f,.5f),new Vector2(0,-21),new Vector2(600,27),16,TextAnchor.MiddleCenter);
            task=UIFactory.Text("Operation stage",root,"",new Vector2(.5f,1),new Vector2(0,-132),new Vector2(780,28),16,TextAnchor.MiddleCenter);
            timer=UIFactory.Text("Operation clock",root,"",new Vector2(1,1),new Vector2(-123,-63),new Vector2(210,26),16,TextAnchor.MiddleCenter);
            waypoint=UIFactory.Text("Objective waypoint",root,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(270,30),17,TextAnchor.MiddleCenter);waypoint.color=new Color(.42f,1,.76f);banner.gameObject.SetActive(false);
        }
        void Update()
        {
            var game=GameManager.Instance;var mission=ClassicEncounter.Instance;if(!root)return;root.gameObject.SetActive(game&&game.IsPlaying);if(!game||!game.IsPlaying||!mission||!mission.Operation)return;
            var op=mission.Operation;task.text=op.Status;int remaining=Mathf.Max(0,Mathf.CeilToInt(FieldOperation.RoundLimit-mission.Elapsed));timer.text="撤离窗口 "+(remaining/60).ToString("00")+":"+(remaining%60).ToString("00");timer.color=remaining<90?new Color(1,.4f,.25f):Color.white;
            if(mission.Cleared&&!wasClear){clearAt=Time.unscaledTime;AudioService.Instance?.Play("victory",Vector3.zero,.45f,false);}wasClear=mission.Cleared;
            banner.gameObject.SetActive(mission.Cleared&&(Time.unscaledTime-clearAt<7||op.Stage>=3));
            detail.text=op.ReadyForExtraction?"前往绿色撤离点 · 站稳 5 秒完成撤离":op.Stage>=3?op.Status:"本区域安全 · 继续完成通信站目标";
            var view=game.Player.GetComponent<PlayerLook>().View;Vector3 screen=view.WorldToViewportPoint(op.Target+Vector3.up*2);
            if(screen.z<0)screen.x=screen.x>.5f?0:1;
            waypoint.rectTransform.anchoredPosition=new Vector2((Mathf.Clamp(screen.x,.13f,.87f)-.5f)*root.rect.width,(Mathf.Clamp(screen.y,.22f,.68f)-.5f)*root.rect.height);
            waypoint.text=(screen.z<0?"↶ ":"◆ ")+(op.Stage<3?"通信站 "+(op.Stage+1):"撤离点")+" · "+Vector3.Distance(game.Player.transform.position,op.Target).ToString("0")+"m";
        }
    }
}
