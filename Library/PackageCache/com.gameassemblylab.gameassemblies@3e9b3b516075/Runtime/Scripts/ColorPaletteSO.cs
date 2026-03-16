using UnityEngine;

[CreateAssetMenu(fileName = "New Color Palette", menuName = "Game Assemblies/Color Palette")]
public class ColorPaletteSO : ScriptableObject
{
    public string paletteName;
    public Sprite previewImage; // Optional: An image preview of the palette

    [Header("Colors")]
    [Tooltip("Collection of 5 colors that make up this palette")]
    [SerializeField] private Color[] paletteColors = new Color[5];

    // Property to ensure we always have exactly 5 colors
    public Color[] Colors
    {
        get
        {
            // Ensure array is always exactly 5 colors
            if (paletteColors.Length != 5)
            {
                Color[] resizedArray = new Color[5];

                // Copy existing colors
                for (int i = 0; i < Mathf.Min(paletteColors.Length, 5); i++)
                {
                    resizedArray[i] = paletteColors[i];
                }

                // Fill remaining slots with white if array was too small
                for (int i = paletteColors.Length; i < 5; i++)
                {
                    resizedArray[i] = Color.white;
                }

                paletteColors = resizedArray;
            }

            return paletteColors;
        }
        set
        {
            // Ensure the set value has exactly 5 colors
            if (value.Length != 5)
            {
                Color[] resizedArray = new Color[5];

                // Copy existing colors
                for (int i = 0; i < Mathf.Min(value.Length, 5); i++)
                {
                    resizedArray[i] = value[i];
                }

                // Fill remaining slots with white if array was too small
                for (int i = value.Length; i < 5; i++)
                {
                    resizedArray[i] = Color.white;
                }

                paletteColors = resizedArray;
            } else
            {
                paletteColors = value;
            }
        }
    }

    // Get a specific color by index (0-4)
    public Color GetColor(int index)
    {
        if (index >= 0 && index < 5)
        {
            return Colors[index];
        }
        return Color.white; // Default return if index is out of range
    }

    // Set a specific color by index (0-4)
    public void SetColor(int index, Color color)
    {
        if (index >= 0 && index < 5)
        {
            Color[] colors = Colors; // This ensures we have exactly 5 colors
            colors[index] = color;
            Colors = colors;
        }
    }
}