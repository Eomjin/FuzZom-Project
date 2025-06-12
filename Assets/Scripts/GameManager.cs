using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

// 점수와 게임 오버 여부, 게임 UI를 관리하는 게임 매니저
public class GameManager : MonoBehaviourPunCallbacks, IPunObservable 
{
    public static GameManager instance
    {
        get
        {
            if (m_instance == null)
            {
                // 씬에서 GameManager 오브젝트를 찾아 할당
                m_instance = FindObjectOfType<GameManager>();
            }

            return m_instance;
        }
    }

    private static GameManager m_instance; // 싱글톤이 할당될 static 변수

    public GameObject playerPrefab; // 생성할 플레이어 캐릭터 프리팹

    private int score = 0; // 현재 게임 점수
    public bool isGameover { get; private set; } // 게임 오버 상태


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) 
    {
        if (stream.IsWriting)
        {
            stream.SendNext(score); // 네트워크를 통해 score 값을 보내기
        }
        else
        {        
            score = (int) stream.ReceiveNext(); // 네트워크를 통해 score 값 받기
            UIManager.instance.UpdateScoreText(score); // 동기화하여 받은 점수를 UI로 표시
        }
    }


    private void Awake() 
    {
        if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 게임 시작과 동시에 플레이어가 될 게임 오브젝트를 생성
    private void Start() 
    {
        Vector3 randomSpawnPos = Random.insideUnitSphere * 5f; // 생성할 랜덤 위치 지정
        randomSpawnPos.y = 0f; // 위치 y값은 0으로 변경

        PhotonNetwork.Instantiate(playerPrefab.name, randomSpawnPos, Quaternion.identity);
    }

    // 점수를 추가하고 UI 갱신
    public void AddScore(int newScore) 
    {
        // 게임 오버가 아닌 상태에서만 점수 증가 가능
        if (!isGameover)
        {
            score += newScore; // 점수 추가
            UIManager.instance.UpdateScoreText(score); // 점수 UI 텍스트 갱신
        }
    }

    // 게임 오버 처리
    public void EndGame() 
    {
        isGameover = true; // 게임 오버 상태를 참으로 변경
        UIManager.instance.SetActiveGameoverUI(true); // 게임 오버 UI를 활성화
    }

    // 키보드 입력을 감지하고 룸을 나가게 함
    private void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    // 룸을 나갈때 자동 실행되는 메서드
    public override void OnLeftRoom() 
    {
        SceneManager.LoadScene("Lobby");
    }
}