using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public enum BulletKind { Water, Bomb }

    public class BulletPooler : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject bulletPrefab;        // Water
        [SerializeField] private GameObject bulletPrefab_bomb;   // Bomb

        [Header("Pool Size (per kind)")]
        [SerializeField] private int poolSize = 100;

        // 종류별 큐
        private readonly Queue<GameObject> poolWater = new Queue<GameObject>();
        private readonly Queue<GameObject> poolBomb = new Queue<GameObject>();

        // 어떤 bullet이 어떤 kind인지 역추적용
        private readonly Dictionary<GameObject, BulletKind> reverse = new Dictionary<GameObject, BulletKind>();

        private void Awake()
        {
            // Water 사전 빌드
            for (int i = 0; i < poolSize; ++i)
                EnqueueNew(BulletKind.Water);

            // Bomb 사전 빌드
            for (int i = 0; i < poolSize; ++i)
                EnqueueNew(BulletKind.Bomb);
        }

        private GameObject Create(BulletKind kind)
        {
            GameObject prefab = kind == BulletKind.Water ? bulletPrefab : bulletPrefab_bomb;
            if (prefab == null) return null;

            var obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            reverse[obj] = kind;
            return obj;
        }

        private void EnqueueNew(BulletKind kind)
        {
            var obj = Create(kind);
            if (obj == null) return;
            if (kind == BulletKind.Water) poolWater.Enqueue(obj);
            else poolBomb.Enqueue(obj);
        }

        private Queue<GameObject> GetQueue(BulletKind kind)
        {
            return kind == BulletKind.Water ? poolWater : poolBomb;
        }

        // ✅ 새 API: 종류 지정해서 꺼내기
        public GameObject Get(BulletKind kind, Transform callerTransform)
        {
            var q = GetQueue(kind);
            if (q.Count == 0) EnqueueNew(kind); // 필요시 1개 확장

            if (q.Count == 0) return null;     // prefab 미지정 등 안전장치

            var bullet = q.Dequeue();
            bullet.SetActive(true);
            bullet.transform.SetParent(callerTransform);
            bullet.transform.position = callerTransform.position;
            return bullet;
        }

        // ✅ 새 API: 어떤 종류였는지 자동 판별해 반납
        public void Return(GameObject bullet)
        {
            if (!bullet) return;

            bullet.SetActive(false);
            bullet.transform.SetParent(transform);
            bullet.transform.position = transform.position;

            if (reverse.TryGetValue(bullet, out var kind))
                GetQueue(kind).Enqueue(bullet);
            else
                Destroy(bullet); // 예상치 못한 외부 오브젝트일 때
        }

        // ⏪ 하위 호환(예전 코드 유지 시): 기본 Water로 꺼내기
        public GameObject GetObjectFromPool_Bullet(Transform callerTransform)
        {
            return Get(BulletKind.Water, callerTransform);
        }

        // ⏪ 하위 호환(예전 코드 유지 시): 자동 판별 반납
        public void ReturnObjectToPool_Bullet(GameObject bullet)
        {
            Return(bullet);
        }
    }
}
