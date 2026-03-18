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

    void OnEnable()  => bank.OnResourceChanged += Refresh;
    void OnDisable() => bank.OnResourceChanged -= Refresh;
    void Start()
    {
        ApplyIcons();
        Refresh();
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
}
