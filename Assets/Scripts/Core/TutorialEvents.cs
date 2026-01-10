using System;
using UnityEngine;

public static class TutorialEvents
{
    private const string ClosedKey = "tutorial_closed_v1";
    private const string FinishedKey = "tutorial_finished_v1";

    /// <summary>Если false — никакие Raise... ничего не делают.</summary>
    public static bool IsActive => !IsClosed && !IsFinished;

    public static bool IsClosed => PlayerPrefs.GetInt(ClosedKey, 0) == 1;
    public static bool IsFinished => PlayerPrefs.GetInt(FinishedKey, 0) == 1;
    public static void CloseTutorial()
    {
        PlayerPrefs.SetInt(ClosedKey, 1);
        PlayerPrefs.Save();
    }

    public static void FinishTutorial()
    {
        PlayerPrefs.SetInt(FinishedKey, 1);
        PlayerPrefs.Save();
    }

    // ===== events =====

    public static event Action RoadConnectedToObelisk;
    public static event Action<int> HousePlaced;
    public static event Action LumberMillPlaced;
    public static event Action BerryPlaced;
    public static event Action ResearchCompleted;

    // ===== raises (с гейтом) =====

    public static void RaiseRoadConnectedToObelisk()
    {
        if (!IsActive) return;
        RoadConnectedToObelisk?.Invoke();
    }

    public static void RaiseHousePlaced(int totalHouses)
    {
        if (!IsActive) return;
        HousePlaced?.Invoke(totalHouses);
    }

    public static void RaiseLumberMillPlaced()
    {
        if (!IsActive) return;
        LumberMillPlaced?.Invoke();
    }

    public static void RaiseBerryPlaced()
    {
        if (!IsActive) return;
        BerryPlaced?.Invoke();
    }

    public static void RaiseResearchCompleted()
    {
        if (!IsActive) return;
        ResearchCompleted?.Invoke();
    }
}