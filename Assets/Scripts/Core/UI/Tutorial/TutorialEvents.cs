using System;

public static class TutorialEvents
{
    // ===== events =====
    public static event Action RoadConnectedToObelisk;
    public static event Action<int> HousePlaced;      // total houses
    public static event Action LumberMillPlaced;
    public static event Action BerryPlaced;
    public static event Action ResearchCompleted;

    // ===== raises =====
    public static void RaiseRoadConnectedToObelisk() => RoadConnectedToObelisk?.Invoke();
    public static void RaiseHousePlaced(int totalHouses) => HousePlaced?.Invoke(totalHouses);
    public static void RaiseLumberMillPlaced() => LumberMillPlaced?.Invoke();
    public static void RaiseBerryPlaced() => BerryPlaced?.Invoke();
    public static void RaiseResearchCompleted() => ResearchCompleted?.Invoke();
}