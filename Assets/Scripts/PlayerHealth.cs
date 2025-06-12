using Photon.Pun;
using UnityEngine;
using UnityEngine.UI; // UI 관련 코드

// 플레이어 캐릭터의 생명체로서의 동작을 담당
public class PlayerHealth : LivingEntity 
{
    public Slider healthSlider; // 체력을 표시할 UI 슬라이더

    public AudioClip deathClip; // 사망 소리
    public AudioClip hitClip; // 피격 소리
    public AudioClip itemPickupClip; // 아이템 습득 소리

    private AudioSource playerAudioPlayer; // 플레이어 소리 재생기
    private Animator playerAnimator; // 플레이어의 애니메이터

    private PlayerMovement playerMovement; // 플레이어 움직임 컴포넌트
    private PlayerShooter playerShooter; // 플레이어 슈터 컴포넌트

    private void Awake() 
    {
        // 사용할 컴포넌트를 가져오기
        playerAnimator = GetComponent<Animator>();
        playerAudioPlayer = GetComponent<AudioSource>();

        playerMovement = GetComponent<PlayerMovement>();
        playerShooter = GetComponent<PlayerShooter>();
    }

    protected override void OnEnable() 
    {
        base.OnEnable(); // LivingEntity의 OnEnable() 실행 (상태 초기화)

        healthSlider.gameObject.SetActive(true);  // 체력 슬라이더 활성화
        healthSlider.maxValue = startingHealth; // 체력 슬라이더의 최대값을 기본 체력값으로 변경
        healthSlider.value = health; // 체력 슬라이더의 값을 현재 체력값으로 변경

        // 플레이어 조작을 받는 컴포넌트들 활성화
        playerMovement.enabled = true;
        playerShooter.enabled = true;
    }

    // 체력 회복
    [PunRPC]
    public override void RestoreHealth(float newHealth) 
    {
        base.RestoreHealth(newHealth); // LivingEntity의 RestoreHealth() 실행 (체력 증가)
        healthSlider.value = health; // 체력 갱신
    }


    // 데미지 처리
    [PunRPC]
    public override void OnDamage(float damage, Vector3 hitPoint,Vector3 hitDirection) 
    {
        if (!dead)
        {
            playerAudioPlayer.PlayOneShot(hitClip);
        }

        base.OnDamage(damage, hitPoint, hitDirection);
        healthSlider.value = health; // 갱신된 체력을 체력 슬라이더에 반영
    }

    public override void Die() 
    {
        base.Die(); // LivingEntity의 Die() 실행(사망 적용)

        healthSlider.gameObject.SetActive(false); // 체력 슬라이더 비활성화
        playerAudioPlayer.PlayOneShot(deathClip); // 사망음 재생

        playerAnimator.SetTrigger("Die");

        // 플레이어 조작을 받는 컴포넌트들 비활성화
        playerMovement.enabled = false;
        playerShooter.enabled = false;

        Invoke("Respawn", 5f); // 5초 뒤에 리스폰
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (!dead)
        {
            IItem item = other.GetComponent<IItem>();

            if (item != null)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    item.Use(gameObject);
                }

                playerAudioPlayer.PlayOneShot(itemPickupClip);
            }
        }
    }

    // 부활 처리
    public void Respawn() 
    {
        if (photonView.IsMine) // 로컬 플레이어만 직접 위치를 변경 가능
        {
            // 원점에서 반경 5유닛 내부의 랜덤한 위치 지정
            Vector3 randomSpawnPos = Random.insideUnitSphere * 5f;
            randomSpawnPos.y = 0f; // 랜덤 위치의 y값을 0으로 변경

            transform.position = randomSpawnPos; // 지정된 랜덤 위치로 이동
        }

        // 컴포넌트들을 리셋하기 위해 게임 오브젝트를 잠시 껐다가 다시 켜기
        // 컴포넌트들의 OnDisable(), OnEnable() 메서드가 실행됨
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}