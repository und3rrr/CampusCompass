using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;
using CampusCompass.Properties;

/// <summary>
/// Представляет карту здания, содержащую узлы (комнаты, лестницы и т.д.) и связи между ними.
/// </summary>
[Serializable]
public class BuildingMap
{
    /// <summary>
    /// Список всех узлов на карте.
    /// </summary>
    public List<Node> Nodes { get; set; }

    // Словарь для фоновых изображений по ключу "building_floor" (например, "1_1" для building 1, floor 1)
    public Dictionary<string, string> BackgroundImages { get; set; } = new Dictionary<string, string>();

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

    /// <summary>
    /// Добавляет фоновое изображение для указанного корпуса и этажа.
    /// </summary>
    /// <param name="building">Номер корпуса.</param>
    /// <param name="floor">Номер этажа.</param>
    /// <param name="filePath">Путь к файлу изображения или имя ресурса.</param>
    public void AddBackgroundImage(int building, int floor, string filePath)
    {
        string key = $"{building}_{floor}";
        BackgroundImages[key] = filePath;
    }

    /// <summary>
    /// Обновляет фоновое изображение для указанного корпуса и этажа.
    /// </summary>
    /// <param name="building">Номер корпуса.</param>
    /// <param name="floor">Номер этажа.</param>
    /// <param name="filePath">Путь к файлу изображения или имя ресурса.</param>
    public void UpdateBackgroundImage(int building, int floor, string filePath)
    {
        string key = $"{building}_{floor}";
        BackgroundImages[key] = filePath;
    }

    /// <summary>
    /// Получает фоновое изображение для указанного корпуса и этажа.
    /// </summary>
    /// <param name="building">Номер корпуса.</param>
    /// <param name="floor">Номер этажа.</param>
    /// <returns>Объект Bitmap, если изображение найдено, иначе null.</returns>
    public Bitmap GetBackgroundImage(int building, int floor)
    {
        string key = $"{building}_{floor}";
        if (BackgroundImages.TryGetValue(key, out string filePath))
        {
            if (File.Exists(filePath))
            {
                return new Bitmap(filePath);
            }
        }
        return null;
    }
}