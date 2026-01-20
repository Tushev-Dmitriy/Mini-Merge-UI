using UnityEngine;

public class Cell : MonoBehaviour
{
    public GameObject CurrentPiece { get; private set; }

    public bool IsEmpty() => CurrentPiece == null;

    //Перемещение фишки в клетку и выравнивание RectTransform
    public void SetPiece(GameObject piece)
    {
        CurrentPiece = piece;

        RectTransform pieceRect = piece.GetComponent<RectTransform>();
        RectTransform cellRect = transform as RectTransform;

        pieceRect.SetParent(cellRect, false);
        pieceRect.anchoredPosition = Vector2.zero;
        pieceRect.localScale = Vector3.one;
        pieceRect.localRotation = Quaternion.identity;

        Vector3 pos = pieceRect.localPosition;
        pos.z = 0f;
        pieceRect.localPosition = pos;

        if (piece.TryGetComponent(out Piece p))
            p.SetCurrentCell(this);
    }

    //Очистка ссылки на фишку в клетке
    public void ClearPiece()
    {
        CurrentPiece = null;
    }
}
