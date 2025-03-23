using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// Форма для отображения и управления навигацией по карте здания.
/// </summary>
public class NavigationForm : Form
{
    private BuildingMap map = new BuildingMap();
    private PictureBox mapPictureBox;
    private ComboBox startComboBox;
    private ComboBox endComboBox;
    private Label instructionsLabel;
    private Button nextStepButton;
    private Button startNavigationButton;
    private Button endNavigationButton;
    private NumericUpDown floorFilter;
    private NumericUpDown buildingFilter;
    private List<Node> currentPath;
    private int currentStep = 0;
    private Bitmap mapBitmap;
    private double zoomLevel = 1.0;

    // Переменные для передвижения карты
    private bool isDraggingMap = false;
    private Point lastMousePosition;

    private Label startLabel;
    private Label endLabel;
    private Label floorLabel;
    private Label buildingLabel;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="NavigationForm"/>.
    /// </summary>
    public NavigationForm()
    {
        Load += (s, e) =>
        {
            Console.WriteLine("NavigationForm: Начало инициализации");
            InitializeComponents();
            LoadInitialMap();
            BeginInvoke(new Action(() =>
            {
                Refresh();
                Console.WriteLine("NavigationForm: Форма перерисована");
            }));
        };
    }

    /// <summary>
    /// Инициализирует компоненты формы.
    /// </summary>
    private void InitializeComponents()
    {
        Console.WriteLine("NavigationForm: InitializeComponents вызван");
        Size = new Size(800, 600);
        MinimumSize = new Size(600, 400);
        Text = "CampusCompass - Навигация";
        DoubleBuffered = true;

        startLabel = new Label { Text = "Начало:", Location = new Point(10, 10), Width = 50, Visible = true };
        endLabel = new Label { Text = "Конец:", Location = new Point(220, 10), Width = 50, Visible = true };
        floorLabel = new Label { Text = "Этаж:", Location = new Point(430, 10), Width = 50, Visible = true };
        buildingLabel = new Label { Text = "Корпус:", Location = new Point(550, 10), Width = 50, Visible = true };

        startComboBox = new ComboBox { Location = new Point(60, 10), Width = 150, Visible = true };
        endComboBox = new ComboBox { Location = new Point(270, 10), Width = 150, Visible = true };
        floorFilter = new NumericUpDown { Location = new Point(480, 10), Width = 50, Minimum = 1, Maximum = 10, Visible = true };
        buildingFilter = new NumericUpDown { Location = new Point(610, 10), Width = 50, Minimum = 1, Maximum = 5, Visible = true };

        mapPictureBox = new PictureBox
        {
            Location = new Point(10, 40),
            Size = new Size(780, 400),
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
            Visible = true
        };
        mapBitmap = new Bitmap(mapPictureBox.Width, mapPictureBox.Height);
        mapPictureBox.Image = mapBitmap;

        instructionsLabel = new Label
        {
            Location = new Point(10, 450),
            Size = new Size(780, 30),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right,
            Visible = true
        };
        nextStepButton = new Button
        {
            Text = "Следующий шаг",
            Location = new Point(10, 490),
            Size = new Size(150, 30),
            Enabled = false,
            Visible = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        startNavigationButton = new Button
        {
            Text = "В путь",
            Location = new Point(10, 490),
            Size = new Size(150, 30),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Visible = true
        };
        endNavigationButton = new Button
        {
            Text = "Завершить маршрут",
            Location = new Point(170, 490),
            Size = new Size(150, 30),
            Visible = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        Button editButton = new Button
        {
            Text = "Режим редактора",
            Location = new Point(330, 490),
            Size = new Size(150, 30),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Visible = true
        };
        Button saveButton = new Button
        {
            Text = "Сохранить",
            Location = new Point(490, 490),
            Size = new Size(150, 30),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Visible = true
        };
        Button loadButton = new Button
        {
            Text = "Загрузить",
            Location = new Point(650, 490),
            Size = new Size(150, 30),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Visible = true
        };

        startLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        endLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        floorLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        buildingLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        startComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        endComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        floorFilter.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        buildingFilter.Anchor = AnchorStyles.Left | AnchorStyles.Top;

        Controls.AddRange(new Control[] { mapPictureBox, startComboBox, endComboBox, floorFilter,
            buildingFilter, instructionsLabel, nextStepButton, startNavigationButton, endNavigationButton,
            editButton, saveButton, loadButton, startLabel, endLabel, floorLabel, buildingLabel });

        startComboBox.SelectedIndexChanged += CalculatePath;
        endComboBox.SelectedIndexChanged += CalculatePath;
        startNavigationButton.Click += StartNavigation;
        nextStepButton.Click += ShowNextStep;
        endNavigationButton.Click += EndNavigation;
        editButton.Click += OpenEditor;
        saveButton.Click += SaveMap;
        loadButton.Click += LoadMap;
        floorFilter.ValueChanged += FilterMap;
        buildingFilter.ValueChanged += FilterMap;
        mapPictureBox.Paint += DrawMap;
        mapPictureBox.MouseWheel += MapPictureBox_MouseWheel;
        mapPictureBox.MouseDown += MapPictureBox_MouseDown;
        mapPictureBox.MouseMove += MapPictureBox_MouseMove;
        mapPictureBox.MouseUp += MapPictureBox_MouseUp;
        Resize += (s, e) => mapPictureBox.Invalidate();
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки мыши на карте для начала перемещения карты.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MapPictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isDraggingMap = true;
            lastMousePosition = e.Location;
        }
    }

    /// <summary>
    /// Обрабатывает событие перемещения мыши для перемещения карты.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MapPictureBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDraggingMap)
        {
            int deltaX = (int)((e.X - lastMousePosition.X) / zoomLevel);
            int deltaY = (int)((e.Y - lastMousePosition.Y) / zoomLevel);

            // Смещаем все узлы карты
            foreach (var node in map.Nodes)
            {
                node.X += deltaX;
                node.Y += deltaY;
            }

            lastMousePosition = e.Location;
            mapPictureBox.Invalidate();
        }
    }

    /// <summary>
    /// Обрабатывает событие отпускания кнопки мыши для завершения перемещения карты.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MapPictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isDraggingMap = false;
        }
    }

    /// <summary>
    /// Обрабатывает событие прокрутки колесика мыши для изменения масштаба карты.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MapPictureBox_MouseWheel(object sender, MouseEventArgs e)
    {
        if (e.Delta > 0)
            zoomLevel *= 1.1;
        else
            zoomLevel /= 1.1;

        zoomLevel = Math.Max(0.5, Math.Min(zoomLevel, 3.0));
        mapPictureBox.Invalidate();
    }

    /// <summary>
    /// Обновляет отображение карты при изменении фильтров этажа или корпуса.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void FilterMap(object sender, EventArgs e)
    {
        mapPictureBox.Invalidate();
    }

    /// <summary>
    /// Отрисовывает карту с узлами, связями и текущим маршрутом.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void DrawMap(object sender, PaintEventArgs e)
    {
        if (mapBitmap.Width != mapPictureBox.Width || mapBitmap.Height != mapPictureBox.Height)
        {
            mapBitmap = new Bitmap(mapPictureBox.Width, mapPictureBox.Height);
        }

        using (Graphics g = Graphics.FromImage(mapBitmap))
        {
            g.Clear(Color.White);
            int currentFloor = (int)floorFilter.Value;
            int currentBuilding = (int)buildingFilter.Value;

            var visibleNodes = map.Nodes.Where(n => n.Floor == currentFloor && n.Building == currentBuilding);

            foreach (var node in visibleNodes)
            {
                foreach (var conn in node.Connections)
                {
                    if (conn.Key.Floor == currentFloor && conn.Key.Building == currentBuilding)
                    {
                        using (var pen = new Pen(Color.Black, 2))
                        {
                            g.DrawLine(pen,
                                (float)(node.X * zoomLevel), (float)(node.Y * zoomLevel),
                                (float)(conn.Key.X * zoomLevel), (float)(conn.Key.Y * zoomLevel));
                        }
                    }
                }
            }

            foreach (var node in visibleNodes)
            {
                Brush brush = node.IsSelected ? Brushes.Red :
                            node.Type == NodeType.Staircase ? Brushes.Green :
                            node.Type == NodeType.BuildingTransition ? Brushes.Purple :
                            Brushes.Blue;
                float nodeSize = (float)(20 * zoomLevel);
                if (node.Type == NodeType.Staircase)
                    g.FillRectangle(brush, (float)(node.X * zoomLevel - nodeSize / 2),
                        (float)(node.Y * zoomLevel - nodeSize / 2), nodeSize, nodeSize);
                else
                    g.FillEllipse(brush, (float)(node.X * zoomLevel - nodeSize / 2),
                        (float)(node.Y * zoomLevel - nodeSize / 2), nodeSize, nodeSize);
                g.DrawString(node.Name, new Font("Arial", (float)(10 * zoomLevel)), Brushes.Black,
                    (float)(node.X * zoomLevel + nodeSize / 2), (float)(node.Y * zoomLevel));
            }

            if (currentPath != null && currentStep < currentPath.Count)
            {
                for (int i = 0; i < currentStep && i < currentPath.Count - 1; i++)
                {
                    if (currentPath[i].Floor == currentFloor && currentPath[i].Building == currentBuilding &&
                        currentPath[i + 1].Floor == currentFloor && currentPath[i + 1].Building == currentBuilding)
                    {
                        using (var pen = new Pen(Color.Red, 4))
                        {
                            g.DrawLine(pen,
                                (float)(currentPath[i].X * zoomLevel), (float)(currentPath[i].X * zoomLevel),
                                (float)(currentPath[i + 1].X * zoomLevel), (float)(currentPath[i + 1].Y * zoomLevel));
                        }
                        g.DrawString((i + 1).ToString(), new Font("Arial", (float)(12 * zoomLevel), FontStyle.Bold),
                            Brushes.White, (float)(currentPath[i].X * zoomLevel - 5), (float)(currentPath[i].Y * zoomLevel - 5));
                    }
                }
            }
        }
        mapPictureBox.Image = mapBitmap;
    }

    /// <summary>
    /// Вычисляет маршрут между выбранными узлами.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void CalculatePath(object sender, EventArgs e)
    {
        if (startComboBox.SelectedItem != null && endComboBox.SelectedItem != null)
        {
            var start = (Node)startComboBox.SelectedItem;
            var end = (Node)endComboBox.SelectedItem;
            (currentPath, _) = Dijkstra.FindShortestPath(start, end, map);
            currentStep = 0;
            instructionsLabel.Text = "Нажмите 'В путь' для начала навигации";
            nextStepButton.Enabled = false;
            nextStepButton.Visible = false;
            endNavigationButton.Visible = false;
            startNavigationButton.Visible = true;
            mapPictureBox.Invalidate();
            Refresh();
            Application.DoEvents();
        }
    }

    /// <summary>
    /// Начинает навигацию по маршруту.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void StartNavigation(object sender, EventArgs e)
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            currentStep = 0;
            nextStepButton.Enabled = true;
            nextStepButton.Visible = true;
            endNavigationButton.Visible = true;
            startNavigationButton.Visible = false;

            var startNode = currentPath[0];
            floorFilter.Value = startNode.Floor;
            buildingFilter.Value = startNode.Building;

            ShowNextStep(null, null);
            Refresh();
            Application.DoEvents();
        }
    }

    /// <summary>
    /// Завершает текущий маршрут.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void EndNavigation(object sender, EventArgs e)
    {
        currentPath = null;
        currentStep = 0;
        instructionsLabel.Text = "Маршрут завершен. Выберите новый маршрут.";
        nextStepButton.Enabled = false;
        nextStepButton.Visible = false;
        endNavigationButton.Visible = false;
        startNavigationButton.Visible = true;
        mapPictureBox.Invalidate();
        Refresh();
        Application.DoEvents();
    }

    /// <summary>
    /// Показывает следующий шаг маршрута.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void ShowNextStep(object sender, EventArgs e)
    {
        if (currentPath != null && currentStep < currentPath.Count - 1)
        {
            var current = currentPath[currentStep];
            var next = currentPath[currentStep + 1];
            instructionsLabel.Text = GetInstruction(current, next);

            if (current.Floor != next.Floor)
            {
                floorFilter.Value = next.Floor;
            }

            CenterMapOnNode(next);

            currentStep++;
            mapPictureBox.Invalidate();
        }
        else
        {
            instructionsLabel.Text = "Вы пришли!";
            nextStepButton.Enabled = false;
            nextStepButton.Visible = false;
            endNavigationButton.Visible = false;
            startNavigationButton.Visible = true;
            Refresh();
            Application.DoEvents();
        }
    }

    /// <summary>
    /// Центрирует карту на указанном узле.
    /// </summary>
    /// <param name="node">Узел, на котором нужно центрировать карту.</param>
    private void CenterMapOnNode(Node node)
    {
        int centerX = mapPictureBox.Width / 2;
        int centerY = mapPictureBox.Height / 2;
        int offsetX = centerX - (int)(node.X * zoomLevel);
        int offsetY = centerY - (int)(node.Y * zoomLevel);

        foreach (var n in map.Nodes)
        {
            n.X = (int)((n.X * zoomLevel + offsetX) / zoomLevel);
            n.Y = (int)((n.Y * zoomLevel + offsetY) / zoomLevel);
        }
    }

    /// <summary>
    /// Генерирует текстовую инструкцию для перехода между двумя узлами.
    /// </summary>
    /// <param name="from">Текущий узел.</param>
    /// <param name="to">Следующий узел.</param>
    /// <returns>Текстовая инструкция для пользователя.</returns>
    private string GetInstruction(Node from, Node to)
    {
        if (from.Floor != to.Floor)
            return from.Floor < to.Floor ?
                $"Поднимитесь по лестнице на {to.Floor} этаж" :
                $"Спуститесь по лестнице на {to.Floor} этаж";

        if (from.Building != to.Building)
            return $"Перейдите в корпус {to.Building}";

        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        if (Math.Abs(dx) > Math.Abs(dy))
            return dx > 0 ? "Идите направо" : "Идите налево";
        return dy > 0 ? "Идите вниз" : "Идите вверх";
    }

    /// <summary>
    /// Открывает форму редактора карты.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OpenEditor(object sender, EventArgs e)
    {
        Console.WriteLine("NavigationForm: Открытие режима редактора");
        var editorForm = new EditorForm(map, mapBitmap);
        editorForm.ShowDialog();
        foreach (var node in map.Nodes)
        {
            node.IsSelected = false;
        }
        UpdateComboBoxes();
        mapPictureBox.Invalidate();
        Console.WriteLine("NavigationForm: Режим редактора закрыт");
    }

    /// <summary>
    /// Сохраняет карту в файл в формате JSON.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void SaveMap(object sender, EventArgs e)
    {
        using (var sfd = new SaveFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" })
        {
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Сериализуем объект map в JSON
                    string json = JsonConvert.SerializeObject(map, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Игнорируем циклические ссылки
                        Formatting = Formatting.Indented // Для читаемого формата
                    });

                    // Записываем JSON в файл
                    File.WriteAllText(sfd.FileName, json);
                    MessageBox.Show("Карта успешно сохранена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении карты: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    [Obsolete("Использование BinaryFormatter устарело из-за проблем безопасности. Используйте JSON-сериализацию.")]
    private void LoadMapLegacy(string fileName)
    {
        using (var fs = new FileStream(fileName, FileMode.Open))
        {
            var formatter = new BinaryFormatter();
            map = (BuildingMap)formatter.Deserialize(fs);
        }
    }

    /// <summary>
    /// Загружает карту из файла в формате JSON.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void LoadMap(object sender, EventArgs e)
    {
        using (var ofd = new OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" })
        {
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Читаем JSON из файла
                    string json = File.ReadAllText(ofd.FileName);

                    // Десериализуем JSON в объект BuildingMap
                    map = JsonConvert.DeserializeObject<BuildingMap>(json, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore // Игнорируем циклические ссылки
                    });

                    UpdateComboBoxes();
                    mapPictureBox.Invalidate();
                    MessageBox.Show("Карта успешно загружена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке карты: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    /// <summary>
    /// Обновляет списки начальных и конечных узлов в выпадающих меню.
    /// </summary>
    private void UpdateComboBoxes()
    {
        startComboBox.Items.Clear();
        endComboBox.Items.Clear();
        foreach (var node in map.Nodes)
        {
            startComboBox.Items.Add(node);
            endComboBox.Items.Add(node);
        }
    }

    /// <summary>
    /// Загружает начальную карту с узлами и связями.
    /// </summary>
    private void LoadInitialMap()
    {
        Console.WriteLine("NavigationForm: LoadInitialMap вызван");
        map.Nodes.Clear();

        // Первый этаж
        var entrance1 = new Node("Вход", 50, 350, 1, 1);
        var corridor1_1 = new Node("Коридор 1-1", 100, 350, 1, 1);
        var corridor1_2 = new Node("Коридор 1-2", 150, 350, 1, 1);
        var corridor1_3 = new Node("Коридор 1-3", 200, 350, 1, 1);
        var corridor1_4 = new Node("Коридор 1-4", 250, 350, 1, 1);
        var corridor1_5 = new Node("Коридор 1-5", 300, 350, 1, 1);
        var corridor1_6 = new Node("Коридор 1-6", 350, 350, 1, 1);
        var corridor1_7 = new Node("Коридор 1-7", 400, 350, 1, 1);
        var corridor1_8 = new Node("Коридор 1-8", 450, 350, 1, 1);
        var corridor1_9 = new Node("Коридор 1-9", 500, 350, 1, 1);
        var stairs1_left = new Node("Лестница 1-левая", 50, 300, 1, 1, NodeType.Staircase);
        var stairs1_right = new Node("Лестница 1-правая", 550, 300, 1, 1, NodeType.Staircase);
        var room_dekanat = new Node("Деканат", 100, 250, 1, 1); // Бывший "Лекала"
        var room4 = new Node("Кабинет 4", 150, 250, 1, 1);
        var room3 = new Node("Кабинет 3", 200, 250, 1, 1);
        var room2 = new Node("Кабинет 2", 250, 250, 1, 1);
        var room1 = new Node("Кабинет 1", 300, 250, 1, 1);
        var room_garderob = new Node("Гардероб", 400, 400, 1, 1); // Бывший "Гаражов"
        var room_tochka = new Node("Точка кипения", 500, 400, 1, 1); // Бывшая "Точка (пункционная)"

        // Второй этаж
        var stairs2_left = new Node("Лестница 2-левая", 50, 300, 2, 1, NodeType.Staircase);
        var stairs2_right = new Node("Лестница 2-правая", 550, 300, 2, 1, NodeType.Staircase);
        var corridor2_1 = new Node("Коридор 2-1", 150, 300, 2, 1);
        var corridor2_2 = new Node("Коридор 2-2", 250, 300, 2, 1);
        var corridor2_3 = new Node("Коридор 2-3", 350, 300, 2, 1);
        var corridor2_4 = new Node("Коридор 2-4", 450, 300, 2, 1);
        var corridor2_5 = new Node("Коридор 2-5", 550, 300, 2, 1);
        var corridor2_6 = new Node("Коридор 2-6", 650, 300, 2, 1);
        var room273 = new Node("273", 100, 150, 2, 1);
        var room271 = new Node("271", 150, 150, 2, 1);
        var room272 = new Node("272", 200, 150, 2, 1);
        var room270 = new Node("270", 250, 150, 2, 1);
        var room268 = new Node("268", 300, 150, 2, 1);
        var room266 = new Node("266", 350, 150, 2, 1);
        var room264 = new Node("264", 400, 150, 2, 1);
        var room262 = new Node("262", 450, 150, 2, 1);
        var room260 = new Node("260", 500, 150, 2, 1);
        var room269 = new Node("269", 300, 250, 2, 1);
        var room267 = new Node("267", 350, 250, 2, 1);
        var room265 = new Node("265", 400, 250, 2, 1);
        var room263 = new Node("263", 450, 250, 2, 1);
        var room261 = new Node("261", 500, 250, 2, 1);
        var room227 = new Node("227", 550, 200, 2, 1);
        var room276 = new Node("276", 150, 350, 2, 1);
        var room279 = new Node("279", 200, 350, 2, 1);

        // Добавление узлов в карту
        map.AddNode(entrance1);
        map.AddNode(corridor1_1);
        map.AddNode(corridor1_2);
        map.AddNode(corridor1_3);
        map.AddNode(corridor1_4);
        map.AddNode(corridor1_5);
        map.AddNode(corridor1_6);
        map.AddNode(corridor1_7);
        map.AddNode(corridor1_8);
        map.AddNode(corridor1_9);
        map.AddNode(stairs1_left);
        map.AddNode(stairs1_right);
        map.AddNode(room_dekanat);
        map.AddNode(room4);
        map.AddNode(room3);
        map.AddNode(room2);
        map.AddNode(room1);
        map.AddNode(room_garderob);
        map.AddNode(room_tochka);

        map.AddNode(stairs2_left);
        map.AddNode(stairs2_right);
        map.AddNode(corridor2_1);
        map.AddNode(corridor2_2);
        map.AddNode(corridor2_3);
        map.AddNode(corridor2_4);
        map.AddNode(corridor2_5);
        map.AddNode(corridor2_6);
        map.AddNode(room273);
        map.AddNode(room271);
        map.AddNode(room272);
        map.AddNode(room270);
        map.AddNode(room268);
        map.AddNode(room266);
        map.AddNode(room264);
        map.AddNode(room262);
        map.AddNode(room260);
        map.AddNode(room269);
        map.AddNode(room267);
        map.AddNode(room265);
        map.AddNode(room263);
        map.AddNode(room261);
        map.AddNode(room227);
        map.AddNode(room276);
        map.AddNode(room279);

        AdjustNodePositions();

        // Связи на первом этаже
        map.AddConnection(entrance1, corridor1_1, 50);
        map.AddConnection(corridor1_1, corridor1_2, 50);
        map.AddConnection(corridor1_2, corridor1_3, 50);
        map.AddConnection(corridor1_3, corridor1_4, 50);
        map.AddConnection(corridor1_4, corridor1_5, 50);
        map.AddConnection(corridor1_5, corridor1_6, 50);
        map.AddConnection(corridor1_6, corridor1_7, 50);
        map.AddConnection(corridor1_7, corridor1_8, 50);
        map.AddConnection(corridor1_8, corridor1_9, 50);
        map.AddConnection(corridor1_1, stairs1_left, 50);
        map.AddConnection(corridor1_9, stairs1_right, 50);
        map.AddConnection(corridor1_1, room_dekanat, 50);
        map.AddConnection(corridor1_2, room4, 50);
        map.AddConnection(corridor1_3, room3, 50);
        map.AddConnection(corridor1_4, room2, 50);
        map.AddConnection(corridor1_5, room1, 50);
        map.AddConnection(corridor1_7, room_garderob, 50);
        map.AddConnection(corridor1_8, room_tochka, 50);

        // Связи между этажами через лестницы
        map.AddConnection(stairs1_left, stairs2_left, 50); // Левая лестница
        map.AddConnection(stairs1_right, stairs2_right, 50); // Правая лестница

        // Связи на втором этаже
        map.AddConnection(stairs2_left, corridor2_1, 50);
        map.AddConnection(corridor2_1, corridor2_2, 100);
        map.AddConnection(corridor2_2, corridor2_3, 100);
        map.AddConnection(corridor2_3, corridor2_4, 100);
        map.AddConnection(corridor2_4, corridor2_5, 100);
        map.AddConnection(corridor2_5, corridor2_6, 100);
        map.AddConnection(corridor2_6, stairs2_right, 50);
        map.AddConnection(corridor2_1, room273, 150);
        map.AddConnection(corridor2_1, room271, 100);
        map.AddConnection(corridor2_1, room272, 50);
        map.AddConnection(corridor2_2, room270, 100);
        map.AddConnection(corridor2_2, room268, 50);
        map.AddConnection(corridor2_3, room266, 50);
        map.AddConnection(corridor2_3, room264, 50);
        map.AddConnection(corridor2_4, room262, 50);
        map.AddConnection(corridor2_4, room260, 50);
        map.AddConnection(corridor2_2, room269, 50);
        map.AddConnection(corridor2_3, room267, 50);
        map.AddConnection(corridor2_3, room265, 50);
        map.AddConnection(corridor2_4, room263, 50);
        map.AddConnection(corridor2_4, room261, 50);
        map.AddConnection(corridor2_5, room227, 50);
        map.AddConnection(corridor2_1, room276, 100);
        map.AddConnection(corridor2_1, room279, 50);

        UpdateComboBoxes();
        mapPictureBox.Invalidate();
        Console.WriteLine($"NavigationForm: Карта загружена, узлов: {map.Nodes.Count}");
    }

    /// <summary>
    /// Корректирует позиции узлов на карте для лучшего отображения.
    /// </summary>
    private void AdjustNodePositions()
    {
        foreach (var node in map.Nodes)
        {
            node.X = (int)(node.X * 1.35);
            node.Y = (int)(node.Y * 1.35);
        }
    }
}