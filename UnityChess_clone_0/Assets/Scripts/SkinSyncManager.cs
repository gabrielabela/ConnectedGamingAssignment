using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class SkinSyncManager : NetworkBehaviour
{
    [Header("Avatar UI")]
    public Image hostAvatarImage;
    public Image clientAvatarImage;

    private NetworkVariable<int> hostSkinId = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> clientSkinId = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private FirebaseManager firebaseManager;

    public override void OnNetworkSpawn()
    {
        firebaseManager = FindObjectOfType<FirebaseManager>();

        // Flip references on the client so each player's local avatar is correct
        if (!IsHost)
        {
            // Swap host/client image references on the client
            var temp = hostAvatarImage;
            hostAvatarImage = clientAvatarImage;
            clientAvatarImage = temp;
            Debug.Log("[SkinSyncManager] Avatar UI flipped on client.");
        }

        // Subscribe to changes
        hostSkinId.OnValueChanged += (_, newId) => LoadImageForPlayer(true, newId);
        clientSkinId.OnValueChanged += (_, newId) => LoadImageForPlayer(false, newId);

        if (IsClient && !IsHost)
        {
            // Tell host what skin this client has equipped
            if (int.TryParse(firebaseManager.equippedSkinId, out int parsedId))
                RequestInitialSkinServerRpc(parsedId);
            else
                Debug.LogError("[SkinSyncManager] Invalid equippedSkinId format on client.");
        }
        else if (IsHost)
        {
            if (int.TryParse(firebaseManager.equippedSkinId, out int parsedId))
                hostSkinId.Value = parsedId;
            else
                Debug.LogError("[SkinSyncManager] Invalid equippedSkinId format on host.");
        }

        // Force initial image load for existing values
        if (hostSkinId.Value >= 0)
            LoadImageForPlayer(true, hostSkinId.Value);

        if (clientSkinId.Value >= 0)
            LoadImageForPlayer(false, clientSkinId.Value);

    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestInitialSkinServerRpc(int skinId)
    {
        clientSkinId.Value = skinId;
    }

    public void UpdateEquippedSkin(int skinId)
    {
        if (IsHost)
        {
            hostSkinId.Value = skinId;
            Debug.Log($"[SkinSyncManager] Host updated skin to {skinId}");
        }
        else
        {
            RequestInitialSkinServerRpc(skinId);
            Debug.Log($"[SkinSyncManager] Client requested skin update to {skinId}");
        }
    }

    private void LoadImageForPlayer(bool isHostPlayer, int skinId)
    {
        if (firebaseManager == null || firebaseManager.allSkins == null || skinId < 0 || skinId >= firebaseManager.allSkins.Count)
        {
            Debug.LogWarning($"[SkinSyncManager] Skipping image load for skinId={skinId} (invalid index or firebaseManager)");
            return;
        }

        string url = firebaseManager.allSkins[skinId].previewImageURL;
        Image targetImage = GetTargetImage(isHostPlayer);

        if (targetImage != null)
        {
            StartCoroutine(DownloadImage(url, targetImage));
        }
        else
        {
            Debug.LogWarning("[SkinSyncManager] Target image is null for LoadImageForPlayer");
        }
    }

    private Image GetTargetImage(bool isHostPlayer)
    {
        // This method decides whose avatar image to update depending on context
        bool iAmHost = IsHost;

        if (iAmHost)
        {
            return isHostPlayer ? hostAvatarImage : clientAvatarImage;
        }
        else
        {
            return isHostPlayer ? hostAvatarImage : clientAvatarImage;
        }
    }

    private IEnumerator DownloadImage(string url, Image target)
    {
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            target.sprite = sprite;
            Debug.Log($"[SkinSyncManager] Image updated for avatar");
        }
        else
        {
            Debug.LogError($"[SkinSyncManager] Failed to download image: {req.error}");
        }
    }
}

