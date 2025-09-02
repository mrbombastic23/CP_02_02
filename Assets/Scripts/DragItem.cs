using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public bool isTarget;   // usado por GameManager
    private VocabItem vocabItem;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private RectTransform canvasRect;

    private Transform originParent;
    private Vector2 originAnchoredPos;

    private Vector2 pointerOffset;            // offset entre pointer y el centro del item (en coords del canvas)
    private bool processedThisDrag = false;   // si DropZone / GameManager ya procesó el drop

    // ---------------- SETUP (llamado desde GameManager al instanciar) ----------------
    public void Setup(VocabItem vocab, bool target, Canvas mainCanvas)
    {
        vocabItem = vocab;
        isTarget = target;
        canvas = mainCanvas;
        canvasRect = canvas.transform as RectTransform;

        // asigna sprite si existe
        var img = GetComponent<Image>();
        if (img != null && vocab != null && vocab.sprite != null)
        {
            img.sprite = vocab.sprite;
            img.enabled = true;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // seguridad: si canvas no fue set por Setup (fallback)
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvasRect = canvas.transform as RectTransform;
        }
    }

    // ---------------- DRAG ----------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        processedThisDrag = false;

        // guardar origen
        originParent = transform.parent;
        originAnchoredPos = rectTransform.anchoredPosition;

        // Traer al canvas para que coordenadas sean consistentes
        transform.SetParent(canvas.transform, true); // mantiene la posición visual inicialmente

        // calcular offset en coordenadas del canvas
        Vector2 localPointer;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPointer
        );

        // anchoredPosition ahora está en espacio del canvas (por SetParent)
        pointerOffset = rectTransform.anchoredPosition - localPointer;

        // visual / interacción
        canvasGroup.alpha = 0.85f;
        canvasGroup.blocksRaycasts = false;

        transform.SetAsLastSibling(); // que quede encima
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPointer;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPointer))
        {
            rectTransform.anchoredPosition = localPointer + pointerOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Esperar un frame para que OnDrop en DropZone tenga la oportunidad de ejecutarse
        StartCoroutine(EndDragRoutine());
    }

    private IEnumerator EndDragRoutine()
    {
        yield return null; // deja que se resuelva el OnDrop
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (!processedThisDrag)
        {
            ReturnToOrigin();
        }

        processedThisDrag = false;
    }

    // ---------------- Funciones usadas por GameManager ----------------
    public void SnapTo(RectTransform dropZone)
    {
        processedThisDrag = true;

        // poner como hijo de la dropZone y centrar
        transform.SetParent(dropZone, false); // false: no conservar world pos (queremos que se ajuste al dropZone)
        rectTransform.anchoredPosition = Vector2.zero;
        canvasGroup.blocksRaycasts = false; // ya no interactuable
    }

    public void ReturnToOrigin()
    {
        // volver al parent original (por ejemplo PlayArea con GridLayoutGroup)
        transform.SetParent(originParent, false);

        // si el padre tiene layout, la posición puede ser controlada por el layout.
        // forzamos anchoredPosition para cuando no haya layout; si hay LayoutGroup, Unity lo
        // reubicará automáticamente (no siempre es necesario setear anchoredPosition).
        rectTransform.anchoredPosition = originAnchoredPos;
    }
}
