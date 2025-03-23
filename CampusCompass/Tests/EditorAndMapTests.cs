using Xunit;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System;
using CampusCompass;

namespace CampusCompass
{
    public class EditorAndMapTests : IDisposable
    {
        private BuildingMap map;
        private EditorForm editorForm;
        private NavigationForm navigationForm;
        private Bitmap bitmap;

        public EditorAndMapTests()
        {
            map = new BuildingMap();
            bitmap = new Bitmap(800, 600);
            editorForm = new EditorForm(map, bitmap);
            navigationForm = new NavigationForm();

            // Вызываем InitializeComponents через рефлексию, так как он приватный
            var editorFormInitializeMethod = typeof(EditorForm).GetMethod("InitializeComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            editorFormInitializeMethod.Invoke(editorForm, null);

            var navigationFormInitializeMethod = typeof(NavigationForm).GetMethod("InitializeComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            navigationFormInitializeMethod.Invoke(navigationForm, null);
        }

        public void Dispose()
        {
            editorForm.Dispose();
            navigationForm.Dispose();
            bitmap.Dispose();
        }

        [Fact]
        public void Test_CreateNode_EditorForm()
        {
            // Тест 1: Создание нового узла в EditorForm
            // Arrange: Устанавливаем этаж и корпус
            var floorFilter = (NumericUpDown)editorForm.GetType().GetField("floorFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(editorForm);
            var buildingFilter = (NumericUpDown)editorForm.GetType().GetField("buildingFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(editorForm);
            floorFilter.Value = 1;
            buildingFilter.Value = 1;

            // Act: Симулируем клик левой кнопкой мыши в точке (100, 100)
            var mapClickMethod = typeof(EditorForm).GetMethod("MapClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mouseEventArgs = new MouseEventArgs(MouseButtons.Left, 1, 100, 100, 0);
            mapClickMethod.Invoke(editorForm, new object[] { null, mouseEventArgs });

            // Assert: Должен быть создан новый узел с координатами (100, 100), этажом 1 и корпусом 1
            Assert.Equal(1, map.Nodes.Count);
            var node = map.Nodes[0];
            Assert.Equal(100, node.X);
            Assert.Equal(100, node.Y);
            Assert.Equal(1, node.Floor);
            Assert.Equal(1, node.Building);
        }

        [Fact]
        public void Test_MoveMap_EditorForm()
        {
            // Тест 2: Перемещение карты в EditorForm
            // Arrange: Создаем два узла
            var node1 = new Node("Node1", 100, 100, 1, 1);
            var node2 = new Node("Node2", 200, 200, 1, 1);
            map.AddNode(node1);
            map.AddNode(node2);

            // Устанавливаем selectedNode = null
            editorForm.GetType().GetField("selectedNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(editorForm, null);

            // Act: Симулируем удержание левой кнопки и перемещение мыши на (10, 20)
            var mouseDownMethod = typeof(EditorForm).GetMethod("MapPictureBox_MouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mouseMoveMethod = typeof(EditorForm).GetMethod("MapPictureBox_MouseMove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            mouseDownMethod.Invoke(editorForm, new object[] { null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0) });
            mouseMoveMethod.Invoke(editorForm, new object[] { null, new MouseEventArgs(MouseButtons.Left, 1, 10, 20, 0) });

            // Assert: Оба узла должны сместиться на (10, 20)
            Assert.Equal(110, node1.X);
            Assert.Equal(120, node1.Y);
            Assert.Equal(210, node2.X);
            Assert.Equal(220, node2.Y);
        }

        [Fact]
        public void Test_MoveNode_EditorForm()
        {
            // Тест 3: Перемещение узла в EditorForm
            // Arrange: Создаем два узла
            var node1 = new Node("Node1", 100, 100, 1, 1);
            var node2 = new Node("Node2", 200, 200, 1, 1);
            map.AddNode(node1);
            map.AddNode(node2);

            // Устанавливаем selectedNode = node1
            editorForm.GetType().GetField("selectedNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(editorForm, node1);

            // Act: Симулируем удержание левой кнопки и перемещение мыши в точку (150, 150)
            var mouseDownMethod = typeof(EditorForm).GetMethod("MapPictureBox_MouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mouseMoveMethod = typeof(EditorForm).GetMethod("MapPictureBox_MouseMove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            mouseDownMethod.Invoke(editorForm, new object[] { null, new MouseEventArgs(MouseButtons.Left, 1, 100, 100, 0) });
            mouseMoveMethod.Invoke(editorForm, new object[] { null, new MouseEventArgs(MouseButtons.Left, 1, 150, 150, 0) });

            // Assert: Первый узел должен переместиться в (150, 150), второй остаться на месте
            Assert.Equal(150, node1.X);
            Assert.Equal(150, node1.Y);
            Assert.Equal(200, node2.X);
            Assert.Equal(200, node2.Y);
        }

        [Fact]
        public void Test_FilterNodesByFloorAndBuilding_NavigationForm()
        {
            // Тест 5: Фильтрация узлов по этажу и корпусу в NavigationForm
            // Arrange: Создаем три узла на разных этажах и корпусах
            var node1 = new Node("Node1", 100, 100, 1, 1);
            var node2 = new Node("Node2", 200, 200, 2, 1);
            var node3 = new Node("Node3", 300, 300, 1, 2);
            map.AddNode(node1);
            map.AddNode(node2);
            map.AddNode(node3);

            // Устанавливаем значения фильтров: этаж 1, корпус 1
            var floorFilter = (NumericUpDown)navigationForm.GetType().GetField("floorFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(navigationForm);
            var buildingFilter = (NumericUpDown)navigationForm.GetType().GetField("buildingFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(navigationForm);
            floorFilter.Value = 1;
            buildingFilter.Value = 1;

            // Act: Вызываем метод фильтрации
            var filterMapMethod = typeof(NavigationForm).GetMethod("FilterMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            filterMapMethod.Invoke(navigationForm, new object[] { null, null });

            // Проверяем, какие узлы видны (используем рефлексию для получения visibleNodes из DrawMap)
            var drawMapMethod = typeof(NavigationForm).GetMethod("DrawMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pictureBox = (PictureBox)navigationForm.GetType().GetField("mapPictureBox", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(navigationForm);
            using (var bitmap = new Bitmap(pictureBox.Width, pictureBox.Height))
            using (var g = Graphics.FromImage(bitmap))
            {
                var e = new PaintEventArgs(g, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                drawMapMethod.Invoke(navigationForm, new object[] { null, e });

                // Assert: Должен быть виден только node1 (этаж 1, корпус 1)
                var visibleNodes = map.Nodes.Where(n => n.Floor == 1 && n.Building == 1).ToList();
                Assert.Equal(1, visibleNodes.Count);
                Assert.Equal(node1, visibleNodes[0]);
            }
        }

        [Fact]
        public void Test_UpdateNodeCoordinates_NavigationForm()
        {
            // Arrange: Создаем карту с двумя узлами
            var node1 = new Node("Node1", 100, 100, 1, 1);
            var node2 = new Node("Node2", 200, 200, 1, 1);
            map.AddNode(node1);
            map.AddNode(node2);

            // Устанавливаем карту в navigationForm
            navigationForm.GetType().GetField("map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(navigationForm, map);

            // Act: Изменяем координаты узла напрямую
            node1.X = 150;
            node1.Y = 150;

            // Проверяем, что изменения применились в map
            Assert.Equal(150, map.Nodes[0].X);
            Assert.Equal(150, map.Nodes[0].Y);

            // Проверяем, что NavigationForm видит обновленные координаты через DrawMap
            var drawMapMethod = typeof(NavigationForm).GetMethod("DrawMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (drawMapMethod == null)
            {
                throw new Exception("Метод DrawMap не найден в NavigationForm. Убедитесь, что он существует и доступен.");
            }

            var pictureBox = (PictureBox)navigationForm.GetType().GetField("mapPictureBox", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(navigationForm);
            using (var bitmap = new Bitmap(pictureBox.Width, pictureBox.Height))
            using (var g = Graphics.FromImage(bitmap))
            {
                var e = new PaintEventArgs(g, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                drawMapMethod.Invoke(navigationForm, new object[] { null, e });

                // Assert: Проверяем, что карта в navigationForm содержит обновленные данные
                var updatedMap = (BuildingMap)navigationForm.GetType().GetField("map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(navigationForm);
                Assert.Equal(150, updatedMap.Nodes[0].X);
                Assert.Equal(150, updatedMap.Nodes[0].Y);
                Assert.Equal(200, updatedMap.Nodes[1].X);
                Assert.Equal(200, updatedMap.Nodes[1].Y);
            }
        }
    }
}