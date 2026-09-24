"""Locally authored articulated stylized people; no image-to-3D service."""
import math
import geometry as g

def person(name,role,loc,scale=1):
    root=g.empty(name,loc);root.scale=(scale,)*3;root['role']=role
    clothes={'police':'navy','marshal':'orange','chef':'white','barista':'cream','clerk':'ochre','cleaner':'teal','attendant':'red','traveler':'navy'}[role]
    torso=g.empty(name+'_torso',(0,0,0),root)
    g.loft(name+'_tailored_torso',[(.84,.28,.19,0),(1.02,.31,.22,0),(1.31,.37,.24,0),(1.49,.36,.22,0),(1.59,.23,.17,0)],clothes,torso)
    g.cylinder(name+'_neck',(0,0,1.65),.14,.22,'skin',torso)
    g.loft(name+'_head',[(1.65,.13,.12,-.045),(1.70,.26,.25,-.02),(1.82,.37,.32,0),(2.05,.43,.36,.015),(2.28,.38,.32,.035),(2.42,.24,.23,.035),(2.46,.035,.03,.035)],'skin',torso,32)
    # Face landmarks on the forward (-Y) plane: recessed dark eyes and shaped nose.
    for side in (-1,1):
        g.uvball(name+'_ear',(side*.405,.025,2.02),(.09,.065,.14),'skin',torso)
        g.uvball(name+'_eye_white',(side*.15,-.322,2.06),(.089,.036,.097),'white',torso)
        g.uvball(name+'_iris',(side*.145,-.355,2.058),(.045,.022,.060),'hair',torso)
        g.uvball(name+'_eye_glint',(side*.135,-.375,2.082),(.012,.009,.016),'white',torso)
        g.curve(name+'_brow',[(side*.25,-.31,2.20),(side*.16,-.351,2.22),(side*.075,-.33,2.20)],'hair',.024,torso,False)
    g.uvball(name+'_nose',(0,-.368,1.99),(.082,.082,.076),'skin',torso)
    g.curve(name+'_mouth',[(-.14,-.293,1.84),(0,-.32,1.815),(.14,-.293,1.84)],'hair',.014,torso,False)
    # A fitted scalp cap and individual front locks, leaving the face uncovered.
    g.loft(name+'_haircap',[(2.22,.39,.335,.045),(2.36,.32,.28,.045),(2.47,.16,.14,.045),(2.49,.025,.023,.045)],'hair',torso)
    if role in ('barista','clerk','traveler'):
        for j in range(4):
            dx=-.23+j*.14
            g.curve(name+'_fringe',[(dx,.06,2.44),(dx-.06,-.2,2.38),(dx-.025,-.33,2.26)],'hair',.075,torso,False)
    if role in ('barista','clerk'):
        g.uvball(name+'_ponytail',(0,.32,2.18),(.28,.23,.4),'hair',torso)
    if role in ('police','marshal','attendant','cleaner'):
        hat={'police':'navy','marshal':'charcoal','attendant':'red','cleaner':'teal'}[role]
        g.uvball(name+'_cap',(0,.02,2.39),(.44,.35,.13),hat,torso)
        g.cube(name+'_peak',(0,-.32,2.35),(.6,.36,.055),hat,.07,torso)
        g.cube(name+'_badge',(0,-.318,2.43),(.14,.025,.13),'ochre',.02,torso)
    if role=='chef':
        g.cylinder(name+'_hatband',(0,0,2.42),.33,.17,'white',torso)
        for dx,dy in [(-.2,0),(.2,0),(0,-.14),(0,.14),(0,0)]:g.uvball(name+'_chefhat',(dx,dy,2.64),(.23,.22,.22),'white',torso)
    if role in ('chef','barista'):
        apron='wood' if role=='chef' else 'olive'
        g.cube(name+'_apron',(0,-.233,1.10),(.55,.055,.74),apron,.035,torso)
        for dx in (-.22,.22):g.cube(name+'_strap',(dx,-.225,1.48),(.055,.04,.27),apron,.014,torso)
        g.cube(name+'_pocket',(0,-.273,1.08),(.32,.025,.21),apron,.014,torso)
    elif role=='marshal':
        for dx in (-.24,.24):g.cube(name+'_reflective',(dx,-.224,1.29),(.10,.022,.53),'yellow',.005,torso)
        g.cube(name+'_reflective_band',(0,-.23,1.05),(.59,.02,.07),'white',.003,torso)
    elif role=='police':
        g.cube(name+'_vest',(0,-.218,1.30),(.64,.07,.45),'charcoal',.04,torso)
        for dx in (-.18,.18):g.cube(name+'_pouch',(dx,-.28,1.26),(.16,.07,.18),'navy',.03,torso)
        g.cube(name+'_chest_badge',(-.15,-.28,1.49),(.09,.025,.11),'ochre',.02,torso)
    for j in range(3):g.uvball(name+'_button',(.035,-.242,1.28+j*.09),(.02,.018,.02),'charcoal',torso)
    g.cube(name+'_belt',(0,0,.94),(.61,.44,.10),'charcoal',.03,torso)
    g.cube(name+'_buckle',(0,-.24,.94),(.10,.04,.07),'metal',.01,torso)
    limbs=[]
    for side in (-1,1):
        leg=g.empty(name+('_leg_L' if side<0 else '_leg_R'),(side*.18,0,.9),root);limbs.append(('leg',side,leg))
        g.loft(name+'_trouser',[(0,.145,.165,0),(-.25,.15,.16,0),(-.43,.12,.13,0),(-.66,.10,.11,0)],'navy' if role!='traveler' else 'cream',leg)
        g.cube(name+'_shoe',(0,-.12,-.76),(.27,.48,.23),'charcoal',.10,leg)
        g.cube(name+'_sole',(0,-.13,-.86),(.28,.49,.045),'white',.015,leg)
        arm=g.empty(name+('_arm_L' if side<0 else '_arm_R'),(side*.40,0,1.43),torso);limbs.append(('arm',side,arm))
        g.loft(name+'_sleeve',[(.06,.12,.145,0),(-.17,.135,.145,0),(-.34,.1,.115,-.02)],clothes,arm)
        g.uvball(name+'_forearm',(side*.01,-.025,-.49),(.095,.11,.24),'skin',arm)
        g.uvball(name+'_hand',(side*.01,-.05,-.70),(.105,.085,.13),'skin',arm)
        g.uvball(name+'_thumb',(-side*.085,-.085,-.665),(.043,.044,.073),'skin',arm)
        if side==1 and role in ('marshal','police'):
            g.beam(name+'_wand',(0,-.10,-.64),(0,-.1,-1.25),.045,'orange' if role=='marshal' else 'charcoal',arm,False)
        if side==-1 and role=='police':
            g.cube(name+'_shield',(0,-.17,-.47),(.5,.07,.86),'glass',.07,arm)
            g.cube(name+'_shield_strap',(0,-.21,-.48),(.52,.035,.12),'navy',.02,arm)
        if side==1 and role=='barista':
            g.cylinder(name+'_cup',(0,-.1,-.66),.1,.24,'cream',arm)
            g.cylinder(name+'_lid',(0,-.1,-.535),.11,.025,'charcoal',arm)
    if role=='traveler':g.cube(name+'_backpack',(0,.31,1.32),(.57,.32,.70),'olive',.12,torso)
    if role=='cleaner':g.beam(name+'_mop',(.48,-.22,.14),(.48,-.1,1.53),.035,'wood_light',root,False)
    return {'root':root,'torso':torso,'limbs':limbs,'role':role}

def animate_walk(p,start,end,rate=1.2):
    for t in [start+i/8 for i in range(int((end-start)*8)+1)]:
        v=math.sin((t-start)*math.tau*rate)
        for typ,side,o in p['limbs']:
            a=v*side*(.32 if typ=='leg' else -.35)
            g.key(o,t,'rotation_euler',(a,0,side*.07 if typ=='arm' else 0))
        g.key(p['torso'],t,'rotation_euler',(0,0,v*.025))

def animate_idle(p,start,end):
    for t in [start+i*.5 for i in range(int((end-start)*2)+1)]:
        v=math.sin(t*2)
        g.key(p['torso'],t,'rotation_euler',(0,v*.012,v*.018))

def move(p,points):
    for i,(t,x,y) in enumerate(points):
        g.key(p['root'],t,'location',(x,y,.25))
        if i+1<len(points):dx=points[i+1][1]-x;dy=points[i+1][2]-y
        else:dx=x-points[i-1][1];dy=y-points[i-1][2]
        yaw=math.atan2(dx,-dy) if dx or dy else 0
        g.key(p['root'],t,'rotation_euler',(0,0,yaw))
