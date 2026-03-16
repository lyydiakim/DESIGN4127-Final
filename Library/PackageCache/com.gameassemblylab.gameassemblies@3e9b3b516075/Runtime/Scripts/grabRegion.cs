using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class grabRegion : MonoBehaviour
{
    private playerController pController;
    public bool debug = false;
    // Start is called before the first frame update
    void Start()
    {
        pController = transform.parent.GetComponent <playerController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D  other)
    {
        if (debug) Debug.Log("In Trigger with: " + other);
        // When absorbing (Y held), playerConsumeArea handles multigrab—don't add here to avoid double-add
        if (pController.isAbsorbingResources) return;
        if (pController.isCarryingObject != true)
        //if(pController.listobjectsToGrab.Count < pController.maxObjectsToCarry)
        {
            if (TagUtilities.HasTag(other.gameObject, TagType.Grabbable))
            {
                // Don't grab Consumable resources—they're instant-collect on contact.
                var resourceObj = other.GetComponent<ResourceObject>();
                bool isConsumable = resourceObj != null && resourceObj.resourceType != null
                    && resourceObj.resourceType.typeOfBehavior == Resource.ResourceBehavior.Consumable;
                if (!isConsumable)
                {
                    if (debug) Debug.Log("GRABABBLE OBJECT DEFINED: " + other);
                    if (!pController.listobjectsToGrab.Contains(other.gameObject))
                        pController.listobjectsToGrab.Add(other.gameObject);
                }
            }
            if (TagUtilities.HasTag(other.gameObject, TagType.Workable))
            {
                if (debug) Debug.Log("LABOR OBJECT DEFINED: " + other);
                if (!pController.listObjectsToLabor.Contains(other.gameObject))
                    pController.listObjectsToLabor.Add(other.gameObject);
            }
            if (TagUtilities.HasTag(other.gameObject, TagType.Inspectable))
            {
                if (debug) Debug.Log("INSPECTABLE OBJECT DEFINED");
                if (!pController.listObjectsToInspect.Contains(other.gameObject))
                    pController.listObjectsToInspect.Add(other.gameObject);
                pController.doUpdate = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (debug) Debug.Log("Trigger edit with: " + other);
        // When absorbing, playerConsumeArea owns the list—don't remove here
        if (pController.isAbsorbingResources) return;
        if (pController.isCarryingObject != true)
        //if (pController.listobjectsToGrab.Count < pController.maxObjectsToCarry)
        {
            if (TagUtilities.HasTag(other.gameObject, TagType.Grabbable))
            {
                if (debug) Debug.Log("GRABABBLE OBJECT RELEASED");
                pController.listobjectsToGrab.Remove(other.gameObject);
                if (pController.objectToGrab == other.gameObject)
                    pController.objectToGrab = null;
            }

        }
            if (TagUtilities.HasTag(other.gameObject, TagType.Workable))
            {
                if (debug) Debug.Log("LABOR OBJECT RELEASED");
                pController.listObjectsToLabor.Remove(other.gameObject);
                if (pController.objectToLabor == other.gameObject)
                {
                    pController.cancelLabor();
                    pController.objectToLabor = null;
                    var station = other.gameObject.GetComponent<Station>();
                    if (station != null) station.isBeingInspected = false;
                }
            }
            if (TagUtilities.HasTag(other.gameObject, TagType.Inspectable))
            {
                if (debug) Debug.Log("INSPECTABLE OBJECT RELEASED");
                pController.listObjectsToInspect.Remove(other.gameObject);
                if (pController.objectToInspect == other.gameObject)
                {
                    pController.objectToInspect = null;
                    var station = other.gameObject.GetComponent<Station>();
                    if (station != null) station.isBeingInspected = false;
                }
            }
       // }
    }

}
