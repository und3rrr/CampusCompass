using Xunit;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CampusCompass
{
    public class RouteTests
    {
        private BuildingMap map;

        public RouteTests()
        {
            map = new BuildingMap();
        }

        [Fact]
        public void Test_RouteCalculation_SimplePath()
        {
            // Тест 4: Построение маршрута в NavigationForm
            // Arrange: Создаем карту с тремя узлами A → B → C
            var nodeA = new Node("A", 0, 0, 1, 1);
            var nodeB = new Node("B", 100, 100, 1, 1);
            var nodeC = new Node("C", 200, 200, 1, 1);

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(nodeC);

            map.AddConnection(nodeA, nodeB, 10);
            map.AddConnection(nodeB, nodeC, 10);

            // Act: Строим маршрут от A до C
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeC, map);

            // Assert: Путь должен быть A → B → C, длина пути 20
            Assert.NotNull(path);
            Assert.Equal(3, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(nodeB, path[1]);
            Assert.Equal(nodeC, path[2]);
            Assert.Equal(20, (int)distance);
        }

        [Fact]
        public void Test_LongRouteCalculation_LargeNumberOfNodes()
        {
            // Тест 7: Тестирование длинных маршрутов
            // Arrange: Создаем цепочку из 100 узлов
            List<Node> nodes = new List<Node>();
            for (int i = 0; i < 100; i++)
            {
                var node = new Node($"Node{i}", i * 10, 0, 1, 1);
                nodes.Add(node);
                map.AddNode(node);
            }

            // Связываем узлы в цепочку: 0 → 1 → 2 → ... → 99
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                map.AddConnection(nodes[i], nodes[i + 1], 10);
            }

            // Act: Строим маршрут от первого узла до последнего
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodes[0], nodes[99], map);

            // Assert: Путь должен содержать все 100 узлов, длина пути 990
            Assert.NotNull(path);
            Assert.Equal(100, path.Count);
            Assert.Equal(nodes[0], path[0]);
            Assert.Equal(nodes[99], path[99]);
            Assert.Equal(990, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_SameStartAndEndNode()
        {
            // Тест 8: Тестирование случая, когда начальная и конечная точки совпадают
            // Arrange: Создаем один узел
            var nodeA = new Node("A", 0, 0, 1, 1);
            map.AddNode(nodeA);

            // Act: Строим маршрут от A до A
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeA, map);

            // Assert: Путь должен содержать только узел A, длина пути 0
            Assert.NotNull(path);
            Assert.Single(path); // Исправлено: было Assert.Equal(1, path.Count)
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(0, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_NoPathExists()
        {
            // Тест 9: Проверка случая, когда пути между узлами нет
            // Arrange: Создаем два несвязанных узла
            var nodeA = new Node("A", 0, 0, 1, 1);
            var nodeB = new Node("B", 100, 100, 1, 1);

            map.AddNode(nodeA);
            map.AddNode(nodeB);

            // Не добавляем связи между узлами

            // Act: Строим маршрут от A до B
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeB, map);

            // Assert: Путь должен содержать только начальный узел, так как пути нет
            Assert.NotNull(path);
            Assert.Single(path); // Исправлено: было Assert.Equal(1, path.Count)
            Assert.Equal(nodeA, path[0]);
            Assert.True(double.IsPositiveInfinity(distance)); // Расстояние должно быть бесконечным
        }

        [Fact]
        public void Test_RouteCalculation_MultiplePaths()
        {
            // Тест 10: Проверка выбора кратчайшего пути, если есть несколько маршрутов
            // Arrange: Создаем граф с несколькими путями
            // A → B → D (длина 20)
            // A → C → D (длина 30)
            var nodeA = new Node("A", 0, 0, 1, 1);
            var nodeB = new Node("B", 100, 100, 1, 1);
            var nodeC = new Node("C", 200, 0, 1, 1);
            var nodeD = new Node("D", 300, 100, 1, 1);

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(nodeC);
            map.AddNode(nodeD);

            map.AddConnection(nodeA, nodeB, 10);
            map.AddConnection(nodeB, nodeD, 10); // Путь A → B → D = 20
            map.AddConnection(nodeA, nodeC, 15);
            map.AddConnection(nodeC, nodeD, 15); // Путь A → C → D = 30

            // Act: Строим маршрут от A до D
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeD, map);

            // Assert: Путь должен быть A → B → D (кратчайший, длина 20)
            Assert.NotNull(path);
            Assert.Equal(3, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(nodeB, path[1]);
            Assert.Equal(nodeD, path[2]);
            Assert.Equal(20, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_WithCycle()
        {
            // Тест 11: Проверка маршрута в графе с циклами
            // Arrange: Создаем граф с циклом
            // A → B → C → D
            //   ↖     ↙
            //     D ← A (цикл)
            var nodeA = new Node("A", 0, 0, 1, 1);
            var nodeB = new Node("B", 100, 100, 1, 1);
            var nodeC = new Node("C", 200, 200, 1, 1);
            var nodeD = new Node("D", 300, 300, 1, 1);

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(nodeC);
            map.AddNode(nodeD);

            map.AddConnection(nodeA, nodeB, 10);
            map.AddConnection(nodeB, nodeC, 10);
            map.AddConnection(nodeC, nodeD, 10);
            map.AddConnection(nodeD, nodeA, 5); // Цикл
            map.AddConnection(nodeD, nodeB, 5); // Дополнительный цикл

            // Act: Строим маршрут от A до C
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeC, map);

            // Assert: Путь должен быть A → D → C, длина 15
            Assert.NotNull(path);
            Assert.Equal(15, (int)distance);
            Assert.Equal(3, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(nodeD, path[1]);
            Assert.Equal(nodeC, path[2]);
        }

        [Fact]
        public void Test_RouteCalculation_WithZeroDistance()
        {
            // Тест 12: Проверка маршрута с нулевыми расстояниями
            // Arrange: Создаем граф с нулевыми расстояниями
            // A → B → C → D
            // Расстояния: A-B = 0, B-C = 0, C-D = 10
            var nodeA = new Node("A", 0, 0, 1, 1);
            var nodeB = new Node("B", 100, 100, 1, 1);
            var nodeC = new Node("C", 200, 200, 1, 1);
            var nodeD = new Node("D", 300, 300, 1, 1);

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(nodeC);
            map.AddNode(nodeD);

            map.AddConnection(nodeA, nodeB, 0);
            map.AddConnection(nodeB, nodeC, 0);
            map.AddConnection(nodeC, nodeD, 10);

            // Act: Строим маршрут от A до D
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeD, map);

            // Assert: Путь должен быть A → B → C → D, длина 10
            Assert.NotNull(path);
            Assert.Equal(4, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(nodeB, path[1]);
            Assert.Equal(nodeC, path[2]);
            Assert.Equal(nodeD, path[3]);
            Assert.Equal(10, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_WithNegativeDistance()
        {
            // Тест 13: Проверка маршрута с отрицательными расстояниями
            // Arrange: Создаем граф с отрицательным расстоянием
            var nodeA = new Node("A", 0, 0, 1, 1);
            var nodeB = new Node("B", 100, 100, 1, 1);
            var nodeC = new Node("C", 200, 200, 1, 1);

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(nodeC);

            map.AddConnection(nodeA, nodeB, 10);

            // Act & Assert: Ожидаем исключение при попытке добавить отрицательное расстояние
            Assert.Throws<ArgumentException>(() =>
            {
                map.AddConnection(nodeB, nodeC, -5); // Отрицательное расстояние
            });
        }

        [Fact]
        public void Test_RouteCalculation_DifferentFloorsWithoutStaircase()
        {
            // Тест 14: Проверка маршрута между узлами на разных этажах без лестницы
            // Arrange: Создаем два узла на разных этажах без лестницы
            var nodeA = new Node("A", 0, 0, 1, 1, NodeType.Room); // Этаж 1
            var nodeB = new Node("B", 100, 100, 2, 1, NodeType.Room); // Этаж 2

            map.AddNode(nodeA);
            map.AddNode(nodeB);

            // Не добавляем лестницу (узел типа Staircase)

            // Act: Строим маршрут от A до B
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeB, map);

            // Assert: Путь должен содержать только начальный узел, так как пути нет
            Assert.NotNull(path);
            Assert.Single(path); // Исправлено: было Assert.Equal(1, path.Count)
            Assert.Equal(nodeA, path[0]);
            Assert.True(double.IsPositiveInfinity(distance)); // Расстояние должно быть бесконечным
        }

        [Fact]
        public void Test_RouteCalculation_LargeGraphPerformance()
        {
            // Тест 15: Проверка производительности на большом графе (10,000 узлов)
            // Arrange: Создаем граф с 10,000 узлов, соединенных в цепочку
            List<Node> nodes = new List<Node>();
            for (int i = 0; i < 10000; i++)
            {
                var node = new Node($"Node{i}", i * 10, 0, 1, 1);
                nodes.Add(node);
                map.AddNode(node);
            }

            // Связываем узлы в цепочку: 0 → 1 → 2 → ... → 9999
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                map.AddConnection(nodes[i], nodes[i + 1], 1);
            }

            // Act: Строим маршрут от первого узла до последнего
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodes[0], nodes[9999], map);

            // Assert: Путь должен содержать все 10,000 узлов, длина пути 9999
            Assert.NotNull(path);
            Assert.Equal(10000, path.Count);
            Assert.Equal(nodes[0], path[0]);
            Assert.Equal(nodes[9999], path[9999]);
            Assert.Equal(9999, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_MultipleStaircases()
        {
            // Тест 16: Проверка маршрута с несколькими лестницами (выбор оптимального пути)
            // Arrange: Создаем граф с двумя лестницами между этажами
            // Этаж 1: A → Staircase1 → Staircase2
            // Этаж 2: Staircase1' → Staircase2' → B
            // A → Staircase1 → Staircase1' → B (длина 30)
            // A → Staircase2 → Staircase2' → B (длина 20)
            var nodeA = new Node("A", 0, 0, 1, 1, NodeType.Room); // Этаж 1
            var staircase1 = new Node("Staircase1", 50, 50, 1, 1, NodeType.Staircase); // Этаж 1
            var staircase2 = new Node("Staircase2", 100, 100, 1, 1, NodeType.Staircase); // Этаж 1
            var staircase1Prime = new Node("Staircase1'", 50, 50, 2, 1, NodeType.Staircase); // Этаж 2
            var staircase2Prime = new Node("Staircase2'", 100, 100, 2, 1, NodeType.Staircase); // Этаж 2
            var nodeB = new Node("B", 200, 200, 2, 1, NodeType.Room); // Этаж 2

            map.AddNode(nodeA);
            map.AddNode(staircase1);
            map.AddNode(staircase2);
            map.AddNode(staircase1Prime);
            map.AddNode(staircase2Prime);
            map.AddNode(nodeB);

            map.AddConnection(nodeA, staircase1, 10);
            map.AddConnection(nodeA, staircase2, 5);
            map.AddConnection(staircase1, staircase1Prime, 5); // Переход по лестнице 1
            map.AddConnection(staircase2, staircase2Prime, 5); // Переход по лестнице 2
            map.AddConnection(staircase1Prime, nodeB, 15);
            map.AddConnection(staircase2Prime, nodeB, 10);

            // Act: Строим маршрут от A до B
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeB, map);

            // Assert: Путь должен быть A → Staircase2 → Staircase2' → B (кратчайший, длина 20)
            Assert.NotNull(path);
            Assert.Equal(4, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(staircase2, path[1]);
            Assert.Equal(staircase2Prime, path[2]);
            Assert.Equal(nodeB, path[3]);
            Assert.Equal(20, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_IsolatedEndNode()
        {
            // Тест 17: Проверка маршрута, когда конечный узел изолирован
            // Arrange: Создаем граф с изолированным конечным узлом
            var nodeA = new Node("A", 0, 0, 1, 1);
            var nodeB = new Node("B", 100, 100, 1, 1);
            var nodeC = new Node("C", 200, 200, 1, 1);

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(nodeC);

            // Связываем только A и B, C остается изолированным
            map.AddConnection(nodeA, nodeB, 10);

            // Act: Строим маршрут от A до C
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeC, map);

            // Assert: Путь должен содержать только начальный узел, так как C изолирован
            Assert.NotNull(path);
            Assert.Single(path); // Исправлено: было Assert.Equal(1, path.Count)
            Assert.Equal(nodeA, path[0]);
            Assert.True(double.IsPositiveInfinity(distance));
        }

        [Fact]
        public void Test_RouteCalculation_MultiplePathsSameLength()
        {
            // Тест 18: Проверка маршрута, когда есть несколько путей одинаковой длины
            // Arrange: Создаем граф с двумя путями одинаковой длины
            // A → B → D (длина 20)
            // A → C → D (длина 20)
            var nodeA = new Node("A", 0, 0, 1, 1);
            var nodeB = new Node("B", 100, 100, 1, 1);
            var nodeC = new Node("C", 200, 0, 1, 1);
            var nodeD = new Node("D", 300, 100, 1, 1);

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(nodeC);
            map.AddNode(nodeD);

            map.AddConnection(nodeA, nodeB, 10);
            map.AddConnection(nodeB, nodeD, 10); // Путь A → B → D = 20
            map.AddConnection(nodeA, nodeC, 10);
            map.AddConnection(nodeC, nodeD, 10); // Путь A → C → D = 20

            // Act: Строим маршрут от A до D
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeD, map);

            // Assert: Путь должен быть либо A → B → D, либо A → C → D, длина 20
            Assert.NotNull(path);
            Assert.Equal(20, (int)distance);
            Assert.Equal(3, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(nodeD, path[2]);
            Assert.True(
                path[1] == nodeB || // Путь A → B → D
                path[1] == nodeC    // Путь A → C → D
            );
        }

        [Fact]
        public void Test_RouteCalculation_OnlyThroughStaircase()
        {
            // Тест 19: Проверка маршрута, где узлы на разных этажах связаны только через лестницу
            // Arrange: Создаем граф с узлами на разных этажах
            // Этаж 1: A → B → Staircase1
            // Этаж 2: Staircase2 → C
            // Путь должен быть A → B → Staircase1 → Staircase2 → C
            var nodeA = new Node("A", 0, 0, 1, 1, NodeType.Room); // Этаж 1
            var nodeB = new Node("B", 50, 50, 1, 1, NodeType.Room); // Этаж 1
            var staircase1 = new Node("Staircase1", 100, 100, 1, 1, NodeType.Staircase); // Этаж 1
            var staircase2 = new Node("Staircase2", 100, 100, 2, 1, NodeType.Staircase); // Этаж 2
            var nodeC = new Node("C", 200, 200, 2, 1, NodeType.Room); // Этаж 2

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(staircase1);
            map.AddNode(staircase2);
            map.AddNode(nodeC);

            map.AddConnection(nodeA, nodeB, 5);
            map.AddConnection(nodeB, staircase1, 5);
            map.AddConnection(staircase1, staircase2, 5); // Переход между этажами
            map.AddConnection(staircase2, nodeC, 10);

            // Act: Строим маршрут от A до C
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeC, map);

            // Assert: Путь должен быть A → B → Staircase1 → Staircase2 → C, длина 25
            Assert.NotNull(path);
            Assert.Equal(5, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(nodeB, path[1]);
            Assert.Equal(staircase1, path[2]);
            Assert.Equal(staircase2, path[3]);
            Assert.Equal(nodeC, path[4]);
            Assert.Equal(25, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_MultiFloorWithCycles()
        {
            // Тест 20: Проверка маршрута с циклами и узлами на разных этажах
            // Arrange: Создаем граф с циклами и лестницами
            // Этаж 1: A → B → Staircase1
            // Этаж 2: Staircase2 → C → D
            // Цикл: C → D → C
            var nodeA = new Node("A", 0, 0, 1, 1, NodeType.Room); // Этаж 1
            var nodeB = new Node("B", 50, 50, 1, 1, NodeType.Room); // Этаж 1
            var staircase1 = new Node("Staircase1", 100, 100, 1, 1, NodeType.Staircase); // Этаж 1
            var staircase2 = new Node("Staircase2", 100, 100, 2, 1, NodeType.Staircase); // Этаж 2
            var nodeC = new Node("C", 200, 200, 2, 1, NodeType.Room); // Этаж 2
            var nodeD = new Node("D", 300, 300, 2, 1, NodeType.Room); // Этаж 2

            map.AddNode(nodeA);
            map.AddNode(nodeB);
            map.AddNode(staircase1);
            map.AddNode(staircase2);
            map.AddNode(nodeC);
            map.AddNode(nodeD);

            map.AddConnection(nodeA, nodeB, 5);
            map.AddConnection(nodeB, staircase1, 5);
            map.AddConnection(staircase1, staircase2, 5); // Переход между этажами
            map.AddConnection(staircase2, nodeC, 10);
            map.AddConnection(nodeC, nodeD, 10);
            // Убрали перезапись расстояния C → D
            // map.AddConnection(nodeD, nodeC, 5); // Цикл на этаже 2

            // Act: Строим маршрут от A до D
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeD, map);

            // Assert: Путь должен быть A → B → Staircase1 → Staircase2 → C → D, длина 35
            Assert.NotNull(path);
            Assert.Equal(6, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(nodeB, path[1]);
            Assert.Equal(staircase1, path[2]);
            Assert.Equal(staircase2, path[3]);
            Assert.Equal(nodeC, path[4]);
            Assert.Equal(nodeD, path[5]);
            Assert.Equal(35, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_SameCoordinatesDifferentFloors()
        {
            // Тест 21: Проверка маршрута с узлами на разных этажах, но с одинаковыми координатами
            // Arrange: Создаем граф с узлами, у которых одинаковые X, Y, но разные этажи
            // Этаж 1: A → Staircase1
            // Этаж 2: Staircase2 → B
            // A и B имеют одинаковые координаты
            var nodeA = new Node("A", 100, 100, 1, 1, NodeType.Room); // Этаж 1
            var staircase1 = new Node("Staircase1", 150, 150, 1, 1, NodeType.Staircase); // Этаж 1
            var staircase2 = new Node("Staircase2", 150, 150, 2, 1, NodeType.Staircase); // Этаж 2
            var nodeB = new Node("B", 100, 100, 2, 1, NodeType.Room); // Этаж 2

            map.AddNode(nodeA);
            map.AddNode(staircase1);
            map.AddNode(staircase2);
            map.AddNode(nodeB);

            map.AddConnection(nodeA, staircase1, 10);
            map.AddConnection(staircase1, staircase2, 5); // Переход между этажами
            map.AddConnection(staircase2, nodeB, 10);

            // Act: Строим маршрут от A до B
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeB, map);

            // Assert: Путь должен быть A → Staircase1 → Staircase2 → B, длина 25
            Assert.NotNull(path);
            Assert.Equal(4, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(staircase1, path[1]);
            Assert.Equal(staircase2, path[2]);
            Assert.Equal(nodeB, path[3]);
            Assert.Equal(25, (int)distance);
        }

        [Fact]
        public void Test_RouteCalculation_MultipleFloorsWithStaircases()
        {
            // Тест 22: Проверка маршрута между узлами на этажах 1 и 3 через этаж 2
            // Arrange: Создаем граф с узлами на трех этажах
            // Этаж 1: A → Staircase1
            // Этаж 2: Staircase2 → Staircase3
            // Этаж 3: Staircase4 → B
            var nodeA = new Node("A", 0, 0, 1, 1, NodeType.Room); // Этаж 1
            var staircase1 = new Node("Staircase1", 50, 50, 1, 1, NodeType.Staircase); // Этаж 1
            var staircase2 = new Node("Staircase2", 50, 50, 2, 1, NodeType.Staircase); // Этаж 2
            var staircase3 = new Node("Staircase3", 100, 100, 2, 1, NodeType.Staircase); // Этаж 2
            var staircase4 = new Node("Staircase4", 100, 100, 3, 1, NodeType.Staircase); // Этаж 3
            var nodeB = new Node("B", 200, 200, 3, 1, NodeType.Room); // Этаж 3

            map.AddNode(nodeA);
            map.AddNode(staircase1);
            map.AddNode(staircase2);
            map.AddNode(staircase3);
            map.AddNode(staircase4);
            map.AddNode(nodeB);

            map.AddConnection(nodeA, staircase1, 10);
            map.AddConnection(staircase1, staircase2, 5); // Переход с этажа 1 на этаж 2
            map.AddConnection(staircase2, staircase3, 5); // Связь на этаже 2
            map.AddConnection(staircase3, staircase4, 5); // Переход с этажа 2 на этаж 3
            map.AddConnection(staircase4, nodeB, 10);

            // Act: Строим маршрут от A до B
            (List<Node> path, double distance) = Dijkstra.FindShortestPath(nodeA, nodeB, map);

            // Assert: Путь должен быть A → Staircase1 → Staircase2 → Staircase3 → Staircase4 → B, длина 35
            Assert.NotNull(path);
            Assert.Equal(6, path.Count);
            Assert.Equal(nodeA, path[0]);
            Assert.Equal(staircase1, path[1]);
            Assert.Equal(staircase2, path[2]);
            Assert.Equal(staircase3, path[3]);
            Assert.Equal(staircase4, path[4]);
            Assert.Equal(nodeB, path[5]);
            Assert.Equal(35, (int)distance);
        }
    }
}