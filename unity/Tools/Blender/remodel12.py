"""Portrait-derived remodelling, with CC0 MakeHuman face/hand topology.
Original 0.11 sources are retained. All modelling/export work runs in Blender.
"""
import sys,math,random
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
import build_operators as b
import bpy, bmesh
from mathutils import Vector

b.SOURCE=b.ROOT/'ArtSource/Blender12';b.SOURCE.mkdir(exist_ok=True)
base=[];faces=[];uv=[];grp=''
for line in (b.ROOT/'ArtSource/HumanBase/base.obj').read_text().splitlines():
 if line.startswith('v '):base.append(tuple(map(float,line.split()[1:])))
 elif line.startswith('vt '):uv.append(tuple(map(float,line.split()[1:3])))
 elif line.startswith('g '):grp=line[2:]
 elif line.startswith('f ') and grp=='body':faces.append([int(v.split('/')[0])-1 for v in line.split()[1:]])

def subset(name,predicate,transform,mat,sub=1):
 selected=[f for f in faces if all(predicate(base[i]) for i in f)];ids=sorted({i for f in selected for i in f});lookup={v:k for k,v in enumerate(ids)}
 return b.mesh(name,[transform(base[i]) for i in ids],[[lookup[i] for i in f] for f in selected],mat,sub=sub)

def face(role):
 if b.owner=='guardian':return
 female=role.get('female',False);fw=.97 if female else 1.04
 def transform(p):
  x,y,z=p; yy=1.721+(y-7.284)*(.084 if y>7.284 else .104)
  # Portrait-specific cheek/jaw taper on connected facial loops.
  jaw=max(0,1-abs(y-6.65)/.85);xx=x*.113*fw*(1-jaw*(.065 if female else -.025))
  zz=z*.095-.027
  return (xx,yy,zz)
 head=subset('Continuous anatomical face and neck',lambda p:p[1]>5.82,transform,'skin',1)
 b.apply(head);bm=bmesh.new();bm.from_mesh(head.data)
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(0,0,1.595),plane_no=(0,0,1),clear_inner=True)
 bm.to_mesh(head.data);bm.free()
 b.loft('Continuous neck under collar',[(1.46,.065,.065,0),(1.56,.056,.062,0),(1.62,.052,.057,.01)],'skin',fold=0,sub=1)
 # Eyeballs sit behind the existing sculpted eyelids, rather than pasted onto the face.
 for side in (-1,1):
  eye=transform((side*.308,7.284,1.245));x,y,z=eye
  b.ell('Inset eyeball',eye,(.012,.010,.012),'eye',32,20)
  b.ell('Iris',(x,y,z+.0117),(.005,.005,.001),'armor',24,16)
  b.ell('Pupil',(x,y,z+.0126),(.0023,.0023,.0006),'black',20,12)
  b.ell('Corneal reflection',(x-.002,y+.002,z+.0131),(.001,.001,.0004),'ivory',12,8)
  for k in range(12):
   t=k/11;xx=side*(.011+t*.043);yy=1.726+.007*math.sin(t*math.pi)-t*.006;zz=.116-t*.020
   b.tube('Individual brow hair',[(xx,yy,zz),(xx+side*.004,yy+.003,zz+.001)],.0009,'hair')
 # Subtle lips follow the actual mouth position of the base topology.
 if b.owner in ('engineer','supplier'):
  def beard(p):
   x,y,z=transform(p);return (x+(.001 if x>0 else -.001),y,z+.0018)
  subset('Closely trimmed shaped beard',lambda p:6.16<p[1]<6.91 and p[2]>.50 and (p[1]<6.70 or abs(p[0])>.26),beard,'hair',1)
 if b.owner not in ('medic','engineer','infiltrator'):hair(female)
 if b.owner=='medic':
  for side in (-1,1):
   for k in range(18):
    x=side*(.086+k*.0012);z=-.046+k*.004
    b.tube('Tapered bob strand',[(x,1.748,z),(side*.102,1.67,z+.009),(side*.087,1.607,z+.013)],.0038,'hair',[1,1,.1])

def hair(female):
 # Overlapping slender tapered locks follow a swept scalp; no straight helmet edge.
 w=.101 if female else .107;depth=.104;center=1.736
 vs=[];fs=[]
 def surface(a,t,lift=0):
  limit=1.43+.38*(1-math.cos(a))*.5+.09*math.sin(3*a)
  phi=t*limit
  return (math.sin(a)*math.sin(phi)*(w+lift),center+math.cos(phi)*(.109+lift),-.008+math.cos(a)*math.sin(phi)*(depth+lift))
 for j in range(13):
  for k in range(48):vs.append(surface(k/48*math.tau,.025+j/12*.98))
 for j in range(12):
  for k in range(48):a=j*48+k;c=j*48+(k+1)%48;fs.append((a,a+48,c+48,c))
 b.mesh('Sculpted asymmetric hair mass',vs,fs,'hair',sub=1)
 for k in range(95):
  a=b.rng.uniform(-math.pi,math.pi);t=b.rng.uniform(.12,.84);sweep=-.42 if b.owner in ('assault','marksman') else .22
  pts=[surface(a+sweep*f,t+f*.20,.003+math.sin(f*math.pi)*.004) for f in (0,.33,.66,1)]
  # A few pointed front locks break the hairline at the temple.
  if abs(a)<.65 and k%4==0:pts[-1]=(pts[-1][0]-.007,pts[-1][1]-.018,pts[-1][2]+.005)
  b.tube('Swept tapered hair lock',pts,.0025+b.rng.random()*.0018,'hair',[.7,1,.6,.015])

oldbody=b.body;oldaccessories=b.accessories
def remove_matching(words):
 for o in list(b.objects):
  if any(o.name.startswith(word) for word in words):b.objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)

def hand(side,at):
 # Relaxed anatomical hand replaces separate cylindrical fingers.
 wrist=Vector((side*4.31,2.45,1.76));direction=Vector((side*.47,-1.15,1.26));rotation=direction.rotation_difference(Vector((side*.01,-1,.18)))
 def transform(p):
  v=rotation@(Vector(p)-wrist)*.087;return (at[0]+v.x,at[1]+v.y,at[2]+v.z)
 subset('Tailored five finger glove',lambda p:p[0]*side>3.67 and p[1]<2.70 and p[2]>1.52,transform,'leather',1)

def body(role):
 oldbody(role)
 remove_matching(['Jacket front zipper','Zipper tooth','Standing collar','Contoured plate carrier','MOLLE woven row','MOLLE bar tack','Quick access magazine','Layered shoulder shell','Shoulder secondary scale','Armor recessed rivet','Anatomical glove palm','Articulated glove finger','Glove knuckle pad','Opposable glove thumb','Boot sculpted vamp','Rubber toe reinforcement','Layered sole welt','Outsole lug','Floating patella protector','Knee inset','Knee plate fastener'])
 b.group='Body';b.loft('Close fitting neck gaiter',[(1.43,.095,.09,0),(1.5,.081,.08,0),(1.565,.062,.069,0)],'trim',fold=.004,sub=1);w=role['width'];heavy=b.owner=='guardian';female=role.get('female',False)
 b.tube('Continuous garment zipper',[(0,1.0,.158),(0,1.25,.180),(0,1.40,.176),(0,1.49,.096)],.0022,'leather')
 # A fitted soft carrier wraps the rib cage, with a narrower waist and real side returns.
 if b.owner not in ('marksman','medic','scout','infiltrator'):
  b.loft('Fitted ballistic carrier',[(1.08,w*.85,.183,.002),(1.115,w*.92,.190,0),(1.29,w*1.02,.190,-.005),(1.39,w*.94,.177,-.004),(1.41,w*.83,.160,-.004)],'armor' if heavy else 'leather',fold=.003,sub=1)
  for k in range(3):
   b.pouch('Slim angled magazine',((k-1)*.09,1.145,.209),(.078,.15,.047),'trim')
  for side in (-1,1):b.ribbon('Carrier stitched shoulder yoke',[(side*w*.59,1.13,.16),(side*w*.63,1.36,.17),(side*w*.57,1.46,.11),(side*w*.55,1.49,-.02)],.044,'leather')
 else:
  # Contoured zipper panels keep light roles recognisably light.
  for side in (-1,1):b.ribbon('Tailored contrast side panel',[(side*w*.67,1.0,.103),(side*w*.76,1.12,.13),(side*w*.87,1.34,.10),(side*w*.75,1.43,.08)],.055,'armor' if b.owner=='medic' else 'leather')
 for side in (-1,1):
  b.group='LeftLeg' if side<0 else 'RightLeg';cx=side*(.155 if heavy else .13)
  b.loft('Shaped reinforced kneepad',[(.39,.059,.015,.107),(.415,.078,.023,.11),(.49,.080,.03,.117),(.54,.059,.014,.111)],'armor' if heavy or b.owner=='assault' and side<0 else 'leather',cx,fold=0,sides=24,sub=1)
  # Low-profile boot toe, heel and continuous welt follow a footprint.
  vs=[];fs=[]
  for j,(y,scale) in enumerate([(.018,.97),(.038,1),(.063,.96),(.113,.88),(.135,.50)]):
   for k in range(32):
    a=k/32*math.tau;z=.051+math.cos(a)*.15;wx=.083*(.9+.1*math.cos(a));vs.append((cx+math.sin(a)*wx*scale,y,z*scale))
  for j in range(4):
   for k in range(32):a=j*32+k;c=j*32+(k+1)%32;fs.append((a,c,c+32,a+32))
  fs.extend([tuple(reversed(range(32))),tuple(128+k for k in range(32))]);b.mesh('Contoured leather boot and rubber toe',vs,fs,'ivory' if b.owner=='medic' else 'leather',sub=1)
  b.tube('Continuous boot welt',[(cx+math.sin(k/32*math.tau)*.082,.038,.051+math.cos(k/32*math.tau)*.146) for k in range(33)],.007,'sole')
  b.group='LeftArm' if side<0 else 'RightArm';ax=side*(w+.03);fore=ax+side*.027
  hand(side,(fore,.91,.066))
  if heavy or b.owner=='assault' and side<0:
   # Wrapped deltoid cap: broad top, curved side protection, inset lower edge.
   vs=[];fs=[]
   for j in range(7):
    t=j/6;y=1.47-t*.24;r=.063+.064*math.sin(t*math.pi*.75)
    for k in range(13):
     a=-.18+k/12*math.pi*1.36;vs.append((ax+side*math.sin(a)*r,y,-.005+math.cos(a)*r))
   for j in range(6):
    for k in range(12):a=j*13+k;fs.append((a,a+13,a+14,a+1))
   o=b.mesh('Sculpted wraparound shoulder armor',vs,fs,'armor',sub=1);m=o.modifiers.new('Ceramic shell thickness','SOLIDIFY');m.thickness=.013
 b.group='Body'

def accessories(role):
 oldaccessories(role)
 if b.owner in ('medic','engineer'):
  for o in b.objects:
   if any(o.name.startswith(n) for n in ['Protective helmet shell','Helmet ear protector','Helmet chin webbing','Safety helmet brow','Goggle','Helmet raised spine']):o.location.z+=.040
 if b.owner=='medic':
  remove_matching(['Hip utility pouch','Split medical coat hem']);b.group='Body'
  b.loft('Tailored flared medical coat',[(.75,.30,.205,0),(.80,.295,.20,0),(.86,.28,.19,0),(.94,.255,.17,0),(1.0,.205,.148,0)],'cloth',fold=.005,sub=1)
 if b.owner=='marksman':remove_matching(['Backpack padded main body','Backpack compression strap','Pack ladder lock','Hip utility pouch','Thigh cargo pocket','Pleated chest pocket'])
 if b.owner=='infiltrator':remove_matching(['Pleated chest pocket'])
 # Bake a mild asymmetrical alert stance into the visual mesh, keeping gameplay pivots.
 for o in b.objects:
  b.apply(o)
  g=o['part_group']
  for vertex in o.data.vertices:
   x,y,z=b.U(o.matrix_world@vertex.co)
   if g.endswith('Leg'):
    cx=(-1 if g=='LeftLeg' else 1)*(.155 if b.owner=='guardian' else .13)
    x=cx+(x-cx)*(.86 if role.get('female') else .94);y*=1.045
    z+=max(0,1-y/.94)*(.027 if g=='LeftLeg' else -.035)
   elif y<1.53:
    y=.94+(y-.9)*.96 if y>.9 else y+.04
   if g.endswith('Arm'):
    ax=(-1 if g=='LeftArm' else 1)*(role['width']+.03);weight=max(0,min(1,(1.46-y)/.65))
    x=ax+(x-ax)*.8+(-1 if g=='LeftArm' else 1)*.025*weight
    z+=math.sin(weight*math.pi*.55)*.025
    blend=max(0,min(1,(1.20-y)/.09));theta=-.80*blend;dy=y-1.14;dz=z-.025
    y=1.14+dy*math.cos(theta)-dz*math.sin(theta);z=.025+dy*math.sin(theta)+dz*math.cos(theta)
   # Subtle axial weight shift instead of perfectly parallel mannequin limbs.
   if y>.96:x+=min(.013,(y-.96)*.02)
   vertex.co=o.matrix_world.inverted()@b.V((x,y,z))

b.face=face;b.body=body;b.accessories=accessories;b.LOD_RATIOS=(.5,.13)
for name,role in b.ROLES.items():
 role['width']={'assault':.213,'scout':.219,'medic':.205,'engineer':.249,'guardian':.28,'marksman':.195,'infiltrator':.205,'supplier':.251}[name]
 role['skin']={'assault':'B58E78','scout':'A68066','medic':'C0A087','engineer':'AA8164','guardian':'AA8164','marksman':'C6AEA0','infiltrator':'AC9375','supplier':'AD805F'}[name]
b.ROLES['assault']['armor']='AA5728';b.ROLES['assault']['hair']='67391F'
b.ROLES['marksman']['cloth']='252933';b.ROLES['marksman']['pants']='2A2E37'

if __name__=='__main__':
 ids=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else list(b.ROLES)
 for role in ids:b.build(role)
