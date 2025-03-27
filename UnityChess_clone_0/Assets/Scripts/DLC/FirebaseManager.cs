using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Firebase.Firestore; // Make sure this is at the top of your script


public class SkinData
{
    public string skinName;
    public string previewImageURL;
    public int price;
    public string userID = "0";
}

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public FirebaseFirestore db;
    public string userID;

    public List<SkinData> allSkins = new List<SkinData>();
    public string profileImageURL;
    public int playerCurrency;
    public List<string> ownedSkins = new List<string>();
    public string equippedSkinId = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        FirebaseFirestore.DefaultInstance.Settings.PersistenceEnabled = false;
        db = FirebaseFirestore.DefaultInstance;

        // Load unique userID from ParrelSync argument (or fallback to default)
#if UNITY_EDITOR
        string[] args = System.Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            if (arg.StartsWith("--userID="))
            {
                userID = arg.Replace("--userID=", "");
                break;
            }
        }
#endif

        if (string.IsNullOrEmpty(userID))
        {
            userID = "0"; // fallback default
        }

        Debug.Log("Using Firebase userID: " + userID);
    }
    public void FetchStoreData(System.Action onComplete)
    {
        StartCoroutine(FetchAllDataCoroutine(onComplete));
    }

    private IEnumerator FetchAllDataCoroutine(System.Action onComplete)
    {
        var photoTask = db.Collection("DisplayPhoto").GetSnapshotAsync();
        var userTask = db.Collection("Users").Document(userID).GetSnapshotAsync();

        yield return new WaitUntil(() => photoTask.IsCompleted && userTask.IsCompleted);

        if (photoTask.Exception == null)
        {
            QuerySnapshot snapshot = photoTask.Result;
            allSkins.Clear();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                try
                {
                    string imageUrl = doc.GetValue<string>("imageURL");
                    int price = doc.GetValue<int>("price");

                    SkinData skin = new SkinData
                    {
                        skinName = $"Skin {doc.Id}",
                        previewImageURL = imageUrl,
                        price = price
                    };

                    allSkins.Add(skin);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing DisplayPhoto/{doc.Id}: {e.Message}");
                }
            }

            Debug.Log($"Loaded {allSkins.Count} skins from Firestore.");
        }
        else
        {
            Debug.LogError("Failed to load DisplayPhoto skins: " + photoTask.Exception.Message);
        }

        if (userTask.Exception == null && userTask.Result.Exists)
        {
            var userDoc = userTask.Result;
            try
            {
                string currencyString = userDoc.GetValue<string>("currency");
                playerCurrency = int.Parse(currencyString);
                if (userDoc.ContainsField("ownedSkins"))
                {
                    ownedSkins = userDoc.GetValue<List<string>>("ownedSkins");
                }
                else
                {
                    ownedSkins = new List<string>();
                    Debug.Log("No ownedSkins field found, initializing empty list.");
                }


                if (userDoc.ContainsField("equippedSkin"))
                {
                    equippedSkinId = userDoc.GetValue<string>("equippedSkin");
                    Debug.Log("Equipped skin: " + equippedSkinId);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing user data: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Failed to load user data from Users/" + userID);
        }

        onComplete?.Invoke();
    }

    public void SaveOwnedSkinsToFirestore()
    {
        DocumentReference userRef = db.Collection("Users").Document(userID);
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            { "ownedSkins", ownedSkins }
        };

        userRef.UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Owned skins saved to Firestore.");
            }
            else
            {
                Debug.LogError("Failed to save owned skins: " + task.Exception);
            }
        });
    }

    public void DownloadAndApplyProfileImage(string url)
    {
        StartCoroutine(DownloadAndSaveImage(url));
    }

    private IEnumerator DownloadAndSaveImage(string imageUrl)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                byte[] imageBytes = tex.EncodeToPNG();
                string path = GetLocalProfileImagePath();

                System.IO.File.WriteAllBytes(path, imageBytes);
                Debug.Log("Saved purchased image locally at: " + path);
            }
            else
            {
                Debug.LogError("Failed to download and save image: " + request.error);
            }
        }
    }

    public string GetLocalProfileImagePath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "profileImage_" + userID + ".png");
    }

    public void SaveCurrencyToFirestore()
    {
        DocumentReference userRef = db.Collection("Users").Document(userID);
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            { "currency", playerCurrency.ToString() }
        };

        userRef.UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Currency updated in Firestore.");
            }
            else
            {
                Debug.LogError("Failed to update currency: " + task.Exception);
            }
        });
    }

    public void SaveEquippedSkinToFirestore(string skinId)
    {
        equippedSkinId = skinId;

        DocumentReference userRef = db.Collection("Users").Document(userID);
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            { "equippedSkin", skinId }
        };

        userRef.UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Equipped skin updated in Firestore.");
            }
            else
            {
                Debug.LogError("Failed to update equipped skin: " + task.Exception);
            }
        });
    }

    public void PurchaseSkin(string skinId)
    {
        if (!ownedSkins.Contains(skinId))
        {
            ownedSkins.Add(skinId);
            SaveOwnedSkinsToFirestore();

            // ✅ Log DLC purchase here
            AnalyticsLogger.Instance?.LogDLCPurchase(skinId, userID);
        }
    }

}
