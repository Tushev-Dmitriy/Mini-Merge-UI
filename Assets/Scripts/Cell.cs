using UnityEngine;

public class Cell : MonoBehaviour
{
    //Ссылка на фишку в клетке
    public GameObject CurrentPiece { get; private set; }

    //Проверка пустая ли клетка
    public bool IsEmpty() => CurrentPiece == null;

    //Установка фишки
    public void SetPiece(GameObject piece)
    {
        CurrentPiece = piece;
        piece.transform.SetParent(transform);
        piece.transform.localPosition = Vector3.zero;
    }

    //Очистка клетки
    public void ClearPiece() => CurrentPiece = null;
}
