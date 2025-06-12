using Photon.Pun;
using UnityEngine;

// 게임 점수를 증가시키는 아이템
public class Coin : MonoBehaviourPun, IItem 
{
    public int score = 200; // 증가할 점수

    public void Use(GameObject target) 
    {
        GameManager.instance.AddScore(score);

        PhotonNetwork.Destroy(gameObject);
    }
}