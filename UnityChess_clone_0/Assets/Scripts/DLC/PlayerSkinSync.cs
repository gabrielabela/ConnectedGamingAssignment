using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerSkinSync : NetworkBehaviour
{
    public Image avatarImage; // Assign in Inspector
    public FirebaseManager firebaseManager; // Assign in Inspector or FindObjectOfType

    private NetworkVariable<int> equippedSkinId = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // ✅ Fetch selected equipped ID
            int id;
            if (int.TryParse(firebaseManager.equippedSkinId, out id))
            {
                equippedSkinId.Value = id;
            }
            else
            {
                Debug.LogWarning("equippedSkinId not valid, defaulting to 0.");
                equippedSkinId.Value = 0;
            }
        }

        // Listen for changes
        equippedSkinId.OnValueChanged += OnSkinChanged;

        // Apply immediately
        OnSkinChanged(-1, equippedSkinId.Value);
    }

    private void OnSkinChanged(int oldId, int newId)
    {
        if (newId >= 0 && newId < firebaseManager.allSkins.Count)
        {
            string url = firebaseManager.allSkins[newId].previewImageURL;
            StartCoroutine(LoadImage(url));
        }
        else
        {
            Debug.LogError("Invalid skin ID: " + newId);
        }
    }

    private IEnumerator LoadImage(string url)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                avatarImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogError("Failed to load synced skin: " + req.error);
            }
        }
    }
}
