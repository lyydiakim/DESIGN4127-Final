using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "Game Assemblies/Resource")]
public class Resource : ScriptableObject
{
    public string resourceName;
    public Sprite icon;
    [Tooltip("Color tint applied to the icon sprite in UI. Use white for no tint.")]
    public Color iconTint = Color.white;
    public GameObject resourcePrefab; // Reference to the prefab representing the resource

    public enum ResourceBehavior
    {
        Static,
        Decays,
        Consumable
    }
    [Tooltip("Static: never decays. Decays: destroys after lifespan. Consumable: used when consumed.")]
    public ResourceBehavior typeOfBehavior = ResourceBehavior.Static;
    [Tooltip("How long (seconds) before this resource decays and is destroyed. Only used when typeOfBehavior is Decays.")]
    public float lifespan = 2f;
}

