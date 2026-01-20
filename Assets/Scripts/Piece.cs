using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class Piece : MonoBehaviour, ICustomDraggable
{
    private Vector2 _startAnchoredPos;
    private Transform _originalParent;
    private bool _isDragging;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private RectTransform _dragParent;

    [Header("Текущий уровень")]
    public PieceLevelSO CurrentLevelSO;

    private const float ScaleFactor = 1.3f;
    private const float ScaleDuration = 0.2f;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("Piece должен быть внутри Canvas!");
            return;
        }

        //Дочерний элемент Canvas с индексом 1 - dragParent
        if (_canvas.transform.childCount > 1)
            _dragParent = _canvas.transform.GetChild(1) as RectTransform;
        else
            _dragParent = _canvas.transform as RectTransform;
    }

    private void Update()
    {
        if (!_isDragging || _canvas == null) return;

        OnDrag();

#if UNITY_ANDROID
        if (Input.touchCount == 0 || Input.GetTouch(0).phase == TouchPhase.Ended)
            OnDragEnd();
#else
        if (Input.GetMouseButtonUp(0))
            OnDragEnd();
#endif
    }

    private void OnMouseDown()
    {
        OnDragStart();
    }

    public void OnDragStart()
    {
        _startAnchoredPos = _rectTransform.anchoredPosition;
        _originalParent = _rectTransform.parent;
        _isDragging = true;

        //Временный родитель dragParent, чтобы кусочек был над всеми
        if (_dragParent != null)
            _rectTransform.SetParent(_dragParent, true);

        _rectTransform.SetAsLastSibling();

        //z всегда = 1
        Vector3 pos = _rectTransform.localPosition;
        pos.z = 1f;
        _rectTransform.localPosition = pos;
    }

    public void OnDrag()
    {
        if (_canvas == null) return;

        Vector2 localPoint;

#if UNITY_ANDROID
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, touch.position,
            _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null,
            out localPoint);
#else
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, Input.mousePosition,
            _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null,
            out localPoint);
#endif

        _rectTransform.anchoredPosition = localPoint;

        //z = 1
        _rectTransform.localPosition = new Vector3(_rectTransform.localPosition.x,
                                                   _rectTransform.localPosition.y,
                                                   1f);
    }

    public void OnDragEnd()
    {
        _isDragging = false;

        Cell targetCell = GetCellUnderMouse();
        if (targetCell == null)
        {
            ReturnToStart();
            return;
        }

        if (targetCell.IsEmpty())
        {
            targetCell.SetPiece(gameObject);
        }
        else if (targetCell.CurrentPiece.TryGetComponent<Piece>(out var otherPiece) &&
                 otherPiece.CurrentLevelSO.Level == CurrentLevelSO.Level)
        {
            Merge(targetCell, otherPiece);
        }
        else
        {
            ReturnToStart();
        }
    }

    private void ReturnToStart()
    {
        _rectTransform.SetParent(_originalParent, true);
        _rectTransform.anchoredPosition = _startAnchoredPos;
        _rectTransform.localPosition = new Vector3(_rectTransform.localPosition.x,
                                                   _rectTransform.localPosition.y,
                                                   0f);
    }

    private void Merge(Cell cell, Piece otherPiece)
    {
        if (CurrentLevelSO.NextLevelSO == null)
        {
            ReturnToStart();
            return;
        }

        GameObject newPiece = Instantiate(CurrentLevelSO.NextLevelSO.Prefab, cell.transform.position, Quaternion.identity);
        newPiece.tag = $"Piece_{CurrentLevelSO.NextLevelSO.Level}";

        cell.SetPiece(newPiece);

        Destroy(otherPiece.gameObject);
        Destroy(gameObject);

        //DOTween анимация
        RectTransform newRect = newPiece.GetComponent<RectTransform>();
        newRect.localScale = Vector3.zero;
        Sequence seq = DOTween.Sequence();
        seq.Append(newRect.DOScale(Vector3.one * ScaleFactor, ScaleDuration));
        seq.Append(newRect.DOScale(Vector3.one, ScaleDuration));
        seq.Join(newRect.DOPunchScale(Vector3.one * 0.1f, 0.3f, 1));

        Image img = newPiece.GetComponent<Image>();
        if (img != null)
            seq.Join(img.DOFade(1f, 0.5f));

        Piece newPieceScript = newPiece.GetComponent<Piece>();
        if (newPieceScript != null)
        {
            newPieceScript.CurrentLevelSO = CurrentLevelSO.NextLevelSO;
            newPieceScript.CurrentLevelSO.NextLevelSO = CurrentLevelSO.NextLevelSO.NextLevelSO;
        }
    }

    private Cell GetCellUnderMouse()
    {
        Cell[] allCells = FindObjectsOfType<Cell>();
        float minDistance = float.MaxValue;
        Cell closestCell = null;

        foreach (var cell in allCells)
        {
            RectTransform rect = cell.GetComponent<RectTransform>();
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera ?? Camera.main, rect.position);
#if UNITY_ANDROID
            Vector2 pointerPos = Input.touchCount > 0 ? (Vector2)Input.GetTouch(0).position : Vector2.zero;
#else
            Vector2 pointerPos = Input.mousePosition;
#endif
            float distance = Vector2.Distance(pointerPos, screenPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCell = cell;
            }
        }

        return minDistance < 100f ? closestCell : null;
    }
}
