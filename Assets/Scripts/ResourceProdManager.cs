using System.Collections.Generic;
using UnityEngine;

public class ResourceProdManager : MonoBehaviour
{
    public static ResourceProdManager Instance { get; private set; }

    private readonly List<ProductionBuilding> producers = new();
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

            foreach (var prod in producers)
            {
                if (prod != null)
                    prod.CheckProductionReq();
            }
        }
    }

    public void RegisterProducer(ProductionBuilding prod)
    {
        if (!producers.Contains(prod))
            producers.Add(prod);
    }

    public void UnregisterProducer(ProductionBuilding prod)
    {
        if (producers.Contains(prod))
            producers.Remove(prod);
    }
}