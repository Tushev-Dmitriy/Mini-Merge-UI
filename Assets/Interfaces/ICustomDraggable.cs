using UnityEngine;

public interface ICustomDraggable
{
    //Начало перетаскивания
    void OnDragStart();

    //Обновление позиции во время перетаскивания
    void OnDrag();

    //Завершение перетаскивания
    void OnDragEnd();
}
