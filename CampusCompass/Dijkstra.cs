using System;
using System.Collections.Generic;

/// <summary>
/// Статический класс, реализующий алгоритм Дейкстры для поиска кратчайшего пути на карте здания.
/// </summary>
public static class Dijkstra
{
    /// <summary>
    /// Находит кратчайший путь между двумя узлами на карте здания с использованием алгоритма Дейкстры.
    /// </summary>
    /// <param name="start">Начальный узел маршрута.</param>
    /// <param name="end">Конечный узел маршрута.</param>
    /// <param name="map">Карта здания, содержащая все узлы и связи между ними.</param>
    /// <returns>Кортеж, содержащий список узлов (путь) и общую длину пути. Если путь невозможен, возвращается путь, содержащий только начальный узел, и бесконечное расстояние.</returns>
    public static (List<Node> path, double distance) FindShortestPath(Node start, Node end, BuildingMap map)
    {
        // Словарь для хранения минимальных расстояний от начального узла до каждого узла
        var distances = new Dictionary<Node, double>();
        // Словарь для хранения предыдущих узлов в кратчайшем пути
        var previous = new Dictionary<Node, Node>();
        // Приоритетная очередь для выбора узла с минимальным расстоянием
        var priorityQueue = new CustomPriorityQueue<Node, double>();

        // Инициализация
        foreach (var node in map.Nodes)
        {
            distances[node] = double.PositiveInfinity;
            previous[node] = null;
        }
        distances[start] = 0;
        priorityQueue.Enqueue(start, 0);

        while (priorityQueue.Count > 0)
        {
            // Извлекаем узел с минимальным расстоянием
            Node current = priorityQueue.Dequeue();

            // Если достигли конечного узла, завершаем поиск
            if (current == end) break;

            // Обрабатываем соседей текущего узла
            foreach (var neighbor in current.Connections)
            {
                // Проверяем, можно ли перейти к соседнему узлу
                if (current.Floor != neighbor.Key.Floor)
                {
                    // Если этажи разные, переход возможен только через узел типа Staircase
                    if (current.Type != NodeType.Staircase || neighbor.Key.Type != NodeType.Staircase)
                    {
                        continue; // Пропускаем этот переход
                    }
                }

                double newDistance = distances[current] + neighbor.Value;
                if (newDistance < distances[neighbor.Key])
                {
                    distances[neighbor.Key] = newDistance;
                    previous[neighbor.Key] = current;
                    // Обновляем приоритет в очереди
                    priorityQueue.Enqueue(neighbor.Key, newDistance);
                }
            }
        }

        // Восстановление пути
        List<Node> path = new List<Node>();
        Node step = end;

        // Если конечный узел не достижим (расстояние бесконечно), возвращаем путь только с начальным узлом
        if (double.IsPositiveInfinity(distances[end]))
        {
            path.Add(start);
        }
        else
        {
            while (step != null)
            {
                path.Add(step);
                step = previous.ContainsKey(step) ? previous[step] : null;
            }
            path.Reverse();
        }

        double totalDistance = distances[end];
        return (path, totalDistance);
    }
}