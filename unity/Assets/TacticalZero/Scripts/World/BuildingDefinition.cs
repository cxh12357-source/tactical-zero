using System;
using UnityEngine;

namespace TacticalZero
{
    [Serializable] public sealed class BuildingDefinition
    {
        public string Id, Name, Briefing;
        public string previewImage => "Maps/Previews/"+Id;
        public string Feature => Id=="harbor"?"内庭回廊 · 住宅套间":Id=="office"?"双交通核心 · 玻璃办公区":Id=="hospital"?"双走廊病区 · 地下后勤":"生产车间 · 开敞检修夹层";
        public string[] Floors, Spaces;
        public int SpawnFloor, ExitFloor;
        public float HallWidth;
        public string Scene => "Classic_Sweep_" + Id;
        public string Space(int floor, int room) => Spaces[floor].Split('|')[room];
        public Vector3 Spawn => new Vector3(-22, SpawnFloor * BuildingMission.FloorHeight, -14);
        public Vector3 Exit => new Vector3(22, ExitFloor * BuildingMission.FloorHeight, 14);
    }

    public static class BuildingCatalog
    {
        public static readonly BuildingDefinition[] Maps = {
            new BuildingDefinition { Id="harbor", Name="海湾公寓", Floors=new[]{"F1","F2","F3","RF"}, ExitFloor=3, HallWidth=6,
                Briefing="从门厅进入，逐户排查。西侧主楼梯与东侧消防通道均可通行。清空整栋建筑后前往屋顶撤离。",
                Spaces=new[]{"物业室|快递间|门厅接待|车库管理|住户会客厅|后门工具间|配电房|自行车库", "住宅201客厅|住宅201卧室|住宅202厨房|住宅202卧室|住宅203客厅|住宅203卧室|洗衣房|储物间", "大户型客厅|书房|主卧|厨房餐厅|客房|设备间|消防控制|屋顶值班室", "电梯机房|水箱设备|排风设备|检修仓|太阳能控制|露台休息区|通信机房|撤离调度"} },
            new BuildingDefinition { Id="office", Name="凌云商务中心", Floors=new[]{"F1","F2","F3","F4"}, ExitFloor=3, HallWidth=8,
                Briefing="玻璃会议室与长廊视野开阔。利用办公桌掩体，交叉检查两侧安全通道，最后在四层屋顶通道撤离。",
                Spaces=new[]{"安保室|咖啡区|接待大厅|访客洽谈|后勤仓库|快件收发|电梯等候厅|物业办公室", "开放办公A|开放办公B|会议室|项目讨论室|打印室|茶水间|财务办公室|电话会议室", "服务器室|档案室|网络运维|备件储藏|管理办公室|人事办公室|保密会议|行政支持", "大型会议厅|总经理办公室|董事会议|展示厅|秘书室|接待休息室|设备间|屋顶通道"} },
            new BuildingDefinition { Id="hospital", Name="市立急救中心", Floors=new[]{"B1","F1","F2","F3"}, SpawnFloor=1, ExitFloor=1, HallWidth=6,
                Briefing="包含地下设备层。留意上下层的脚步回响。完成所有楼层清剿后返回一楼救护车通道，不能在顶层撤离。",
                Spaces=new[]{"备用电源|设备区|储藏室|停尸间|消毒间|地下通道|值班室|医疗废物间", "急诊大厅|接待分诊|药房|检查室|抢救室|影像检查|救护车调度|输液室", "病房201|病房202|护士站|治疗室|病房203|病房204|配药室|隔离病房", "手术一室|手术二室|器械室|医生办公室|麻醉准备|术后观察|清洗消毒|值班休息"} },
            new BuildingDefinition { Id="factory", Name="东港制造厂", Floors=new[]{"G","M","F2"}, HallWidth=12,
                Briefing="生产车间、货架和检修夹层组成交叉路线。巡查两条楼梯及行政层，清场后返回地面装卸平台。",
                Spaces=new[]{"主生产车间|传送带控制|原料仓库|维修间|装配车间|成品仓库|装卸调度|质量检测", "检修步道A|检修步道B|管线控制|配电平台|风机检修|观察控制|工具库|安全值班", "行政办公室|生产调度|会议室|培训室|工艺设计|档案室|员工休息|安保监控"} }
        };
        public static int SelectedMap { get=>Mathf.Clamp(PlayerPrefs.GetInt("TZ3D.Building.Map",0),0,3); set {PlayerPrefs.SetInt("TZ3D.Building.Map",value);PlayerPrefs.Save();} }
        public static string SelectedWeapon { get=>PlayerPrefs.GetString("TZ3D.Building.Weapon","M4A1"); set {PlayerPrefs.SetString("TZ3D.Building.Weapon",value);PlayerPrefs.Save();} }
        public static bool Enabled = true;
        public static bool InScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.StartsWith("Classic_Sweep_");
        public static readonly string[] Weapons={"M4A1","USP","MG3","M700","P90","M1014","AK47","VECTOR","RPK","AWP","DMR","MP6","Shotgun"};
        public static float Mobility(string id)=>id=="USP"?1.18f:id=="MG3"||id=="RPK"?.82f:id=="P90"||id=="VECTOR"||id=="MP6"?1.1f:id=="AWP"||id=="M700"?.92f:1;
        public static bool Sniper(string id)=>id=="AWP"||id=="M700";
        public static string Category(string id)=>Sniper(id)?"狙击枪":id=="USP"?"手枪":id=="MG3"||id=="RPK"?"轻机枪":id=="P90"||id=="VECTOR"||id=="MP6"?"冲锋枪":id=="M1014"||id=="Shotgun"?"霰弹枪":id=="DMR"?"精确射手步枪":"突击步枪";
        public static WeaponDefinition Weapon(WeaponDefinition original)
        {
            var gun=UnityEngine.Object.Instantiate(original);
            bool sniper=Sniper(gun.Id),mg=gun.Id=="MG3"||gun.Id=="RPK",smg=Mobility(gun.Id)==1.1f;
            gun.Configure(gun.Id,gun.DisplayName,gun.Kind,sniper?FireMode.SemiAutomatic:gun.Mode,
                sniper?115:gun.Damage, sniper?1.2f:gun.Interval, sniper?5:mg?60:gun.Magazine,
                sniper?75:mg?480:Mathf.Max(gun.Reserve,gun.Magazine*12),gun.ReloadSeconds, sniper?110:smg?45:gun.Range,gun.Pellets,gun.HipSpread,gun.Recoil,gun.Model,gun.ShotAudio);
            return gun;
        }
    }
    public enum BuildingStage { MISSION_BRIEFING, INFILTRATION, CLEAR_BUILDING, EXTRACTION_AVAILABLE, EXTRACTING, MISSION_SUCCESS, MISSION_FAILED }
}
