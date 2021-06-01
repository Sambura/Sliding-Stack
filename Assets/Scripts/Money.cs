using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : MonoBehaviour
{
    public GameObject pickupEffect;
    public float effectLifespan;
    public int poolCapacity = 50;
    [HideInInspector] public Rect rectangle;
    [HideInInspector] public float height;

    private static ComponentPool<ParticleSystem> effectsPool;

    public void OnPickup()
    {
        ParticleSystem effect = effectsPool.GetFromPool();
        effect.transform.position = transform.position;
        effect.gameObject.SetActive(true);
        effect.Play();
        Destroy(gameObject);
    }

    void Start()
    {
        rectangle = new Rect(transform.position.x - 0.5f, transform.position.z - 0.5f, 1, 1);
        height = transform.position.y;
        if (effectsPool == null)
            effectsPool = new ComponentPool<ParticleSystem>(pickupEffect, poolCapacity);
    }
}
