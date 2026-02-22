using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DEV ONLY - Makes a UI panel draggable with the mouse.
/// Attach to a RectTransform inside a Canvas.
/// Not intended for production use.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableUIPanel : MonoBehaviour, IPointerDownHandler, IDragHandler
{
	private RectTransform _rectTransform;
	private Canvas _canvas;

	private Vector2 _pointerOffset;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
		_canvas = GetComponentInParent<Canvas>();

		if (_canvas == null)
		{
			Debug.LogError("DraggableUIPanel requires a parent Canvas.");
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_rectTransform,
			eventData.position,
			eventData.pressEventCamera,
			out _pointerOffset
		);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (_canvas == null)
			return;

		Vector2 localPointerPosition;

		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_canvas.transform as RectTransform,
			eventData.position,
			eventData.pressEventCamera,
			out localPointerPosition))
		{
			_rectTransform.localPosition = localPointerPosition - _pointerOffset;
		}
	}
}