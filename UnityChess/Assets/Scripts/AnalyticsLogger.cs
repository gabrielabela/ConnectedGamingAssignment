using UnityEngine;
using UnityEngine.Analytics;
using Firebase.Firestore;
using System.Collections.Generic;

public class AnalyticsLogger : MonoBehaviour
{
    public static AnalyticsLogger Instance { get; private set; }
    private FirebaseFirestore db;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            db = FirebaseFirestore.DefaultInstance;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LogMatchEvent(string eventType, string userId)
    {
        Debug.Log($"[AnalyticsLogger] Logged Match Event: {eventType} for UserID: {userId}");

        // Unity Analytics
        Analytics.CustomEvent("match_event", new Dictionary<string, object>
        {
            { "type", eventType }, // "start" or "end"
            { "user_id", userId },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        });

        // Firebase Logging
        var data = new Dictionary<string, object>
        {
            { "eventType", eventType },
            { "userId", userId },
            { "timestamp", Timestamp.GetCurrentTimestamp() }
        };

        db.Collection("matchEvents").AddAsync(data);

    }

    public void LogDLCPurchase(string skinId, string userId)
    {
        Debug.Log($"[AnalyticsLogger] Logged DLC Purchase: SkinID: {skinId}, UserID: {userId}");

        // Unity Analytics
        Analytics.CustomEvent("dlc_purchase", new Dictionary<string, object>
        {
            { "skin_id", skinId },
            { "user_id", userId },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        });

        // Firebase Logging
        var data = new Dictionary<string, object>
        {
            { "skinId", skinId },
            { "userId", userId },
            { "timestamp", Timestamp.GetCurrentTimestamp() }
        };

        db.Collection("dlcPurchases").AddAsync(data);

    }
}
