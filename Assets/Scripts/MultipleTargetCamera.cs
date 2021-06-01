using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultipleTargetCamera : MonoBehaviour
{
    [Header("Common settings")]
    public List<Transform> targets;
    public Vector3 offset;
    [Header("Camera movement settings")]
    public float smoothing;
    public bool freezeX;
    public bool freezeY;
    public bool freezeZ;

    private Vector3 velocity;

	private void LateUpdate()
	{
        Vector3 newLocation = targets[0].position;

        for (int i = 1; i < targets.Count; i++)
            newLocation += targets[i].position;

        newLocation /= targets.Count;
        if (freezeX) newLocation.x = 0;
        if (freezeY) newLocation.y = 0;
        if (freezeZ) newLocation.z = 0;
        newLocation += offset;

        transform.position = Vector3.SmoothDamp(transform.position, newLocation, ref velocity, smoothing);
	}
}
