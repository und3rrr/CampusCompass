using System;
using System.Collections.Generic;

/// <summary>
/// Реализует приоритетную очередь (min-heap) для элементов с заданным приоритетом.
/// </summary>
/// <typeparam name="TElement">Тип элементов в очереди.</typeparam>
/// <typeparam name="TPriority">Тип приоритета, должен реализовать <see cref="IComparable{TPriority}"/>.</typeparam>
public class CustomPriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private readonly List<(TElement Element, TPriority Priority)> elements = new List<(TElement, TPriority)>();

    /// <summary>
    /// Количество элементов в очереди.
    /// </summary>
    public int Count => elements.Count;

    /// <summary>
    /// Добавляет элемент в очередь с заданным приоритетом.
    /// </summary>
    /// <param name="element">Элемент для добавления.</param>
    /// <param name="priority">Приоритет элемента.</param>
    public void Enqueue(TElement element, TPriority priority)
    {
        elements.Add((element, priority));
        int index = elements.Count - 1;
        SiftUp(index);
    }

    /// <summary>
    /// Извлекает элемент с минимальным приоритетом из очереди.
    /// </summary>
    /// <returns>Элемент с минимальным приоритетом.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если очередь пуста.</exception>
    public TElement Dequeue()
    {
        if (elements.Count == 0)
        {
            throw new InvalidOperationException("Очередь пуста.");
        }

        var result = elements[0].Element;
        elements[0] = elements[elements.Count - 1];
        elements.RemoveAt(elements.Count - 1);

        if (elements.Count > 0)
        {
            SiftDown(0);
        }

        return result;
    }

    /// <summary>
    /// Выполняет просеивание вверх для поддержания структуры кучи.
    /// </summary>
    /// <param name="index">Индекс элемента для просеивания.</param>
    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (elements[parent].Priority.CompareTo(elements[index].Priority) <= 0)
            {
                break;
            }
            Swap(index, parent);
            index = parent;
        }
    }

    /// <summary>
    /// Выполняет просеивание вниз для поддержания структуры кучи.
    /// </summary>
    /// <param name="index">Индекс элемента для просеивания.</param>
    private void SiftDown(int index)
    {
        int minIndex = index;
        int leftChild;
        int rightChild;

        while (true)
        {
            leftChild = 2 * index + 1;
            rightChild = 2 * index + 2;

            if (leftChild < elements.Count && elements[leftChild].Priority.CompareTo(elements[minIndex].Priority) < 0)
            {
                minIndex = leftChild;
            }

            if (rightChild < elements.Count && elements[rightChild].Priority.CompareTo(elements[minIndex].Priority) < 0)
            {
                minIndex = rightChild;
            }

            if (minIndex == index)
            {
                break;
            }

            Swap(index, minIndex);
            index = minIndex;
        }
    }

    /// <summary>
    /// Меняет местами два элемента в очереди.
    /// </summary>
    /// <param name="i">Индекс первого элемента.</param>
    /// <param name="j">Индекс второго элемента.</param>
    private void Swap(int i, int j)
    {
        var temp = elements[i];
        elements[i] = elements[j];
        elements[j] = temp;
    }
}