using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerChecker : MonoBehaviour
{
    private SteamVR_TrackedObject trackedObject;

    [SerializeField]
    private bool trigger;
	// Use this for initialization
	void Start ()
    {
        trackedObject = GetComponent<SteamVR_TrackedObject>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        var device = SteamVR_Controller.Input((int)trackedObject.index);

        trigger = device.GetPress(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
	}
}
