using System;
using System.Collections.Generic;

/// <summary>
/// Представляет узел на карте здания (например, комнату, лестницу или переход между корпусами).
/// </summary>
[Serializable] // Добавляем атрибут для сериализации
public class Node
{
    /// <summary>
    /// Имя узла (например, "Комната 101").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Координата X узла на карте.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Координата Y узла на карте.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Этаж, на котором расположен узел.
    /// </summary>
    public int Floor { get; set; }

    /// <summary>
    /// Идентификатор корпуса, к которому относится узел.
    /// </summary>
    public int Building { get; set; }

    /// <summary>
    /// Тип узла (например, комната, лестница или переход между корпусами).
    /// </summary>
    public NodeType Type { get; set; }

    /// <summary>
    /// Указывает, выбран ли узел в данный момент.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Словарь связей с другими узлами, где ключ — узел назначения, а значение — расстояние.
    /// </summary>
    public Dictionary<Node, int> Connections { get; set; }

    /// <summary>
    /// Указывает, является ли узел коридором.
    /// </summary>
    public bool IsCorridor { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Node"/> с заданными параметрами.
    /// </summary>
    /// <param name="name">Имя узла.</param>
    /// <param name="x">Координата X узла.</param>
    /// <param name="y">Координата Y узла.</param>
    /// <param name="floor">Этаж узла.</param>
    /// <param name="building">Идентификатор корпуса.</param>
    /// <param name="type">Тип узла (по умолчанию <see cref="NodeType.Room"/>).</param>
    public Node(string name, int x, int y, int floor, int building, NodeType type = NodeType.Room)
    {
        Name = name;
        X = x;
        Y = y;
        Floor = floor;
        Building = building;
        Type = type;
        IsSelected = false;
        Connections = new Dictionary<Node, int>();
        IsCorridor = name.StartsWith("Коридор");
    }

    /// <summary>
    /// Возвращает строковое представление узла.
    /// </summary>
    /// <returns>Имя узла.</returns>
    public override string ToString()
    {
        return Name;
    }
}

/// <summary>
/// Перечисляет возможные типы узлов на карте.
/// </summary>
public enum NodeType
{
    /// <summary>
    /// Комната или кабинет.
    /// </summary>
    Room,

    /// <summary>
    /// Лестница, соединяющая этажи.
    /// </summary>
    Staircase,

    /// <summary>
    /// Переход между корпусами.
    /// </summary>
    BuildingTransition
}