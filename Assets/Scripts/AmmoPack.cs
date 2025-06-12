using Photon.Pun;
using UnityEngine;

// 총알을 충전하는 아이템
public class AmmoPack : MonoBehaviourPun, IItem {
    public int ammo = 30; // 충전할 총알 수

    public void Use(GameObject target) 
    {
        PlayerShooter playerShooter = target.GetComponent<PlayerShooter>();

        if (playerShooter != null && playerShooter.gun != null)
        {
            playerShooter.gun.photonView.RPC("AddAmmo", RpcTarget.All, ammo);
        }

        PhotonNetwork.Destroy(gameObject);
    }
}