using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI; // AI, 내비게이션 시스템 관련 코드를 가져오기

// 적 AI 구현
public class Enemy : LivingEntity 
{
    public LayerMask whatIsTarget; 

    private LivingEntity targetEntity; // 추적할 대상
    private NavMeshAgent pathFinder; // 경로계산 AI 에이전트

    public ParticleSystem hitEffect; 
    public AudioClip deathSound; 
    public AudioClip hitSound; 

    private Animator enemyAnimator; 
    private AudioSource enemyAudioPlayer; 
    private Renderer enemyRenderer; 

    public float damage = 20f; // 공격력
    public float timeBetAttack = 0.5f; // 공격 간격
    private float lastAttackTime; // 마지막 공격 시점

    private FuzzyLogic FuzzyLogic;
    private FuzzyLogic.FuzzyVariable distanceVariable;
    private FuzzyLogic.FuzzyVariable speedVariable;

    // 추적대상 찾기
    private bool hasTarget
    {
        get
        {
            if (targetEntity != null && !targetEntity.dead)
            {
                return true;
            }

            return false;
        }
    }

    private void Awake() 
    {
        pathFinder = GetComponent<NavMeshAgent>();
        enemyAnimator = GetComponent<Animator>();
        enemyAudioPlayer = GetComponent<AudioSource>();

        enemyRenderer = GetComponentInChildren<Renderer>();

        InitializeFuzzyLogic();
    }

    private void InitializeFuzzyLogic()
    {
        FuzzyLogic = new FuzzyLogic();

        distanceVariable = FuzzyLogic.AddVariable("Distance");
        distanceVariable.AddSet("Near", new FuzzyLogic.FuzzySet(0, 10, x => Mathf.Max(0, 1 - x / 10)));
        distanceVariable.AddSet("Far", new FuzzyLogic.FuzzySet(10, 20, x => Mathf.Max(0, (x - 10) / 10)));

        speedVariable = FuzzyLogic.AddVariable("Speed");
        speedVariable.AddSet("Slow", new FuzzyLogic.FuzzySet(0.5f, 1.5f, x => Mathf.Max(0, 1 - (x - 0.5f) / 1)));
        speedVariable.AddSet("Fast", new FuzzyLogic.FuzzySet(1.5f, 3f, x => Mathf.Max(0, (x - 1.5f) / 1.5f)));
    }

    // 적 AI의 초기 스펙
    [PunRPC]
    public void Setup(float newHealth, float newDamage, float newSpeed, Color skinColor) 
    {
        startingHealth = newHealth; // 체력 설정
        health = newHealth;
  
        damage = newDamage; // 공격력 설정
      
        pathFinder.speed = newSpeed; // 이동속도 설정
   
        enemyRenderer.material.color = skinColor;
    }

    private void Start() 
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        StartCoroutine(UpdatePath());
    }

    private void Update() 
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        enemyAnimator.SetBool("HasTarget", hasTarget);
    }

    // 주기적으로 추적할 대상의 위치를 찾아 경로를 갱신
    private IEnumerator UpdatePath() 
    {
        // 살아있는 동안 무한 루프
        while (!dead)
        {
            if (hasTarget)
            {
                // 추적 대상 존재 : 경로를 갱신하고 AI 이동을 계속 진행
                pathFinder.isStopped = false;
                pathFinder.SetDestination(targetEntity.transform.position);

                float distanceToTarget = Vector3.Distance(transform.position, targetEntity.transform.position);
                float nearMembership = distanceVariable.Fuzzify("Near", distanceToTarget);
                float farMembership = distanceVariable.Fuzzify("Far", distanceToTarget);

                float slowSpeed = speedVariable.Defuzzify("Slow", nearMembership);
                float fastSpeed = speedVariable.Defuzzify("Fast", farMembership);

                pathFinder.speed = slowSpeed + fastSpeed;
            }
            else
            {
                // 추적 대상 없음 : AI 이동 중지
                pathFinder.isStopped = true;

                Collider[] colliders =
                    Physics.OverlapSphere(transform.position, 20f, whatIsTarget);

                // 모든 콜라이더들을 순회하면서, 살아있는 플레이어를 찾기
                for (int i = 0; i < colliders.Length; i++)
                {
                    LivingEntity livingEntity = colliders[i].GetComponent<LivingEntity>();

                    if (livingEntity != null && !livingEntity.dead)
                    {
                        targetEntity = livingEntity;

                        break;
                    }
                }
            }

            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }


    // 대미지 처리
    [PunRPC]
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal) 
    {
        if (!dead)
        {
            hitEffect.transform.position = hitPoint;
            hitEffect.transform.rotation = Quaternion.LookRotation(hitNormal);
            hitEffect.Play();

            enemyAudioPlayer.PlayOneShot(hitSound);
        }

        base.OnDamage(damage, hitPoint, hitNormal);
    }

    // 사망 처리
    public override void Die()
    {
        base.Die();

        // 다른 AI들을 방해하지 않도록 자신의 모든 콜라이더들을 비활성화
        Collider[] enemyColliders = GetComponents<Collider>();
        for (int i = 0; i < enemyColliders.Length; i++)
        {
            enemyColliders[i].enabled = false;
        }

        // AI 추적을 중지하고 내비메쉬 컴포넌트를 비활성화
        pathFinder.isStopped = true;
        pathFinder.enabled = false;

        enemyAnimator.SetTrigger("Die");

        enemyAudioPlayer.PlayOneShot(deathSound);
    }

    private void OnTriggerStay(Collider other) 
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!dead && Time.time >= lastAttackTime + timeBetAttack)
        {
            LivingEntity attackTarget
                = other.GetComponent<LivingEntity>();

            if (attackTarget != null && attackTarget == targetEntity)
            {
                // 최근 공격 시간을 갱신
                lastAttackTime = Time.time;

                // 상대방의 피격 위치와 피격 방향을 근삿값으로 계산
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = transform.position - other.transform.position;

                // 공격 실행
                attackTarget.OnDamage(damage, hitPoint, hitNormal);
            }
        }
    }
}