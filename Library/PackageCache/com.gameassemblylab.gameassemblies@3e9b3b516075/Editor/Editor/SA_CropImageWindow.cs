using UnityEngine;
using UnityEditor;
using System.IO;

public class CropImageWindow : EditorWindow
{
    private Texture2D sourceImage;
    private Texture2D previewImage;
    private Vector2 scrollPosition;
    private float displaySize = 300f;

    // Crop rectangle parameters
    private int cropX = 0;
    private int cropY = 0;
    private int cropWidth = 250;
    private int cropHeight = 250;

    // Save parameters
    private string savePath = "Assets/Game Assemblies/2d Assets/Asset Tools Outputs/";
    private string fileName = "cropped_image";

    // Category selection
    private string[] categories = new string[] { "Player", "Resource", "Station", "Goal" };
    private int selectedCategoryIndex = 0;

    // Target resolutions for each category
    private Vector2Int[] targetResolutions = new Vector2Int[] {
        new Vector2Int(512, 512), // Player
        new Vector2Int(256, 256), // Resource
        new Vector2Int(512, 512), // Station
        new Vector2Int(256, 256)  // Goal
    };

    [MenuItem("Game Assemblies/Asset Tools/Crop Image")]
    public static void ShowWindow()
    {
        GetWindow<CropImageWindow>("Crop Image");
    }

    private void OnEnable()
    {
        // Set default save path
        savePath = "Assets/Game Assemblies/2d Assets/Asset Tools Outputs/";
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Crop Image Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Image Loading Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("1. Load Image", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        sourceImage = (Texture2D)EditorGUILayout.ObjectField("Source Image", sourceImage, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck() && sourceImage != null)
        {
            // Set default crop area when image is loaded
            //cropWidth = Mathf.Min(100, sourceImage.width);
            //cropHeight = Mathf.Min(100, sourceImage.height);
            fileName = sourceImage.name + "_" + categories[selectedCategoryIndex].ToLower();

            // Generate initial preview
            UpdateCropPreview();
        }

        if (sourceImage != null)
        {
            // Display the source image
            GUILayout.Label("Source Image:");
            Rect imageRect = GUILayoutUtility.GetRect(displaySize, displaySize);
            GUI.DrawTexture(imageRect, sourceImage, ScaleMode.ScaleToFit);

            // Draw crop rectangle on the image
            DrawCropRect(imageRect);

            GUILayout.Space(5);
            GUILayout.Label($"Image Size: {sourceImage.width} x {sourceImage.height}");
            GUILayout.Label($"Crop Region: ({cropX}, {cropY}) - {cropWidth} x {cropHeight}");
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Crop Settings Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("2. Set Crop Area", EditorStyles.boldLabel);

        if (sourceImage != null)
        {
            EditorGUI.BeginChangeCheck();

            // Crop parameters
            cropX = EditorGUILayout.IntSlider("X Position", cropX, 0, Mathf.Max(0, sourceImage.width - cropWidth));
            cropY = EditorGUILayout.IntSlider("Y Position", cropY, 0, Mathf.Max(0, sourceImage.height - cropHeight));
            cropWidth = EditorGUILayout.IntSlider("Width", cropWidth, 1, sourceImage.width - cropX);
            cropHeight = EditorGUILayout.IntSlider("Height", cropHeight, 1, sourceImage.height - cropY);

            if (EditorGUI.EndChangeCheck())
            {
                // Auto-update preview when settings change
                UpdateCropPreview();
            }

            GUILayout.Space(10);

            // Preview button
            if (GUILayout.Button("Update Preview", GUILayout.Height(30)))
            {
                UpdateCropPreview();
            }
        } else
        {
            GUILayout.Label("Load an image to start cropping", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Preview Section
        if (previewImage != null)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("3. Preview", EditorStyles.boldLabel);

            // Display the cropped preview
            GUILayout.Label("Cropped Result:");
            Rect previewRect = GUILayoutUtility.GetRect(displaySize, displaySize);
            GUI.DrawTexture(previewRect, previewImage, ScaleMode.ScaleToFit);

            GUILayout.Space(5);
            GUILayout.Label($"Current Size: {previewImage.width} x {previewImage.height}");
            GUILayout.Label($"Target Size: {targetResolutions[selectedCategoryIndex].x} x {targetResolutions[selectedCategoryIndex].y} (will be resized when saved)");

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }

        // Save Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("4. Save Cropped Image", EditorStyles.boldLabel);

        if (previewImage != null)
        {
            savePath = EditorGUILayout.TextField("Save Directory", savePath);
            fileName = EditorGUILayout.TextField("File Name", fileName);

            // Category dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Category");

            EditorGUI.BeginChangeCheck();
            selectedCategoryIndex = EditorGUILayout.Popup(selectedCategoryIndex, categories);
            if (EditorGUI.EndChangeCheck())
            {
                // Update filename when category changes
                if (sourceImage != null)
                {
                    fileName = sourceImage.name + "_" + categories[selectedCategoryIndex].ToLower();
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Add help text about the target resolution
            EditorGUILayout.HelpBox("The image will be resized based on the selected category while maintaining its aspect ratio.\nAny extra space will be filled with white background.\nTarget sizes:\nPlayers: 512x512px, Resources: 256x256px, Stations: 1024x1024px, Goals: 256x256px", MessageType.Info);

            if (GUILayout.Button("Save to Project", GUILayout.Height(30)))
            {
                SaveCroppedImage();
            }

            if (GUILayout.Button("Save to File", GUILayout.Height(30)))
            {
                SaveCroppedImageToFile();
            }
        } else
        {
            GUILayout.Label("Complete steps above to enable saving", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    private void DrawCropRect(Rect displayRect)
    {
        if (sourceImage == null) return;

        // Calculate scale factor between display and actual image
        float scaleX = displayRect.width / sourceImage.width;
        float scaleY = displayRect.height / sourceImage.height;
        float scale = Mathf.Min(scaleX, scaleY);

        // Calculate the actual displayed image rect (centered in the display area)
        float imageWidth = sourceImage.width * scale;
        float imageHeight = sourceImage.height * scale;
        float offsetX = displayRect.x + (displayRect.width - imageWidth) / 2;
        float offsetY = displayRect.y + (displayRect.height - imageHeight) / 2;

        Rect actualImageRect = new Rect(offsetX, offsetY, imageWidth, imageHeight);

        // Now calculate crop rect in display coordinates
        Rect cropRect = new Rect(
            actualImageRect.x + cropX * scale,
            actualImageRect.y + cropY * scale,
            cropWidth * scale,
            cropHeight * scale
        );

        // Draw crop rectangle
        Color originalColor = GUI.color;
        GUI.color = new Color(1, 1, 0, 0.3f);
        GUI.Box(cropRect, "");

        // Draw outline
        Handles.color = Color.yellow;
        Handles.DrawLine(new Vector3(cropRect.x, cropRect.y), new Vector3(cropRect.x + cropRect.width, cropRect.y));
        Handles.DrawLine(new Vector3(cropRect.x + cropRect.width, cropRect.y), new Vector3(cropRect.x + cropRect.width, cropRect.y + cropRect.height));
        Handles.DrawLine(new Vector3(cropRect.x + cropRect.width, cropRect.y + cropRect.height), new Vector3(cropRect.x, cropRect.y + cropRect.height));
        Handles.DrawLine(new Vector3(cropRect.x, cropRect.y + cropRect.height), new Vector3(cropRect.x, cropRect.y));

        GUI.color = originalColor;
    }

    private void UpdateCropPreview()
    {
        if (sourceImage == null) return;

        // Ensure crop region is valid
        cropX = Mathf.Clamp(cropX, 0, sourceImage.width - 1);
        cropY = Mathf.Clamp(cropY, 0, sourceImage.height - 1);
        cropWidth = Mathf.Clamp(cropWidth, 1, sourceImage.width - cropX);
        cropHeight = Mathf.Clamp(cropHeight, 1, sourceImage.height - cropY);

        // Make sure source texture is readable
        string path = AssetDatabase.GetAssetPath(sourceImage);
        bool needsReimport = false;
        TextureImporter importer = null;

        if (!string.IsNullOrEmpty(path))
        {
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                needsReimport = true;
            }
        }

        try
        {
            // Create a new texture for the cropped image
            previewImage = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false);

            // Unity's GetPixels uses bottom-left as origin, but our Y coordinate is from top
            // Adjust Y coordinate to get from the bottom
            int adjustedY = sourceImage.height - cropY - cropHeight;

            // Read the pixels from the source image within the crop rect
            Color[] pixels = sourceImage.GetPixels(cropX, adjustedY, cropWidth, cropHeight);

            // Set the pixels in the preview texture
            previewImage.SetPixels(pixels);
            previewImage.Apply();
        } catch (System.Exception e)
        {
            Debug.LogError("Error creating crop preview: " + e.Message);
            previewImage = null;
        }

        // Restore original import settings if needed
        if (needsReimport && importer != null)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }

    private Texture2D ResizeWithWhiteBackground(Texture2D source, Vector2Int targetSize)
    {
        // Create a new texture with the target dimensions
        Texture2D result = new Texture2D(targetSize.x, targetSize.y, TextureFormat.RGBA32, false);

        // Fill the entire texture with white
        Color[] fillPixels = new Color[targetSize.x * targetSize.y];
        for (int i = 0; i < fillPixels.Length; i++)
        {
            fillPixels[i] = Color.white;
        }
        result.SetPixels(fillPixels);
        result.Apply();

        // Calculate the scaling factor to maintain aspect ratio
        float scaleX = (float)targetSize.x / source.width;
        float scaleY = (float)targetSize.y / source.height;
        float scale = Mathf.Min(scaleX, scaleY); // Use the smallest scale to fit within target

        // Calculate the scaled dimensions
        int scaledWidth = Mathf.RoundToInt(source.width * scale);
        int scaledHeight = Mathf.RoundToInt(source.height * scale);

        // Create a temporary texture for the scaled image
        Texture2D scaledTexture = new Texture2D(scaledWidth, scaledHeight, TextureFormat.RGBA32, false);

        // Scale the source image using bilinear filtering
        for (int y = 0; y < scaledHeight; y++)
        {
            for (int x = 0; x < scaledWidth; x++)
            {
                float sourceX = x / (float)scaledWidth * source.width;
                float sourceY = y / (float)scaledHeight * source.height;

                // Get the pixel color using bilinear interpolation
                int x1 = Mathf.FloorToInt(sourceX);
                int y1 = Mathf.FloorToInt(sourceY);
                int x2 = Mathf.Min(x1 + 1, source.width - 1);
                int y2 = Mathf.Min(y1 + 1, source.height - 1);

                float u = sourceX - x1;
                float v = sourceY - y1;

                Color c11 = source.GetPixel(x1, y1);
                Color c12 = source.GetPixel(x1, y2);
                Color c21 = source.GetPixel(x2, y1);
                Color c22 = source.GetPixel(x2, y2);

                Color topMix = Color.Lerp(c11, c21, u);
                Color bottomMix = Color.Lerp(c12, c22, u);
                Color finalColor = Color.Lerp(topMix, bottomMix, v);

                scaledTexture.SetPixel(x, y, finalColor);
            }
        }
        scaledTexture.Apply();

        // Calculate position to center the scaled image
        int xOffset = (targetSize.x - scaledWidth) / 2;
        int yOffset = (targetSize.y - scaledHeight) / 2;

        // Copy the scaled image to the result texture
        for (int y = 0; y < scaledHeight; y++)
        {
            for (int x = 0; x < scaledWidth; x++)
            {
                // Calculate destination coordinates
                int destX = x + xOffset;
                int destY = y + yOffset;

                // Get the scaled pixel
                Color pixel = scaledTexture.GetPixel(x, y);

                // Blend with white background based on alpha
                if (pixel.a < 1.0f)
                {
                    pixel = Color.Lerp(Color.white, pixel, pixel.a);
                    pixel.a = 1.0f; // Make fully opaque
                }

                result.SetPixel(destX, destY, pixel);
            }
        }
        result.Apply();

        // Clean up the temporary texture
        DestroyImmediate(scaledTexture);

        return result;
    }

    private void SaveCroppedImage()
    {
        if (previewImage == null) return;

        // Get the appropriate target resolution for the selected category
        Vector2Int targetSize = targetResolutions[selectedCategoryIndex];

        // Create a resized version with white background
        Texture2D finalImage = ResizeWithWhiteBackground(previewImage, targetSize);

        // Ensure the directory exists
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // Add category subfolder
        string categoryFolder = Path.Combine(savePath, categories[selectedCategoryIndex]);
        if (!Directory.Exists(categoryFolder))
        {
            Directory.CreateDirectory(categoryFolder);
        }

        // Create full path
        string fullPath = Path.Combine(categoryFolder, fileName + ".png");

        // Convert to PNG bytes
        byte[] bytes = finalImage.EncodeToPNG();

        // Save the file
        File.WriteAllBytes(fullPath, bytes);

        // Refresh AssetDatabase to show the new file
        AssetDatabase.Refresh();

        // Get the saved asset and update its import settings
        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log("Cropped image saved to: " + fullPath);

        // Ping the saved asset in the Project view
        Object savedAsset = AssetDatabase.LoadAssetAtPath(fullPath, typeof(Texture2D));
        if (savedAsset != null)
        {
            EditorGUIUtility.PingObject(savedAsset);
        }

        // Clean up
        DestroyImmediate(finalImage);
    }

    private void SaveCroppedImageToFile()
    {
        if (previewImage == null) return;

        // Get the appropriate target resolution for the selected category
        Vector2Int targetSize = targetResolutions[selectedCategoryIndex];

        // Create a resized version with white background
        Texture2D finalImage = ResizeWithWhiteBackground(previewImage, targetSize);

        // Ask user for save location
        string defaultName = fileName + ".png";
        string path = EditorUtility.SaveFilePanel(
            "Save Cropped Image",
            "", // Default directory
            defaultName,
            "png"
        );

        // Check if user canceled
        if (string.IsNullOrEmpty(path))
        {
            DestroyImmediate(finalImage);
            return;
        }

        // Convert to PNG bytes
        byte[] bytes = finalImage.EncodeToPNG();

        // Save the file
        File.WriteAllBytes(path, bytes);

        EditorUtility.DisplayDialog("Image Saved", "Cropped image saved successfully to:\n" + path, "OK");

        // Clean up
        DestroyImmediate(finalImage);
    }
}