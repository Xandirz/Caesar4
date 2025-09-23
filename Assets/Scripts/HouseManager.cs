using UnityEngine;
using System.Collections.Generic;

public class HouseManager : MonoBehaviour
{
    public static HouseManager Instance { get; private set; }

    private readonly List<House> houses = new();
    public float checkInterval = 1f;
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
            foreach (var house in houses)
            {
                if (house != null)
                    house.CheckUpgradeConditions();
            }
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
}