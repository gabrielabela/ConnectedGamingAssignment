using UnityEngine; 
using Firebase.Firestore; 
using Firebase.Extensions; 
using System;
using System.Collections; 
using System.Collections.Generic; 
using UnityEngine.Networking;

// Simple data container for each skin item
public class SkinData
{
    public string skinName; // Name of the skin
    public string previewImageURL; // URL pointing to the preview image of this skin
    public int price; // Integer cost of the skin in currency
    public string userID = "0"; 
}

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; } // Singleton reference

    public FirebaseFirestore db; // Firestore database reference
    public string userID; // Currently logged in user's ID

    public List<SkinData> allSkins = new List<SkinData>(); // List of all available store skins
    public string profileImageURL; // Profile image URL for the user
    public int playerCurrency; // Local cached currency value
    public List<string> ownedSkins = new List<string>(); // List of skins this user owns
    public string equippedSkinId = null; // Currently equipped skin ID

    private void Awake()
    {
        // Ensure singleton behavior
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicates
            return;
        }

        Instance = this; // Assign singleton instance
        DontDestroyOnLoad(gameObject); // Persist across scene loads

        // Disable Firestore local caching to force real-time reads
        FirebaseFirestore.DefaultInstance.Settings.PersistenceEnabled = false;
        db = FirebaseFirestore.DefaultInstance; // Set the Firestore instance

        // Check if userID was passed via command line in ParrelSync (only in editor)
#if UNITY_EDITOR
        string[] args = System.Environment.GetCommandLineArgs(); // Get arguments passed to Unity Editor
        foreach (string arg in args)
        {
            if (arg.StartsWith("--userID=")) // Look for "--userID=" argument
            {
                userID = arg.Replace("--userID=", ""); // Extract the actual ID value
                break; // Stop once found
            }
        }
#endif

        // If no userID was provided, fallback to default "0"
        if (string.IsNullOrEmpty(userID))
        {
            userID = "0";
        }

        Debug.Log("Using Firebase userID: " + userID); // Log current userID
    }

    // Public method to start fetching store and user data from Firebase
    public void FetchStoreData(System.Action onComplete)
    {
        StartCoroutine(FetchAllDataCoroutine(onComplete)); // Start coroutine and pass callback
    }

    // Coroutine to handle loading both skins and user profile data
    private IEnumerator FetchAllDataCoroutine(System.Action onComplete)
    {
        // Start Firestore queries to fetch skins and user document
        var photoTask = db.Collection("DisplayPhoto").GetSnapshotAsync(); // Get all skin documents
        var userTask = db.Collection("Users").Document(userID).GetSnapshotAsync(); // Get user document

        // Wait until both tasks are finished
        yield return new WaitUntil(() => photoTask.IsCompleted && userTask.IsCompleted);

        // Check if skin data loaded successfully
        if (photoTask.Exception == null)
        {
            QuerySnapshot snapshot = photoTask.Result; // Get snapshot result
            allSkins.Clear(); // Clear previous data

            // Loop through all skin documents
            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                try
                {
                    // Read image URL and price from Firestore
                    string imageUrl = doc.GetValue<string>("imageURL");
                    int price = doc.GetValue<int>("price");

                    // Create new skin object with parsed data
                    SkinData skin = new SkinData
                    {
                        skinName = $"Skin {doc.Id}", // Assign skin name using document ID
                        previewImageURL = imageUrl,
                        price = price
                    };

                    allSkins.Add(skin); // Add to local list
                }
                catch (System.Exception e)
                {
                    // Log error if something went wrong reading this document
                    Debug.LogError($"Error parsing DisplayPhoto/{doc.Id}: {e.Message}");
                }
            }

            Debug.Log($"Loaded {allSkins.Count} skins from Firestore."); // Log how many were loaded
        }
        else
        {
            // Log if skin query failed
            Debug.LogError("Failed to load DisplayPhoto skins: " + photoTask.Exception.Message);
        }

        // Check if user data was successfully loaded and exists
        if (userTask.Exception == null && userTask.Result.Exists)
        {
            var userDoc = userTask.Result;
            try
            {
                // Parse currency from user document (stored as string)
                string currencyString = userDoc.GetValue<string>("currency");
                playerCurrency = int.Parse(currencyString); // Convert to int

                // If user has a list of owned skins, load it
                if (userDoc.ContainsField("ownedSkins"))
                {
                    ownedSkins = userDoc.GetValue<List<string>>("ownedSkins");
                }
                else
                {
                    // No skins yet — initialize empty
                    ownedSkins = new List<string>();
                    Debug.Log("No ownedSkins field found, initializing empty list.");
                }

                // If user has an equipped skin, load it
                if (userDoc.ContainsField("equippedSkin"))
                {
                    equippedSkinId = userDoc.GetValue<string>("equippedSkin");
                    Debug.Log("Equipped skin: " + equippedSkinId);
                }
            }
            catch (System.Exception e)
            {
                // Log any issues parsing the document
                Debug.LogError("Error parsing user data: " + e.Message);
            }
        }
        else
        {
            // Failed to load the user document
            Debug.LogError("Failed to load user data from Users/" + userID);
        }

        // Invoke the onComplete callback
        onComplete?.Invoke();
    }

    // Saves the list of skins the player owns to Firestore
    public void SaveOwnedSkinsToFirestore()
    {
        DocumentReference userRef = db.Collection("Users").Document(userID); // Reference user document

        // Build update payload with owned skins list
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            { "ownedSkins", ownedSkins }
        };

        // Send update to Firestore
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

    // Starts the download of a profile image from a URL and saves it locally
    public void DownloadAndApplyProfileImage(string url)
    {
        StartCoroutine(DownloadAndSaveImage(url)); // Start coroutine
    }

    // Coroutine to download an image and write it to local disk
    private IEnumerator DownloadAndSaveImage(string imageUrl)
    {
        // Make a request for the image texture
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest(); // Wait for download

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Convert to texture
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                byte[] imageBytes = tex.EncodeToPNG(); // Convert texture to PNG
                string path = GetLocalProfileImagePath(); // Build local path

                System.IO.File.WriteAllBytes(path, imageBytes); // Save image to disk
                Debug.Log("Saved purchased image locally at: " + path);
            }
            else
            {
                Debug.LogError("Failed to download and save image: " + request.error);
            }
        }
    }

    // Returns the full path to the user's profile image file on local storage
    public string GetLocalProfileImagePath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "profileImage_" + userID + ".png");
    }

    // Saves the user's current currency value to Firestore
    public void SaveCurrencyToFirestore()
    {
        DocumentReference userRef = db.Collection("Users").Document(userID);

        // Convert currency to string and prepare update dictionary
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            { "currency", playerCurrency.ToString() }
        };

        // Update Firestore document
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

    // Sets the equipped skin locally and saves it to Firestore
    public void SaveEquippedSkinToFirestore(string skinId)
    {
        equippedSkinId = skinId; // Update local reference

        DocumentReference userRef = db.Collection("Users").Document(userID);

        // Payload with new equipped skin ID
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            { "equippedSkin", skinId }
        };

        // Send update to Firestore
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

    // Adds a skin to the owned list and logs it as a DLC purchase
    public void PurchaseSkin(string skinId)
    {
        if (!ownedSkins.Contains(skinId)) // Only add if not already owned
        {
            ownedSkins.Add(skinId);
            SaveOwnedSkinsToFirestore(); // Save to Firestore

            // Log the purchase using analytics (if available)
            AnalyticsLogger.Instance?.LogDLCPurchase(skinId, userID);
        }
    }

    // Saves the current chess game state (FEN string) to Firestore
    public void SaveCurrentGameState(string fen)
    {
        DocumentReference userRef = db.Collection("Users").Document(userID);

        // Build update dictionary with FEN string
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            { "lastSavedGame", fen }
        };

        // Send update to Firestore
        userRef.UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("[FirebaseManager] Saved game state to Firestore.");
            }
            else
            {
                Debug.LogError("[FirebaseManager] Failed to save game state: " + task.Exception);
            }
        });
    }

    // Loads the saved game state from Firestore and sends it via callback
    public void LoadSavedGameState(Action<string> onComplete)
    {
        DocumentReference userRef = db.Collection("Users").Document(userID);

        // Fetch the document and extract the FEN string
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                string fen = task.Result.ContainsField("lastSavedGame")
                    ? task.Result.GetValue<string>("lastSavedGame")
                    : null;

                onComplete?.Invoke(fen); // Pass to callback
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] No saved game state found.");
                onComplete?.Invoke(null); // No game state
            }
        });
    }
}
