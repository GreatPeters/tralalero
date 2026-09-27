using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // A continuous curved road segment for scenes that otherwise move straight and turn at spots
    // (RestStop). When the shark crosses the arc's start line it follows the circle while still moving
    // forward, keeping its lateral lane offset, then resumes straight movement on the exit heading.
    // Used for the rest-area entrance ramp that peels right off the mainline.
    public sealed class RouteArcDriver : MonoBehaviour
    {
        [Tooltip("Arc start on the road centreline; forward is the entry heading.")]
        public Transform start;
        public float radius = 20f;
        [Tooltip("Signed turn: +90 turns right, -90 turns left.")]
        public float turnDegrees = 90f;
        public float laneHalfWidth = 8f;

        private static readonly List<RouteArcDriver> Drivers = new();
        private PlayerScript rider;
        private float progress, lane;

        public float Length => Mathf.Abs(turnDegrees) * Mathf.Deg2Rad * radius;

        private void OnEnable() => Drivers.Add(this);
        private void OnDisable() { Drivers.Remove(this); rider = null; }

        public static bool TryAdvance(PlayerScript player, float step)
        {
            foreach (var d in Drivers)
                if (d != null && d.Advance(player, step)) return true;
            return false;
        }

        // Centreline pose at a distance along the arc (pure math for tests and authoring).
        public static void Evaluate(Vector3 origin, float entryYaw, float radius, float turnDegrees, float distance, out Vector3 point, out Vector3 forward)
        {
            float sign = Mathf.Sign(turnDegrees);
            var f0 = Quaternion.Euler(0, entryYaw, 0) * Vector3.forward;
            var r0 = Quaternion.Euler(0, entryYaw, 0) * Vector3.right;
            var centre = origin + r0 * sign * radius;
            float angle = Mathf.Clamp(distance / Mathf.Max(.01f, radius), 0, Mathf.Abs(turnDegrees) * Mathf.Deg2Rad) * Mathf.Rad2Deg * sign;
            var rot = Quaternion.Euler(0, angle, 0);
            point = centre + rot * (origin - centre);
            forward = rot * f0;
        }

        private bool Advance(PlayerScript player, float step)
        {
            if (start == null || player == null) return false;
            var f0 = Vector3.ProjectOnPlane(start.forward, Vector3.up).normalized;
            if (rider == null)
            {
                var offset = player.transform.position - start.position;
                float along = Vector3.Dot(offset, f0);
                var heading = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
                if (along < 0 || along > 4 || Mathf.Abs(offset.y) > 3 || Vector3.Dot(heading, f0) < .9f) return false;
                lane = Vector3.Dot(offset, Vector3.Cross(Vector3.up, f0));
                if (Mathf.Abs(lane) > laneHalfWidth) return false;
                rider = player; progress = along;
            }
            if (rider != player) return false;
            // A restarted run (respawn at the chapter start) must not resume a stale arc.
            Evaluate(start.position, start.eulerAngles.y, radius, turnDegrees, progress, out var current, out _);
            if (Vector3.ProjectOnPlane(player.transform.position - current, Vector3.up).magnitude > laneHalfWidth + 4) { rider = null; return false; }
            progress += step;
            Evaluate(start.position, start.eulerAngles.y, radius, turnDegrees, progress, out var point, out var forward);
            point.y = player.transform.position.y;
            player.ApplyContinuousRoutePose(point, forward, lane);
            if (progress >= Length) rider = null; // straight movement continues on the exit heading
            return true;
        }

        public void ResetForRun() => rider = null;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (start == null) return;
            Gizmos.color = new Color(1, .6f, .1f);
            var prev = start.position;
            for (int i = 1; i <= 16; i++)
            {
                Evaluate(start.position, start.eulerAngles.y, radius, turnDegrees, Length * i / 16f, out var p, out _);
                Gizmos.DrawLine(prev, p); prev = p;
            }
        }
#endif
    }
}
