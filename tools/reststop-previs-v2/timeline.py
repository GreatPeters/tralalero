"""Game-unit route with measured 7.8-unit/s travel and explicit encounter holds."""
from math import sin,cos,pi,hypot,atan2
from bisect import bisect_right
FPS=24;SPEED=7.8;CENTER=(0.,42.)
STAGES=[('01','진입 구간',0,25),('02','주차 공간',25,50),('03','건물 앞 휴식 공간',50,75),('04','건물 입구',75,100),('05','쿼터뷰 방어전',100,160),('06','식당',160,190),('07','대형 편의점',190,220),('08','대형 화장실',220,250),('09','주유소',250,275),('10','주유소 출구',275,300)]
PATHS=[
 [(-230,-245),(-240,-190),(-210,-145),(-163,-136),(-128,-124)],
 [(-128,-124),(-100,-124),(-100,-70),(-56,-70),(-56,-48),(-96,-48),(-96,-32)],
 [(-96,-32),(-88,-12),(11,-12),(11,-28),(-36,-28),(-36,-8)],
 [(-36,-8),(-36,22),(22,22),(22,66),(-16,66),(-16,42),(0,42)],
 [(0,42)],
 [(0,42),(40,42),(40,78),(104,78),(104,14),(118,14),(118,36)],
 [(118,36),(193,36),(193,65),(138,65),(138,82),(206,82)],
 [(206,82),(260,82),(260,108),(218,108),(218,48),(266,48),(266,36)],
 [(266,36),(288,20),(288,-26),(248,-26),(248,-64),(276,-82),(288,-88)],
 [(288,-88),(324,-108),(338,-148),(383,-170),(397,-216),(398,-230)],
]
def smooth(x):
 x=max(0.,min(1.,x));return x*x*(3-2*x)
def angle_delta(a,b):return (b-a+pi)%(2*pi)-pi
def visibility_frames(start,end,fps=FPS):
 f=max(1,round(start*fps)+1);e=max(f,round(end*fps)+1);return max(1,f-1),f,e,e+1
def rounded(points):
 if len(points)<3:return points
 result=[points[0]]
 for a,b,c in zip(points,points[1:],points[2:]):
  l1=hypot(b[0]-a[0],b[1]-a[1]);l2=hypot(c[0]-b[0],c[1]-b[1]);r=min(9,l1*.4,l2*.4)
  enter=(b[0]+(a[0]-b[0])*r/l1,b[1]+(a[1]-b[1])*r/l1);leave=(b[0]+(c[0]-b[0])*r/l2,b[1]+(c[1]-b[1])*r/l2)
  result.append(enter)
  for j in range(1,17):
   t=j/16;u=1-t;result.append((u*u*enter[0]+2*u*t*b[0]+t*t*leave[0],u*u*enter[1]+2*u*t*b[1]+t*t*leave[1]))
 result.append(points[-1]);return result
CURVES=[rounded(p) for p in PATHS];LENGTHS=[];CUM=[];HOLDS=[]
for i,points in enumerate(CURVES):
 d=[0.]
 for a,b in zip(points,points[1:]):d.append(d[-1]+hypot(b[0]-a[0],b[1]-a[1]))
 CUM.append(d);LENGTHS.append(d[-1]);duration=STAGES[i][3]-STAGES[i][2];HOLDS.append(duration-d[-1]/SPEED if i!=4 else 60)
def sample_distance(i,distance):
 pts=CURVES[i];cum=CUM[i]
 if len(pts)==1:return pts[0]
 distance=max(0.,min(distance,cum[-1]));j=min(len(pts)-2,max(0,bisect_right(cum,distance)-1));u=(distance-cum[j])/max(1e-9,cum[j+1]-cum[j]);return (pts[j][0]*(1-u)+pts[j+1][0]*u,pts[j][1]*(1-u)+pts[j+1][1]*u)
def stage_index(t):return min(9,next((i for i,(_,_,a,b) in enumerate(STAGES) if a<=t<b),9))
def center_route(t):
 i=stage_index(max(0.,min(300.,t)))
 if i==4:return CENTER
 elapsed=max(0.,t-STAGES[i][2]);travel=LENGTHS[i]/SPEED;pause_at=travel*(.12 if i==3 else .48);hold=max(0.,HOLDS[i])
 moving=elapsed if elapsed<pause_at else pause_at if elapsed<pause_at+hold else elapsed-hold
 return sample_distance(i,moving*SPEED)
def route(t):
 p=center_route(t)
 if 100<=t<=160:return p
 offset=0.
 for when,direction in [(31.5,-1),(35.5,-1),(43.5,-1),(47,-1),(61,-1),(172,-1),(204,1),(211,-1),(263,1),(288,-1)]:
  u=(t-(when-1.1))/2.2
  if 0<u<1:offset+=direction*2.2*sin(pi*u)**2
 if offset:
  a=center_route(max(0,t-.05));b=center_route(min(300,t+.05));dx=b[0]-a[0];dy=b[1]-a[1];n=max(.001,hypot(dx,dy));return p[0]-dy/n*offset,p[1]+dx/n*offset
 return p
def vent_active(t,door):return 107+door*2 <=t<109+door*2 or 127+door*2<=t<129+door*2 or 147+door*2<=t<149+door*2
def defense():
 dt=1/FPS;enemies=[]
 for i in range(54):
  t=101+i*1.02;phase=min(2,int((t-100)/20));door=(i%2) if phase==0 else 2+i%2 if phase==1 else i%4
  enemies.append(dict(id=i,spawn=t,angle=door*pi/2,door=door,distance=15.0,speed=3.8,hp=3 if i%9==8 else 2,role='police',dead=None,poses=[]))
 yaw=0.;rows=[];bullets=[];pending=[];lastshot=-10;maxrate=0.;stuns=0
 for f in range(60*FPS+1):
  t=100+f/FPS
  for hit in list(pending):
   if t>=hit['time']:
    e=enemies[hit['id']];e['hp']-=1
    if e['hp']<=0 and e['dead'] is None:e['dead']=t
    pending.remove(hit)
  active=[]
  for e in enemies:
   if t<e['spawn']:continue
   stunned=vent_active(t,e['door']) and 7<e['distance']<12
   if stunned:stuns+=1
   if e['dead'] is None:e['distance']=max(3.8,e['distance']-e['speed']*dt*(.15 if stunned else 1.))
   x=CENTER[0]+sin(e['angle'])*e['distance'];y=CENTER[1]+cos(e['angle'])*e['distance'];e['poses'].append((t,x,y,stunned))
   if e['dead'] is None:active.append((e['distance']+abs(angle_delta(yaw,e['angle']))*.8+(5 if any(h['id']==e['id'] for h in pending) else 0),e,x,y))
  old=yaw
  if active:
   active.sort(key=lambda a:a[0]);_,target,x,y=active[0];delta=angle_delta(yaw,target['angle']);cap=(pi/2-.0002)*dt;yaw+=max(-cap,min(cap,delta))
   if abs(angle_delta(yaw,target['angle']))<.10 and t-lastshot>=.28 and not any(h['id']==target['id'] for h in pending):
    target_speed=target['speed']*(.15 if vent_active(t,target['door']) and 7<target['distance']<12 else 1)
    travel=max(.03,(target['distance']-1.7)/(32+target_speed));arrival=t+travel;hit_distance=max(3.8,target['distance']-target_speed*travel)
    hit_x=CENTER[0]+sin(target['angle'])*hit_distance;hit_y=CENTER[1]+cos(target['angle'])*hit_distance
    bullets.append(dict(t=t,end=arrival,x=hit_x,y=hit_y,yaw=yaw,id=target['id']));pending.append(dict(time=arrival,id=target['id']));lastshot=t
  maxrate=max(maxrate,abs(yaw-old)/dt);rows.append((t,yaw))
 return {'yaw':rows,'enemies':enemies,'bullets':bullets,'maximum_degrees_per_second':maxrate*180/pi,'shots':len(bullets),'vent_slow_frames':stuns,'survivors_at_timer_end':sum(e['dead'] is None for e in enemies)}
if __name__=='__main__':
 import json
 report=[{'stage':s[0],'length':round(l,2),'moving_speed':SPEED,'hold_seconds':round(h,2)} for s,l,h in zip(STAGES,LENGTHS,HOLDS)]
 print(json.dumps(report,ensure_ascii=False,indent=2))
 assert all(h>=-1e-4 for h in HOLDS)
 d=defense();print({k:v for k,v in d.items() if k not in ('yaw','enemies','bullets')})
