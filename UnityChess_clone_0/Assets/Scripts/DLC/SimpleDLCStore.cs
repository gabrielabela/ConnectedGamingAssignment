using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Unity.Netcode;

public class SimpleDLCStore : MonoBehaviour
{
    [Header("UI References")]
    public GameObject storePanel;
    public List<GameObject> dlcPrefabsInScene;

    [Header("External UI")]
    public Image profileImageUI;
    public TextMeshProUGUI currencyTextUI;

    [Header("Firebase")]
    public FirebaseManager firebaseManager;

    private void Start()
    {
        storePanel.SetActive(false);

        // Already inside FetchStoreData callback
        firebaseManager.FetchStoreData(() =>
        {
            PopulateProfileAndCurrency();
            PopulatePrefabs();

            // 🔄 Apply current equipped skin in SkinSyncManager
            if (int.TryParse(firebaseManager.equippedSkinId, out int parsedId))
            {
                var syncManager = FindObjectOfType<SkinSyncManager>();
                syncManager?.UpdateEquippedSkin(parsedId);
            }
        });

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            storePanel.SetActive(!storePanel.activeSelf);
        }
    }

    private void PopulateProfileAndCurrency()
    {
        currencyTextUI.text = $"Currency: {firebaseManager.playerCurrency}";

        string path = firebaseManager.GetLocalProfileImagePath();
        if (System.IO.File.Exists(path))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            profileImageUI.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            Debug.Log("Loaded profile image from local storage.");
        }
        else if (!string.IsNullOrEmpty(firebaseManager.profileImageURL))
        {
            StartCoroutine(DownloadImage(firebaseManager.profileImageURL, profileImageUI));
        }
    }

    private void PopulatePrefabs()
    {
        List<SkinData> skins = firebaseManager.allSkins;

        for (int i = 0; i < dlcPrefabsInScene.Count; i++)
        {
            if (i >= skins.Count) break;

            GameObject prefabGO = dlcPrefabsInScene[i];
            SkinData skin = skins[i];
            string skinId = i.ToString();

            Image previewImage = prefabGO.transform.Find("PreviewImage").GetComponent<Image>();
            Button purchaseButton = prefabGO.transform.Find("PurchaseButton").GetComponent<Button>();
            TextMeshProUGUI costText = purchaseButton.transform.Find("CostText").GetComponent<TextMeshProUGUI>();

            purchaseButton.onClick.RemoveAllListeners();

            if (firebaseManager.ownedSkins.Contains(skinId))
            {
                if (firebaseManager.equippedSkinId == skinId)
                {
                    costText.text = "Equipped";
                    purchaseButton.interactable = false;
                }
                else
                {
                    costText.text = "Equip";
                    purchaseButton.interactable = true;

                    purchaseButton.onClick.AddListener(() =>
                    {
                        firebaseManager.SaveEquippedSkinToFirestore(skinId);
                        firebaseManager.DownloadAndApplyProfileImage(skin.previewImageURL);
                        StartCoroutine(DownloadImage(skin.previewImageURL, profileImageUI));

                        var syncManager = FindObjectOfType<SkinSyncManager>();
                        syncManager?.UpdateEquippedSkin(int.Parse(skinId));

                        RefreshAllButtons();
                    });
                }
            }
            else
            {
                costText.text = $"${skin.price}";
                purchaseButton.interactable = true;

                purchaseButton.onClick.AddListener(() =>
                {
                    if (firebaseManager.playerCurrency >= skin.price)
                    {
                        firebaseManager.playerCurrency -= skin.price;
                        firebaseManager.SaveCurrencyToFirestore();
                        currencyTextUI.text = $"Currency: {firebaseManager.playerCurrency}";

                        // ✅ NEW: Use PurchaseSkin(), which includes analytics logging
                        firebaseManager.PurchaseSkin(skinId);


                        firebaseManager.SaveEquippedSkinToFirestore(skinId);
                        firebaseManager.DownloadAndApplyProfileImage(skin.previewImageURL);
                        StartCoroutine(DownloadImage(skin.previewImageURL, profileImageUI));

                        var syncManager = FindObjectOfType<SkinSyncManager>();
                        syncManager?.UpdateEquippedSkin(int.Parse(skinId));

                        Debug.Log($"Purchased and equipped skin {skinId}");
                        RefreshAllButtons();
                    }
                    else
                    {
                        Debug.Log("Not enough currency!");
                    }
                });
            }

            StartCoroutine(DownloadImage(skin.previewImageURL, previewImage));
        }
    }

    private void RefreshAllButtons()
    {
        PopulateProfileAndCurrency();
        PopulatePrefabs();
    }

    private IEnumerator DownloadImage(string url, Image target)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                target.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogError("Failed to download image: " + request.error);
            }
        }
    }
}
