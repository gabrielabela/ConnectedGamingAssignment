//using System.Globalization;
//using System;
//using Unity.Netcode;
//using UnityChess;
//using UnityEngine;
//using System.Collections;

//public class PlayerState : NetworkBehaviour
//{
//    public NetworkVariable<Side> AssignedSide = new(
//        Side.White,
//        NetworkVariableReadPermission.Everyone,
//        NetworkVariableWritePermission.Server
//    );

//    public override void OnNetworkSpawn()
//    {
//        if (IsServer)
//        {
//            Side side = PlayerManager.Instance.GetSideForClient(OwnerClientId);
//            AssignedSide.Value = side;
//        }
//        if (IsOwner)
//        {
//            StartCoroutine(WaitForLocalPlayerManagerAndSetSide());
//        }

//    }

//    private IEnumerator WaitForLocalPlayerManagerAndSetSide()
//    {
//        while (LocalPlayerManager.Instance == null)
//        {
//            yield return null; // wait one frame
//        }
//        LocalPlayerManager.Instance.MySide = AssignedSide.Value;
//        Debug.Log($"Side assigned to LocalPlayerManager: {AssignedSide.Value}");
//    }




//}
