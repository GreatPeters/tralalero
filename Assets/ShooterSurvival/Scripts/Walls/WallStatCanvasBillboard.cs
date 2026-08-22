using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class WallStatCanvasBillboard : MonoBehaviour
    {
        private Camera cachedCamera;

        private void OnEnable()
        {
            FaceMainCamera();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall -= FaceMainCameraIfAvailable;
            UnityEditor.EditorApplication.delayCall += FaceMainCameraIfAvailable;
        }
#endif

        private void LateUpdate()
        {
            FaceMainCamera();
        }

        public void FaceCamera(Camera targetCamera)
        {
            if (targetCamera == null)
                return;

            Quaternion targetRotation = targetCamera.transform.rotation;
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
                transform.rotation = targetRotation;
        }

        private void FaceMainCamera()
        {
            if (!Application.isPlaying && !gameObject.scene.IsValid())
                return;

            if (cachedCamera == null ||
                !cachedCamera.isActiveAndEnabled ||
                (!Application.isPlaying && cachedCamera.gameObject.scene != gameObject.scene))
            {
                cachedCamera = ResolveMainCamera();
            }

            FaceCamera(cachedCamera);
        }

        private Camera ResolveMainCamera()
        {
            if (Application.isPlaying)
                return Camera.main;

            foreach (GameObject candidate in GameObject.FindGameObjectsWithTag("MainCamera"))
            {
                if (candidate.scene == gameObject.scene &&
                    candidate.TryGetComponent(out Camera camera) &&
                    camera.isActiveAndEnabled)
                {
                    return camera;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private void FaceMainCameraIfAvailable()
        {
            if (this != null && isActiveAndEnabled)
                FaceMainCamera();
        }
#endif
    }
}
