import bpy,runpy
from pathlib import Path
base=Path(__file__).resolve().parent
module=runpy.run_path(str(base/'build_operators.py'));g=module['export_game'].__globals__
for role,data in g['ROLES'].items():
 path=g['SOURCE']/(role+'.blend');bpy.ops.wm.open_mainfile(filepath=str(path))
 g['owner']=role;g['objects']=[o for o in bpy.data.objects if o.type=='MESH' and 'part_group' in o];g['atlasmat']=next(m for m in bpy.data.materials if m.name.startswith(role+' Game Atlas'))
 g['PIVOTS'].update(LeftArm=(-(data['width']+.03),1.45,0),RightArm=((data['width']+.03),1.45,0),LeftLeg=(-(.155 if role=='guardian' else .13),.9,0),RightLeg=((.155 if role=='guardian' else .13),.9,0))
 counts=module['export_game'](prepared=True);bpy.ops.object.select_all(action='DESELECT');bpy.ops.wm.save_as_mainfile(filepath=str(path))
 print('REEXPORTED',role,counts,flush=True)
