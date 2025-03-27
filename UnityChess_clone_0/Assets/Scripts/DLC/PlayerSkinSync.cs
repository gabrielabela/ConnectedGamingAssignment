//using System.Collections;
//using Unity.Netcode;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Networking;

//public class PlayerSkinSync : NetworkBehaviour
//{
//    public FirebaseManager firebaseManager;
//    public Image localPlayerImage;
//    public Image opponentImage;

//    private NetworkVariable<int> equippedSkinId = new NetworkVariable<int>(
//        -1,
//        NetworkVariableReadPermission.Everyone,
//        NetworkVariableWritePermission.Owner
//    );

//    public override void OnNetworkSpawn()
//    {
//        if (firebaseManager == null)
//            firebaseManager = FindObjectOfType<FirebaseManager>();

//        equippedSkinId.OnValueChanged += OnSkinChanged;

//        if (IsOwner)
//        {
//            // Local player loads their own skin
//            if (int.TryParse(firebaseManager.equippedSkinId, out int id))
//                equippedSkinId.Value = id;
//        }

//        // Apply once at spawn
//        OnSkinChanged(-1, equippedSkinId.Value);
//    }

//    private void OnSkinChanged(int oldVal, int newVal)
//    {
//        if (firebaseManager == null || firebaseManager.allSkins.Count == 0)
//            return;

//        if (newVal < 0 || newVal >= firebaseManager.allSkins.Count)
//            return;

//        string url = firebaseManager.allSkins[newVal].previewImageURL;

//        if (OwnerClientId == NetworkManager.Singleton.LocalClientId)
//        {
//            // I'm looking at myself
//            if (localPlayerImage != null)
//                StartCoroutine(LoadImage(url, localPlayerImage));
//        }
//        else
//        {
//            // I'm looking at the other player
//            if (opponentImage != null)
//                StartCoroutine(LoadImage(url, opponentImage));
//        }
//    }

//    private IEnumerator LoadImage(string url, Image target)
//    {
//        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
//        {
//            yield return req.SendWebRequest();

//            if (req.result == UnityWebRequest.Result.Success)
//            {
//                Texture2D tex = DownloadHandlerTexture.GetContent(req);
//                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
//                target.sprite = sprite;
//            }
//        }
//    }

//    public void SetEquippedSkinId(int id)
//    {
//        if (IsOwner)
//        {
//            equippedSkinId.Value = id;
//        }
//    }

//    public void ForceSkinResync(int id)
//    {
//        if (IsOwner)
//        {
//            equippedSkinId.Value = id; // Triggers OnValueChanged
//        }
//    }

//    public void ApplyOpponentSkin()
//    {
//        if (!IsOwner)
//        {
//            OnSkinChanged(-1, equippedSkinId.Value);
//        }
//    }

//}
