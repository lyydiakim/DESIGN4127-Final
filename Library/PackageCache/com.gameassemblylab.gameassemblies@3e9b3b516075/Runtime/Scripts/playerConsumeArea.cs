using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerConsumeArea : MonoBehaviour
{
    private playerController pController;
    public bool debug = true;
    void Start()
    {
        pController = transform.parent.GetComponent<playerController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debug) Debug.Log("In Trigger with: " + other);

        // Consumable: instant collect. Source of truth is Resource.typeOfBehavior (fallback to tag for non-ResourceObject).
        bool isConsumable = false;
        var resourceObj = other.GetComponent<ResourceObject>();
        if (resourceObj != null && resourceObj.resourceType != null)
            isConsumable = resourceObj.resourceType.typeOfBehavior == Resource.ResourceBehavior.Consumable;
        else
            isConsumable = TagUtilities.HasTag(other.gameObject, TagType.Resource) && TagUtilities.HasTag(other.gameObject, TagType.Consumable);

        if (isConsumable)
        {
            if (debug) Debug.Log("Consumed: " + other);
            Destroy(other.gameObject);
            pController.capital += 1;
        }
        else if (pController.isAbsorbingResources && TagUtilities.HasTag(other.gameObject, TagType.Resource) && TagUtilities.HasTag(other.gameObject, TagType.Grabbable))
        {
            if (debug) Debug.Log("Absorbed: " + other);
            
            pController.sortObsorbedObject(other.gameObject);
            
        }

    }
}
