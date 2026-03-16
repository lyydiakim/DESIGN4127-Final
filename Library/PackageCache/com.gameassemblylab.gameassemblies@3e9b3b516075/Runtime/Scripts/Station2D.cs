using UnityEngine;

/// <summary>
/// Station variant that uses StationDataSO as its configuration source. Assign
/// stationData (inherited from Station) in the inspector; ApplyToStation runs at
/// Start to perform one-time setup (input/output areas, sprite).
/// </summary>
public class Station2D : Station
{
    private void Start()
    {
        if (stationData != null)
        {
            stationData.ApplyToStation(this);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Station2D has no StationDataSO assigned. Using default Station behavior.");
        }

        base.Start();
    }
}
