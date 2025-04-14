using UnityEngine;
using Firebase.Firestore;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using System;

// Singleton class for handling analytics logging throughout the game
// Combines Unity Analytics and Firebase Firestore for dual event tracking
public class AnalyticsLogger : MonoBehaviour
{
    // Singleton instance property - ensures only one instance exists
    public static AnalyticsLogger Instance { get; private set; }

    // Firebase Firestore database reference
    private FirebaseFirestore db;

    // Unity's Awake method - called when script instance is being loaded
    private void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            // First instance - set as singleton and persist across scenes
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize Firebase Firestore instance
            db = FirebaseFirestore.DefaultInstance;
        }
        else
        {
            // If another instance exists, destroy this one to maintain singleton
            Destroy(gameObject);
        }
    }

    // Unity's Start method - called just before any of the Update methods is called the first time
    private void Start()
    {
        // Initialize Unity Services when the game starts
        StartUnityServices();
    }

    // Asynchronously initializes Unity Services including Analytics
    private async void StartUnityServices()
    {
        // Initialize Unity Services
        await UnityServices.InitializeAsync();

        // If analytics service is available, start data collection
        if (AnalyticsService.Instance != null)
        {
            AnalyticsService.Instance.StartDataCollection();
        }
    }

    // Logs match start events to both Unity Analytics and Firebase
    public void MatchStart(string eventType, string userId)
    {
        // Debug log for local verification
        Debug.Log($"[AnalyticsLogger] Logged Match Event: {eventType} for UserID: {userId}");

        // Create Unity Analytics custom event
        CustomEvent matchEvent = new("match_start")
        {
            { "type", eventType },  // Type of match event
            { "user_id", userId },  // Player identifier
            { "time", DateTime.UtcNow.ToString("o") }  // ISO 8601 formatted timestamp
        };
        // Record the event with Unity Analytics
        AnalyticsService.Instance.RecordEvent(matchEvent);

        // Firebase Logging
        // Prepare data dictionary for Firestore
        var data = new Dictionary<string, object>
        {
            { "eventType", eventType },  // Type of match event
            { "userId", userId },  // Player identifier
            { "time", Timestamp.GetCurrentTimestamp() }  // Firestore timestamp
        };
        // Asynchronously add document to Firestore collection
        db.Collection("matchEvents").AddAsync(data);
    }

    // Logs match end events to both Unity Analytics and Firebase
    public void MatchEnd(string eventType, string userId)
    {
        // Debug log for local verification
        Debug.Log($"[AnalyticsLogger] Logged Match Event: {eventType} for UserID: {userId}");

        // Create Unity Analytics custom event
        CustomEvent matchEvent = new("match_end")
        {
            { "type", eventType },  // Type of match event
            { "user_id", userId },  // Player identifier
            { "time", DateTime.UtcNow.ToString("o") }  // ISO 8601 formatted timestamp
        };
        // Record the event with Unity Analytics
        AnalyticsService.Instance.RecordEvent(matchEvent);

        // Firebase Logging
        // Prepare data dictionary for Firestore
        var data = new Dictionary<string, object>
        {
            { "eventType", eventType },  // Type of match event
            { "userId", userId },  // Player identifier
            { "time", Timestamp.GetCurrentTimestamp() }  // Firestore timestamp
        };
        // Asynchronously add document to Firestore collection
        db.Collection("matchEvents").AddAsync(data);
    }

    // Logs DLC purchases to both Unity Analytics and Firebase
    public void LogDLCPurchase(string skinId, string userId)
    {
        // Debug log for local verification
        Debug.Log($"[AnalyticsLogger] Logged DLC Purchase: SkinID: {skinId}, UserID: {userId}");

        // Create Unity Analytics custom event
        CustomEvent dlcEvent = new("dlc_purchase")
        {
            { "skin_id", skinId },  // Identifier for purchased skin/content
            { "user_id", userId },  // Player identifier
            { "time", DateTime.UtcNow.ToString("o") }  // ISO 8601 formatted timestamp
        };
        // Record the event with Unity Analytics
        AnalyticsService.Instance.RecordEvent(dlcEvent);

        // Firebase Logging
        // Prepare data dictionary for Firestore
        var data = new Dictionary<string, object>
        {
            { "skinId", skinId },  // Identifier for purchased skin/content
            { "userId", userId },  // Player identifier
            { "time", Timestamp.GetCurrentTimestamp() }  // Firestore timestamp
        };
        // Asynchronously add document to Firestore collection
        db.Collection("dlcPurchases").AddAsync(data);
    }
}