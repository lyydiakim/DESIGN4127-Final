using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ResampleColorsWindow : EditorWindow
{
    private Texture2D originalImage;
    private Texture2D previewImage;
    private float displaySize = 300f;
    private float minDisplaySize = 200f;
    private float maxDisplaySize = 600f;
    private Vector2 scrollPosition;
    private string savePath = "Assets/Game Assemblies/2d Assets/Asset Tools Outputs/";
    private string fileName = "ResampledImage";
    private bool needsUpdate = false;

    // Color palette settings
    private int colorSampleCount = 8;
    private int minColorSampleCount = 2;
    private int maxColorSampleCount = 32;
    private List<Color> paletteColors = new List<Color>();

    // Brightness adjustment
    private float brightnessOffset = 0f;
    private float minBrightnessOffset = -0.5f;
    private float maxBrightnessOffset = 0.5f;

    // Color Palette Scriptable Object references
    private ColorPaletteSO currentPalette;
    private string newPaletteName = "New Palette";

    // Custom gradient texture
    private Texture2D gradientTexture;

    [MenuItem("Game Assemblies/Asset Tools/Resample Colors")]
    public static void ShowWindow()
    {
        GetWindow<ResampleColorsWindow>("Resample Colors");
    }

    private void OnEnable()
    {
        // Default path
        savePath = "Assets/ResampledColors/";

        // Create gradient texture
        UpdateGradientTexture();

        // Initial color sampling
        UpdateSampledColors();
    }

    private void OnDestroy()
    {
        // Clean up textures when window is closed
        if (gradientTexture != null)
        {
            DestroyImmediate(gradientTexture);
        }
    }

    private void UpdateGradientTexture()
    {
        // Create or recreate the gradient texture
        if (gradientTexture != null)
        {
            DestroyImmediate(gradientTexture);
        }

        int textureWidth = 256;
        gradientTexture = new Texture2D(textureWidth, 16, TextureFormat.RGBA32, false);
        gradientTexture.wrapMode = TextureWrapMode.Clamp;

        // Get colors from palette if available, otherwise use default colors
        Color[] colors = GetCurrentGradientColors();

        // Fill the texture with gradient colors
        for (int x = 0; x < textureWidth; x++)
        {
            float t = x / (float)(textureWidth - 1);
            Color gradientColor = EvaluateGradient(t, colors);

            for (int y = 0; y < gradientTexture.height; y++)
            {
                gradientTexture.SetPixel(x, y, gradientColor);
            }
        }

        gradientTexture.Apply();
    }

    private Color[] GetCurrentGradientColors()
    {
        // If we have a current palette, use its colors
        if (currentPalette != null)
        {
            return currentPalette.Colors;
        }

        // Otherwise return default colors
        return new Color[]
        {
            new Color(0.08f, 0.08f, 0.16f, 1f),  // Dark blue-black (C64 dark blue)
            new Color(0.33f, 0.20f, 0.53f, 1f),  // Purple (NES purple)
            new Color(0.94f, 0.20f, 0.20f, 1f),  // Red (Game Boy Color red)
            new Color(1.00f, 0.73f, 0.20f, 1f),  // Orange-yellow (PICO-8 yellow)
            new Color(0.60f, 1.00f, 0.47f, 1f)   // Light green (Apple II green)
        };
    }

    private Color EvaluateGradient(float t, Color[] colors)
    {
        if (colors.Length == 0)
            return Color.black;

        if (colors.Length == 1)
            return colors[0];

        // Find the two colors to interpolate between
        float segmentLength = 1f / (colors.Length - 1);
        int index = Mathf.Clamp(Mathf.FloorToInt(t / segmentLength), 0, colors.Length - 2);
        float localT = (t - index * segmentLength) / segmentLength;

        return Color.Lerp(colors[index], colors[index + 1], localT);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Resample Colors to Gradient", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Image Display Size Slider
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Display Settings", EditorStyles.boldLabel);
        displaySize = EditorGUILayout.Slider("Preview Size", displaySize, minDisplaySize, maxDisplaySize);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Image Loading Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("1. Load Original Image", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        originalImage = (Texture2D)EditorGUILayout.ObjectField("Original Image", originalImage, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (originalImage != null)
            {
                // Reset the preview when a new image is loaded
                previewImage = null;
                fileName = originalImage.name + "_resampled";
                needsUpdate = true;
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Color Palette Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("2. Color Palette", EditorStyles.boldLabel);

        // Palette Selection
        EditorGUI.BeginChangeCheck();
        currentPalette = (ColorPaletteSO)EditorGUILayout.ObjectField("Current Palette", currentPalette, typeof(ColorPaletteSO), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (currentPalette != null)
            {
                // Update the gradient texture when a new palette is selected
                UpdateGradientTexture();
                UpdateSampledColors();
                needsUpdate = true;
            }
        }

        // Display gradient texture
        if (gradientTexture != null)
        {
            Rect gradientRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40, 20);
            GUI.DrawTexture(gradientRect, gradientTexture, ScaleMode.StretchToFill);
        }

        GUILayout.Space(5);

        // Color editing section
        GUILayout.Label("Edit Palette Colors:");

        // Get the colors we're working with
        Color[] gradientColors = GetCurrentGradientColors();
        bool colorsChanged = false;

        // Create field for each gradient color
        EditorGUI.BeginChangeCheck();

        for (int i = 0; i < 5; i++)
        {
            EditorGUILayout.BeginHorizontal();
            Color newColor = EditorGUILayout.ColorField($"Color {i + 1}", gradientColors[i]);

            // Check if this color changed
            if (newColor != gradientColors[i])
            {
                gradientColors[i] = newColor;
                colorsChanged = true;

                // Update the palette if we have one
                if (currentPalette != null)
                {
                    currentPalette.SetColor(i, newColor);
                    EditorUtility.SetDirty(currentPalette);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        if (EditorGUI.EndChangeCheck() && colorsChanged)
        {
            UpdateGradientTexture();
            UpdateSampledColors();
            needsUpdate = true;
        }

        // Sample Count slider
        EditorGUI.BeginChangeCheck();
        colorSampleCount = EditorGUILayout.IntSlider("Color Sample Count", colorSampleCount, minColorSampleCount, maxColorSampleCount);
        bool sampleCountChanged = EditorGUI.EndChangeCheck();

        // Brightness offset slider
        EditorGUI.BeginChangeCheck();
        brightnessOffset = EditorGUILayout.Slider("Brightness Offset", brightnessOffset, minBrightnessOffset, maxBrightnessOffset);
        bool brightnessChanged = EditorGUI.EndChangeCheck();

        if (sampleCountChanged)
        {
            UpdateSampledColors();
            needsUpdate = true;
        } else if (brightnessChanged)
        {
            needsUpdate = true;
        }

        // Display sampled colors
        if (paletteColors.Count > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label("Sampled Colors:");

            EditorGUILayout.BeginHorizontal();
            float swatchWidth = Mathf.Min(50, (EditorGUIUtility.currentViewWidth - 40) / paletteColors.Count);

            for (int i = 0; i < paletteColors.Count; i++)
            {
                EditorGUI.DrawRect(
                    GUILayoutUtility.GetRect(swatchWidth, 20),
                    paletteColors[i]
                );
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        // Save/Load Palette Section
        GUILayout.Label("Save/Load Palette:", EditorStyles.boldLabel);

        // New palette name field
        newPaletteName = EditorGUILayout.TextField("New Palette Name", newPaletteName);

        EditorGUILayout.BeginHorizontal();

        // Save palette button
        if (GUILayout.Button("Save As New Palette"))
        {
            SaveNewPalette();
        }

        // Create palette from current image
        GUI.enabled = originalImage != null;
        if (GUILayout.Button("Create Palette From Image"))
        {
            CreatePaletteFromImage();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Resampling Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("3. Resample Image Colors", EditorStyles.boldLabel);

        if (originalImage != null)
        {
            if (GUILayout.Button("Update Preview", GUILayout.Height(30)))
            {
                ResampleColors();
                needsUpdate = false;
            }

            if (needsUpdate)
            {
                EditorGUILayout.HelpBox("Click 'Update Preview' to see changes", MessageType.Info);
            }

            GUILayout.Space(10);

            // Side-by-side preview of original and resampled images
            if (originalImage != null)
            {
                // Calculate the width for each preview
                float previewWidth = (EditorGUIUtility.currentViewWidth - 60) / 2; // Account for margins and space between
                float previewHeight = previewWidth * (originalImage.height / (float)originalImage.width);

                // Limit height if needed
                previewHeight = Mathf.Min(previewHeight, displaySize);

                EditorGUILayout.BeginHorizontal();

                // Original image
                EditorGUILayout.BeginVertical(GUILayout.Width(previewWidth));
                GUILayout.Label("Original Image:");
                Rect originalRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                GUI.DrawTexture(originalRect, originalImage, ScaleMode.ScaleToFit);
                GUILayout.Label($"{originalImage.width} x {originalImage.height}");
                EditorGUILayout.EndVertical();

                // Small space between
                GUILayout.Space(10);

                // Resampled image
                EditorGUILayout.BeginVertical(GUILayout.Width(previewWidth));
                GUILayout.Label("Resampled Image:");
                Rect resampledRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                if (previewImage != null)
                {
                    GUI.DrawTexture(resampledRect, previewImage, ScaleMode.ScaleToFit);
                    GUILayout.Label($"{previewImage.width} x {previewImage.height}");
                } else
                {
                    EditorGUI.DrawRect(resampledRect, new Color(0.2f, 0.2f, 0.2f, 1f)); // Gray rectangle if no preview
                    GUILayout.Label("Preview not generated yet");
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
        } else
        {
            GUILayout.Label("Load an image to start resampling", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Save Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("4. Save Resampled Image", EditorStyles.boldLabel);

        if (originalImage != null && previewImage != null)
        {
            savePath = EditorGUILayout.TextField("Save Directory", savePath);
            fileName = EditorGUILayout.TextField("File Name", fileName);

            GUILayout.Space(10);

            if (GUILayout.Button("Save Resampled Image", GUILayout.Height(30)))
            {
                SaveResampledImage();
            }
        } else
        {
            GUILayout.Label("Complete steps above to enable saving", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    private void UpdateSampledColors()
    {
        paletteColors.Clear();
        Color[] colors = GetCurrentGradientColors();

        // Sample colors evenly across the gradient
        for (int i = 0; i < colorSampleCount; i++)
        {
            float t = i / (float)(colorSampleCount - 1);
            paletteColors.Add(EvaluateGradient(t, colors));
        }
    }

    private void ResampleColors()
    {
        if (originalImage == null) return;

        // Make sure we can read the original texture
        string path = AssetDatabase.GetAssetPath(originalImage);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

        if (importer != null)
        {
            bool isReadable = importer.isReadable;
            if (!isReadable)
            {
                // Temporarily make the texture readable
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            // Create a new texture for the resampled version
            int width = originalImage.width;
            int height = originalImage.height;

            if (previewImage == null || previewImage.width != width || previewImage.height != height)
            {
                previewImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
                previewImage.filterMode = FilterMode.Point; // Use nearest neighbor filtering
            }

            // Make sure our sampled colors are up to date
            if (paletteColors.Count != colorSampleCount)
            {
                UpdateSampledColors();
            }

            // Resample the colors
            Color[] pixels = originalImage.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Get the brightness of the original pixel
                float brightness = GetBrightness(pixels[i]);

                // Map brightness to a color from our gradient samples
                newPixels[i] = MapBrightnessToSampledColor(brightness);

                // Preserve the original alpha
                newPixels[i].a = pixels[i].a;
            }

            // Apply the new pixels
            previewImage.SetPixels(newPixels);
            previewImage.Apply();

            // Restore the original import settings if we changed them
            if (!isReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        } else
        {
            Debug.LogError("Could not access texture importer for the original image");
        }
    }

    private float GetBrightness(Color color)
    {
        // Calculate perceived brightness (using common luminance formula)
        float brightness = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;

        // Apply brightness offset (clamping to keep in 0-1 range)
        brightness = Mathf.Clamp01(brightness + brightnessOffset);

        return brightness;
    }

    private Color MapBrightnessToSampledColor(float brightness)
    {
        // Map brightness [0-1] to sampled color index
        float index = brightness * (paletteColors.Count - 1);
        int lowerIndex = Mathf.FloorToInt(index);

        // Clamp to valid indices
        lowerIndex = Mathf.Clamp(lowerIndex, 0, paletteColors.Count - 1);

        // Return the discrete color sample
        return paletteColors[lowerIndex];
    }

    private void SaveResampledImage()
    {
        if (previewImage == null) return;

        // Ensure the directory exists
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // Create full path
        string fullPath = Path.Combine(savePath, fileName + ".png");

        // Convert to PNG bytes
        byte[] bytes = previewImage.EncodeToPNG();

        // Save the file
        File.WriteAllBytes(fullPath, bytes);

        // Refresh AssetDatabase to show the new file
        AssetDatabase.Refresh();

        // Get the saved asset and update its import settings
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
        if (importer != null)
        {
            // Critical settings for quality
            importer.filterMode = FilterMode.Point; // Set nearest neighbor filtering
            importer.textureCompression = TextureImporterCompression.Uncompressed; // No compression
            importer.alphaIsTransparency = true; // Important for transparent images
            importer.mipmapEnabled = false; // Disable mipmaps to prevent filtering artifacts
            importer.npotScale = TextureImporterNPOTScale.None; // Don't resize non-power-of-two textures

            // Disable any filtering or blending options
            SerializedObject serializedImporter = new SerializedObject(importer);
            SerializedProperty textureSettings = serializedImporter.FindProperty("m_TextureSettings");
            if (textureSettings != null)
            {
                // 0 = Nearest Neighbor
                SerializedProperty filterMode = textureSettings.FindPropertyRelative("m_FilterMode");
                if (filterMode != null)
                {
                    filterMode.intValue = 0;
                }
                serializedImporter.ApplyModifiedProperties();
            }

            // Apply all changes to the import settings
            importer.SaveAndReimport();
        }

        Debug.Log("Resampled image saved to: " + fullPath);

        // Ping the saved asset in the Project view
        Object savedAsset = AssetDatabase.LoadAssetAtPath(fullPath, typeof(Texture2D));
        if (savedAsset != null)
        {
            EditorGUIUtility.PingObject(savedAsset);
        }
    }

    private void SaveNewPalette()
    {
        // Ensure we have a valid name
        if (string.IsNullOrEmpty(newPaletteName))
        {
            newPaletteName = "New Palette";
        }

        // Create folder if it doesn't exist
        string palettePath = "Assets/Game Assemblies/Color Palettes";
        if (!Directory.Exists(palettePath))
        {
            Directory.CreateDirectory(palettePath);
        }

        // Create new palette asset
        ColorPaletteSO newPalette = ScriptableObject.CreateInstance<ColorPaletteSO>();
        newPalette.paletteName = newPaletteName;

        // Set colors from current gradient
        Color[] colors = GetCurrentGradientColors();
        newPalette.Colors = colors;

        // Generate a unique filename
        string assetPath = $"{palettePath}/{newPaletteName}.asset";

        // Make sure the path is unique
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        // Save the asset
        AssetDatabase.CreateAsset(newPalette, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the new palette
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newPalette;
        currentPalette = newPalette;

        Debug.Log($"Saved new color palette: {assetPath}");
    }

    private void CreatePaletteFromImage()
    {
        if (originalImage == null) return;

        // Make sure we can read the original texture
        string path = AssetDatabase.GetAssetPath(originalImage);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

        if (importer != null)
        {
            bool isReadable = importer.isReadable;
            if (!isReadable)
            {
                // Temporarily make the texture readable
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            // Create a new palette
            ColorPaletteSO newPalette = ScriptableObject.CreateInstance<ColorPaletteSO>();
            newPalette.paletteName = originalImage.name + " Palette";

            // Using K-means clustering to extract 5 representative colors
            Color[] dominantColors = ExtractDominantColors(originalImage, 5);
            newPalette.Colors = dominantColors;

            // Restore the original import settings if we changed them
            if (!isReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            // Create folder if it doesn't exist
            string palettePath = "Assets/Game Assemblies/Color Palettes";
            if (!Directory.Exists(palettePath))
            {
                Directory.CreateDirectory(palettePath);
            }

            // Generate a unique filename
            string assetPath = $"{palettePath}/{newPalette.paletteName}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            // Save the asset
            AssetDatabase.CreateAsset(newPalette, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the new palette
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newPalette;
            currentPalette = newPalette;

            // Update UI
            UpdateGradientTexture();
            UpdateSampledColors();
            needsUpdate = true;

            Debug.Log($"Created palette from image: {assetPath}");
        }
    }

    private Color[] ExtractDominantColors(Texture2D texture, int colorCount)
    {
        // A simple way to extract dominant colors from the image
        // For a production implementation, consider using K-means clustering

        // Get all pixels
        Color[] pixels = texture.GetPixels();

        // Sort them by brightness
        System.Array.Sort(pixels, (a, b) => GetBrightness(a).CompareTo(GetBrightness(b)));

        // Extract evenly spaced colors
        Color[] dominantColors = new Color[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            int index = Mathf.FloorToInt(i * (float)pixels.Length / colorCount);
            dominantColors[i] = pixels[index];
        }

        return dominantColors;
    }
}