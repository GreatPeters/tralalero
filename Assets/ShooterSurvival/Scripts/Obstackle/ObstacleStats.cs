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


    //열기구 관련
    [Header("Balloon Tween Drop")]
    public Transform balloon;            // 열기구(비활성 시작 권장)
    public SpriteRenderer shadowSprite;  // 바닥 그림자 스프라이트(처음엔 꺼두기)
    public LayerMask groundMask;         // 바닥 레이어

    public float triggerRadius = 6f;     // 이 거리 안 들어오면 텔레그래프 시작
    public float telegraphTime = 1.0f;   // 그림자 커지는 시간
    public float shadowStartScale = 0.2f;
    public float shadowEndScale = 1.8f;

    public float dropHeight = 12f;       // 위에서부터 떨어질 높이
    public float dropTime = 0.18f;     // 바닥까지 떨어지는 시간


    Transform _player;
    Vector3 _impactPoint;
    bool _started;   // 이미 시작했는지


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

        float yaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
        //if (add180) yaw += 180f;
        //yaw += yawOffset;

        if (add180)
        {
            yaw = -90f;
        }
        else
        {
            yaw = 90f;
        }

        if (transform.name.Contains("right"))
        {
            yaw *= -1f;
        }

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
        StopAllCoroutines();
        DOTween.Kill(gameObject);
        _jumpSeq?.Kill();
    }
    void OnDestroy() { _jumpSeq?.Kill(); }



    Vector3 _startLocalPos;
    Quaternion _startLocalRot;
    Vector3 _startLocalScale;
    bool _savedStart;

    bool _lampFallen;
    bool _lampDamagedOnce;   // 플레이어가 닿았을 때 1회만 데미지 주려면 사용
    Vector3 _hinge;          // 바닥 힌지 (Bounds로 자동 계산)

    public ObstaclePattern obstaclePattern;
    public float value = 10f;                 // 플레이어에게 적용할 기본 수치(피해량, 감속값 등)
    public Transform firePos;
    public GameObject projectilePrefab;       // Inspector에서 투사체(이펙트) 프리팹 할당
    public float fireDistance = 12f;          // 발사 트리거 거리
    public float aheadOffset = 3f;            // 플레이어 진행 방향 앞쪽으로 조준할 거리

    private bool hasFired = false;

    void Start()
    {
        if (obstaclePattern == ObstaclePattern.Oldman)
        {
            SimpleProjectile sp = transform.GetComponentInChildren<SimpleProjectile>();
            sp.damage = value;
        }
        else if (obstaclePattern == ObstaclePattern.Dolphin)
        {
            dolphinAnim = GetComponentInChildren<Animator>();
            StartFixedZigZag();
        }
        else if (obstaclePattern == ObstaclePattern.Seagull) // ← 기존 Balloon 패턴 재사용
        {
            // 낙하지점 Y 고정
            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out var hit, 40f, groundMask))
                //if (Physics.SphereCast(new Ray(transform.position + Vector3.up * 50f, Vector3.down), 0.25f, out var hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
                _impactPoint = hit.point;
            else
                _impactPoint = transform.position;

            transform.position = _impactPoint;

            if (shadowSprite)
            {
                shadowSprite.enabled = false;
                shadowSprite.transform.position = _impactPoint + Vector3.up * 0.02f;
                shadowSprite.transform.localScale = Vector3.one * shadowStartScale;
            }
            if (balloon) balloon.gameObject.SetActive(false);

            // 부모에 kinematic Rigidbody 보장(콜백 받기용)
            var rb = GetComponent<Rigidbody>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            // 자식(풍선) 콜라이더를 트리거로 쓰되, 낙하 전엔 꺼둠
            var bcol = balloon ? balloon.GetComponent<Collider>() : null;
            if (bcol) { bcol.isTrigger = true; bcol.enabled = false; }
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
        // _lampDamagedOnce = false;

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
        _started = false;
        _bucketAttached = false;
        _lampFallen = false;
        _lampDamagedOnce = false;

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

        var bcol = balloon ? balloon.GetComponent<Collider>() : null;
        if (bcol)
        {
            bcol.isTrigger = true;
            bcol.enabled = false;
        }

        // 6) 부모 rigidbody 보장(콜백/트리거 안정용)
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }



    private void OnTriggerEnter(Collider other)
    {
        // Light: 미사일이 닿으면 → 쓰러지기만 (데미지는 안 줌)
        if (obstaclePattern == ObstaclePattern.Light && other.CompareTag("BulletTag"))
        {
            ToppleLampOnly();
            return;
        }

        if (!other.CompareTag("Player")) return;

        var playerScript = other.GetComponent<PlayerScript>();
        if (playerScript == null) return;

        switch (obstaclePattern)
        {
            case ObstaclePattern.Hole:
                Debug.Log("홀이다!!!!");
                quaternion toRot = Quaternion.Euler(110f, playerScript.transform.root.rotation.eulerAngles.y, playerScript.transform.root.rotation.eulerAngles.z);
                playerScript.transform.root.DORotateQuaternion(toRot, 1f);

                // 체력 감소
                playerScript.currentHealth = Mathf.Max(0, playerScript.currentHealth - value);

                // 2) X축만 110°로 부드럽게 꺾기 (0.18초), 1초 유지, 원복 안 함
                //StartCoroutine(TiltXOnly110(playerScript, tweenTime: 0.18f, holdSeconds: 1.0f, restore: false));               

                break;

            case ObstaclePattern.Oil:
                // 거미줄: 이동 민감도(또는 속도) 감소
                //playerScript.moveSensitivity = Mathf.Max(1f, playerScript.moveSensitivity - value);
                //StartCoroutine(SpinPlayerForSeconds(playerScript.transform, value, 720f));
                StartCoroutine(SpinAndMovePlayer(playerScript.transform, value));
                break;

            case ObstaclePattern.Ship:
                // 배: 점수 감소
                playerScript.playerScore = Mathf.Max(0, playerScript.playerScore - (int)value);
                break;

            case ObstaclePattern.Seagull:
                {
                    Debug.Log("열기구 즉사 범위 체크");
                    //var ps = other.GetComponent<PlayerScript>();
                    if (playerScript) playerScript.currentHealth = Mathf.Max(0, playerScript.currentHealth - value); // 즉사면 value 크게

                    var bcol = balloon ? balloon.GetComponent<Collider>() : null;
                    if (bcol) bcol.enabled = false; // 중복 타격 방지

                    //갈매기 위로 튀어오르면서 날라가게하기
                    transform.DOMove(new Vector3(transform.position.x - 2f, transform.position.y + 10f, transform.position.z - 2f), 2f).SetEase(Ease.OutQuad); // 위로 쭉~
                    transform.DORotate(new Vector3(360f * 5f, 0, 0), 2f, RotateMode.FastBeyond360); // 회전


                    return; // ← 여기서 메서드 종료 (break 불필요)
                }

            case ObstaclePattern.Light:
                // 체력 감소
                playerScript.currentHealth = Mathf.Max(0, playerScript.currentHealth - value);
                break;

            case ObstaclePattern.Bucket:
                if (_bucketAttached) return;
                StartCoroutine(AttachBucketRoutine(playerScript));
                return;

                // Firework, Whale, Fog, Light 등은 필요 시 추가 효과 구현
        }
    }

    void Update()
    {
        if (!TimeManager.isGameRunning) return;

        if (obstaclePattern == ObstaclePattern.Seagull && !_started)
        {
            if (_player == null)
            {
                var ps = GameManager.S?.playerScript;
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

        var player = GameManager.S.playerScript.gameObject;
        if (player == null)
            return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= fireDistance)
        {
            Debug.Log("쏜다!");
            FireAheadOfPlayer(player.transform);
            hasFired = true;
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
        bucket.SetParent(player.transform, worldPositionStays: false);
        bucket.localPosition = bucketHeadOffset;
        // 보기 좋게 약간 기울여 씌우는 각도 (원하면 identity로)
        bucket.localRotation = Quaternion.Euler(12.37f, 180f, 0f);

        player.canShoot = false;

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
        player.canShoot = true;

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
        // 1️⃣ 충돌 방지
        var col = GetComponent<BoxCollider>();
        if (col != null) col.enabled = false;

        if (player == null) yield break;

        // 2️⃣ 초기 상태 저장
        float elapsed = 0f;
        float spinSpeed = 720f;  // 초당 2바퀴
        float moveDistance = 12f; // 회전 중 앞으로 이동할 거리
        Vector3 startPos = player.position;
        Vector3 forwardDir = player.forward;
        Quaternion originalRot = player.rotation; // 원래 회전값 저장

        // 3️⃣ 앞으로 이동 (DOTween)
        player.DOMove(startPos + forwardDir * moveDistance, duration)
              .SetEase(Ease.InOutSine);

        // 4️⃣ 회전 루프
        while (elapsed < duration)
        {
            if (player != null)
            {
                float dt = Time.deltaTime;
                player.Rotate(Vector3.up, spinSpeed * dt, Space.World);
                elapsed += dt;
            }
            yield return null;
        }

        // 5️⃣ 원래 회전으로 복귀 (부드럽게)
        player.DORotateQuaternion(originalRot, 0.4f)
              .SetEase(Ease.OutSine);

        // 6️⃣ 콜라이더 복구
        yield return new WaitForSeconds(0.5f);
        if (col != null) col.enabled = true;
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

        // DOTween: 우측으로만 자연스럽게 쓰러짐 (transform.forward 축으로 롤)
        float target = 88f, prev = 0f;
        DOVirtual.Float(0f, target, 0.5f, a =>
        {
            float delta = a - prev; prev = a;
            // 오른쪽으로 넘어짐. 반대면 부호를 +delta로 바꿔줘.
            transform.RotateAround(_hinge, transform.forward, -delta);
        })
        .SetEase(Ease.InOutQuad);
    }

    void ResetLampTransform()
    {
        // 진행 중인 넘어짐 트윈 끊기
        DOTween.Kill(transform);

        // 상태/힌지 리셋
        _lampFallen = false;
        _hinge = Vector3.zero;

        // 원래 트랜스폼 복구
        transform.localPosition = _startLocalPos;
        transform.localRotation = _startLocalRot;
        transform.localScale = _startLocalScale;
    }

    IEnumerator TelegraphThenDrop()
    {
        // 1) 그림자 켜고 커지기
        if (shadowSprite)
        {
            shadowSprite.enabled = true;
            shadowSprite.transform.localScale = Vector3.one * shadowStartScale;
            shadowSprite.transform.DOScale(Vector3.one * shadowEndScale, telegraphTime)
                                   .SetEase(Ease.InOutSine);
        }

        yield return new WaitForSeconds(telegraphTime);

        // 2) 열기구 스폰 & 낙하(DOTween)
        if (balloon)
        {
            balloon.gameObject.SetActive(true);

            // 낙하하면서부터 충돌 활성화 (트리거 ON)
            var bcol = balloon.GetComponent<Collider>();
            if (bcol) { bcol.isTrigger = true; bcol.enabled = true; }

            // 아래 낙하 트윈 코드는 그대로
            Animator anim = transform.GetComponentInChildren<Animator>();
            anim.SetTrigger("Fly");

            float bottomOffset = GetBalloonBottomOffset(); // 반높이
            float targetY = _impactPoint.y + bottomOffset - 1.424167f;
            balloon.position = new Vector3(_impactPoint.x, targetY + dropHeight, _impactPoint.z);
            balloon.rotation = Quaternion.identity;

            yield return balloon.DOMoveY(targetY, dropTime)
                                .SetEase(Ease.InQuad)
                                .WaitForCompletion();

            anim.SetTrigger("Land");
            Debug.Log("Land!");
        }

        OnBalloonImpact();
    }

    float GetBalloonBottomOffset()
    {
        var r = balloon.GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds.extents.y;      // 월드 기준 반높이
        var c = balloon.GetComponentInChildren<Collider>();
        if (c != null) return c.bounds.extents.y;
        return 0f;
    }

    void OnBalloonImpact()
    {
        // (반경 즉사 제거) 충돌 데미지는 OnTriggerEnter에서 처리됨
        if (shadowSprite) shadowSprite.enabled = false;

        // 안전: 혹시 켜져있다면 콜라이더 꺼주기
        var bcol = balloon ? balloon.GetComponent<Collider>() : null;
        if (bcol) bcol.enabled = false;

        // 이펙트/사운드 있으면 여기서
    }






    private void FireAheadOfPlayer(Transform playerTransform)
    {
        if (!projectilePrefab || !firePos) return;

        Vector3 targetPos = playerTransform.position + playerTransform.forward * aheadOffset;
        Vector3 dir = (targetPos - firePos.position).normalized;

        GameObject proj = Instantiate(projectilePrefab, firePos.position, Quaternion.LookRotation(dir));
        proj.SetActive(true);
        proj.transform.localScale = Vector3.one;

        var rb = proj.GetComponent<Rigidbody>();
        //Rigidbody rb = proj.AddComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = dir * 30f;

        var sp = proj.GetComponent<SimpleProjectile>();
        if (sp != null)
        {
            sp.damage = value;
            sp.targetTag = "Player";
        }

        Destroy(proj, 5f);
        GameManager.S.RegisterDestroyTarget(proj.gameObject);
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
            rb.isKinematic = true;
            rb.useGravity = false;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
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
