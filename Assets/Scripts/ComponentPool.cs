using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentPool<T> where T : Component
{
    private Queue<T> _pool;
	private GameObject _prefab;

    public T GetFromPool()
	{
		T value = _pool.Dequeue();
		_pool.Enqueue(value);
		return value;
	}

	public ComponentPool(GameObject prefab, int capacity)
	{
		_pool = new Queue<T>(capacity);
		_prefab = prefab;

		for (int i = 0; i < capacity; i++)
		{
			GameObject clone = Object.Instantiate(_prefab, _prefab.transform.parent);
			clone.SetActive(false);
			_pool.Enqueue(clone.GetComponent<T>());
		}
	}
}
