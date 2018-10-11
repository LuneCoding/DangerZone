using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;

public class WeaponFollow : WeaponBehavior
{

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (!networkObject.IsServer)
        {
            transform.position = networkObject.position;
            transform.rotation = networkObject.rotation;
            return;
        }

        networkObject.position = transform.position;
        networkObject.rotation = transform.rotation;
    }
}
