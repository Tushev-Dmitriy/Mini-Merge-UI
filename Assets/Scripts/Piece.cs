using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class Piece : MonoBehaviour
{
    private Vector3 _startPosition;
    private Transform _originalParent;
    private bool _isDragging;
    private Camera _mainCamera;

    [Header("Текущий уровень")]
    public PieceLevelSO CurrentLevelSO;

    private const float ScaleFactor = 1.3f;
    private const float ScaleDuration = 0.2f;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        _startPosition = transform.position;
        _originalParent = transform.parent;
        _isDragging = true;
        transform.SetParent(null);
    }

    private void Update()
    {
        HandleDrag();
        HandleRelease();
    }

    private void HandleDrag()
    {
        if (!_isDragging) return;

#if UNITY_ANDROID
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(touch.position);
            worldPos.z = 0;
            transform.position = worldPos;
        }
#else
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0;
        transform.position = _mainCamera.ScreenToWorldPoint(mousePos);
#endif
    }

    private void HandleRelease()
    {
        if (!_isDragging) return;

#if UNITY_ANDROID
        if (Input.touchCount == 0) return;
        if (Input.GetTouch(0).phase != TouchPhase.Ended) return;
#else
        if (!Input.GetMouseButtonUp(0)) return;
#endif

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
        transform.SetParent(_originalParent);
        transform.localPosition = Vector3.zero;
    }

    private void Merge(Cell cell, Piece otherPiece)
    {
        if (CurrentLevelSO.NextLevelSO == null)
        {
            //Максимальный уровень
            ReturnToStart();
            return;
        }

        GameObject newPiece = Instantiate(CurrentLevelSO.NextLevelSO.Prefab, cell.transform.position, Quaternion.identity);
        newPiece.tag = $"Piece_{CurrentLevelSO.NextLevelSO.Level}";

        cell.SetPiece(newPiece);

        Destroy(otherPiece.gameObject);
        Destroy(gameObject);

        //Анимация dotween
        newPiece.transform.localScale = Vector3.zero;
        Sequence seq = DOTween.Sequence();
        seq.Append(newPiece.transform.DOScale(Vector3.one * ScaleFactor, ScaleDuration));
        seq.Append(newPiece.transform.DOScale(Vector3.one, ScaleDuration));
        seq.Join(newPiece.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 1));

        Image img = newPiece.GetComponent<Image>();
        if (img != null)
        {
            seq.Join(img.DOFade(1f, 0.5f));
        }

        //Установка данных для новой фишки
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
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(_mainCamera, rect.position);
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
