using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PixelArtConverterWindow : EditorWindow
{
    private Texture2D originalImage;
    private Texture2D previewImage;
    private int pixelArtResolution = 32;
    private int minResolution = 8;
    private int maxResolution = 4096;
    private float displaySize = 300f;
    private float minDisplaySize = 200f;
    private float maxDisplaySize = 600f;
    private Vector2 scrollPosition;
    private string savePath = "Assets/Game Assemblies/2d Assets/Asset Tools Outputs/";
    private string fileName = "PixelArt";
    private bool needsUpdate = false;

    // Color Replacement
    private bool showColorReplacementOptions = false;
    private Color colorToReplace = Color.magenta;
    private float colorTolerance = 0.1f;
    private bool useFloodFill = true;
    private bool checkAllCorners = true;

    [MenuItem("Game Assemblies/Asset Tools/Pixel Art Converter")]
    public static void ShowWindow()
    {
        GetWindow<PixelArtConverterWindow>("Pixel Art Converter");
    }

    private void OnEnable()
    {
        // Default path using Application.dataPath
        savePath = "Assets/PixelArt/";
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Pixel Art Converter", EditorStyles.boldLabel);
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
                fileName = originalImage.name + "_pixelated";
                needsUpdate = true;
            }
        }

        if (originalImage != null)
        {
            // Display the original image
            GUILayout.Label("Original Image:");
            Rect previewRect = GUILayoutUtility.GetRect(displaySize, displaySize);
            GUI.DrawTexture(previewRect, originalImage, ScaleMode.ScaleToFit);

            GUILayout.Space(10);
            GUILayout.Label($"Original Resolution: {originalImage.width} x {originalImage.height}");
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Conversion Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("2. Adjust Pixel Resolution", EditorStyles.boldLabel);

        if (originalImage != null)
        {
            // Slider for resolution adjustment
            EditorGUI.BeginChangeCheck();
            pixelArtResolution = EditorGUILayout.IntSlider("Pixel Resolution", pixelArtResolution, minResolution, maxResolution);
            if (EditorGUI.EndChangeCheck())
            {
                needsUpdate = true;
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Update Preview", GUILayout.Height(30)))
            {
                RegeneratePixelArtPreview();
                needsUpdate = false;
            }

            if (needsUpdate)
            {
                EditorGUILayout.HelpBox("Click 'Update Preview' to see changes", MessageType.Info);
            }

            GUILayout.Space(10);

            if (previewImage != null)
            {
                // Display the pixel art preview
                GUILayout.Label("Pixel Art Preview:");
                Rect previewRect = GUILayoutUtility.GetRect(displaySize, displaySize);
                GUI.DrawTexture(previewRect, previewImage, ScaleMode.ScaleToFit);

                GUILayout.Space(10);
                GUILayout.Label($"Pixelated Resolution: {previewImage.width} x {previewImage.height}");
            }
        } else
        {
            GUILayout.Label("Load an image to start converting", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Color Replacement Section
        EditorGUILayout.BeginVertical("box");
        showColorReplacementOptions = EditorGUILayout.Foldout(showColorReplacementOptions, "Color Replacement Options", true);

        if (showColorReplacementOptions && previewImage != null)
        {
            GUILayout.Label("Replace Color with Transparency", EditorStyles.boldLabel);

            colorToReplace = EditorGUILayout.ColorField("Color to Replace", colorToReplace);
            colorTolerance = EditorGUILayout.Slider("Color Tolerance", colorTolerance, 0f, 1f);

            EditorGUILayout.BeginHorizontal();
            useFloodFill = EditorGUILayout.Toggle("Use Flood Fill", useFloodFill);
            GUI.enabled = useFloodFill;
            checkAllCorners = EditorGUILayout.Toggle("Check All Corners", checkAllCorners);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (useFloodFill)
            {
                EditorGUILayout.HelpBox("Flood fill will only remove connected pixels of the selected color, preserving isolated areas of the same color.", MessageType.Info);
            } else
            {
                EditorGUILayout.HelpBox("All pixels matching the selected color will be made transparent.", MessageType.Info);
            }

            if (GUILayout.Button("Make Color Transparent", GUILayout.Height(25)))
            {
                MakeColorTransparent();
                GUIUtility.ExitGUI(); // Prevents UI errors when modifying the texture
            }

            if (GUILayout.Button("Reset Transparency", GUILayout.Height(25)))
            {
                RegeneratePixelArtPreview(); // Regenerate the preview without transparency
                needsUpdate = false;
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Save Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("3. Save Pixel Art", EditorStyles.boldLabel);

        if (originalImage != null && previewImage != null)
        {
            savePath = EditorGUILayout.TextField("Save Directory", savePath);
            fileName = EditorGUILayout.TextField("File Name", fileName);

            GUILayout.Space(10);

            if (GUILayout.Button("Save Pixel Art", GUILayout.Height(30)))
            {
                SavePixelArt();
            }
        } else
        {
            GUILayout.Label("Complete steps above to enable saving", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    private void RegeneratePixelArtPreview()
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

            // Calculate aspect ratio
            float aspectRatio = (float)originalImage.height / originalImage.width;
            int width = pixelArtResolution;
            int height = Mathf.RoundToInt(pixelArtResolution * aspectRatio);

            // Create a new texture for the pixelated version with Point filtering
            if (previewImage == null || previewImage.width != width || previewImage.height != height)
            {
                previewImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
                previewImage.filterMode = FilterMode.Point; // Ensure the texture itself uses Point filtering
            }

            // Create a pixelated version by direct sampling (manual nearest neighbor)
            Color[] pixelatedColors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate sampling coordinates in original texture (with nearest neighbor)
                    float u = (float)x / width;
                    float v = (float)y / height;

                    // Convert to pixel coordinates in original
                    int srcX = Mathf.FloorToInt(u * originalImage.width);
                    int srcY = Mathf.FloorToInt(v * originalImage.height);

                    // Ensure we're inside bounds
                    srcX = Mathf.Clamp(srcX, 0, originalImage.width - 1);
                    srcY = Mathf.Clamp(srcY, 0, originalImage.height - 1);

                    // Copy the pixel directly (true nearest neighbor)
                    pixelatedColors[y * width + x] = originalImage.GetPixel(srcX, srcY);
                }
            }

            // Apply the pixelated data to our texture
            previewImage.SetPixels(pixelatedColors);
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

    private void MakeColorTransparent()
    {
        if (previewImage == null) return;

        if (useFloodFill)
        {
            FloodFillTransparency();
        } else
        {
            // Original method - make all matching pixels transparent
            Color[] pixels = previewImage.GetPixels();

            // Loop through all pixels
            for (int i = 0; i < pixels.Length; i++)
            {
                // Check if this pixel is close to the target color using tolerance
                if (ColorDistance(pixels[i], colorToReplace) <= colorTolerance)
                {
                    // Set alpha to 0 (completely transparent)
                    pixels[i].a = 0f;
                }
            }

            // Apply the changes
            previewImage.SetPixels(pixels);
            previewImage.Apply();
        }
    }

    private void FloodFillTransparency()
    {
        int width = previewImage.width;
        int height = previewImage.height;

        // Create a copy of the texture to work with
        Color[] pixels = previewImage.GetPixels();
        bool[] visited = new bool[pixels.Length];

        // Start flood fill from corners if needed
        if (checkAllCorners)
        {
            // Top-left corner
            FloodFill(0, 0, width, height, pixels, visited);

            // Top-right corner
            FloodFill(width - 1, 0, width, height, pixels, visited);

            // Bottom-left corner
            FloodFill(0, height - 1, width, height, pixels, visited);

            // Bottom-right corner
            FloodFill(width - 1, height - 1, width, height, pixels, visited);
        } else
        {
            // Just start from top-left corner
            FloodFill(0, 0, width, height, pixels, visited);
        }

        // Apply the changes
        previewImage.SetPixels(pixels);
        previewImage.Apply();
    }

    private void FloodFill(int x, int y, int width, int height, Color[] pixels, bool[] visited)
    {
        // Create a queue for the flood fill
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Add the starting pixel
        int startIndex = y * width + x;

        // Only start flood fill if the pixel matches our target color
        if (ColorDistance(pixels[startIndex], colorToReplace) <= colorTolerance)
        {
            queue.Enqueue(new Vector2Int(x, y));
            visited[startIndex] = true;

            // Process the queue
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentIndex = current.y * width + current.x;

                // Make this pixel transparent
                Color currentColor = pixels[currentIndex];
                currentColor.a = 0f;
                pixels[currentIndex] = currentColor;

                // Check all 4 neighboring pixels (up, down, left, right)
                CheckNeighbor(current.x + 1, current.y, width, height, pixels, visited, queue);  // Right
                CheckNeighbor(current.x - 1, current.y, width, height, pixels, visited, queue);  // Left
                CheckNeighbor(current.x, current.y + 1, width, height, pixels, visited, queue);  // Up
                CheckNeighbor(current.x, current.y - 1, width, height, pixels, visited, queue);  // Down
            }
        }
    }

    private void CheckNeighbor(int x, int y, int width, int height, Color[] pixels, bool[] visited, Queue<Vector2Int> queue)
    {
        // Check if the pixel is within bounds
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        int index = y * width + x;

        // Check if we've already visited this pixel
        if (visited[index])
            return;

        // Check if this pixel matches our target color
        if (ColorDistance(pixels[index], colorToReplace) <= colorTolerance)
        {
            // Mark as visited and add to queue
            visited[index] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }
    }

    private float ColorDistance(Color a, Color b)
    {
        // Calculate Euclidean distance between colors (ignoring alpha)
        float rDiff = a.r - b.r;
        float gDiff = a.g - b.g;
        float bDiff = a.b - b.b;

        return Mathf.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
    }

    private void SavePixelArt()
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
            // Critical settings for pixel art
            importer.filterMode = FilterMode.Point; // Set nearest neighbor filtering
            importer.textureCompression = TextureImporterCompression.Uncompressed; // No compression for pixel art
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

        Debug.Log("Pixel art saved to: " + fullPath);

        // Ping the saved asset in the Project view
        Object savedAsset = AssetDatabase.LoadAssetAtPath(fullPath, typeof(Texture2D));
        if (savedAsset != null)
        {
            EditorGUIUtility.PingObject(savedAsset);

            // Output a message about the proper texture settings
            Debug.Log("For best pixel art results, ensure the texture has these settings:\n" +
                     "- Filter Mode: Point\n" +
                     "- Compression: None\n" +
                     "- Generate Mip Maps: Disabled");
        }
    }
}