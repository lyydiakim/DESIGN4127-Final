using UnityEngine;
using UnityEditor;

/// <summary>
/// Shared helper for resolving asset paths in both package and Assets (Samples) layouts.
/// Use for reading assets (prefabs, textures, etc.). For writing new assets, use
/// EnsureAssetPathDirectories then write under Assets/.
/// </summary>
public static class SA_AssetPathHelper
{
    private const string PackageName = "com.gameassemblylab.gameassemblies";
    private const string SamplesRoot = "Samples";

    /// <summary>Find a prefab by relative path (e.g. Samples/Prefabs/...).</summary>
    public static GameObject FindPrefab(string relativePath)
    {
        // Try package path first (for when installed as a package)
        string packagePath = $"Packages/{PackageName}/{relativePath}";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(packagePath);
        if (prefab != null) return prefab;

        // Try Assets path with common package folder names (for development)
        string[] possibleAssetPaths = new[]
        {
            $"Assets/Game-Assemblies-Package/{relativePath}",
            $"Assets/{relativePath}"
        };
        
        foreach (string assetsPath in possibleAssetPaths)
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetsPath);
            if (prefab != null) return prefab;
        }

        // Fallback: search by filename
        string fileName = System.IO.Path.GetFileName(relativePath);
        string[] guids = AssetDatabase.FindAssets($"{System.IO.Path.GetFileNameWithoutExtension(fileName)} t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if ((path.Contains(SamplesRoot) || path.Contains(PackageName)) && path.EndsWith(fileName))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) return prefab;
            }
        }
        return null;
    }

    /// <summary>Find an asset of type T by relative path (e.g. textures, ScriptableObjects).</summary>
    public static T FindAsset<T>(string relativePath) where T : Object
    {
        // Try package path first (for when installed as a package)
        string packagePath = $"Packages/{PackageName}/{relativePath}";
        T asset = AssetDatabase.LoadAssetAtPath<T>(packagePath);
        if (asset != null) return asset;

        // Try Assets path with common package folder names (for development)
        string[] possibleAssetPaths = new[]
        {
            $"Assets/Game-Assemblies-Package/{relativePath}",
            $"Assets/{relativePath}"
        };
        
        foreach (string assetsPath in possibleAssetPaths)
        {
            asset = AssetDatabase.LoadAssetAtPath<T>(assetsPath);
            if (asset != null) return asset;
        }

        // Fallback: search by filename
        string fileName = System.IO.Path.GetFileName(relativePath);
        string filter = $"t:{typeof(T).Name}";
        if (typeof(T) == typeof(Texture2D)) filter = "t:Texture2D";
        string[] guids = AssetDatabase.FindAssets($"{System.IO.Path.GetFileNameWithoutExtension(fileName)} {filter}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if ((path.Contains(SamplesRoot) || path.Contains(PackageName)) && path.EndsWith(fileName))
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) return asset;
            }
        }
        return null;
    }

    /// <summary>Returns search folders for FindAssets: Assets/... and Packages/... .</summary>
    public static string[] GetAssetSearchFolders(string relativeFolder)
    {
        return new[]
        {
            $"Assets/{relativeFolder}",
            $"Packages/{PackageName}/{relativeFolder}"
        };
    }

    /// <summary>Ensures the chain of folders under Assets exists (e.g. Game Assemblies/Databases/Goals).</summary>
    public static void EnsureAssetPathDirectories(string relativePathUnderAssets)
    {
        string[] parts = relativePathUnderAssets.Replace('\\', '/').Trim('/').Split('/');
        if (parts.Length == 0) return;

        string parent = "Assets";
        for (int i = 0; i < parts.Length; i++)
        {
            string folder = $"{parent}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(parent, parts[i]);
            parent = folder;
        }
    }
}
