using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
	public class CameraShake : MonoBehaviour
	{
		// Transform of the player
		public Transform player;
		private Vector3 offset;

		// How long the object should shake for.
		public float _shakeDuration = 0f;

		// Amplitude of the shake. A larger value shakes the camera harder.
		public float _shakeAmount = 0.7f;
		public float _decreaseFactor = 1.0f;
		private float currentShakePriority = 0f;

		Vector3 originalPos;

		void OnEnable()
		{
			originalPos = transform.localPosition;

			if (player != null) offset = transform.position - player.position;
			else return;
		}

		// Shake based on the parameters and the priority
		public void Shake(float ShakeAmount, float ShakeDuration, float DecreaseFactor, float priority = 0f)
		{
			if (TimeManager.Instance.isForwardMarchScene == true) return;

			if (priority >= currentShakePriority || _shakeDuration <= 0f)
			{
				_shakeDuration = ShakeDuration;
				_shakeAmount = ShakeAmount;
				_decreaseFactor = DecreaseFactor;
				currentShakePriority = priority;
			}
		}


		void Update()
		{
			if (player != null)
			{
				Vector3 currentPos = transform.position;
				Vector3 targetPos = new Vector3(currentPos.x, currentPos.y, player.transform.position.z + offset.z);
				transform.position = targetPos;
			}

			if (_shakeDuration > 0)
			{
				transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos + UnityEngine.Random.insideUnitSphere * _shakeAmount, Time.deltaTime * 25f);
				_shakeDuration -= Time.deltaTime * _decreaseFactor;
			}
			else
			{
				if (TimeManager.Instance.isForwardMarchScene == false) transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos, Time.deltaTime * 15f);
				_shakeDuration = 0f;
			}
		}
	}
}