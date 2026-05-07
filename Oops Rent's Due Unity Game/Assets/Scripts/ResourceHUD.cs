using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceHUD : MonoBehaviour
{
    [Header("Bank")]
    [SerializeField] ResourceBank bank;

    [Header("Slot 1 - Money")]
    [SerializeField] Resource moneyResource;
    [SerializeField] TMP_Text moneyText;
    [SerializeField] Image    moneyIcon;

    [Header("Slot 2 - Network")]
    [SerializeField] Resource networkResource;
    [SerializeField] TMP_Text networkText;
    [SerializeField] Image    networkIcon;

    [Header("Slot 3 - Energy")]
    [SerializeField] Resource energyResource;
    [SerializeField] TMP_Text energyText;
    [SerializeField] Image    energyIcon;
    [Header("TextMeshPro")]
    [Tooltip("Optional fallback TMP font if any HUD text reference has a missing font asset.")]
    [SerializeField] TMP_FontAsset fallbackFont;

    void OnEnable()  => bank.OnResourceChanged += Refresh;
    void OnDisable() => bank.OnResourceChanged -= Refresh;
    void Start()
    {
        EnsureHudFonts();
        ApplyIcons();
        Refresh();
    }

    void EnsureHudFonts()
    {
        EnsureFontAssigned(moneyText);
        EnsureFontAssigned(networkText);
        EnsureFontAssigned(energyText);
    }

    void ApplyIcons()
    {
        SetIcon(moneyIcon,   moneyResource);
        SetIcon(networkIcon, networkResource);
        SetIcon(energyIcon,  energyResource);
    }

    void Refresh()
    {
        if (moneyText   != null) moneyText.text   = $"{bank.Get(moneyResource)}";
        if (networkText != null) networkText.text = $"{bank.Get(networkResource)}";
        if (energyText  != null) energyText.text  = $"{bank.Get(energyResource)}";
    }

    static void SetIcon(Image img, Resource res)
    {
        if (img == null || res == null) return;
        img.sprite = res.icon;
        img.color  = res.iconTint;
    }

    void EnsureFontAssigned(TMP_Text text)
    {
        if (text == null || text.font != null) return;

        TMP_FontAsset resolved = fallbackFont;
        if (resolved == null)
            resolved = TMP_Settings.defaultFontAsset;
        if (resolved == null)
            resolved = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (resolved == null)
            resolved = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        if (resolved == null) return;

        text.font = resolved;
        if (resolved.material != null)
            text.fontSharedMaterial = resolved.material;
    }
}
