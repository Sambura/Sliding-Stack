using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IDragHandler
{
	[HideInInspector] static public event System.Action PointerDown;
	[HideInInspector] static public event System.Action<float> HorizontalDrag; // drag delta

	public void OnDrag(PointerEventData eventData)
	{
		HorizontalDrag?.Invoke(eventData.delta.x);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		PointerDown?.Invoke();
	}
}
