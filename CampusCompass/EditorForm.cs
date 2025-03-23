using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

/// <summary>
/// Форма для редактирования карты здания, позволяющая добавлять, удалять и изменять узлы и связи.
/// </summary>
public class EditorForm : Form
{
    private BuildingMap map;
    private PictureBox mapPictureBox;
    private Bitmap mapBitmap;
    private Node selectedNode;
    private Node connectionStart;
    private double zoomLevel = 1.0;
    private NumericUpDown floorFilter;
    private NumericUpDown buildingFilter;
    private TextBox nameInputBox;
    private TextBox nameBox;
    private ComboBox typeBox;

    // Переменные для передвижения карты
    private bool isDraggingMap = false;
    private Point lastMousePosition;

    private Label nameLabel;
    private Label floorLabel;
    private Label buildingLabel;
    private Label typeLabel;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="EditorForm"/>.
    /// </summary>
    /// <param name="map">Карта здания для редактирования.</param>
    /// <param name="bitmap">Изображение карты для отображения.</param>
    public EditorForm(BuildingMap map, Bitmap bitmap)
    {
        this.map = map;
        this.mapBitmap = bitmap;
        Load += (s, e) =>
        {
            Console.WriteLine("EditorForm: Начало инициализации");
            InitializeComponents();
            BeginInvoke(new Action(() =>
            {
                Refresh();
                Console.WriteLine("EditorForm: Форма перерисована");
            }));
        };
    }

    /// <summary>
    /// Инициализирует компоненты формы.
    /// </summary>
    private void InitializeComponents()
    {
        Console.WriteLine("EditorForm: InitializeComponents вызван");
        Size = new Size(900, 600);
        MinimumSize = new Size(700, 400);
        Text = "CampusCompass - Редактор";
        DoubleBuffered = true;

        mapPictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = mapBitmap,
            Visible = true
        };
        Controls.Add(mapPictureBox);

        var panel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 200,
            Visible = true
        };
        var addConnButton = new Button { Text = "Добавить связь", Top = 10, Left = 10, Width = 180, Visible = true };
        var removeButton = new Button { Text = "Удалить узел", Top = 40, Left = 10, Width = 180, Visible = true };
        floorLabel = new Label { Text = "Этаж:", Top = 70, Left = 10, Width = 50, Visible = true };
        floorFilter = new NumericUpDown { Top = 90, Left = 10, Width = 180, Minimum = 1, Maximum = 10, Visible = true };
        buildingLabel = new Label { Text = "Корпус:", Top = 120, Left = 10, Width = 50, Visible = true };
        buildingFilter = new NumericUpDown { Top = 140, Left = 10, Width = 180, Minimum = 1, Maximum = 5, Visible = true };
        nameLabel = new Label { Text = "Имя:", Top = 170, Left = 10, Width = 50, Visible = true };
        nameBox = new TextBox { Top = 190, Left = 10, Width = 180, Visible = true };
        typeLabel = new Label { Text = "Тип:", Top = 220, Left = 10, Width = 50, Visible = true };
        typeBox = new ComboBox
        {
            Top = 240,
            Left = 10,
            Width = 180,
            DataSource = Enum.GetValues(typeof(NodeType)),
            Visible = true
        };

        panel.Controls.AddRange(new Control[] { addConnButton, removeButton, floorLabel, floorFilter,
            buildingLabel, buildingFilter, nameLabel, nameBox, typeLabel, typeBox });
        Controls.Add(panel);

        nameInputBox = new TextBox
        {
            Visible = false,
            Width = 100,
            Height = 20
        };
        mapPictureBox.Controls.Add(nameInputBox);
        nameInputBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter && selectedNode != null)
            {
                selectedNode.Name = nameInputBox.Text;
                nameInputBox.Visible = false;
                UpdateControls();
                mapPictureBox.Invalidate();
            }
        };

        // Привязка событий
        mapPictureBox.MouseClick += MapClick;
        mapPictureBox.MouseMove += MapPictureBox_MouseMove;
        mapPictureBox.MouseWheel += MapPictureBox_MouseWheel;
        mapPictureBox.MouseDown += MapPictureBox_MouseDown;
        mapPictureBox.MouseUp += MapPictureBox_MouseUp;
        mapPictureBox.Paint += DrawMap;
        addConnButton.Click += StartConnection;
        removeButton.Click += RemoveNode;
        floorFilter.ValueChanged += (s, e) => mapPictureBox.Invalidate();
        buildingFilter.ValueChanged += (s, e) => mapPictureBox.Invalidate();
        nameBox.TextChanged += (s, e) => { if (selectedNode != null) { selectedNode.Name = nameBox.Text; mapPictureBox.Invalidate(); } };
        floorFilter.ValueChanged += (s, e) => { if (selectedNode != null) { selectedNode.Floor = (int)floorFilter.Value; mapPictureBox.Invalidate(); } };
        buildingFilter.ValueChanged += (s, e) => { if (selectedNode != null) { selectedNode.Building = (int)buildingFilter.Value; mapPictureBox.Invalidate(); } };
        typeBox.SelectedIndexChanged += (s, e) => { if (selectedNode != null) { selectedNode.Type = (NodeType)typeBox.SelectedItem; mapPictureBox.Invalidate(); } };

        Resize += (s, e) => mapPictureBox.Invalidate();
        FormClosing += (s, e) =>
        {
            if (selectedNode != null)
            {
                selectedNode.IsSelected = false;
            }
            Console.WriteLine("EditorForm: Форма закрывается");
        };
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки мыши на карте для начала перемещения карты или узла.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MapPictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Если узел не выбран, начинаем передвижение карты
            if (selectedNode == null)
            {
                isDraggingMap = true;
                lastMousePosition = e.Location;
            }
        }
        // Оставляем правую кнопку как альтернативный способ передвижения карты (опционально)
        else if (e.Button == MouseButtons.Right)
        {
            isDraggingMap = true;
            lastMousePosition = e.Location;
        }
    }

    /// <summary>
    /// Обрабатывает событие перемещения мыши для перемещения карты или узла.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MapPictureBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (selectedNode != null && !nameInputBox.Visible)
            {
                // Перемещение узла, если узел выбран
                selectedNode.X = Math.Max(0, Math.Min(mapPictureBox.Width - 10, (int)(e.X / zoomLevel)));
                selectedNode.Y = Math.Max(0, Math.Min(mapPictureBox.Height - 10, (int)(e.Y / zoomLevel)));
                mapPictureBox.Invalidate();
            }
            else if (isDraggingMap)
            {
                // Перемещение карты, если узел не выбран
                int deltaX = (int)((e.X - lastMousePosition.X) / zoomLevel);
                int deltaY = (int)((e.Y - lastMousePosition.Y) / zoomLevel);

                foreach (var node in map.Nodes)
                {
                    node.X += deltaX;
                    node.Y += deltaY;
                }

                lastMousePosition = e.Location;
                mapPictureBox.Invalidate();
            }
        }
        else if (e.Button == MouseButtons.Right && isDraggingMap)
        {
            // Перемещение карты с помощью правой кнопки (опционально)
            int deltaX = (int)((e.X - lastMousePosition.X) / zoomLevel);
            int deltaY = (int)((e.Y - lastMousePosition.Y) / zoomLevel);

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
        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
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
    /// Отрисовывает карту с узлами и связями.
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
        }
        mapPictureBox.Image = mapBitmap;
    }

    /// <summary>
    /// Обрабатывает клик мыши на карте для добавления или выбора узла.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MapClick(object sender, MouseEventArgs e)
    {
        var clickedNode = map.Nodes.FirstOrDefault(n =>
            Math.Abs(n.X * zoomLevel - e.X) < 10 * zoomLevel &&
            Math.Abs(n.Y * zoomLevel - e.Y) < 10 * zoomLevel);

        if (e.Button == MouseButtons.Left)
        {
            if (clickedNode == null)
            {
                if (selectedNode != null && nameInputBox.Visible)
                {
                    selectedNode.Name = nameInputBox.Text;
                    nameInputBox.Visible = false;
                }
                else if (selectedNode != null)
                {
                    selectedNode.IsSelected = false;
                    selectedNode = null;
                    UpdateControls(true);
                }
                else
                {
                    selectedNode = new Node($"Node{map.Nodes.Count + 1}", (int)(e.X / zoomLevel), (int)(e.Y / zoomLevel),
                        (int)floorFilter.Value, (int)buildingFilter.Value);
                    selectedNode.IsSelected = true;
                    map.AddNode(selectedNode);

                    nameInputBox.Location = new Point(e.X + 20, e.Y);
                    nameInputBox.Text = selectedNode.Name;
                    nameInputBox.Visible = true;
                    nameInputBox.Focus();
                }
            }
            else
            {
                if (selectedNode != null) selectedNode.IsSelected = false;
                selectedNode = clickedNode;
                selectedNode.IsSelected = true;
                UpdateControls();
            }
            mapPictureBox.Invalidate();
        }
    }

    /// <summary>
    /// Начинает процесс создания связи между двумя узлами.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void StartConnection(object sender, EventArgs e)
    {
        if (selectedNode != null)
        {
            if (connectionStart == null)
            {
                connectionStart = selectedNode;
                MessageBox.Show("Выберите второй узел для создания связи");
            }
            else if (connectionStart != selectedNode)
            {
                map.AddConnection(connectionStart, selectedNode, CalculateDistance(connectionStart, selectedNode));
                connectionStart = null;
                mapPictureBox.Invalidate();
            }
        }
    }

    /// <summary>
    /// Удаляет выбранный узел с карты.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void RemoveNode(object sender, EventArgs e)
    {
        if (selectedNode != null)
        {
            map.RemoveNode(selectedNode);
            selectedNode = null;
            UpdateControls(true);
            mapPictureBox.Invalidate();
        }
    }

    /// <summary>
    /// Обновляет элементы управления на основе выбранного узла.
    /// </summary>
    /// <param name="clear">Указывает, нужно ли очистить элементы управления.</param>
    private void UpdateControls(bool clear = false)
    {
        if (clear || selectedNode == null)
        {
            nameBox.Text = "";
            floorFilter.Value = 1;
            buildingFilter.Value = 1;
            typeBox.SelectedIndex = 0;
        }
        else
        {
            nameBox.Text = selectedNode.Name;
            floorFilter.Value = selectedNode.Floor;
            buildingFilter.Value = selectedNode.Building;
            typeBox.SelectedItem = selectedNode.Type;
        }
    }

    /// <summary>
    /// Вычисляет расстояние между двумя узлами.
    /// </summary>
    /// <param name="n1">Первый узел.</param>
    /// <param name="n2">Второй узел.</param>
    /// <returns>Расстояние между узлами.</returns>
    private int CalculateDistance(Node n1, Node n2)
    {
        return (int)Math.Sqrt(Math.Pow(n2.X - n1.X, 2) + Math.Pow(n2.Y - n1.Y, 2));
    }
}