"""Seconds, frame boundaries and navigation regressions; no Blender dependency."""
import math
import unittest
from timeline import STAGES, FPS, route, defense, angle_delta, visibility_frames

class RestStopTimelineTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):cls.data=defense()

    def test_ten_contiguous_intervals_total_three_hundred_seconds(self):
        self.assertEqual(len(STAGES),10)
        self.assertEqual(STAGES[0][2],0)
        self.assertEqual(STAGES[-1][3],300)
        self.assertEqual(sum(b-a for _,_,a,b in STAGES),300)
        for a,b in zip(STAGES,STAGES[1:]):self.assertEqual(a[3],b[2])
        self.assertEqual(STAGES[4][3]-STAGES[4][2],60)

    def test_defense_is_stationary_for_every_tenth_second(self):
        for n in range(601):self.assertEqual(route(100+n/10),(0,20))

    def test_route_never_teleports_at_stage_boundaries(self):
        for _,_,t,_ in STAGES[1:]:
            a=route(t-.001);b=route(t+.001)
            self.assertLess(math.dist(a,b),.05)

    def test_rotation_limit_per_render_frame(self):
        rows=self.data['yaw']
        for a,b in zip(rows,rows[1:]):
            self.assertLessEqual(abs(b[1]-a[1])*FPS,math.pi/2+1e-9)

    def test_nine_degrees_per_tenth_second(self):
        rows=self.data['yaw']
        def yaw_at(t):
            i=min(len(rows)-2,int((t-100)*FPS));u=(t-rows[i][0])*FPS
            return rows[i][1]*(1-u)+rows[i+1][1]*u
        for i in range(599):
            t=100+i/10
            self.assertLessEqual(abs(yaw_at(t+.1)-yaw_at(t))*180/math.pi,9+1e-7)

    def test_all_directions_and_full_rotation_are_demonstrated(self):
        origins={round(e['angle']/(math.pi/2))%4 for e in self.data['enemies'][:16]}
        self.assertEqual(origins,{0,1,2,3})
        yaws=[r[1] for r in self.data['yaw']]
        self.assertGreater(max(yaws)-min(yaws),2*math.pi)

    def test_hits_have_real_flight_time_and_every_enemy_exits(self):
        for b in self.data['bullets']:
            self.assertLess(b['t'],b['end'])
            self.assertLess(b['end'],160)
            self.assertGreaterEqual(self.data['enemies'][b['id']]['dead']+.001,b['end'])
        self.assertTrue(all(e['dead'] is not None for e in self.data['enemies']))

    def test_heading_wrap_takes_short_route(self):
        self.assertAlmostEqual(angle_delta(math.radians(179),math.radians(-179)),math.radians(2))

    def test_half_frame_impact_retains_hidden_pre_key(self):
        hidden,first,last,after=visibility_frames(101.6875,101.8075)
        self.assertEqual(first-hidden,1)
        self.assertEqual(after-last,1)
        self.assertLess(hidden,first)
        self.assertGreaterEqual(last,first)

    def test_store_to_restroom_passes_through_shared_opening(self):
        for n in range(2180,2251):
            x,y=route(n/10)
            if 67.8<x<69.3:self.assertTrue(19.5<y<24.5)

if __name__=='__main__':unittest.main()
