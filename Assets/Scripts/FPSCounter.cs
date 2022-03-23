using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private int frameBufferCapacity;

    private Queue<float> _frameTimings;
    private float _currentTime;
    private float _frameBufferCapacity;

	public float CurrentFps { get { return _frameBufferCapacity / _currentTime; } } //  ==  1 / (_currentTime / _frameBufferCapacity)

	void Start()
    {
        _frameTimings = new Queue<float>(frameBufferCapacity);
        for (int i = 0; i < frameBufferCapacity; i++)
            _frameTimings.Enqueue(0);
        _frameBufferCapacity = frameBufferCapacity;
    }

    void Update()
    {
        _currentTime += Time.deltaTime;

        _currentTime -= _frameTimings.Dequeue();
        _frameTimings.Enqueue(Time.deltaTime);
    }
}
