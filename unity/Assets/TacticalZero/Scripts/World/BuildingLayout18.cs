using System;
using System.Collections.Generic;
using UnityEngine;
namespace TacticalZero
{
    [Serializable] public sealed class BuildingRoom18
    {
        public string Id,Name;public int Floor;public Rect Area;public bool Open;
        public Vector3 Center=>new Vector3(Area.center.x,Floor*5,Area.center.y);
    }
    [Serializable] public sealed class BuildingPortal18
    {
        public string Id,From,To;public int Floor;public Vector2 Point;public bool AlongX;
    }
    public sealed class BuildingLayout18
    {
        public readonly List<BuildingRoom18> Rooms=new List<BuildingRoom18>();
        public readonly List<BuildingPortal18> Portals=new List<BuildingPortal18>();
        public static readonly string[] Operators={"assault","medic","engineer","scout","guardian","supplier","marksman","infiltrator"};
        public string Signature;
        public static BuildingLayout18 Create(int map)
        {
            var l=new BuildingLayout18();var d=BuildingCatalog.Maps[map];
            for(int f=0;f<d.Floors.Length;f++)
            {
                if(map==0)l.Apartment(f);else if(map==1)l.Office(f);else if(map==2)l.Hospital(f);else l.Factory(f);
            }
            foreach(var portal in l.Portals)
            {
                portal.To="F"+portal.Floor+"/circulation";
                Vector2 normal=portal.AlongX?Vector2.up:Vector2.right;
                foreach(var room in l.Rooms)if(room.Floor==portal.Floor&&room.Id!=portal.From&&(room.Area.Contains(portal.Point+normal*.25f)||room.Area.Contains(portal.Point-normal*.25f))){portal.To=room.Id;break;}
            }
            l.Signature="layout18-"+map+"-"+l.Rooms.Count+"-"+l.Portals.Count;return l;
        }
        BuildingRoom18 Room(int f,string name,float x,float z,float w,float h,bool open=false)
        {
            var r=new BuildingRoom18{Id="F"+f+"R"+Rooms.Count,Name=name,Floor=f,Area=new Rect(x,z,w,h),Open=open};Rooms.Add(r);return r;
        }
        void Door(BuildingRoom18 room,int side,float offset=0,string to="circulation")
        {
            var a=room.Area;var p=side==0?new Vector2(a.center.x+offset,a.yMax):side==2?new Vector2(a.center.x+offset,a.yMin):side==1?new Vector2(a.xMax,a.center.y+offset):new Vector2(a.xMin,a.center.y+offset);
            Portals.Add(new BuildingPortal18{Id="door"+Portals.Count,Floor=room.Floor,From=room.Id,To=to,Point=p,AlongX=side%2==0});
        }
        void Through(BuildingRoom18 r,bool eastWest=false){if(r.Open)return;Door(r,eastWest?1:0);Door(r,eastWest?3:2);}
        void Apartment(int f)
        {
            if(f==3)
            {
                Through(Room(f,"屋顶楼梯值班室",-18,-14,9,6));Through(Room(f,"通信设备间",9,8,9,6));
                Room(f,"水箱检修区",-17,7,8,6,true);Room(f,"排风与太阳能平台",9,-14,9,6,true);return;
            }
            string[] ground={"物业室","后门工具间","快递间","自行车库","门厅接待","杂物间","车库管理","服务配电房"};int n=0;
            foreach(int side in new[]{-1,1})foreach(int band in new[]{-1,1})
            {
                float x=side<0?-18:8,z=band<0?-14:5;
                if(f==0){var r=Room(f,ground[n++],x,z,10,9);Through(r,true);}
                else
                {
                    string unit=(f+1)+"0"+(++n);
                    var living=Room(f,(f==2&&n==2?"维修开放住宅":"住宅"+unit+"客厅玄关"),x,z,5,9);
                    var inner=Room(f,"住宅"+unit+(n%2==0?"厨房餐厅":"卧室书房"),x+5,z,5,9);
                    Through(living,true);Through(inner,true);
                }
            }
            Through(Room(f,f==0?"门厅会客区":f==1?"公共洗衣房":"设备维护室",-7,-14,6,6));
            Through(Room(f,f==0?"快递分拣":f==1?"洗衣储物间":"消防控制室",0,-14,7,6));
            Through(Room(f,f==0?"住户休息室":"公共阳台休息区",-7,8,6,6));
            Through(Room(f,f==0?"清洁工具间":"住户共享厨房",0,8,7,6));
        }
        void Office(int f)
        {
            string[][] names={new[]{"前台挑空大厅","访客洽谈区","咖啡区","安保室","收发室","员工入口","物业办公室","访客等候"},new[]{"开放办公区","项目协作区","玻璃会议室","电话会议室","打印室","茶水间","财务办公室","项目讨论室"},new[]{"服务器机房","网络运维区","档案室","备件维护间","人事办公室","管理办公室","保密会议室","行政支持"},new[]{"大型会议区","接待休息区","总经理办公室","董事会议室","秘书办公室","展示厅","设备出口","会务准备"}};
            var a=names[f];Room(f,a[0],-17,-12,18,13,true);Room(f,a[1],-17,2,18,4,true);
            Through(Room(f,a[2],6,-13,12,7),true);Through(Room(f,a[3],7,-3,11,8),true);
            Through(Room(f,a[4],-18,9,8,5));Through(Room(f,a[5],-8,9,8,5));Through(Room(f,a[6],2,9,7,5));Through(Room(f,a[7],11,9,7,5));
            Through(Room(f,f==0?"后勤储藏室":"维护通道工具室",-17,-15,8,3));
            Through(Room(f,f==0?"消防值守":"楼层设备间",-6,-15,7,3));
        }
        void Hospital(int f)
        {
            string[][] names={new[]{"备用电源机房","设备维修间","医疗仓储","消毒准备","后勤值班台","地下运送站","冷藏停尸间","耗材仓库","医疗废物间","清洁间"},new[]{"急诊检查室","药房","抢救室","分诊室","护士分诊台","救护车调度","影像检查","接待候诊","输液室","诊疗室"},new[]{"病房201","病房202","治疗室","配药室","护士站","清洁工作区","病房203","病房204","隔离病房","值班室"},new[]{"手术一室","手术二室","准备间","器械消毒","手术护士站","麻醉准备","术后观察","医生办公室","清洗室","值班休息"}};
            var a=names[f];Through(Room(f,a[0],-18,-14,10,7));Through(Room(f,a[1],-6,-14,8,7));Through(Room(f,a[2],4,-14,14,7));
            Through(Room(f,a[3],-17,-1.5f,10,3),true);Room(f,a[4],-3,-1.5f,7,3,true);Through(Room(f,a[5],8,-1.5f,9,3),true);
            Through(Room(f,a[6],-18,7,7,7));Through(Room(f,a[7],-9,7,8,7));Through(Room(f,a[8],1,7,8,7));Through(Room(f,a[9],11,7,7,7));
        }
        void Factory(int f)
        {
            if(f==0)
            {
                Room(f,"主生产车间",-18,-6,19,12,true);Room(f,"装卸平台",-18,9,12,6,true);Room(f,"成品发运区",-4,9,9,6,true);
                Room(f,"传送带加工区",3,-5,14,10,true);Through(Room(f,"原料仓库",7,9,11,6));
                Through(Room(f,"维修间",-18,-15,9,6));Through(Room(f,"质量检测",-7,-15,10,6));Through(Room(f,"设备电源室",6,-15,12,6));Room(f,"控制值班台",-5,5.5f,10,3,true);
            }
            else if(f==1)
            {
                Through(Room(f,"西侧检修控制室",-18,-15,9,6));Through(Room(f,"检修工具库",-7,-15,9,6));Through(Room(f,"配电控制室",5,-15,13,6));
                Through(Room(f,"风机维护室",-18,9,9,6));Through(Room(f,"设备监控室",-7,9,10,6));Through(Room(f,"安全值班室",5,9,13,6));
                Room(f,"西侧检修平台",-18,-6,4,12,true);Room(f,"东侧检修平台",12,-6,6,12,true);
            }
            else
            {
                Through(Room(f,"行政办公室",-18,-14,10,8));Through(Room(f,"生产调度",-6,-14,9,8));Through(Room(f,"会议室",6,-14,12,8));
                Through(Room(f,"工艺设计",-18,7,9,7));Through(Room(f,"培训室",-7,7,10,7));Through(Room(f,"档案室",5,7,6,7));Through(Room(f,"值班办公室",13,7,5,7));
                Room(f,"员工休息区",-15,-2,11,5,true);Room(f,"服务调度中庭",4,-2,12,5,true);
            }
        }
    }
}
