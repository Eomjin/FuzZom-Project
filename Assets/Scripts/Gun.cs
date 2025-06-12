using System.Collections;
using Photon.Pun;
using UnityEngine;

// 총을 구현
public class Gun : MonoBehaviourPun, IPunObservable 
{
    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄창이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } 

    public Transform fireTransform; // 총알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    private LineRenderer bulletLineRenderer; // 총알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기
    public AudioClip shotClip; // 발사 소리
    public AudioClip reloadClip; // 재장전 소리

    public float damage = 25; // 공격력
    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄약
    public int magCapacity = 25; // 탄창 용량
    public int magAmmo; // 현재 탄창에 남아있는 탄약

    public float timeBetFire = 0.12f; // 총알 발사 간격
    public float reloadTime = 1.8f; // 재장전 소요 시간
    private float lastFireTime; // 총을 마지막으로 발사한 시점

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) 
    {
        if (stream.IsWriting)
        {
            stream.SendNext(ammoRemain); // 남은 탄약수를 네트워크를 통해 보내기
            stream.SendNext(magAmmo); // 탄창의 탄약수를 네트워크를 통해 보내기
            stream.SendNext(state); // 현재 총의 상태를 네트워크를 통해 보내기
        }
        else
        {
            ammoRemain = (int) stream.ReceiveNext(); // 남은 탄약수를 네트워크를 통해 받기
            magAmmo = (int) stream.ReceiveNext(); // 탄창의 탄약수를 네트워크를 통해 받기
            state = (State) stream.ReceiveNext(); // 현재 총의 상태를 네트워크를 통해 받기
        }
    }

    // 남은 탄약을 추가
    [PunRPC]
    public void AddAmmo(int ammo) 
    {
        ammoRemain += ammo;
    }

    private void Awake() 
    {
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        bulletLineRenderer.positionCount = 2;
        bulletLineRenderer.enabled = false;
    }


    private void OnEnable() 
    {
        magAmmo = magCapacity; // 현재 탄창을 가득채우기
        state = State.Ready; // 총의 현재 상태를 총을 쏠 준비가 된 상태로 변경
        lastFireTime = 0; // 마지막으로 총을 쏜 시점을 초기화
    }


    // 발사 시도
    public void Fire() 
    {
        // 현재 상태가 발사 가능한 상태
        // && 마지막 총 발사 시점에서 timeBetFire 이상의 시간이 지남
        if (state == State.Ready
            && Time.time >= lastFireTime + timeBetFire)
        {
            lastFireTime = Time.time; // 마지막 총 발사 시점을 갱신
            Shot(); // 실제 발사 처리 실행
        }
    }

    private void Shot() 
    {
        photonView.RPC("ShotProcessOnServer", RpcTarget.MasterClient);

        magAmmo--; // 남은 탄환의 수를 -1
        if (magAmmo <= 0)
        {
            state = State.Empty; // 탄창에 남은 탄약이 없다면, 총의 현재 상태를 Empty으로 갱신
        }
    }

    // 호스트에서 실행되는, 실제 발사 처리
    [PunRPC]
    private void ShotProcessOnServer() 
    {
        RaycastHit hit;
        Vector3 hitPosition = Vector3.zero;

        if (Physics.Raycast(fireTransform.position,
            fireTransform.forward, out hit, fireDistance))
        {
            IDamageable target =
                hit.collider.GetComponent<IDamageable>();

            if (target != null)
            {
                target.OnDamage(damage, hit.point, hit.normal);
            }

            hitPosition = hit.point;
        }
        else
        {
            hitPosition = fireTransform.position +
                          fireTransform.forward * fireDistance;
        }

        photonView.RPC("ShotEffectProcessOnClients", RpcTarget.All, hitPosition);
    }

    [PunRPC]
    private void ShotEffectProcessOnClients(Vector3 hitPosition) 
    {
        StartCoroutine(ShotEffect(hitPosition));
    }

    private IEnumerator ShotEffect(Vector3 hitPosition) 
    {
        muzzleFlashEffect.Play(); // 총구 화염 효과 재생
        shellEjectEffect.Play(); // 탄피 배출 효과 재생
        gunAudioPlayer.PlayOneShot(shotClip); // 총격 소리 재생

        bulletLineRenderer.SetPosition(0, fireTransform.position); // 선의 시작점은 총구의 위치
        bulletLineRenderer.SetPosition(1, hitPosition); // 선의 끝점은 입력으로 들어온 충돌 위치
        bulletLineRenderer.enabled = true; // 라인 렌더러를 활성화하여 총알 궤적을 그린다

        yield return new WaitForSeconds(0.03f); // 0.03초 동안 잠시 처리를 대기

        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() 
    {
        if (state == State.Reloading ||
            ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            return false;
        }

        StartCoroutine(ReloadRoutine());
        return true; // 재장전 처리 실행
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() 
    {
        state = State.Reloading; // 현재 상태를 재장전 중 상태로 전환
        gunAudioPlayer.PlayOneShot(reloadClip); // 재장전 소리 재생

        yield return new WaitForSeconds(reloadTime); // 재장전 소요 시간 만큼 처리를 쉬기

        int ammoToFill = magCapacity - magAmmo; // 탄창에 채울 탄약을 계산

        if (ammoRemain < ammoToFill)
        {
            ammoToFill = ammoRemain;
        }

        magAmmo += ammoToFill; // 탄창을 채움
        ammoRemain -= ammoToFill; // 남은 탄약에서, 탄창에 채운만큼 탄약을 뺸다

        state = State.Ready; // 총의 현재 상태를 발사 준비된 상태로 변경
    }
}