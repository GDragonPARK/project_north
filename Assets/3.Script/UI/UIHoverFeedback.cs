using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;
    private Vector3 targetScale;
    public float hoverScaleMultiplier = 1.15f;
    public float clickScaleMultiplier = 0.85f;
    public float animationSpeed = 15f;

    private bool isHovering = false;
    private bool isClicking = false;

    private void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("<color=cyan>[UI] Hover ENTER - Mouse Detected!</color>");
        isHovering = true;
        if (!isClicking)
        {
            targetScale = originalScale * hoverScaleMultiplier;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("<color=cyan>[UI] Hover EXIT</color>");
        isHovering = false;
        if (!isClicking)
        {
            targetScale = originalScale;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("<color=orange>[UI] Click DOWN</color>");
        isClicking = true;
        targetScale = originalScale * clickScaleMultiplier;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isClicking = false;
        if (isHovering)
        {
            targetScale = originalScale * hoverScaleMultiplier;
        }
        else
        {
            targetScale = originalScale;
        }
    }
}
