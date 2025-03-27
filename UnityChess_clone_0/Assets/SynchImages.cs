//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using Unity.Netcode;

//public class SynchImages : MonoBehaviour
//{
//    public Image hostAvatarImage;
//    public Image clientAvatarImage;

//    private IEnumerator Start()
//    {
//        yield return new WaitUntil(() => FindObjectsOfType<PlayerSkinSync>().Length == 2);

//        PlayerSkinSync[] players = FindObjectsOfType<PlayerSkinSync>();

//        foreach (var player in players)
//        {
//            if (player.OwnerClientId == NetworkManager.Singleton.LocalClientId)
//            {
//                // This is me
//                player.localPlayerImage = NetworkManager.Singleton.IsHost ? hostAvatarImage : clientAvatarImage;
//            }
//            else
//            {
//                // This is the opponent
//                player.opponentImage = NetworkManager.Singleton.IsHost ? clientAvatarImage : hostAvatarImage;
//            }
//        }

//        // Trigger skin refresh now that image refs are assigned
//        yield return new WaitForSeconds(0.2f);
//        foreach (var player in players)
//            player.OnNetworkSpawn();
//    }
//}
