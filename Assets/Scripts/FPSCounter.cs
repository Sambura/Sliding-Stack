using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private int frameBufferCapacity;

    private Queue<float> _frameTimings;
    private float _totalTime;
    private float _currentTime;
    private float _totalFrames;
    private const float _maxFrames = 1e7f;
    private float _frameBufferCapacity;

	public float CurrentFps { get { return _frameBufferCapacity / _currentTime; } }
	public float AverageFps { get { return _totalFrames / _totalTime; } }

	void Start()
    {
        _frameTimings = new Queue<float>(frameBufferCapacity);
        for (int i = 0; i < frameBufferCapacity; i++)
		{
            _frameTimings.Enqueue(0);
		}
        _totalFrames = float.Epsilon;
        _frameBufferCapacity = frameBufferCapacity;
    }

    void Update()
    {
        _totalTime += Time.deltaTime;
        _currentTime += Time.deltaTime;
        _totalFrames++;

        _currentTime -= _frameTimings.Dequeue();
        _frameTimings.Enqueue(Time.deltaTime);

        if (_totalFrames > _maxFrames)
		{
            _totalFrames = 1;
            _totalTime = Time.deltaTime;
		}
    }
}
