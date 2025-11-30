using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class TimeManager : MonoBehaviour
    {
        #region Singleton
        public static TimeManager Instance;
        private void Awake() => Instance = this;
        #endregion

        public static float timeFactor = 1;
        public static bool isGameRunning = false;
        public bool isForwardMarchScene = false;
    }
}