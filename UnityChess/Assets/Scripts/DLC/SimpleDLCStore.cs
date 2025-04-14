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
    public GameObject storePanel; // The UI panel containing the DLC store
    public List<GameObject> dlcPrefabsInScene; // A list of prefab UI elements for displaying DLCs (skins)

    [Header("External UI")]
    public Image profileImageUI; // UI image component to show the player's profile picture
    public TextMeshProUGUI currencyTextUI; // UI text component showing the player's currency

    [Header("Firebase")]
    public FirebaseManager firebaseManager; // Reference to FirebaseManager to fetch and manage player data

    private void Start()
    {
        storePanel.SetActive(false); // Start with the store hidden

        // Fetch all skin data and user info from Firebase
        firebaseManager.FetchStoreData(() =>
        {
            // Update UI with profile image and currency
            PopulateProfileAndCurrency();

            // Populate each DLC slot in the UI with data
            PopulatePrefabs();

            // If a valid equipped skin ID exists, update SkinSyncManager
            if (int.TryParse(firebaseManager.equippedSkinId, out int parsedId))
            {
                var syncManager = FindObjectOfType<SkinSyncManager>();
                syncManager?.UpdateEquippedSkin(parsedId);
            }
        });
    }

    private void Update()
    {
        // Press D to toggle the DLC store visibility
        if (Input.GetKeyDown(KeyCode.D))
        {
            storePanel.SetActive(!storePanel.activeSelf);
        }
    }

    // Loads currency and profile image from Firebase data and local storage
    private void PopulateProfileAndCurrency()
    {
        // Display player's current currency
        currencyTextUI.text = $"Currency: {firebaseManager.playerCurrency}";

        // Check for locally saved profile image
        string path = firebaseManager.GetLocalProfileImagePath();
        if (System.IO.File.Exists(path))
        {
            // Load the image from file and display it
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            profileImageUI.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            Debug.Log("Loaded profile image from local storage.");
        }
        else if (!string.IsNullOrEmpty(firebaseManager.profileImageURL))
        {
            // Download the profile image if it's not cached locally
            StartCoroutine(DownloadImage(firebaseManager.profileImageURL, profileImageUI));
        }
    }

    // Populates each DLC UI prefab with skin data, handling ownership and purchase logic
    private void PopulatePrefabs()
    {
        List<SkinData> skins = firebaseManager.allSkins;

        for (int i = 0; i < dlcPrefabsInScene.Count; i++)
        {
            if (i >= skins.Count) break; // Stop if there are more prefabs than skins

            GameObject prefabGO = dlcPrefabsInScene[i];
            SkinData skin = skins[i];
            string skinId = i.ToString();

            // Locate UI elements inside the prefab
            Image previewImage = prefabGO.transform.Find("PreviewImage").GetComponent<Image>();
            Button purchaseButton = prefabGO.transform.Find("PurchaseButton").GetComponent<Button>();
            TextMeshProUGUI costText = purchaseButton.transform.Find("CostText").GetComponent<TextMeshProUGUI>();

            purchaseButton.onClick.RemoveAllListeners(); // Clear any previous button listeners

            // If the player already owns the skin
            if (firebaseManager.ownedSkins.Contains(skinId))
            {
                if (firebaseManager.equippedSkinId == skinId)
                {
                    // Already equipped
                    costText.text = "Equipped";
                    purchaseButton.interactable = false;
                }
                else
                {
                    // Offer equip option
                    costText.text = "Equip";
                    purchaseButton.interactable = true;

                    purchaseButton.onClick.AddListener(() =>
                    {
                        firebaseManager.SaveEquippedSkinToFirestore(skinId); // Update equipped skin
                        firebaseManager.DownloadAndApplyProfileImage(skin.previewImageURL); // Save profile image
                        StartCoroutine(DownloadImage(skin.previewImageURL, profileImageUI)); // Show image in UI

                        var syncManager = FindObjectOfType<SkinSyncManager>();
                        syncManager?.UpdateEquippedSkin(int.Parse(skinId)); // Sync skin to multiplayer

                        RefreshAllButtons(); // Refresh UI after equipping
                    });
                }
            }
            else
            {
                // Not owned: show price and handle purchase
                costText.text = $"${skin.price}";
                purchaseButton.interactable = true;

                purchaseButton.onClick.AddListener(() =>
                {
                    if (firebaseManager.playerCurrency >= skin.price)
                    {
                        // Deduct cost and save updated currency
                        firebaseManager.playerCurrency -= skin.price;
                        firebaseManager.SaveCurrencyToFirestore();
                        currencyTextUI.text = $"Currency: {firebaseManager.playerCurrency}";

                        // Add skin to owned list
                        firebaseManager.PurchaseSkin(skinId);

                        // Equip and sync the newly purchased skin
                        firebaseManager.SaveEquippedSkinToFirestore(skinId);
                        firebaseManager.DownloadAndApplyProfileImage(skin.previewImageURL);
                        StartCoroutine(DownloadImage(skin.previewImageURL, profileImageUI));

                        var syncManager = FindObjectOfType<SkinSyncManager>();
                        syncManager?.UpdateEquippedSkin(int.Parse(skinId));

                        Debug.Log($"Purchased and equipped skin {skinId}");
                        RefreshAllButtons(); // Refresh UI state
                    }
                    else
                    {
                        Debug.Log("Not enough currency!");
                    }
                });
            }

            // Load the preview image into the prefab UI
            StartCoroutine(DownloadImage(skin.previewImageURL, previewImage));
        }
    }

    // Refreshes both the currency/profile UI and all skin prefab buttons
    private void RefreshAllButtons()
    {
        PopulateProfileAndCurrency();
        PopulatePrefabs();
    }

    // Downloads an image from a URL and applies it to a UI Image component
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
