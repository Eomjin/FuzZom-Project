using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;


// 적 게임 오브젝트를 주기적으로 생성
public class EnemySpawner : MonoBehaviourPun, IPunObservable 
{
    public Enemy enemyPrefab; // 생성할 적 AI
    public Transform[] spawnPoints; // 적 AI를 소환할 위치들

    public float damageMax = 40f; // 최대 공격력
    public float damageMin = 20f; // 최소 공격력

    public float healthMax = 200f; // 최대 체력
    public float healthMin = 100f; // 최소 체력

    public float speedMax = 3f; // 최대 속도
    public float speedMin = 1f; // 최소 속도

    public Color strongEnemyColor = Color.red; 

    private List<Enemy> enemies = new List<Enemy>(); 

    private int enemyCount = 0; // 남은 적의 수
    private int wave; // 현재 웨이브

   
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) 
    {
        if (stream.IsWriting)
        {
            stream.SendNext(enemies.Count); // 적의 남은 수를 네트워크를 통해 보내기
            stream.SendNext(wave); // 현재 웨이브를 네트워크를 통해 보내기
        }
        else
        {
            enemyCount = (int) stream.ReceiveNext();
            wave = (int) stream.ReceiveNext(); // 현재 웨이브를 네트워크를 통해 받기 
        }
    }

    [System.Obsolete]
    void Awake() 
    {
        PhotonPeer.RegisterType(typeof(Color), 128, ColorSerialization.SerializeColor,
            ColorSerialization.DeserializeColor);
    }

    private void Update() 
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 게임 오버 상태일때는 생성하지 않음
            if (GameManager.instance != null && GameManager.instance.isGameover)
            {
                return;
            }

            // 적을 모두 물리친 경우 다음 스폰 실행
            if (enemies.Count <= 0)
            {
                SpawnWave();
            }
        }

        UpdateUI(); // UI 갱신
    }

    // 웨이브 정보를 UI로 표시
    private void UpdateUI() 
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UIManager.instance.UpdateWaveText(wave, enemies.Count);
        }
        else
        {
            UIManager.instance.UpdateWaveText(wave, enemyCount);
        }
    }

    // 현재 웨이브에 맞춰 적을 생성
    private void SpawnWave() 
    {
        wave++;  // 웨이브 1 증가

        // 현재 웨이브 * 1.5에 반올림 한 개수 만큼 적을 생성
        int spawnCount = Mathf.RoundToInt(wave * 1.5f);

        // spawnCount 만큼 적을 생성
        for (int i = 0; i < spawnCount; i++)
        {
            // 적의 세기를 0%에서 100% 사이에서 랜덤 결정
            float enemyIntensity = Random.Range(0f, 1f);
          
            CreateEnemy(enemyIntensity); // 적 생성 처리 실행
        }
    }

    // 적을 생성하고 생성한 적에게 추적할 대상을 할당
    private void CreateEnemy(float intensity)
    {
        float health = Mathf.Lerp(healthMin, healthMax, intensity);
        float damage = Mathf.Lerp(damageMin, damageMax, intensity);
        float speed = Mathf.Lerp(speedMin, speedMax, intensity);

        Color skinColor = Color.Lerp(Color.white, strongEnemyColor, intensity);

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)]; // 생성할 위치를 랜덤으로 결정

        // 적 프리팹으로부터 적을 생성, 네트워크 상의 모든 클라이언트들에게 생성됨
        GameObject createdEnemy = PhotonNetwork.Instantiate(enemyPrefab.gameObject.name, spawnPoint.position, spawnPoint.rotation);
        
        Enemy enemy = createdEnemy.GetComponent<Enemy>();

        // 생성한 적의 능력치와 추적 대상 설정
        enemy.photonView.RPC("Setup", RpcTarget.All, health, damage, speed, skinColor);

        enemies.Add(enemy); // 생성된 적을 리스트에 추가

        enemy.onDeath += () => enemies.Remove(enemy); // 사망한 적을 리스트에서 제거
        enemy.onDeath += () => StartCoroutine(DestroyAfter(enemy.gameObject, 10f)); // 사망한 적을 10 초 뒤에 파괴
        enemy.onDeath += () => GameManager.instance.AddScore(100); // 적 사망시 점수 상승
    }

    // 포톤의 Network.Destroy()는 지연 파괴를 지원하지 않으므로 지연 파괴를 직접 구현함
    IEnumerator DestroyAfter(GameObject target, float delay) 
    {
        yield return new WaitForSeconds(delay);
    
        if (target != null)
        {
            PhotonNetwork.Destroy(target);
        }
    }
}