using UnityEngine;

[CreateAssetMenu(fileName = "PieceLevelData", menuName = "Config/PieceLevel")]
public class PieceLevelSO : ScriptableObject
{
    [Header("Уровень фишки")]
    public int Level;

    [Header("Префаб для этого уровня")]
    public GameObject Prefab;
}
