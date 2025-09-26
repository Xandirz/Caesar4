using UnityEngine;
using System.Collections.Generic;

public class AllBuildingsManager : MonoBehaviour
{
    public static AllBuildingsManager Instance { get; private set; }

    private readonly List<House> houses = new();
    private readonly List<ProductionBuilding> producers = new();
    [SerializeField] private float checkInterval = 5f; // как часто проверять нужды
    private float timer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckNeedsAllProducers();
            CheckNeedsAllHouses();
        }
    }

    public void RegisterHouse(House house)
    {
        if (!houses.Contains(house))
            houses.Add(house);
    }
    public void UnregisterHouse(House house)
    {
        if (houses.Contains(house))
            houses.Remove(house);
    }
    public void RegisterProducer(ProductionBuilding pb)
    {
        if (!producers.Contains(pb))
            producers.Add(pb);
    }

    public void UnregisterProducer(ProductionBuilding pb)
    {
        if (producers.Contains(pb))
            producers.Remove(pb);
    }
    
    private void CheckNeedsAllProducers()
    {
        if (producers.Count == 0) return;

        foreach (var pb in producers)
        {
            if (pb == null) continue;

            pb.ApplyNeedsResult(pb.CheckNeeds());
        }
    }
    
    
    private void CheckNeedsAllHouses()
    {
        if (houses.Count == 0) return;

        foreach (var house in houses)
        {
            if (house == null) continue;

            house.ApplyNeedsResult(house.CheckNeeds());
        }
    }




    public int GetHouseCount()
    {
        return houses.Count;
    }
}
