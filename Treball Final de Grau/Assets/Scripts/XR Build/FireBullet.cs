using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FireBullet : MonoBehaviour
{
    public GameObject bullet;
    public Transform spawn;
    public float fireSpeed = 20.0f;

    void Start()
    {
        XRGrabInteractable grabbable = GetComponent<XRGrabInteractable>();
        grabbable.activated.AddListener(Fire);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Fire(ActivateEventArgs arg) 
    {
        GameObject spawnedBullet = Instantiate(bullet);
        spawnedBullet.transform.position = spawn.position;
        spawnedBullet.GetComponent<Rigidbody>().velocity = spawn.forward * fireSpeed;
        Destroy(spawnedBullet, 5);
    }
}
