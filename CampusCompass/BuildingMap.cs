using System;
using System.Collections.Generic;

/// <summary>
/// Представляет карту здания, содержащую узлы (комнаты, лестницы и т.д.) и связи между ними.
/// </summary>
public class BuildingMap
{
    /// <summary>
    /// Список всех узлов на карте.
    /// </summary>
    public List<Node> Nodes { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BuildingMap"/> с пустым списком узлов.
    /// </summary>
    public BuildingMap()
    {
        Nodes = new List<Node>();
    }

    /// <summary>
    /// Добавляет узел на карту, если он еще не присутствует.
    /// </summary>
    /// <param name="node">Узел, который нужно добавить.</param>
    public void AddNode(Node node)
    {
        if (!Nodes.Contains(node))
        {
            Nodes.Add(node);
        }
    }

    /// <summary>
    /// Удаляет узел с карты и все связанные с ним связи.
    /// </summary>
    /// <param name="node">Узел, который нужно удалить.</param>
    public void RemoveNode(Node node)
    {
        if (Nodes.Contains(node))
        {
            // Удаляем все связи с этим узлом
            foreach (var otherNode in Nodes)
            {
                if (otherNode.Connections.ContainsKey(node))
                {
                    otherNode.Connections.Remove(node);
                }
            }
            Nodes.Remove(node);
        }
    }

    /// <summary>
    /// Добавляет связь между двумя узлами на карте (неориентированный граф).
    /// </summary>
    /// <param name="from">Узел, из которого начинается связь.</param>
    /// <param name="to">Узел, в который ведет связь.</param>
    /// <param name="distance">Расстояние между узлами.</param>
    /// <exception cref="ArgumentException">Выбрасывается, если расстояние отрицательное.</exception>
    public void AddConnection(Node from, Node to, int distance)
    {
        if (distance < 0)
        {
            throw new ArgumentException("Расстояние между узлами не может быть отрицательным.");
        }
        from.Connections[to] = distance;
        to.Connections[from] = distance; // Для неориентированного графа
    }

    /// <summary>
    /// Удаляет связь между двумя узлами.
    /// </summary>
    /// <param name="from">Узел, из которого начинается связь.</param>
    /// <param name="to">Узел, в который ведет связь.</param>
    public void RemoveConnection(Node from, Node to)
    {
        if (from != null && to != null)
        {
            // Удаляем двунаправленную связь
            if (from.Connections.ContainsKey(to))
            {
                from.Connections.Remove(to);
            }
            if (to.Connections.ContainsKey(from))
            {
                to.Connections.Remove(from);
            }
        }
    }
}