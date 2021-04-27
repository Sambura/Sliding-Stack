using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCube : MonoBehaviour
{
    public GameObject pickupEffect;
    public float effectLifespan;
    [HideInInspector] public Rect rectangle;

    private GameController controller;

    public void OnPickup()
	{
        GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
        Destroy(effect, effectLifespan);
        //controller.RemovePlayerCube(thisNode);
	}
    
    void Start()
    {
        controller = FindObjectOfType<GameController>();
        rectangle = new Rect(transform.position.x - 0.5f, transform.position.z - 0.5f, 1, 1);
    }
}
