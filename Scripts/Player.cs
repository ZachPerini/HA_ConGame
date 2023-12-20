using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{

    private Scene scene;
    GameManager gameManager;
    GameObject SpriteAnchor;


    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            if (IsServer)
            {
                gameObject.name = "Player1";
                transform.position = Vector3.zero;
                UpdatePositionServerRpc();
            }
            else
            {
                gameObject.name = "Player2";
                UpdatePositionServerRpc();
            }
        }
    }


    //public override void OnNetworkSpawn()
    //{
    //    int i = 0;
    //    if (NetworkManager.Singleton.IsServer)
    //    {
    //        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
    //        {
    //            i++;
    //            SetNameClientRpc(gameObject.GetComponent<NetworkObject>(), "Player" + i);
    //        }
    //    }
    //}

    //[ClientRpc]
    //private void SetNameClientRpc(NetworkObjectReference player, string newName)
    //{
    //    Debug.Log("Spawner SetNameClientRpc: " + newName);

    //    ((GameObject)player).name = newName;
    //}

    private void Update()
    {
        scene = SceneManager.GetActiveScene();
        if (scene.name == "GameScene")
        {
            SpriteAnchor = GameObject.FindGameObjectWithTag("Anchor");
            gameObject.transform.parent = SpriteAnchor.transform;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePositionServerRpc()
    {

        transform.position = new Vector3(0,0,0.2f);

    }
}

