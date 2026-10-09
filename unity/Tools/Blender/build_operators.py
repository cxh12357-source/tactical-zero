"""Blender portrait modelling source. Run with Blender --background --python ... -- assault.
Coordinates in helpers are game coordinates (X right, Y up, Z forward), in metres.
The .blend retains named editable parts; game exports are evaluated and batched by limb.
"""
import bpy, math, json, sys, random
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'ArtSource/Blender'; OUT=ROOT/'Assets/TacticalZero/Art/BlenderOperators'
SOURCE.mkdir(parents=True,exist_ok=True); OUT.mkdir(parents=True,exist_ok=True)
ROLES={
 'assault':dict(cloth='343A3C',pants='444540',armor='C66B2C',trim='766247',skin='BE967D',hair='713B23',width=.245,female=True),
 'scout':dict(cloth='366774',pants='484A42',armor='315660',trim='796A4E',skin='AE805D',hair='292B2A',width=.24),
 'medic':dict(cloth='D0CEB6',pants='4F5C43',armor='7C926A',trim='697456',skin='C0A085',hair='39352C',width=.224,female=True),
 'engineer':dict(cloth='C18A35',pants='514D43',armor='B17B2E',trim='6B5540',skin='B68C6C',hair='413527',width=.29),
 'guardian':dict(cloth='353B40',pants='3C4246',armor='4C6071',trim='6B6254',skin='AC8E75',hair='272D30',width=.315),
 'marksman':dict(cloth='34373F',pants='373A42',armor='75516F',trim='665F59',skin='D4B39E',hair='B8BFC3',width=.215,female=True),
 'infiltrator':dict(cloth='4E5742',pants='43493A',armor='666E48',trim='766B4C',skin='B6A084',hair='34362A',width=.225),
 'supplier':dict(cloth='A59A7C',pants='77705B',armor='8F7150',trim='746046',skin='AD7B57',hair='3D3027',width=.285),
}
PALETTE=['cloth','pants','armor','trim','skin','hair','leather','metal','ivory','glass','lip','black','stitch','accent','sole','eye']
EXTRA=dict(leather='3C3831',metal='8C9290',ivory='D6D5C5',glass='42CED9',lip='986E5F',black='1C2428',stitch='A49477',accent='C3A465',sole='272B29',eye='D7D4BF')
objects=[]; group='Body'; colors={}; rng=random.Random(110); owner=''; atlasmat=None
def V(p):return Vector((p[0],-p[2],p[1]))
def U(v):return [round(v.x,6),round(v.z,6),round(-v.y,6)]
def linear(c):return c/12.92 if c<.04045 else ((c+.055)/1.055)**2.4
def rgb(h):return tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
def finish(o,name,mat):
 o.name=name;o['part_group']=group;o['surface']=mat;objects.append(o)
 o.data.materials.clear();o.data.materials.append(colors[mat])
 for poly in o.data.polygons:poly.use_smooth=True
 return o
def apply(o):
 bpy.context.view_layer.objects.active=o
 for m in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=m.name)
def box(name,p,s,mat='leather',bevel=.012,rot=(0,0,0)):
 bpy.ops.mesh.primitive_cube_add(size=1,location=V(p));o=bpy.context.object;o.scale=(s[0],s[2],s[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.rotation_euler=(math.radians(rot[0]),math.radians(-rot[2]),math.radians(rot[1]))
 if bevel:
  m=o.modifiers.new('Rounded manufactured edges','BEVEL');m.width=bevel;m.segments=3
  m=o.modifiers.new('Area weighted highlights','WEIGHTED_NORMAL');m.keep_sharp=True
 return finish(o,name,mat)
def ell(name,p,s,mat='skin',segments=24,rings=14):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=V(p));o=bpy.context.object;o.scale=(s[0],s[2],s[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,mat)
def mesh(name,verts,faces,mat,uvs=None,sub=0):
 data=bpy.data.meshes.new(name);data.from_pydata([V(v) for v in verts],[],faces);data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o)
 uv=data.uv_layers.new(name='Tailored UV')
 for poly in data.polygons:
  for li in poly.loop_indices:
   i=data.loops[li].vertex_index;uv.data[li].uv=uvs[i] if uvs else ((verts[i][0]*1.7)%1,(verts[i][1]*1.7)%1)
 if sub:m=o.modifiers.new('Tailored subdivision','SUBSURF');m.levels=sub;m.render_levels=sub
 return finish(o,name,mat)
def loft(name,rows,mat='cloth',cx=0,fold=.015,sides=40,sub=1):
 # rows = height, half width, half depth, front offset. Closed quad surface.
 vs=[];uv=[];fs=[]
 for j,(y,w,d,z) in enumerate(rows):
  for i in range(sides):
   a=i*math.tau/sides;f=fold*math.sin(7*a+j*1.8)*math.sin(math.pi*j/(len(rows)-1))
   vs.append((cx+math.sin(a)*(w+f),y,math.cos(a)*(d+f*.6)+z));uv.append((i/sides,j/(len(rows)-1)))
 for j in range(len(rows)-1):
  for i in range(sides):a=j*sides+i;b=j*sides+(i+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs.extend([tuple(reversed(range(sides))),tuple((len(rows)-1)*sides+i for i in range(sides))])
 return mesh(name,vs,fs,mat,uv,sub)
def tube(name,pts,r=.006,mat='stitch',radii=None):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=6;c.bevel_depth=r;c.bevel_resolution=2
 s=c.splines.new('BEZIER');s.bezier_points.add(len(pts)-1)
 for i,(pt,p) in enumerate(zip(s.bezier_points,pts)):
  pt.co=V(p);pt.handle_left_type=pt.handle_right_type='AUTO';pt.radius=radii[i] if radii else 1
 o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False);return finish(o,name,mat)
def rod(name,a,b,r,mat='metal'):
 d=V(b)-V(a);bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=d.length,location=(V(a)+V(b))*.5);o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();m=o.modifiers.new('Edge bevel','BEVEL');m.width=min(.004,r*.18);m.segments=2;return finish(o,name,mat)
def plate(name,p,s,mat='armor'):
 outline=[(-.35,-.5),(.35,-.5),(.5,-.30),(.48,.28),(.29,.5),(-.29,.5),(-.48,.28),(-.5,-.30)]
 vs=[]
 for depth in (-.4,.6):
  for x,y in outline:vs.append((p[0]+x*s[0],p[1]+y*s[1],p[2]+depth*s[2]-.05*s[2]*x*x))
 fs=[tuple(reversed(range(8))),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
 o=mesh(name,vs,fs,mat);m=o.modifiers.new('Armor chamfers','BEVEL');m.width=.006;m.segments=3;m=o.modifiers.new('Planar armor normals','WEIGHTED_NORMAL');m.keep_sharp=True;return o
def ribbon(name,pts,width,mat='trim'):
 vs=[];uv=[]
 dx=pts[-1][0]-pts[0][0];dy=pts[-1][1]-pts[0][1];length=max(.001,math.hypot(dx,dy));sx=dy/length;sy=-dx/length
 for i,p in enumerate(pts):
  vs.extend([(p[0]-sx*width/2,p[1]-sy*width/2,p[2]),(p[0]+sx*width/2,p[1]+sy*width/2,p[2])]);uv.extend([(0,i/(len(pts)-1)),(1,i/(len(pts)-1))])
 o=mesh(name,vs,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(pts)-1)],mat,uv,0);m=o.modifiers.new('Woven strap thickness','SOLIDIFY');m.thickness=.004

 return o
def buckle(name,p,w=.065,h=.04):
 x,y,z=p;tube(name,[(x-w/2,y-h/2,z),(x+w/2,y-h/2,z),(x+w/2,y+h/2,z),(x-w/2,y+h/2,z),(x-w/2,y-h/2,z)],.0045,'metal');rod(name+' pin',(x,y-h/2,z),(x,y+h/2,z),.0025)
def pouch(name,p,s,mat='trim'):
 x,y,z=p;box(name,p,s,mat,min(s)*.15);box(name+' fold over flap',(x,y+s[1]*.26,z+s[2]*.5),(s[0]*1.025,s[1]*.32,.018),mat,.008)
 tube(name+' reinforced seam',[(x-s[0]*.4,y+s[1]*.1,z+s[2]*.57),(x-s[0]*.4,y-s[1]*.39,z+s[2]*.53),(x+s[0]*.4,y-s[1]*.39,z+s[2]*.53),(x+s[0]*.4,y+s[1]*.1,z+s[2]*.57)],.0018,'stitch')
 box(name+' pull tab',(x,y+s[1]*.06,z+s[2]*.61),(.022,.041,.009),'leather',.003)
def hair_lock(name,pts,width,mat='hair'):
 vs=[];uv=[];faces=[]
 for j,p in enumerate(pts):
  taper=math.sin(math.pi*(j+.5)/(len(pts)+.1))**.55 if j<len(pts)-1 else .01
  for k in range(5):
   u=k/4*2-1;vs.append((p[0]+u*width*taper,p[1]+(1-u*u)*.003,p[2]+(1-u*u)*.005));uv.append((k/4,j/(len(pts)-1)))
 for j in range(len(pts)-1):
  for k in range(4):a=j*5+k;faces.append((a,a+1,a+6,a+5))
 o=mesh(name,vs,faces,mat,uv,1);m=o.modifiers.new('Lock thickness','SOLIDIFY');m.thickness=.003;return o
def face(role):
 female=role.get('female',False);fw=.95 if female else 1.06
 if owner=='guardian':
  loft('Neck seal',[(1.46,.065,.069,0),(1.53,.067,.072,0),(1.59,.065,.068,0)],'black',fold=.002);return
 rows=[(1.566,.037,.048,.054),(1.58,.060,.068,.04),(1.605,.075,.077,.022),(1.635,.087,.086,.012),(1.666,.094,.097,.002),(1.700,.097,.101,0),(1.735,.094,.105,-.005),(1.77,.088,.096,-.007),(1.80,.068,.075,-.01),(1.819,.025,.027,-.011)]
 vs=[];uv=[];fs=[];sides=64
 for j,(y,w,d,z) in enumerate(rows):
  for i in range(sides):
   a=i*math.tau/sides;x=math.sin(a)*w*fw;front=max(0,math.cos(a));zz=math.cos(a)*d+z
   # Integrated bridge, nose tip, cheekbones, muzzle and brow planes.
   zz+=front**12*(.022*math.exp(-((y-1.703)/.034)**2)*math.exp(-(x/.018)**2)+.058*math.exp(-((y-1.668)/.019)**2)*math.exp(-(x/.022)**2))
   zz+=front**5*(.012*math.exp(-((abs(x)-.056)/.02)**2-((y-1.678)/.026)**2)+.014*math.exp(-(x/.040)**2-((y-1.624)/.028)**2))
   vs.append((x,y,zz));uv.append((i/sides,j/(len(rows)-1)))
 for j in range(len(rows)-1):
  for i in range(sides):a=j*sides+i;b=j*sides+(i+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs+=[tuple(reversed(range(sides))),tuple((len(rows)-1)*sides+i for i in range(sides))];mesh('Sculpted face - bridge cheek jaw planes',vs,fs,'skin',uv,2)
 loft('Neck anatomy',[(1.46,.062,.067,0),(1.50,.064,.063,.005),(1.55,.055,.064,.012),(1.585,.055,.063,.009)],'skin',fold=0,sub=1)
 for s in (-1,1):
  x=s*.043;ell('Eye sclera',(x,1.706,.100),(.020,.0065,.007),'eye');ell('Iris',(x,1.706,.107),(.0058,.006,.002),'armor',20,12);ell('Pupil',(x,1.706,.109),(.0026,.0035,.001),'black',16,10);ell('Eye catchlight',(x-.0015,1.708,.110),(.001,.001,.0006),'ivory',12,8)
  tube('Upper eyelid',[(x-.021,1.704,.101),(x-.011,1.711,.109),(x+.009,1.711,.109),(x+.021,1.705,.100)],.0023,'skin');tube('Lower eyelid',[(x-.021,1.704,.101),(x,1.701,.108),(x+.021,1.705,.100)],.002,'skin')
  tube('Expressive brow',[(x-s*.025,1.730,.109),(x,1.737,.109),(x+s*.028,1.729,.097)],.004,'hair',[.7,1,.2])
  ell('Ear pinna',(s*.098*fw,1.686,-.005),(.016,.034,.022),'skin');tube('Ear helix',[(s*.11*fw,1.662,.002),(s*.119*fw,1.685,.007),(s*.108*fw,1.714,.001),(s*.099*fw,1.699,.015)],.004,'skin');ell('Nostril',(s*.014,1.651,.131),(.006,.0035,.004),'lip',16,8)
 tube('Upper lip cupid bow',[(-.031,1.623,.103),(-.012,1.626,.119),(0,1.623,.122),(.012,1.626,.119),(.031,1.623,.103)],.003,'lip',[.3,.9,.65,.9,.3]);tube('Lower lip',[(-.029,1.621,.104),(0,1.616,.121),(.029,1.621,.104)],.0038,'lip',[.2,1,.2]);tube('Mouth separation',[(-.028,1.622,.108),(0,1.622,.123),(.028,1.622,.108)],.0011,'black')
 if owner in ('engineer','supplier','scout'):
  if owner!='scout':ell('Trimmed beard underlayer',(0,1.592,.057),(.074,.032,.066),'hair',32,18)
  for k in range(75 if owner!='scout' else 25):
   x=rng.uniform(-.073,.073);y=1.585+abs(x)*.45+rng.random()*.03;z=.099-abs(x)*.35
   hair_lock('Individual beard lock',[(x,y+.017,z),(x+.003,y-.009,z+.01),(x,y-.025,z-.002)],.0028)
 if owner not in ('guardian','infiltrator','medic','engineer'):
  # Layered locks with an asymmetrical side part instead of a single helmet-like hair mass.
  hv=[];hf=[]
  for j in range(10):
   for k in range(40):
    a=k/40*math.tau;phi=.02+j/9*(1.22+.63*(1-math.cos(a))/2);hv.append((math.sin(phi)*math.sin(a)*.103*fw,1.743+math.cos(phi)*.105,-.009+math.sin(phi)*math.cos(a)*.107))
  for j in range(9):
   for k in range(40):a=j*40+k;b=j*40+(k+1)%40;hf.append((a,b,b+40,a+40))
  mesh('Close cropped hair cap',hv,hf,'hair',sub=1)
  count=45 if owner in ('assault','marksman') else 28
  for k in range(count):
   x=rng.uniform(-.084,.074);z=rng.uniform(-.06,.065);top=1.805+.035*math.cos(x*13)+rng.random()*.018
   sweep=-.042 if owner in ('assault','marksman') else .022
   hair_lock('Swept hair clump %02d'%k,[(x,top-.024,z-.02),(x+sweep*.5,top,z+.014),(x+sweep,top-.023,z+.05),(x+sweep*1.5,top-.075-rng.random()*.045,z+.065)],.008+rng.random()*.009)
 if owner=='medic':
  for s in (-1,1):
   for k in range(8):hair_lock('Jaw length bob',[(s*(.081+k*.003),1.726,.006),(s*.106,1.681,.02+k*.006),(s*.087,1.591,.023+k*.005)],.009)
 if owner=='assault':tube('Cheek scar',[(.061,1.688,.092),(.065,1.672,.096),(.06,1.655,.096)],.0013,'lip')

def body(role):
 global group
 w=role['width'];female=role.get('female',False);heavy=owner=='guardian';group='Body'
 rows=[(.89,w*.77,.135,0),(.93,w*.79,.145,0),(1.0,w*.72,.135,0),(1.10,w*.74,.134,0),(1.22,w*.90,.154,.005),(1.34,w,.161,0),(1.43,w*.96,.143,-.01),(1.48,w*.61,.105,0),(1.50,.087,.074,0)]
 loft('Tailored jacket panels',rows,'cloth',fold=.009,sub=2)
 if owner=='medic':loft('Split medical coat hem',[(.75,.265,.166,.006),(.79,.262,.17,0),(.84,.245,.16,0),(.94,.198,.137,0),(1.0,.20,.138,0)],'cloth',fold=.012)
 # Zipper teeth and folded collar, separate hems and reinforcing seams.
 tube('Jacket front zipper',[(0,.95,.150),(0,1.10,.142),(0,1.23,.163),(0,1.36,.163),(0,1.48,.085)],.0028,'leather')
 for k in range(20):
  y=1.0+k*.018;z=.142 if y<1.10 else .142+min(1,(y-1.10)/.13)*.02;box('Zipper tooth',((k%2-.5)*.006,y,z),(.006,.004,.004),'metal',.001)
 for s in (-1,1):
  plate('Standing collar',(s*.06,1.49,.065),(.106,.132,.045),'cloth')

  path=[(s*w*.71,1.01,.154),(s*w*.76,1.25,.18),(s*w*.64,1.45,.132),(s*w*.65,1.50,-.02),(s*w*.65,1.42,-.155),(s*w*.69,1.04,-.17)]
  ribbon('Padded backpack harness',path,.049)
  buckle('Harness adjuster',(s*w*.74,1.37,.187));pouch('Hip utility pouch',(s*(w+.008),.955,.02),(.115,.175,.115))
 if owner not in ('scout','medic','engineer','infiltrator'):
  plate('Contoured plate carrier',(0,1.278,.149),(w*1.60,.375,.073),'armor' if heavy else 'leather')
  for k in range(3):
   y=1.31+k*.039;box('MOLLE woven row',(0,y,.202),(w*1.36,.015,.012),'trim',.003)
   for j in range(5):box('MOLLE bar tack',((j-2)*w*.26,y,.211),(.005,.016,.004),'stitch',.001)
  for k in range(4 if heavy else 3):pouch('Quick access magazine',((k-(1.5 if heavy else 1))*.106,1.134,.218),(.092,.159,.064))
 else:
  for s in (-1,1):pouch('Pleated chest pocket',(s*.105,1.337,.155),(.117,.12,.044),'armor' if owner=='medic' else 'cloth')
 loft('Contoured equipment belt',[(.925,w*.81,.155,0),(.931,w*.82,.156,0),(.985,w*.81,.156,0),(.992,w*.8,.153,0)],'leather',fold=0,sub=0)
 buckle('Waist quick release',(0,.96,.172),.078,.051)
 for s in (-1,1):
  for k in range(3):box('Belt keeper',(s*(.075+k*.044),.96,.163),(.016,.077,.014),'trim',.003)
 # Anatomical limbs. Cloth folds are mesh geometry, detail does not alter collision.
 for s in (-1,1):
  group='LeftLeg' if s<0 else 'RightLeg';cx=s*(.155 if heavy else .13)
  loft('Trouser with compression folds',[(.15,.074,.082,.02),(.19,.08,.086,.005),(.26,.088,.087,-.003),(.31,.077,.085,0),(.36,.086,.092,.006),(.40,.08,.088,.016),(.46,.09,.100,.025),(.52,.102,.107,.013),(.61,.118,.119,0),(.71,.123,.123,0),(.84,.125,.126,0),(.91,.105,.121,0)],'pants',cx,fold=.009,sub=1)

  pouch('Thigh cargo pocket',(cx+s*.09,.726,.092),(.12,.20,.075),'armor' if owner=='medic' else 'trim')
  for y in (.55,.82):ribbon('Thigh strap',[(cx-.112,y,.078),(cx,y,.133),(cx+.112,y,.078)],.026,'leather')
  plate('Floating patella protector',(cx,.47,.104),(.164,.194,.052),'armor' if heavy or owner=='assault' and s<0 else 'leather')
  plate('Knee inset',(cx,.479,.139),(.116,.132,.016),'leather')
  for dx in (-.058,.058):ell('Knee plate fastener',(cx+dx,.485,.154),(.007,.007,.003),'metal',12,8)
  if heavy:
   plate('Shin greave',(cx,.29,.078),(.172,.24,.075),'armor');plate('Shin raised ridge',(cx,.29,.13),(.035,.21,.015),'metal')
  loft('Boot ankle gusset',[(.06,.098,.108,.042),(.12,.094,.10,.018),(.18,.084,.091,0),(.24,.088,.089,0),(.255,.094,.098,0)],'ivory' if owner=='medic' else 'leather',cx,fold=.003)
  ell('Boot sculpted vamp',(cx,.094,.075),(.101,.067,.171),'ivory' if owner=='medic' else 'leather',32,18)
  box('Layered sole welt',(cx,.039,.06),(.202,.047,.325),'sole',.014)
  ell('Rubber toe reinforcement',(cx,.09,.197),(.094,.047,.052),'leather',28,16)
  for k in range(6):
   y=.104+k*.024;z=.186-k*.019
   tube('Crossed laces',[(cx-.043,y,z),(cx+.043,y+.018,z-.014),(cx-.043,y+.019,z-.018)],.0027,'trim')
   for dx in (-.051,.051):ell('Brass boot eyelet',(cx+dx,y,z),(.005,.005,.003),'metal',12,8)
  for k in range(5):
   for side in (-1,1):box('Outsole lug',(cx+side*.091,.025,-.052+k*.061),(.027,.027,.034),'sole',.004)
  group='LeftArm' if s<0 else 'RightArm';ax=s*(w+.03)
  loft('Shaped shoulder and sleeve',[(1.125,.081,.080,.015),(1.16,.096,.094,.008),(1.20,.091,.091,.006),(1.29,.105,.100,0),(1.39,.103,.107,-.005),(1.45,.073,.076,0),(1.475,.026,.033,0)],'cloth',ax,fold=.007)
  rolled=owner in ('assault','scout','medic','engineer','supplier')
  if rolled:loft('Thick rolled cuff',[(1.12,.089,.084,.018),(1.134,.103,.103,.013),(1.167,.104,.103,.009),(1.186,.098,.1,.008)],'cloth',ax,fold=.003)
  fore=ax+s*.027;loft('Forearm anatomy',[(.88,.052,.057,.07),(.92,.059,.06,.064),(1.02,.075,.072,.044),(1.09,.085,.078,.025),(1.14,.080,.076,.017)],'skin' if rolled else 'cloth',fore,fold=.002)
  if heavy or owner=='assault' and s<0:
   plate('Layered shoulder shell',(ax+s*.015,1.365,.073),(.274 if heavy else .24,.25,.145),'armor');plate('Shoulder secondary scale',(ax+s*.018,1.233,.101),(.223,.135,.072),'armor')
   for dy in (-.065,.06):ell('Armor recessed rivet',(ax+s*.085,1.36+dy,.17),(.007,.007,.004),'metal',12,8)
  if heavy:plate('Forearm bracer',(fore,1.026,.106),(.162,.225,.056),'armor')
  box('Wrist closure',(fore,.918,.066),(.12,.046,.12),'leather',.013)
  hx=fore+s*.012;ell('Anatomical glove palm',(hx,.849,.07),(.059,.079,.040),'leather')
  for k in range(4):
   fx=hx-.042+k*.028;length=.070+(.012 if k in (1,2) else 0)
   tube('Articulated glove finger',[(fx,.827,.077),(fx,.792,.086),(fx,.827-length,.10)],.011,'leather',[1,.97,.75])
   plate('Glove knuckle pad',(fx,.854,.109),(.021,.029,.009),'trim')
  tube('Opposable glove thumb',[(hx-s*.046,.876,.077),(hx-s*.074,.842,.095),(hx-s*.070,.812,.107)],.015,'leather',[1,.85,.7])
 group='Body';face(role)

def accessories(role):
 global group
 w=role['width'];group='Body'
 # Functional microphone, kept as named source geometry and a Unity compatibility marker.
 ell('Radio earpiece',(-.106,1.699,-.008),(.016,.025,.025),'leather')
 tube('Microphone boom',[(-.112,1.692,.011),(-.125,1.652,.055),(-.046,1.636,.124)],.0035,'black');box('Microphone',(-.043,1.636,.127),(.025,.012,.014),'black',.005)
 if owner in ('assault','supplier','scout'):
  loft('Folded neck scarf',[(1.425,.107,.091,.022),(1.462,.117,.104,.017),(1.499,.095,.096,.01),(1.526,.081,.086,.006)],'trim',fold=.011,sub=1)
  for k in range(4):tube('Scarf fold ridge',[(-.09,1.49-k*.011,.048),(0,1.437+k*.012,.13),(.08,1.48-k*.012,.066)],.0028,'trim')
  if owner=='assault':ribbon('Loose scarf tail',[(-.065,1.49,-.085),(-.13,1.39,-.195),(-.18,1.24,-.211)],.1,'trim')
 if owner in ('medic','engineer','guardian'):
  if owner=='guardian':
   loft('Closed composite helmet',[(1.578,.067,.075,.025),(1.614,.10,.099,.011),(1.66,.119,.109,0),(1.735,.117,.12,-.006),(1.79,.1,.111,-.009),(1.833,.058,.06,-.015),(1.838,.015,.022,-.012)],'armor',fold=0,sub=1)
   plate('Faceted face plate',(0,1.658,.112),(.177,.13,.035),'armor');box('Visor recess',(0,1.721,.116),(.184,.032,.026),'black',.01);box('Narrow illuminated slit',(0,1.724,.132),(.164,.008,.007),'glass',.003)
   plate('Helmet crown ridge',(0,1.807,.024),(.11,.064,.125),'armor')
   for s in (-1,1):plate('Temple shield',(s*.104,1.712,.05),(.041,.119,.046),'armor')
  else:
   loft('Protective helmet shell',[(1.704,.117,.112,-.007),(1.719,.117,.119,-.006),(1.774,.110,.116,-.009),(1.822,.073,.084,-.01),(1.836,.027,.032,-.01)],'ivory' if owner=='medic' else 'armor',fold=0,sub=2)
   for s in (-1,1):
    ell('Helmet ear protector',(s*.106,1.689,-.004),(.024,.042,.032),'armor');ribbon('Helmet chin webbing',[(s*.1,1.707,.03),(s*.087,1.633,.04),(s*.035,1.59,.058)],.013,'leather')
   if owner=='engineer':
    box('Safety helmet brow',(0,1.723,.079),(.265,.027,.146),'armor',.018)
    for s in (-1,1):
     ell('Goggle rubber seal',(s*.048,1.768,.101),(.043,.03,.02),'leather');ell('Goggle tinted lens',(s*.048,1.768,.117),(.034,.022,.009),'black');tube('Goggle metal frame',[(s*.048-.035,1.759,.126),(s*.048,1.790,.121),(s*.048+.035,1.76,.126),(s*.048,1.747,.128),(s*.048-.035,1.759,.126)],.0028,'metal')
    tube('Helmet raised spine',[(0,1.73,.119),(0,1.83,.02),(0,1.813,-.08)],.009,'armor')
 if owner=='marksman':
  cap=ell('Asymmetric wool beret',(.022,1.81,-.012),(.117,.045,.111),'armor');cap.rotation_euler.y=.17
  tube('Beret piping',[(-.098,1.786,.026),(-.077,1.791,.092),(0,1.792,.108),(.084,1.8,.063)],.004,'leather')
  for s in (-1,1):ribbon('Beret leaf insignia',[(.018,1.811,.098),(.018+s*.018,1.82,.103),(.018+s*.01,1.837,.090)],.007,'accent')
  # Draped asymmetric cape with pleats, shaped shoulder and pointed hem.
  vs=[];uv=[];fs=[]
  for j in range(15):
   t=j/14
   for k in range(17):
    a=.75+k/16*2.5;r=.24+min(1,t*5)*.18+t*.025;x=math.sin(a)*r;y=1.50-t*(.68+.1*math.sin(k*.6));z=-.015+math.cos(a)*r*.88-.009*math.sin(k*1.7)
    vs.append((x,y,z));uv.append((k/16,t))
  for j in range(14):
   for k in range(16):a=j*17+k;fs.append((a,a+1,a+18,a+17))
  o=mesh('Draped sharpshooter half cape',vs,fs,'armor',uv,1);m=o.modifiers.new('Lined cape thickness','SOLIDIFY');m.thickness=.006
  rod('Optic transport tube',(.15,1.1,-.21),(.15,1.78,-.21),.047,'leather')
  for y in (1.15,1.44,1.70):box('Optic tube strap',(.15,y,-.21),(.11,.026,.106),'trim',.008)
 if owner=='infiltrator':
  vs=[];fs=[];uv=[]
  hoodrows=[(1.47,.163,.80),(1.56,.158,.80),(1.66,.153,.80),(1.76,.15,.80),(1.85,.137,.38),(1.93,.067,0),(1.965,.009,0)]
  for j,(y,r,start) in enumerate(hoodrows):
   for k in range(33):
    a=start+k/32*(math.tau-2*start);vs.append((math.sin(a)*r,y,-.018+math.cos(a)*r*1.04));uv.append((k/32,j/6))
  for j in range(6):
   for k in range(32):a=j*33+k;fs.append((a,a+1,a+34,a+33))
  o=mesh('Open pointed hood',vs,fs,'armor',uv,1);m=o.modifiers.new('Hood double fabric','SOLIDIFY');m.thickness=.008
  loft('Wrapped face veil',[(1.574,.077,.083,.040),(1.60,.088,.11,.040),(1.641,.106,.13,.038),(1.683,.106,.13,.034)],'cloth',fold=.006,sub=1)
  for k in range(42):
   a=rng.uniform(-2.8,2.8);y=rng.uniform(1.19,1.49);r=.22+(1.49-y)*.3;x=math.sin(a)*r;z=-.012+math.cos(a)*r*.79
   hair_lock('Layered camouflage leaf',[(x,y,z),(x*1.20,y-.06,z*1.15),(x*1.26,y-.18-rng.random()*.06,z*1.28)],.035+rng.random()*.018,'armor' if k%3 else 'cloth')
 if owner=='supplier':
  ell('Canvas field cap',(0,1.802,-.018),(.105,.058,.10),'cloth');box('Curved cap brim',(0,1.775,.108),(.20,.018,.15),'cloth',.022);plate('Cap insignia',(0,1.805,.077),(.045,.026,.005),'trim')
 # Role-specific back silhouettes and exposed equipment.
 bw=.32 if owner=='supplier' else .30 if owner in ('engineer','guardian') else .25
 bh=.69 if owner=='supplier' else .63 if owner=='guardian' else .49
 box('Backpack padded main body',(0,1.22,-.219),(bw,bh,.18),'armor' if owner=='medic' else 'trim',.048)
 for s in (-1,1):
  ribbon('Backpack compression strap',[(s*bw*.33,.99,-.322),(s*bw*.33,1.28,-.328),(s*bw*.33,1.52,-.302)],.029,'leather')
  buckle('Pack ladder lock',(s*bw*.33,1.27,-.335),.05,.037)
 if owner=='scout':
  for s in (-1,1):
   x=s*.093;rod('Antenna base',(x,1.43,-.29),(x,1.61,-.29),.014,'metal');rod('Flexible radio antenna',(x,1.59,-.29),(x,1.95+(s+1)*.045,-.29),.004,'black')
  box('Radio face',(0,1.35,-.332),(.21,.20,.035),'armor',.013)
  for k in range(4):box('Radio cooling vent',(0,1.30+k*.026,-.356),(.14,.009,.009),'black',.001)
  rod('Monocular housing',(.073,1.714,.101),(.073,1.714,.142),.023,'leather');ell('Monocular lens',(.073,1.714,.148),(.019,.019,.004),'glass')
  for s in (-1,1):rod('Compact binocular barrel',(s*.034,1.15,.24),(s*.034,1.32,.24),.026,'leather')
 if owner=='medic':
  box('Medical pack face',(0,1.28,-.319),(.25,.38,.028),'ivory',.035)
  for s in (-1,1):pouch('Insulated medical side pod',(s*.18,1.21,-.20),(.112,.29,.15),'ivory')
  ell('Medical insignia disc',(0,1.295,-.343),(.079,.079,.005),'armor')
  tube('Medical heartbeat emblem',[(-.066,1.29,-.351),(-.037,1.29,-.351),(-.021,1.321,-.351),(0,1.255,-.351),(.021,1.33,-.351),(.031,1.29,-.351),(.065,1.29,-.351)],.005,'ivory')
  for s in (-1,1):
   for k in range(3):rod('Sealed medical vial',(s*.098+(k-1)*.024,1.30,.214),(s*.098+(k-1)*.024,1.38,.214),.010,'ivory');box('Vial cap',(s*.098+(k-1)*.024,1.386,.214),(.021,.014,.019),'armor',.003)
 if owner=='engineer':
  box('Open tool rack frame',(0,1.28,-.325),(.32,.43,.052),'armor',.012)
  box('Tool rack recess',(0,1.3,-.359),(.265,.32,.023),'black',.008)
  for k in range(4):rod('Slotted rack tool',(-.09+k*.06,1.13,-.387),(-.09+k*.06,1.39,-.387),.01,'metal');box('Tool grip',(-.09+k*.06,1.15,-.387),(.028,.1,.035),'leather',.006)
  rod('Long wrench shaft',(.17,1.28,-.255),(.21,1.85,-.255),.017,'metal')
  tube('Open wrench head',[(.174,1.87,-.255),(.168,1.815,-.255),(.21,1.80,-.255),(.25,1.825,-.255),(.242,1.88,-.255)],.016,'metal')
 if owner=='guardian':
  plate('Back carried ballistic shield',(0,1.2,-.34),(.48,.83,.065),'armor')
  for y in (.91,1.17,1.42):box('Shield reinforcing ribs',(0,y,-.386),(.43,.023,.025),'metal',.007)
  for s in (-1,1):
   for y in (.90,1.10,1.30,1.50):ell('Shield fastener',(s*.187,y,-.395),(.008,.008,.005),'metal',12,8)
 if owner=='supplier':
  for s in (-1,1):
   for y in (1.08,1.36):pouch('External ammunition parcel',(s*.208,y,-.20),(.135,.215,.20),'armor')
  rod('Rolled sleeping mat',(-.21,1.64,-.223),(.21,1.64,-.223),.091,'trim')
  for s in (-1,1):
   x=s*.145;tube('Bedroll cinch',[(x,1.64,-.122),(x,1.728,-.22),(x,1.64,-.316),(x,1.551,-.22),(x,1.64,-.122)],.012,'leather')
  pts=[]
  for i in range(70):a=i/69*math.pi*5;r=.082*(1-i/80);pts.append((.218,1.64+math.sin(a)*r,-.223+math.cos(a)*r))
  tube('Exposed blanket spiral',pts,.006,'cloth')

def make_materials(role):
 global colors,atlasmat
 colors={};pal={**EXTRA,**{k:v for k,v in role.items() if k in PALETTE}}
 # A single atlas per character makes five limb renderers share one material in Unity.
 size=1024;tile=size//4;y,x=np.mgrid[0:tile,0:tile];randoms=np.random.default_rng(711);pixels=np.ones((size,size,4),np.float32)
 for i,key in enumerate(PALETTE):
  color=np.array(rgb(pal[key]));noise=randoms.random((tile,tile));weave=(np.sin(x*math.pi/2)*np.cos(y*math.pi/2))*.018
  mottling=np.sin(x*.13+np.cos(y*.06))*np.cos(y*.12)*.025;variation=(noise-.5)*.035+mottling+weave
  if key in ('skin','lip','eye','glass'):variation=(noise-.5)*.012
  if key in ('armor','metal'):
   scratches=((noise>.989)&(x%11<2))*.18;variation+=scratches
  a=i//4*tile;b=i%4*tile;pixels[a:a+tile,b:b+tile,:3]=np.clip(color[None,None,:]*(1+variation[:,:,None]),0,1)
  m=bpy.data.materials.new(owner+' '+key);m.diffuse_color=(*[linear(c) for c in color],1);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=m.diffuse_color;bs.inputs['Roughness'].default_value=.38 if key=='metal' else .73;bs.inputs['Metallic'].default_value=.65 if key=='metal' else .15 if key=='armor' else 0
  if key=='glass':bs.inputs['Emission Color'].default_value=m.diffuse_color;bs.inputs['Emission Strength'].default_value=.8
  colors[key]=m
 image=bpy.data.images.new(owner+'_albedo',width=size,height=size);image.pixels.foreach_set(pixels.reshape(-1));image.filepath_raw=str(OUT/(owner+'_albedo.png'));image.file_format='PNG';image.save();bpy.data.images.remove(image);image=bpy.data.images.load(str(OUT/(owner+'_albedo.png')));image.pack()
 atlasmat=bpy.data.materials.new(owner+' Game Atlas');atlasmat.use_nodes=True;bs=atlasmat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.68
 tex=atlasmat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;atlasmat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 # A tangent-space textile normal atlas, quiet enough to preserve face shading.
 normal=np.ones((size,size,4),np.float32);normal[:,:,:3]=(.5,.5,1)
 for i,key in enumerate(PALETTE):
  if key not in ('cloth','pants','trim','leather'):continue
  a=i//4*tile;b=i%4*tile;normal[a:a+tile,b:b+tile,0]=.5+np.sin(x*math.pi/2)*.065;normal[a:a+tile,b:b+tile,1]=.5+np.sin(y*math.pi/2)*.065
 ni=bpy.data.images.new(owner+'_normal',width=size,height=size);ni.colorspace_settings.name='Non-Color';ni.pixels.foreach_set(normal.reshape(-1));ni.filepath_raw=str(OUT/(owner+'_normal.png'));ni.file_format='PNG';ni.save();ni.pack()
 nt=atlasmat.node_tree.nodes.new('ShaderNodeTexImage');nt.image=ni;nm=atlasmat.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.4;atlasmat.node_tree.links.new(nt.outputs['Color'],nm.inputs['Color']);atlasmat.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
 mask=np.zeros((size,size,4),np.float32)
 for i,key in enumerate(PALETTE):
  a=i//4*tile;b=i%4*tile;mask[a:a+tile,b:b+tile,0]=.7 if key=='metal' else .22 if key=='armor' else 0;mask[a:a+tile,b:b+tile,3]=.67 if key in ('metal','glass','eye') else .40 if key=='armor' else .30 if key=='skin' else .20
 mi=bpy.data.images.new(owner+'_mask',width=size,height=size,alpha=True);mi.colorspace_settings.name='Non-Color';mi.pixels.foreach_set(mask.reshape(-1));mi.filepath_raw=str(OUT/(owner+'_mask.png'));mi.file_format='PNG';mi.save();mi.pack()
 mt=atlasmat.node_tree.nodes.new('ShaderNodeTexImage');mt.image=mi;separate=atlasmat.node_tree.nodes.new('ShaderNodeSeparateColor');atlasmat.node_tree.links.new(mt.outputs['Color'],separate.inputs['Color']);atlasmat.node_tree.links.new(separate.outputs['Red'],bs.inputs['Metallic']);inv=atlasmat.node_tree.nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;atlasmat.node_tree.links.new(mt.outputs['Alpha'],inv.inputs[1]);atlasmat.node_tree.links.new(inv.outputs[0],bs.inputs['Roughness'])

LOD_RATIOS=(.70,.23)
PIVOTS={'Body':(0,0,0),'LeftLeg':(-.13,.90,0),'RightLeg':(.13,.90,0),'LeftArm':(-.27,1.45,0),'RightArm':(.27,1.45,0)}
def export_game(prepared=False):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:
  if not prepared:apply(o)
  if not o.data.uv_layers:o.data.uv_layers.new(name='UV')
  o.data.uv_layers.active.name='UVMap'
  tile=PALETTE.index(o['surface']);uv=o.data.uv_layers.active
  if not prepared:
   for loop in uv.data:
    u,v=loop.uv;loop.uv=((tile%4+.035+.93*max(0,min(1,u)))/4,(tile//4+.035+.93*max(0,min(1,v)))/4)
  o.data.materials.clear();o.data.materials.append(atlasmat)
 # Keep named editable geometry in .blend; evaluate copies for exports.
 groups=[];fbx=[]
 for name,pivot in PIVOTS.items():
  copies=[]
  for o in objects:
   if o['part_group']!=name:continue
   cp=o.copy();cp.data=o.data.copy();bpy.context.collection.objects.link(cp);copies.append(cp)
  bpy.ops.object.select_all(action='DESELECT')
  for o in copies:o.select_set(True)
  bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
  bpy.context.scene.cursor.location=V(pivot);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
  bpy.context.view_layer.objects.active=o;fbx.append(o)
  def data_for(obj,lod):
   me=obj.data;me.calc_loop_triangles();positions=[];normals=[];uvs=[];indices=[];lookup={}
   for t in me.loop_triangles:
    face=[]
    for li in t.loops:
     vi=me.loops[li].vertex_index;v=me.vertices[vi];n=me.corner_normals[li].vector;uv=me.uv_layers.active.data[li].uv
     key=(vi,round(n.x,4),round(n.y,4),round(n.z,4),round(uv.x,5),round(uv.y,5))
     if key not in lookup:
      lookup[key]=len(positions)//3;positions+=U(v.co);normals+=U(n);uvs += [round(uv.x,6),round(uv.y,6)]
     face.append(lookup[key])
    indices.extend((face[0],face[1],face[2]))
   return dict(name=name,lod=lod,pivot=list(pivot),positions=positions,normals=normals,uvs=uvs,indices=indices)
  game_lod=o.copy();game_lod.data=o.data.copy();bpy.context.collection.objects.link(game_lod);bpy.context.view_layer.objects.active=game_lod
  d=game_lod.modifiers.new('Game detail optimization','DECIMATE');d.ratio=LOD_RATIOS[0];apply(game_lod);groups.append(data_for(game_lod,0));bpy.data.objects.remove(game_lod,do_unlink=True)
  low=o.copy();low.data=o.data.copy();bpy.context.collection.objects.link(low);bpy.context.view_layer.objects.active=low
  d=low.modifiers.new('Distance LOD simplification','DECIMATE');d.ratio=LOD_RATIOS[1];apply(low);groups.append(data_for(low,1));bpy.data.objects.remove(low,do_unlink=True)
 bpy.ops.object.select_all(action='DESELECT')
 for o in fbx:o.select_set(True)
 bpy.context.view_layer.objects.active=fbx[0]
 bpy.ops.export_scene.fbx(filepath=str(SOURCE/(owner+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True)
 document=dict(role=owner,source='Blender '+bpy.app.version_string,groups=groups)
 (OUT/(owner+'.mesh.json')).write_text(json.dumps(document,separators=(',',':')),encoding='utf-8')
 counts={str(lod):sum(len(g['indices'])//3 for g in groups if g['lod']==lod) for lod in (0,1)}
 for o in fbx:bpy.data.objects.remove(o,do_unlink=True)
 return counts
def editable_rig():
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.select_all(action='DESELECT');bpy.ops.object.armature_add(location=(0,0,0));rig=bpy.context.object;rig.name=owner+' editable posing skeleton';rig.show_in_front=True
 bpy.ops.object.mode_set(mode='EDIT');bones=rig.data.edit_bones
 for b in list(bones):bones.remove(b)
 def bone(name,a,b,parent=None):
  e=bones.new(name);e.head=V(a);e.tail=V(b)
  if parent:e.parent=bones[parent]
 bone('Root',(0,0,0),(0,.2,0));bone('Pelvis',(0,.84,0),(0,1.0,0),'Root');bone('Spine',(0,1.,0),(0,1.45,0),'Pelvis');bone('Head',(0,1.45,0),(0,1.83,0),'Spine')
 for side,s in [('Left',-1),('Right',1)]:
  ax=s*(ROLES[owner]['width']+.03);cx=s*(.155 if owner=='guardian' else .13)
  bone(side+'UpperArm',(ax,1.45,0),(ax,1.13,.02),'Spine');bone(side+'Forearm',(ax,1.13,.02),(ax+s*.027,.91,.066),side+'UpperArm');bone(side+'Hand',(ax+s*.027,.91,.066),(ax+s*.039,.77,.1),side+'Forearm')
  bone(side+'Thigh',(cx,.90,0),(cx,.47,.02),'Pelvis');bone(side+'Calf',(cx,.47,.02),(cx,.16,0),side+'Thigh');bone(side+'Foot',(cx,.16,0),(cx,.06,.20),side+'Calf')
 bpy.ops.object.mode_set(mode='OBJECT')
 for o in objects:
  g=o['part_group'];y=sum((o.matrix_world@v.co).z for v in o.data.vertices)/max(1,len(o.data.vertices))
  if g.endswith('Arm'):
   side=g[:-3];candidates=[(side+'UpperArm',1.13),(side+'Forearm',.91),(side+'Hand',-1)]
  elif g.endswith('Leg'):
   side=g[:-3];candidates=[(side+'Thigh',.47),(side+'Calf',.16),(side+'Foot',-1)]
  else:candidates=[('Head',1.55),('Spine',.95),('Pelvis',-1)]
  name=next(n for n,h in candidates if y>h);vg=o.vertex_groups.new(name=name);vg.add(list(range(len(o.data.vertices))),1,'REPLACE')
  m=o.modifiers.new('Editable articulated pose','ARMATURE');m.object=rig;o.parent=rig
 return rig
def stage_and_save(counts):
 editable_rig()
 bpy.ops.object.select_all(action='DESELECT');scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
 try:
  prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
  for device in prefs.devices:device.use=device.type=='OPTIX'
  if any(device.use for device in prefs.devices):scene.cycles.device='GPU'
 except Exception as error:print('Using CPU render:',error)
 scene.render.resolution_x=800;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.world=bpy.data.worlds.new('Portrait studio atmosphere');scene.world.color=(.16,.16,.16)
 scene.view_settings.view_transform='AgX'
 bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,0));floor=bpy.context.object;floor.name='Studio ground';m=bpy.data.materials.new('Studio charcoal');m.diffuse_color=(.027,.036,.047,1);floor.data.materials.append(m)
 for name,at,power,size,col in [('Key',(-3,4,4),480,4,(1,.84,.70)),('Fill',(3,2,3),250,3,(.67,.81,1)),('Rim',(1,3,-3),650,3,(.34,.60,1))]:
  bpy.ops.object.light_add(type='AREA',location=V(at));l=bpy.context.object;l.name=name;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.data.color=col;l.rotation_euler=(V((0,1,0))-l.location).to_track_quat('-Z','Y').to_euler()
 bpy.ops.object.camera_add(location=V((2.3,1.65,5.5)));camera=bpy.context.object;camera.name='Portrait inspection camera';camera.rotation_euler=(V((0,.98,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=2.13;scene.camera=camera
 # Reference is an image empty, visible for editing but excluded from rendering/export.
 ref=ROOT/'Assets/TacticalZero/Resources/Classic/Portraits'/(owner+'.png');image=bpy.data.images.load(str(ref));image.pack()
 empty=bpy.data.objects.new('Original portrait reference - '+owner,None);bpy.context.collection.objects.link(empty);empty.empty_display_type='IMAGE';empty.data=image;empty.empty_display_size=2.1;empty.location=V((-1.3,1,0));empty.rotation_euler=(math.pi/2,0,0);empty.hide_render=True
 scene['role']=owner;scene['game_triangles']=json.dumps(counts);scene['reference']='Original in-project portrait, rear and hidden details interpreted in 3D.'
 for area in bpy.context.screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=3;area.spaces.active.region_3d.view_location=V((0,1,0))
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(owner+'.blend')))
 scene.render.filepath=str(SOURCE/(owner+'_render.png'));bpy.ops.render.render(write_still=True)
 print('OPERATOR_COMPLETE '+owner+' '+json.dumps(counts),flush=True)
def build(id):
 global owner,objects,rng
 bpy.ops.wm.read_factory_settings(use_empty=True);owner=id;objects=[];rng=random.Random(117+list(ROLES).index(id));role=ROLES[id]
 PIVOTS.update(LeftArm=(-(role['width']+.03),1.45,0),RightArm=((role['width']+.03),1.45,0),LeftLeg=(-(.155 if id=='guardian' else .13),.9,0),RightLeg=((.155 if id=='guardian' else .13),.9,0))
 make_materials(role);body(role);accessories(role);counts=export_game();stage_and_save(counts)
if __name__=='__main__':
 ids=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else list(ROLES)
 for id in ids:build(id)
