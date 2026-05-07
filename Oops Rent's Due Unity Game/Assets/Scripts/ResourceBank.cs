using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceBank : MonoBehaviour
{
    public static ResourceBank Instance { get; private set; }

    public event Action OnResourceChanged;

    [SerializeField] List<ResourceStartAmount> startingAmounts;

    private readonly Dictionary<Resource, int> _counts = new Dictionary<Resource, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var entry in startingAmounts)
            if (entry.resource != null)
                _counts[entry.resource] = entry.amount;
    }

    public int Get(Resource resource)
    {
        return _counts.TryGetValue(resource, out int amount) ? amount : 0;
    }

    public void Add(Resource resource, int amount)
    {
        if (resource == null) return;
        if (!_counts.ContainsKey(resource)) _counts[resource] = 0;
        _counts[resource] += amount;
        OnResourceChanged?.Invoke();
    }

    public bool TrySpend(Resource resource, int amount)
    {
        if (Get(resource) < amount) return false;
        _counts[resource] -= amount;
        OnResourceChanged?.Invoke();
        return true;
    }

    public bool CanAfford(List<ResourceCost> costs)
    {
        foreach (var cost in costs)
            if (Get(cost.resource) < cost.amount) return false;
        return true;
    }

    public bool TrySpendAll(List<ResourceCost> costs)
    {
        if (!CanAfford(costs)) return false;
        foreach (var cost in costs)
            _counts[cost.resource] -= cost.amount;
        OnResourceChanged?.Invoke();
        return true;
    }
}

[Serializable]
public class ResourceCost
{
    public Resource resource;
    public int amount;
}

[Serializable]
public class ResourceStartAmount
{
    public Resource resource;
    public int amount;
}
