using UnityEngine;
using Firebase.Firestore;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using System;

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

    private void Start()
    {
        StartUnityServices();
    }

    private async void StartUnityServices()
    {
        await UnityServices.InitializeAsync();
        if (AnalyticsService.Instance != null)
        {
            AnalyticsService.Instance.StartDataCollection();
        }
    }

    public void MatchStart(string eventType, string userId)
    {
        Debug.Log($"[AnalyticsLogger] Logged Match Event: {eventType} for UserID: {userId}");

        CustomEvent matchEvent = new("match_start")
        {
            { "type", eventType }, // "start" or "end"
            { "user_id", userId },
            { "time", DateTime.UtcNow.ToString("o") }
        };
        AnalyticsService.Instance.RecordEvent(matchEvent);

        // Firebase Logging
        var data = new Dictionary<string, object>
        {
            { "eventType", eventType },
            { "userId", userId },
            { "time", Timestamp.GetCurrentTimestamp() }
        };
        db.Collection("matchEvents").AddAsync(data);
    }

    public void MatchEnd(string eventType, string userId)
    {
        Debug.Log($"[AnalyticsLogger] Logged Match Event: {eventType} for UserID: {userId}");

        CustomEvent matchEvent = new("match_end")
        {
            { "type", eventType }, // "start" or "end"
            { "user_id", userId },
            { "time", DateTime.UtcNow.ToString("o") }
        };
        AnalyticsService.Instance.RecordEvent(matchEvent);

        // Firebase Logging
        var data = new Dictionary<string, object>
        {
            { "eventType", eventType },
            { "userId", userId },
            { "time", Timestamp.GetCurrentTimestamp() }
        };
        db.Collection("matchEvents").AddAsync(data);
    }

    public void LogDLCPurchase(string skinId, string userId)
    {
        Debug.Log($"[AnalyticsLogger] Logged DLC Purchase: SkinID: {skinId}, UserID: {userId}");

        // Unity Analytics (new format)
        CustomEvent dlcEvent = new("dlc_purchase")
        {
            { "skin_id", skinId },
            { "user_id", userId },
            { "time", DateTime.UtcNow.ToString("o") }
        };
        AnalyticsService.Instance.RecordEvent(dlcEvent);

        // Firebase Logging
        var data = new Dictionary<string, object>
        {
            { "skinId", skinId },
            { "userId", userId },
            { "time", Timestamp.GetCurrentTimestamp() }
        };
        db.Collection("dlcPurchases").AddAsync(data);
    }
}
