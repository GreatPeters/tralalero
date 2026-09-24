"""Deterministic seconds-based route and bounded-rate defense simulation."""
from math import atan2, cos, sin, pi, hypot

FPS = 24
STAGES = [
    ('01', '진입 구간', 0, 25), ('02', '주차 공간', 25, 50),
    ('03', '건물 앞 휴식 공간', 50, 75), ('04', '건물 입구', 75, 100),
    ('05', '360도 방어전', 100, 160), ('06', '식당', 160, 190),
    ('07', '편의점', 190, 220), ('08', '대형 화장실', 220, 250),
    ('09', '주유소', 250, 275), ('10', '주유소 출구', 275, 300),
]
ROUTE = [
    (0, -76, -92), (7, -72, -74), (15, -63, -62), (25, -51, -52),
    (33, -38, -52), (41, -30, -38), (50, -15, -28),
    (59, -17, -18), (67, -9, -12), (75, -7, -7),
    (83, 0, -5), (91, 0, 5), (98, 0, 20), (100, 0, 20),
    (160, 0, 20), (168, 12, 20), (175, 24, 22), (183, 30, 22), (190, 39, 22),
    (198, 46, 22), (202, 49, 24), (207, 54, 26), (213, 60, 24), (220, 68, 22),
    (229, 77, 22), (233, 85, 22), (236, 86, 28), (244, 87, 22), (250, 88, 15),
    (258, 88, -5), (265, 87, -24), (275, 89, -43),
    (283, 101, -50), (290, 108, -62), (300, 112, -86),
]

def smooth(x):
    x = max(0.0, min(1.0, x))
    return x*x*(3-2*x)

def route(t):
    if t <= 0: return ROUTE[0][1:]
    for a, b in zip(ROUTE, ROUTE[1:]):
        if a[0] <= t <= b[0]:
            u = (t-a[0])/(b[0]-a[0]);u = smooth(u)
            return a[1]+(b[1]-a[1])*u, a[2]+(b[2]-a[2])*u
    return ROUTE[-1][1:]

def angle_delta(a, b):
    return (b-a+pi)%(2*pi)-pi

def visibility_frames(start,end,fps=FPS):
    first=max(1,round(start*fps)+1)
    last=max(first,round(end*fps)+1)
    return max(1,first-1),first,last,last+1

def defense():
    """Player yaw is unwrapped; emitted projectiles cause the recorded kills."""
    dt=1/FPS; yaw=0.0; rows=[]; bullets=[]; shots=0; last_shot=-10
    enemies=[]
    # Clockwise waves interleave directions; short diagonal approaches are added late.
    for i in range(32):
        angle=(i%4)*pi/2 + (pi/8 if i>=16 else 0)
        distance=16.5 if i%2==0 else 17.5
        enemies.append(dict(id=i,spawn=101+(i//4)*7+(i%4)*.15,angle=angle,distance=distance,
                            speed=.92 if i<16 else 1.1,hp=2 if i in (23,27,31) else 1,dead=None,poses=[]))
    maximum=0; pending=[]
    for f in range(60*FPS+1):
        t=100+f/FPS
        for hit in list(pending):
            if t >= hit['time']:
                e=enemies[hit['id']];e['hp']-=1
                if e['hp']<=0 and e['dead'] is None:e['dead']=t
                pending.remove(hit)
        active=[]
        for e in enemies:
            if t<e['spawn']:continue
            dist=max(2.8,e['distance']-(t-e['spawn'])*e['speed'])
            x=sin(e['angle'])*dist;y=20+cos(e['angle'])*dist
            if e['dead'] is not None:
                # Keep the last live location during the fall.
                if e['poses']:x,y=e['poses'][-1][1:3]
            e['poses'].append((t,x,y))
            if e['dead'] is None:active.append((dist,e,x,y))
        old=yaw;target=None
        if active:
            # Finish an already aimed target; otherwise prioritize physical approach.
            active.sort(key=lambda a:a[0]+abs(angle_delta(yaw,a[1]['angle']))*1.4)
            _,target,x,y=active[0]
            delta=angle_delta(yaw,target['angle']);cap=pi/2*dt
            yaw+=max(-cap,min(cap,delta))
            if abs(angle_delta(yaw,target['angle']))<.07 and t-last_shot>=.42:
                if not any(p['id']==target['id'] for p in pending):
                    travel=hypot(x,y-20)/24
                    bullets.append(dict(t=t,end=t+travel,x=x,y=y,yaw=yaw,id=target['id']))
                    pending.append(dict(time=t+travel,id=target['id']))
                    last_shot=t;shots+=1
        speed=abs(yaw-old)/dt;maximum=max(maximum,speed)
        rows.append((t,yaw))
    assert maximum<=pi/2+1e-9
    assert max(r[1] for r in rows)-min(r[1] for r in rows)>2*pi
    assert all(e['dead'] is not None for e in enemies)
    return {'yaw':rows,'enemies':enemies,'bullets':bullets,'maximum_degrees_per_second':maximum*180/pi,'shots':shots}

if __name__=='__main__':
    d=defense()
    assert sum(b-a for _,_,a,b in STAGES)==300
    assert all(route(t)==(0,20) for t in (100,110,130,159.99,160))
    # Measure continuous interpolation at exactly 0.1 second, not only whole frames.
    def yaw_at(t):
        k=min(len(d['yaw'])-2,int((t-100)*FPS));u=(t-d['yaw'][k][0])*FPS
        return d['yaw'][k][1]*(1-u)+d['yaw'][k+1][1]*u
    max_01=max(abs(yaw_at(100+i/10+.1)-yaw_at(100+i/10))*180/pi for i in range(599))
    assert max_01<=9+1e-7
    print({'duration':300,'defense':60,'max_deg_sec':d['maximum_degrees_per_second'],'max_degrees_in_0_1s':max_01,'enemies':len(d['enemies']),'shots':d['shots']})
