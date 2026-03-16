using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoWindow : MonoBehaviour
{   
    public Image ownerSprite;
    public GameObject inputPanel;
    public GameObject outputPanel;

    public GameObject inputResource;
    public GameObject outputResource;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private static Color GetIconTint(Resource r)
    {
        if (r == null) return Color.white;
        return r.iconTint.a < 0.01f ? Color.white : r.iconTint;
    }

    public void InitializeResources(List<Resource> produces, List<Resource> consumes)
    {
        if(inputResource == null || outputResource == null) return;

        foreach (Resource c in consumes) //input
        {
            GameObject inputObjects = Instantiate(inputResource);
            inputObjects.transform.SetParent(inputPanel.transform, false);
            var inputImage = inputObjects.GetComponentInChildren<Image>(true);
            if (inputImage != null)
            {
                if (c.icon != null) inputImage.sprite = c.icon;
                inputImage.color = GetIconTint(c);
            }
        }

        foreach (Resource p in produces) //output
        {
            GameObject outputObjects = Instantiate(outputResource);
            outputObjects.transform.SetParent(outputPanel.transform, false);
            var outputImage = outputObjects.GetComponentInChildren<Image>(true);
            if (outputImage != null)
            {
                if (p.icon != null) outputImage.sprite = p.icon;
                outputImage.color = GetIconTint(p);
            }
        }
    }
}
