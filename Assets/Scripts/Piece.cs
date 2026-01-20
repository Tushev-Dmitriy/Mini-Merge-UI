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

    public PieceLevelSO CurrentLevelSO;
    public Cell CurrentCell { get; private set; }

    private const float ScaleFactor = 1.3f;
    private const float ScaleDuration = 0.2f;

    //Инициализия ссылок на RectTransform, Canvas и dragParent
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        if (_canvas.transform.childCount > 1)
            _dragParent = _canvas.transform.GetChild(1) as RectTransform;
        else
            _dragParent = _canvas.transform as RectTransform;
    }

    //Обработка перетаскивания и отпускания фишки
    private void Update()
    {
        if (!_isDragging) return;

        OnDrag();

#if UNITY_ANDROID
        if (Input.touchCount == 0 || Input.GetTouch(0).phase == TouchPhase.Ended)
            OnDragEnd();
#else
        if (Input.GetMouseButtonUp(0))
            OnDragEnd();
#endif
    }

    //Начало drag по клику мыши
    private void OnMouseDown()
    {
        OnDragStart();
    }

    //Сохранение стартовой позиции и перенос фишки в dragParent
    public void OnDragStart()
    {
        _startAnchoredPos = _rectTransform.anchoredPosition;
        _originalParent = _rectTransform.parent;
        _isDragging = true;

        if (CurrentCell != null) CurrentCell.ClearPiece();

        _rectTransform.SetParent(_dragParent, true);
        _rectTransform.SetAsLastSibling();
        SetZ(1f);
    }

    //Движение фишки за курсором
    public void OnDrag()
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            Input.mousePosition,
            _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null,
            out localPoint);

        _rectTransform.anchoredPosition = localPoint;
        SetZ(1f);
    }

    //Определение куда отпустили фишку и выбор действия
    public void OnDragEnd()
    {
        _isDragging = false;

        Cell targetCell = GetCellUnderMouse();

        if (targetCell == null || targetCell == CurrentCell)
        {
            ReturnToCell();
            return;
        }

        if (targetCell.IsEmpty())
        {
            MoveToCell(targetCell);
        }
        else if (targetCell.CurrentPiece.TryGetComponent(out Piece other) &&
                 other.CurrentLevelSO.Level == CurrentLevelSO.Level)
        {
            Merge(targetCell, other);
        }
        else
        {
            ReturnToCell();
        }
    }

    //Перемещение фишки в новую клетку
    private void MoveToCell(Cell cell)
    {
        cell.SetPiece(gameObject);
        CurrentCell = cell;
        SetZ(0f);
    }

    //Возвращение фишки обратно в текущую клетку
    private void ReturnToCell()
    {
        if (CurrentCell != null)
        {
            CurrentCell.SetPiece(gameObject);
        }
        else
        {
            _rectTransform.SetParent(_originalParent, false);
            _rectTransform.anchoredPosition = _startAnchoredPos;
            SetZ(0f);
        }
    }

    //Объединение двух фишек одинакового уровня
    private void Merge(Cell cell, Piece other)
    {
        if (CurrentLevelSO.NextLevelSO == null)
        {
            ReturnToCell();
            return;
        }

        GameObject newPiece =
            Instantiate(CurrentLevelSO.NextLevelSO.Prefab, cell.transform);

        cell.SetPiece(newPiece);

        Destroy(other.gameObject);
        Destroy(gameObject);

        AnimateMerge(newPiece.GetComponent<RectTransform>());
    }

    //Анимация объединения
    private void AnimateMerge(RectTransform rect)
    {
        rect.DOKill(true);

        rect.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Append(rect.DOScale(Vector3.one * ScaleFactor, ScaleDuration)
                       .SetEase(Ease.OutBack));
        seq.Append(rect.DOScale(Vector3.one, ScaleDuration)
                       .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            rect.localScale = Vector3.one;
        });
    }

    //Принудительная z позиция
    private void SetZ(float z)
    {
        Vector3 pos = _rectTransform.localPosition;
        pos.z = z;
        _rectTransform.localPosition = pos;
    }

    //Поиск ближайшей клетки под курсором
    private Cell GetCellUnderMouse()
    {
        Cell[] cells = FindObjectsOfType<Cell>();
        Vector2 pointer = Input.mousePosition;

        float min = float.MaxValue;
        Cell closest = null;

        foreach (var cell in cells)
        {
            Vector2 screen =
                RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, cell.transform.position);

            float dist = Vector2.Distance(pointer, screen);
            if (dist < min)
            {
                min = dist;
                closest = cell;
            }
        }

        return min < 100f ? closest : null;
    }

    //Обновление ссылки на текущую клетку
    public void SetCurrentCell(Cell cell)
    {
        CurrentCell = cell;
    }
}
