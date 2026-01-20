using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Все клетки на сцене")]
    public List<Cell> AllCells;

    //Спавн новой фишки рандомно в одну из пустых клеток
    public void SpawnPiece(PieceLevelSO levelSO)
    {
        if (levelSO == null) return;

        List<Cell> emptyCells = AllCells.Where(c => c.IsEmpty()).ToList();
        if (emptyCells.Count == 0)
        {
            Debug.LogWarning("No empty cells");
            return;
        }

        Cell chosenCell = emptyCells[Random.Range(0, emptyCells.Count)];

        GameObject newPiece = Instantiate(levelSO.Prefab, chosenCell.transform.position, Quaternion.identity);

        chosenCell.SetPiece(newPiece);

        newPiece.transform.localScale = Vector3.one;

        Piece pieceScript = newPiece.GetComponent<Piece>();
        if (pieceScript != null)
        {
            pieceScript.CurrentLevelSO = levelSO;
            pieceScript.CurrentLevelSO.NextLevelSO = levelSO.NextLevelSO;
            newPiece.tag = $"Piece_{levelSO.Level}";
        }
    }
}
