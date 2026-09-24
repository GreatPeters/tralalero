using System;
using System.Collections;
using DG.Tweening;
using IndianOceanAssets.ShooterSurvival;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ObstacleStats : MonoBehaviour
{
    //양동이 관련
    [Header("Bucket (Fish Tub)")]
    public Transform bucket;
    public float bucketAttachSeconds = 3.0f;
    public Vector3 bucketHeadOffset = new Vector3(0, 1.3f, 0.2f);
    public Vector3 bucketDetachImpulse = new Vector3(0, 2f, -4f);
    public Vector3 bucketDetachAngularImpulse = new Vector3(-20f, 0f, 0f);
    public bool destroyAfterDetach = true;
    bool _bucketAttached;
    Transform _bucketStartParent;
    Vector3 _bucketStartLocalPos;
    Quaternion _bucketStartLocalRot;
    bool _bucketSaved;
    PlayerScript bucketBlockedPlayer;

    void ReleaseBucketShootBlock()
    {
        if (bucketBlockedPlayer != null) bucketBlockedPlayer.SetBucketShootBlock(this, false);
        bucketBlockedPlayer = null;
    }


    //열기구 관련
    [Header("Seagull approach")]
    public Transform balloon;            // 열기구(비활성 시작 권장)
    public SpriteRenderer shadowSprite;  // 바닥 그림자 스프라이트(처음엔 꺼두기)
    public LayerMask groundMask;         // 바닥 레이어

    public float triggerRadius = 28f;   // 이 거리 안 들어오면 텔레그래프 시작
    public float telegraphTime = 1.0f;   // 그림자 커지는 시간
    // The authored 10.24-unit shadow sprite grows from 2.808m to 3.744m.
    public float shadowStartScale = 0.27421875f;
    public float shadowEndScale = 0.365625f;

    public float dropHeight = 12f;       // 위에서부터 떨어질 높이
    public float dropTime = 0.18f;     // 바닥까지 떨어지는 시간


    Transform _player;
    Vector3 _impactPoint;
    bool _seagullHit;
    bool _started;   // 이미 시작했는지
    private bool seagullContactActive;
    private Collider[] seagullColliders;
    private Animator seagullAnimator;
    [Header("Seagull contact")]
    [Min(.1f)] public float seagullSpinSeconds = 1.2f;
    [Min(0f)] public float seagullLandingOffset = .15f;


    //돌고래 관련
    [Header("Dolphin Jump (min)")]
    public Transform pointA;              // 시작/좌측 등 원하는 고정 지점
    public Transform pointB;              // 반대편 고정 지점
    public float jumpHeight = 3f;         // 포물선 높이
    public float jumpTime = 1.2f;       // 한 번 점프 시간
    public bool lookAlongPath = true;     // 진행방향 바라보기(거슬리면 끄세요)
    public bool flipYawOnReverse = true;  // B->A로 돌아갈 때 Yaw 180° 추가
    public float yawOffset = 0f;          // 메시 전방 보정이 필요하면 90/-90/180 등
    private Animator dolphinAnim;


    Tween _jumpSeq;

    // 목적지 쪽으로 'Yaw'만 맞추기 (+옵션으로 180도 뒤집기)
    void SetYawToward(Vector3 from, Vector3 to, bool add180 = false)
    {
        Vector3 flat = to - from; flat.y = 0f;
        if (flat.sqrMagnitude < 1e-6f) return;

        float yaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y+yawOffset+(add180?180f:0f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        //Debug.Log(transform.rotation);
    }
    void RestartActAnim()
    {
        if (dolphinAnim == null) return;

        // "Act"는 Animator 안의 상태 이름으로 바꿔줘
        dolphinAnim.Play("act", 0, 0f); // layer 0, normalizedTime 0
    }

    public void StartFixedZigZag()
    {
        if (pointA == null || pointB == null) { Debug.LogWarning("pointA/pointB 지정 필요"); return; }

        Vector3 aPos = pointA.position;
        Vector3 bPos = pointB.position;

        transform.position = aPos;

        _jumpSeq?.Kill();

        // 시작할 때도 한 번 Act 재생
        RestartActAnim();

        _jumpSeq = DOTween.Sequence()
            // A 에서 출발할 때 방향만 맞추기
            .AppendCallback(() =>
            {
                if (lookAlongPath) SetYawToward(aPos, bPos, false);
            })
            // A -> B 점프
            .Append(DoParabolaLeg(aPos, bPos))
            // ★ B 지점 도착: Act 애니 처음부터
            .AppendCallback(() =>
            {
                RestartActAnim();
                if (lookAlongPath) SetYawToward(bPos, aPos, flipYawOnReverse);
            })
            // B -> A 점프
            .Append(DoParabolaLeg(bPos, aPos))
            // ★ A 지점 도착: 또 Act 애니 처음부터
            .AppendCallback(() =>
            {
                RestartActAnim();
                // 다음 루프에서 다시 A->B로 나갈 준비 (원하면 방향 다시 맞추기)
                if (lookAlongPath) SetYawToward(aPos, bPos, false);
            })
            .SetLoops(-1, LoopType.Restart);
    }




    Tween DoParabolaLeg(Vector3 from, Vector3 to)
    {
        const int steps = 20;
        Vector3[] path = new Vector3[steps];
        for (int i = 0; i < steps; i++)
        {
            float t = i / (steps - 1f);
            Vector3 p = Vector3.Lerp(from, to, t);
            p.y += 4f * jumpHeight * t * (1f - t);
            path[i] = p;
        }

        return transform
            .DOPath(path, jumpTime, PathType.Linear, PathMode.Full3D, 1)
            .SetEase(Ease.Linear); // 회전은 건드리지 않음
    }






    void OnDisable()
    {
        if (obstaclePattern == ObstaclePattern.Seagull) SetSeagullContact(false);
        if (shipShot != null) Destroy(shipShot);
        ReleaseBucketShootBlock();
        StopAllCoroutines();
        DOTween.Kill(gameObject);
        if (obstaclePattern == ObstaclePattern.Light) DOTween.Kill(transform);
        if (obstaclePattern == ObstaclePattern.Seagull && balloon != null) DOTween.Kill(balloon);
        _jumpSeq?.Kill();
    }
    void OnDestroy() { _jumpSeq?.Kill(); }



    Vector3 _startLocalPos;
    Quaternion _startLocalRot;
    Vector3 _startLocalScale;
    bool _savedStart;

    bool _lampFallen;
    bool _lampSettled;
    [Header("Light")]
    [Tooltip("끄면 자동 사격으로 무력화되지 않는 고정 피해 장애물입니다.")]
    public bool canBeShotDown = true;
    Vector3 _hinge;          // 바닥 힌지 (Bounds로 자동 계산)
    Collider[] lampColliders;
    bool[] lampColliderEnabled;

    public ObstaclePattern obstaclePattern;
    [Tooltip("기믹 효과 값. Seagull은 최대 체력 대비 피해율(%)입니다.")]
    public float value = 10f;
    public Transform firePos;
    public GameObject projectilePrefab;       // Inspector에서 투사체(이펙트) 프리팹 할당
    public float fireDistance = 12f;          // 발사 트리거 거리
    public float aheadOffset = 3f;            // 플레이어 진행 방향 앞쪽으로 조준할 거리

    private bool hasFired = false;
    [Min(.1f)] public float shipApproachSeconds = 7.5f;
    private GameObject shipShot;

    void Start()
    {
        if (obstaclePattern == ObstaclePattern.Oldman)
        {
            SimpleProjectile sp = transform.GetComponentInChildren<SimpleProjectile>();
            if(sp!=null){sp.damage=value;sp.damageCause=PlayerDamageCause.Paddle;}
        }
        else if (obstaclePattern == ObstaclePattern.Dolphin)
        {
            dolphinAnim = GetComponentInChildren<Animator>();
            StartFixedZigZag();
        }
        else if (obstaclePattern == ObstaclePattern.Seagull && !_started)
        {
            // Initial spawners may assign the final placement after OnEnable.
            // Reuse the same initialization rather than a separate collider path.
            InitSeagull();
        }

        // if (obstaclePattern == ObstaclePattern.Bucket)
        // {
        //     // 👉 버킷 참조 없으면 자기 자신을 버킷으로 사용
        //     if (bucket == null) bucket = transform;

        //     // 물리/충돌 기본 세팅
        //     var rb = bucket.GetComponent<Rigidbody>();
        //     if (!rb) rb = bucket.gameObject.AddComponent<Rigidbody>();
        //     rb.isKinematic = true;
        //     rb.useGravity = false;

        //     var col = bucket.GetComponent<Collider>();
        //     if (col) col.isTrigger = true;

        //     // 👉 “고정물”이므로 이동 트윈/회전 없음
        //     // 위치/회전은 프리팹/씬에서 배치한 그대로 사용
        // }
    }

    void OnEnable()
    {
        // hasFired = false;
        // _started = false;
        // _bucketAttached = false;
        // _lampFallen = false;

        // if (obstaclePattern == ObstaclePattern.Seagull)
        // {
        //     _started = false;

        //     if (shadowSprite)
        //     {
        //         shadowSprite.enabled = false;
        //         shadowSprite.transform.localScale = Vector3.one * shadowStartScale;
        //     }

        //     if (balloon) balloon.gameObject.SetActive(false);

        //     var bcol = balloon ? balloon.GetComponent<Collider>() : null;
        //     if (bcol) bcol.enabled = false;
        // }
        // else if (obstaclePattern == ObstaclePattern.Dolphin)
        // {
        //     transform.position = pointA.position;
        //     transform.rotation = Quaternion.identity;
        //     StartFixedZigZag();
        // }

        ResetState();
    }

    void ResetState()
    {
        // 1) 남아있는 것 정리 (Enable 시점에도 안전하게)
        StopAllCoroutines();
        DOTween.Kill(gameObject);
        _jumpSeq?.Kill();

        // 2) 플래그 리셋
        hasFired = false;
        if (shipShot != null) Destroy(shipShot);
        _started = false;
        _bucketAttached = false;
        _lampFallen = false;

        if (obstaclePattern == ObstaclePattern.Light && !_savedStart)
        {
            _savedStart = true;
            _startLocalPos = transform.localPosition;
            _startLocalRot = transform.localRotation;
            _startLocalScale = transform.localScale;
        }

        // 3) 패턴별 초기화
        if (obstaclePattern == ObstaclePattern.Seagull)
        {
            // impactPoint도 재계산해주는게 베스트 (Start에만 있으면 위치 누적됨)
            InitSeagull();
        }
        else if (obstaclePattern == ObstaclePattern.Dolphin)
        {
            if (!dolphinAnim) dolphinAnim = GetComponentInChildren<Animator>(); // 추가
            if (pointA) transform.position = pointA.position;
            transform.rotation = Quaternion.identity;
            StartFixedZigZag();
        }
        else if (obstaclePattern == ObstaclePattern.Light)
        {
            ResetLampTransform();
        }
        else if (obstaclePattern == ObstaclePattern.Bucket)
        {
            InitBucket();
        }


    }

    void InitSeagull()
    {
        _seagullHit = false;
        seagullColliders = balloon != null ? balloon.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>();
        seagullAnimator = balloon != null ? balloon.GetComponent<Animator>() : null;
        if (seagullAnimator != null) seagullAnimator.enabled = true;
        SetSeagullContact(false);
        var rootCollider = GetComponent<Collider>();
        if (rootCollider != null) rootCollider.enabled = false;
        if (balloon != null) balloon.gameObject.SetActive(false);
        // 0) 남아있을 수 있는 트윈 정리(선택)
        if (shadowSprite) shadowSprite.transform.DOKill();
        if (balloon) balloon.DOKill();

        // 1) 플레이어 캐시 초기화 + 시작 플래그
        _player = null;
        _started = false;

        // 2) 낙하지점(impact) 재계산  ✅ 풀링에서 중요
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down,
            out var hit, 40f, groundMask))
        {
            _impactPoint = hit.point;
        }
        else
        {
            _impactPoint = transform.position;
        }

        // 3) 이 오브젝트 기준점을 impact로 고정(네 코드랑 동일 컨셉)
        transform.position = _impactPoint;

        // 4) 그림자 초기화
        if (shadowSprite)
        {
            shadowSprite.enabled = false;
            shadowSprite.transform.position = _impactPoint + Vector3.up * 0.02f;
            shadowSprite.transform.localScale = Vector3.one * shadowStartScale;
        }

        // 5) 풍선 비활성 + 콜라이더 비활성
        if (balloon) balloon.gameObject.SetActive(false);

        SetSeagullContact(false);

        // 6) 부모 rigidbody 보장(콜백/트리거 안정용)
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }



    private void OnTriggerEnter(Collider other)
    {
        if (Application.isPlaying && !TimeManager.isGameRunning) return;
        // Light: 미사일이 닿으면 → 쓰러지기만 (데미지는 안 줌)
        if (obstaclePattern == ObstaclePattern.Light && other.CompareTag("BulletTag"))
        {
            ReactToProjectile();
            return;
        }

        var playerScript = other.GetComponentInParent<PlayerScript>();
        if (playerScript == null) return;

        switch (obstaclePattern)
        {
            case ObstaclePattern.Hole:
                playerScript.DieFromHazard(true);
                break;

            case ObstaclePattern.Oil:
                // 거미줄: 이동 민감도(또는 속도) 감소
                //playerScript.moveSensitivity = Mathf.Max(1f, playerScript.moveSensitivity - value);
                //StartCoroutine(SpinPlayerForSeconds(playerScript.transform, value, 720f));
                var slip = playerScript.GetComponent<OilSteeringEffect>();
                if (slip == null) slip = playerScript.gameObject.AddComponent<OilSteeringEffect>();
                slip.Apply(playerScript, value);
                break;

            case ObstaclePattern.Ship:
                // 배: 점수 감소
                playerScript.playerScore = Mathf.Max(0, playerScript.playerScore - (int)value);
                break;

            case ObstaclePattern.Seagull:
                {
                    TrySeagullContact(other);
                    return;
                }

            case ObstaclePattern.Light:
                if (_lampFallen && !_lampSettled) return;
                if (_lampSettled) playerScript.TryTakeFallenPoleDamage(Time.time);
                else playerScript.DieFromHazard(false,PlayerDamageCause.Pole);
                GetComponent<LampImpactFeedback>()?.Pulse();
                break;

            case ObstaclePattern.Bucket:
                if (_bucketAttached) return;
                StartCoroutine(AttachBucketRoutine(playerScript));
                return;

                // Firework, Whale, Fog, Light 등은 필요 시 추가 효과 구현
        }
    }

    public void ReactToProjectile()
    {
        if (!enabled || obstaclePattern != ObstaclePattern.Light) return;
        GetComponent<LampImpactFeedback>()?.Pulse();
        if (canBeShotDown) ToppleLampOnly();
    }

    private void OnCollisionEnter(Collision collision) => OnTriggerEnter(collision.collider);

    private void OnTriggerStay(Collider other)
    {
        if ((obstaclePattern == ObstaclePattern.Light && _lampSettled) || obstaclePattern == ObstaclePattern.Seagull)
            OnTriggerEnter(other);
    }

    private void OnCollisionStay(Collision collision) => OnTriggerStay(collision.collider);

    void Update()
    {
        if (obstaclePattern == ObstaclePattern.Seagull && seagullAnimator != null)
            seagullAnimator.speed = TimeManager.isGameRunning ? Mathf.Max(0f, TimeManager.timeFactor) : 0f;
        if (!TimeManager.isGameRunning) return;

        if (obstaclePattern == ObstaclePattern.Seagull && !_started)
        {
            if (_player == null)
            {
                var ps = GameManager.S != null ? GameManager.S.playerScript : FindFirstObjectByType<PlayerScript>();
                if (ps != null) _player = ps.transform; else return;
            }

            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = _player.position; b.y = 0f;
            if (Vector3.Distance(a, b) <= triggerRadius)
            {
                _started = true;
                StartCoroutine(TelegraphThenDrop());
                return; // 이 프레임은 여기까지
            }
        }

        // Ship일 때만, 아직 발사하지 않았고, 플레이어가 발사 거리 안으로 들어오면 발사
        if (obstaclePattern != ObstaclePattern.Ship || hasFired)
            return;

        if (_player == null)
        {
            var target = GameManager.S != null ? GameManager.S.playerScript : FindFirstObjectByType<PlayerScript>();
            if (target != null) _player = target.transform;
        }
        if (_player == null)
            return;

        var shipTarget = _player.GetComponent<PlayerScript>();
        if (shipTarget != null && EnemyScript_space.IsWithinApproachWindow(_player.position,
            _player.forward, transform.position, shipTarget.ForwardMoveSpeed, shipApproachSeconds, 18f))
        {
            hasFired = FireFromBow();
        }
    }



    // 콜라이더 재활성(중복 히트 방지용)
    IEnumerator ReenableColliderAfter(Collider c, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (c) c.enabled = true;
    }


    IEnumerator AttachBucketRoutine(PlayerScript player)
    {
        _bucketAttached = true;

        // 충돌 막기
        var col = bucket.GetComponent<Collider>();
        if (col) col.enabled = false;

        // 물리 세팅 확보
        var rb = bucket.GetComponent<Rigidbody>();
        if (!rb) rb = bucket.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 머리에 씌우기 + 발사 금지
        Vector3 worldScale = bucket.lossyScale;
        bucket.SetParent(player.transform, worldPositionStays: false);
        Vector3 parentScale = player.transform.lossyScale;
        bucket.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, worldScale.z / parentScale.z);
        bucket.localPosition = bucketHeadOffset;
        // The mesh opens along +Z. Point it down over the head with a 12-degree tilt.
        bucket.localRotation = Quaternion.Euler(78f, 180f, 0f);

        bucketBlockedPlayer = player;
        player.SetBucketShootBlock(this, true);

        // 유지
        yield return new WaitForSeconds(bucketAttachSeconds);

        // 해제 & 뒤로 튕기기
        bucket.SetParent(null, true);
        rb.isKinematic = false;
        rb.useGravity = true;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero; 
        rb.angularVelocity = Vector3.zero;
#endif
        rb.AddTorque(bucketDetachAngularImpulse, ForceMode.Impulse);
        rb.AddForce(bucketDetachImpulse, ForceMode.Impulse);

        // 발사 복구
        ReleaseBucketShootBlock();

        if (destroyAfterDetach)
        {
            // Destroy 금지. 연출 후 원복해서 풀 재사용.
            yield return new WaitForSeconds(0.6f); // 튕김 연출 시간
            InitBucket();                          // 부모/물리/콜라이더 복구
        }
        else
        {
            yield return new WaitForSeconds(0.15f);
            if (col) col.enabled = true;
            _bucketAttached = false;
        }
    }





    public IEnumerator SpinAndMovePlayer(Transform player, float duration)
    {
        if (player == null) yield break;
        var customizer=player.GetComponentInChildren<PlayerCosmeticCustomizer>(true);
        if(customizer==null||customizer.modelRoot==null)yield break;
        var spin=customizer.modelRoot.GetComponent<CosmeticHitSpin>()??customizer.modelRoot.gameObject.AddComponent<CosmeticHitSpin>();
        spin.Play(duration);yield return new WaitForSeconds(duration);
    }
    void ToppleLampOnly()
    {
        if (_lampFallen) return;
        _lampFallen = true;

        // 바닥 힌지 자동 산출(렌더러 → 콜라이더 순)
        if (_hinge == Vector3.zero)
        {
            var r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                var b = r.bounds;
                _hinge = new Vector3(b.center.x, b.min.y, b.center.z);
            }
            else
            {
                var c = GetComponentInChildren<Collider>();
                var b = c.bounds;
                _hinge = new Vector3(b.center.x, b.min.y, b.center.z);
            }
        }

        // Read the original bounds before disabling physics, then disarm before moving.
        lampColliders = GetComponentsInChildren<Collider>(true);
        lampColliderEnabled = new bool[lampColliders.Length];
        for (int i = 0; i < lampColliders.Length; i++)
        {
            lampColliderEnabled[i] = lampColliders[i].enabled;
            lampColliders[i].enabled = false;
        }

        // DOTween: 우측으로만 자연스럽게 쓰러짐 (transform.forward 축으로 롤)
        float target = 88f, prev = 0f;
        DOVirtual.Float(0f, target, 0.5f, a =>
        {
            float delta = a - prev; prev = a;
            // 오른쪽으로 넘어짐. 반대면 부호를 +delta로 바꿔줘.
            transform.RotateAround(_hinge, transform.forward, -delta);
        })
        .SetEase(Ease.InOutQuad)
        .OnComplete(() =>
        {
            _lampSettled = true;
            for (int i = 0; i < lampColliders.Length; i++)
                if (lampColliders[i] != null) lampColliders[i].enabled = lampColliderEnabled[i];
        })
        .SetTarget(transform);
    }

    void ResetLampTransform()
    {
        // 진행 중인 넘어짐 트윈 끊기
        DOTween.Kill(transform);

        if (lampColliders != null)
        {
            for (int i = 0; i < lampColliders.Length; i++)
                if (lampColliders[i] != null) lampColliders[i].enabled = lampColliderEnabled[i];
            lampColliders = null;
            lampColliderEnabled = null;
        }

        // 상태/힌지 리셋
        _lampFallen = false;
        _lampSettled = false;
        _hinge = Vector3.zero;

        // 원래 트랜스폼 복구
        transform.localPosition = _startLocalPos;
        transform.localRotation = _startLocalRot;
        transform.localScale = _startLocalScale;
    }

    IEnumerator TelegraphThenDrop()
    {
        if (shadowSprite)
        {
            shadowSprite.enabled = true;
            shadowSprite.transform.localScale = Vector3.one * shadowStartScale;
        }
        float elapsed = 0f;
        while (elapsed < telegraphTime)
        {
            yield return null;
            elapsed += SeagullDelta();
            if (shadowSprite) shadowSprite.transform.localScale = Vector3.one * Mathf.Lerp(
                shadowStartScale, shadowEndScale, Mathf.Clamp01(elapsed / Mathf.Max(.001f, telegraphTime)));
        }
        if (balloon == null) yield break;
        Vector3 landing = _impactPoint + Vector3.up * seagullLandingOffset;
        Vector3 start = landing + Vector3.up * dropHeight;
        balloon.SetPositionAndRotation(start, transform.rotation);
        balloon.gameObject.SetActive(true);
        if (seagullAnimator != null) { seagullAnimator.ResetTrigger("Land"); seagullAnimator.SetTrigger("Fly"); }
        SetSeagullContact(true);
        elapsed = 0f; bool folding = false;
        while (elapsed < dropTime)
        {
            yield return null;
            elapsed += SeagullDelta();
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.001f, dropTime));
            balloon.position = Vector3.Lerp(start, landing, 1f - (1f - t) * (1f - t));
            if (!folding && t >= .6f)
            { folding = true; if (seagullAnimator != null) seagullAnimator.SetTrigger("Land"); }
        }
        balloon.position = landing;
        OnBalloonImpact();
    }

    private static float SeagullDelta() => TimeManager.isGameRunning
        ? Time.deltaTime * Mathf.Max(0f, TimeManager.timeFactor) : 0f;

    private IEnumerator WaitForSeagull(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds) { yield return null; elapsed += SeagullDelta(); }
    }

    void OnBalloonImpact()
    {
        if (shadowSprite) shadowSprite.enabled = false;
        // Keep the bird touchable while landed/departing. Finishing the descent
        // must not disable contact before the next physics step.
        if (balloon != null) StartCoroutine(SeagullLeave());
    }

    IEnumerator SeagullLeave()
    {
        yield return WaitForSeagull(.6f);
        // An early landing must remain an obstacle until the runner reaches it.
        // Lateral dodges still count as passing; compare progress along the road.
        while (ShouldKeepSeagullLanded()) yield return null;
        if (balloon == null) yield break;
        if (seagullAnimator != null) seagullAnimator.SetTrigger("Fly");
        Vector3 start = balloon.position, end = start + transform.forward * 4f + Vector3.up * 6f;
        float elapsed = 0f;
        while (elapsed < .9f)
        { yield return null; elapsed += SeagullDelta(); balloon.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / .9f)); }
        SetSeagullContact(false);
        if (balloon != null) balloon.gameObject.SetActive(false);
    }

    private bool ShouldKeepSeagullLanded() => _player != null && !_seagullHit &&
        Vector3.Dot(_player.position - _impactPoint, transform.forward) < 2f;

    private void SetSeagullContact(bool active)
    {
        seagullContactActive = active && !_seagullHit;
        if (seagullColliders == null) return;
        foreach (var collider in seagullColliders)
            if (collider != null) { collider.isTrigger = true; collider.enabled = seagullContactActive; }
    }

    public bool TrySeagullContact(Collider other)
    {
        if (!isActiveAndEnabled || !TimeManager.isGameRunning || obstaclePattern != ObstaclePattern.Seagull ||
            _seagullHit || !seagullContactActive || balloon == null || !balloon.gameObject.activeInHierarchy || other == null)
            return false;
        // Ignore invisible weapon/child colliders. Only the player's body counts.
        var player = other.GetComponent<PlayerScript>();
        if (player == null || player.currentHealth <= 0f) return false;
        _seagullHit = true; SetSeagullContact(false);
        // Cancel descent/ordinary departure before the hit animation owns the bird.
        // Start the two independent reactions only after stopping those coroutines.
        StopAllCoroutines();
        if (shadowSprite != null) shadowSprite.enabled = false;
        player.ApplyDamage(player.MaxHealth*Mathf.Clamp(value,0f,100f)/100f,PlayerDamageCause.Seagull);
        StartCoroutine(SpinAndMovePlayer(player.transform, seagullSpinSeconds));
        StartCoroutine(SeagullKnockback(player.transform));
        return true;
    }

    private Vector3 SeagullKnockbackDirection(Transform player)
    {
        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .001f) forward = transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 away = Vector3.ProjectOnPlane(balloon.position - player.position, Vector3.up).normalized;
        float side = Vector3.Dot(away, right) < -.05f ? -1f : 1f;
        return (away + forward + right * (side * 1.4f)).normalized;
    }

    private IEnumerator SeagullKnockback(Transform player)
    {
        Vector3 start = balloon.position, direction = SeagullKnockbackDirection(player);
        Quaternion rotation = balloon.rotation;
        if (seagullAnimator != null)
        {
            seagullAnimator.ResetTrigger("Land");
            // A spread-wing silhouette reads clearly while stunned and tumbling.
            // The authored Fly state has no root-transform curves.
            seagullAnimator.Play("Fly", 0, 0f);
            seagullAnimator.Update(0f);
            seagullAnimator.enabled = false;
        }
        float elapsed = 0f;
        const float duration = 1.1f;
        while (elapsed < duration && balloon != null)
        {
            yield return null;
            elapsed += SeagullDelta();
            if (balloon != null) ApplySeagullKnockbackPose(start, direction, rotation, Mathf.Clamp01(elapsed / duration));
        }
        if (balloon != null) balloon.gameObject.SetActive(false);
    }

    private void ApplySeagullKnockbackPose(Vector3 start, Vector3 direction, Quaternion rotation, float progress)
    {
        // Carry the current impact pose into an outward/upward arc, with three tumbles.
        Vector3 axis = (Vector3.Cross(Vector3.up, direction) + Vector3.up * .25f).normalized;
        balloon.SetPositionAndRotation(
            start + direction * (7.5f * progress) + Vector3.up * (2f * progress + 3.5f * Mathf.Sin(Mathf.PI * progress)),
            Quaternion.AngleAxis(1080f * progress, axis) * rotation);
    }

    private bool FireFromBow()
    {
        if (!projectilePrefab || !firePos) return false;

        // FirePos is authored along the cannon toward the road. Preserve the
        // waiting ship's heading when the approaching player triggers the shot.
        Vector3 dir = firePos.forward;

        GameObject proj = Instantiate(projectilePrefab, firePos.position, Quaternion.LookRotation(dir));
        proj.SetActive(true);
        // The template is a tiny imported mesh nested under a scaled ship.
        proj.transform.localScale = projectilePrefab.transform.lossyScale;
        proj.name = "CannonBall_Shot";
        foreach (var renderer in proj.GetComponentsInChildren<Renderer>(true))
        { renderer.enabled = true; renderer.forceRenderingOff = false; }

        var collider = proj.GetComponent<Collider>();
        if (collider == null) collider = proj.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        var sp = proj.GetComponent<SimpleProjectile>() ?? proj.AddComponent<SimpleProjectile>();
        sp.Launch(dir, 30f, value, 8f);
        sp.damageCause=PlayerDamageCause.Cannon;
        shipShot = proj;
        if (GameManager.S != null) GameManager.S.RegisterDestroyTarget(proj.gameObject);
        return true;
    }

    Vector3 _bucketStartLocalScale;

    void CacheBucketStart()
    {
        if (_bucketSaved) return;
        if (!bucket) return;

        _bucketSaved = true;
        _bucketStartParent = bucket.parent;
        _bucketStartLocalPos = bucket.localPosition;
        _bucketStartLocalRot = bucket.localRotation;
        _bucketStartLocalScale = bucket.localScale; // ✅ 추가
    }

    void InitBucket()
    {
        ReleaseBucketShootBlock();
        if (bucket == null)
        {
            var col = GetComponentInChildren<Collider>(true);
            bucket = col ? col.transform : transform;
        }

        CacheBucketStart();

        // ✅ 핵심: 월드 유지 X (로컬 기준으로 정확히 복구)
        bucket.SetParent(_bucketStartParent, worldPositionStays: false);

        // ✅ 원래 값 복구 (지금 네 코드에 이게 없음)
        bucket.localPosition = _bucketStartLocalPos;
        bucket.localRotation = _bucketStartLocalRot;
        bucket.localScale = _bucketStartLocalScale;

        var rb = bucket.GetComponent<Rigidbody>();
        if (rb)
        {
            if (!rb.isKinematic)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var c = bucket.GetComponent<Collider>();
        if (c)
        {
            c.enabled = true;
            c.isTrigger = true;
        }

        _bucketAttached = false;
    }


}

// #if UNITY_EDITOR
// [CustomEditor(typeof(ObstacleStats))]
// public class ObstacleStatsEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         var script = (ObstacleStats)target;

//         // 기본 필드
//         script.obstaclePattern = (ObstaclePattern)EditorGUILayout.EnumPopup("Obstacle Pattern", script.obstaclePattern);
//         script.value = EditorGUILayout.FloatField("Value", script.value);

//         // Ship일 때만 추가 필드 보이기
//         if (script.obstaclePattern == ObstaclePattern.Ship)
//         {
//             script.projectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", script.projectilePrefab, typeof(GameObject), true);
//             script.fireDistance = EditorGUILayout.FloatField("Fire Distance", script.fireDistance);
//             script.aheadOffset = EditorGUILayout.FloatField("Ahead Offset", script.aheadOffset);
//             script.firePos = (Transform)EditorGUILayout.ObjectField("Fire Position", script.firePos, typeof(Transform), true);
//         }
//         else if (script.obstaclePattern == ObstaclePattern.Dolphin)
//         {
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Dolphin Fixed Jump", EditorStyles.boldLabel);

//             script.pointA = (Transform)EditorGUILayout.ObjectField("Point A", script.pointA, typeof(Transform), true);
//             script.pointB = (Transform)EditorGUILayout.ObjectField("Point B", script.pointB, typeof(Transform), true);

//             script.jumpHeight = EditorGUILayout.FloatField("Jump Height", script.jumpHeight);
//             script.jumpTime = EditorGUILayout.FloatField("Jump Time", script.jumpTime);
//             script.lookAlongPath = EditorGUILayout.Toggle("Look Along Path", script.lookAlongPath);
//         }
//         else if (script.obstaclePattern == ObstaclePattern.Seagull)
//         {
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Balloon Tween Drop", EditorStyles.boldLabel);

//             script.balloon = (Transform)EditorGUILayout.ObjectField("Balloon (Transform)", script.balloon, typeof(Transform), true);
//             script.shadowSprite = (SpriteRenderer)EditorGUILayout.ObjectField("Shadow Sprite", script.shadowSprite, typeof(SpriteRenderer), true);

//             // groundMask 표시 (LayerMaskField)
//             var layers = UnityEditorInternal.InternalEditorUtility.layers;
//             int mask = 0;
//             for (int i = 0; i < layers.Length; i++)
//             {
//                 int id = LayerMask.NameToLayer(layers[i]);
//                 if (((1 << id) & script.groundMask.value) != 0) mask |= (1 << i);
//             }
//             int newMask = EditorGUILayout.MaskField("Ground Mask", mask, layers);
//             int finalMask = 0;
//             for (int i = 0; i < layers.Length; i++)
//             {
//                 if ((newMask & (1 << i)) != 0) finalMask |= (1 << LayerMask.NameToLayer(layers[i]));
//             }
//             script.groundMask = finalMask;

//             // (선택) 텔레그래프/낙하 파라미터도 노출
//             script.triggerRadius = EditorGUILayout.FloatField("Trigger Radius", script.triggerRadius);
//             script.telegraphTime = EditorGUILayout.FloatField("Telegraph Time", script.telegraphTime);
//             script.shadowStartScale = EditorGUILayout.FloatField("Shadow Start Scale", script.shadowStartScale);
//             script.shadowEndScale = EditorGUILayout.FloatField("Shadow End Scale", script.shadowEndScale);
//             script.dropHeight = EditorGUILayout.FloatField("Drop Height", script.dropHeight);
//             script.dropTime = EditorGUILayout.FloatField("Drop Time", script.dropTime);
//         }

//         else if (script.obstaclePattern == ObstaclePattern.Bucket)
//         {
//             EditorGUILayout.Space(); EditorGUILayout.LabelField("Bucket (Fish Tub)", EditorStyles.boldLabel);
//             script.bucket = (Transform)EditorGUILayout.ObjectField("Bucket", script.bucket, typeof(Transform), true);
//             script.bucketAttachSeconds = EditorGUILayout.FloatField("Attach Seconds", script.bucketAttachSeconds);
//             script.bucketHeadOffset = EditorGUILayout.Vector3Field("Head Offset", script.bucketHeadOffset);
//             script.bucketDetachImpulse = EditorGUILayout.Vector3Field("Detach Impulse", script.bucketDetachImpulse);
//             script.bucketDetachAngularImpulse = EditorGUILayout.Vector3Field("Detach Angular Impulse", script.bucketDetachAngularImpulse);
//             script.destroyAfterDetach = EditorGUILayout.Toggle("Destroy After Detach", script.destroyAfterDetach);

//             EditorGUILayout.HelpBox("고정형 트리거 버킷: 닿으면 2초 씌워지고 뒤로 튕긴 후 소멸.", MessageType.Info);
//         }


//         //if (GUI.changed) EditorUtility.SetDirty(script);
//     }
// }
// #endif
