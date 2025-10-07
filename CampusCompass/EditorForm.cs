using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

    private bool isAddingCorridor = false; // Флаг для режима добавления коридора
    private List<Node> corridorNodes = new List<Node>(); // Временный список для точек коридора
    private Node lastCorridorNode; // Последняя добавленная точка в коридоре

    // Переменные для передвижения карты
    private bool isDraggingMap = false;
    private Point lastMousePosition;

    private Label nameLabel;
    private Label floorLabel;
    private Label buildingLabel;
    private Label typeLabel;

    private Button finishCorridorButton; // Кнопка завершения коридора как поле класса

    // Смещение для вида карты
    private int offsetX = 0;
    private int offsetY = 0;

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
                AdjustView(); // Автоматическая настройка вида при загрузке
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
                mapPictureBox.Invalidate();
            }
        };

        // Кнопка для выбора фона
        var selectBgButton = new Button { Text = "Выбрать фон", Top = 270, Left = 10, Width = 180 };
        selectBgButton.Click += SelectBackground;

        // Кнопка для начала добавления коридора
        var addCorridorButton = new Button { Text = "Добавить коридор", Top = 300, Left = 10, Width = 180 };
        addCorridorButton.Click += StartAddCorridor;

        // Кнопка для завершения коридора
        finishCorridorButton = new Button { Text = "Завершить коридор", Top = 330, Left = 10, Width = 180 };
        finishCorridorButton.Click += FinishCorridor;
        finishCorridorButton.Enabled = false;

        panel.Controls.Add(selectBgButton);
        panel.Controls.Add(addCorridorButton);
        panel.Controls.Add(finishCorridorButton);

        // Привязка событий
        mapPictureBox.MouseClick += MapClick;
        mapPictureBox.MouseMove += MapPictureBox_MouseMove;
        mapPictureBox.MouseWheel += MapPictureBox_MouseWheel;
        mapPictureBox.MouseDown += MapPictureBox_MouseDown;
        mapPictureBox.MouseUp += MapPictureBox_MouseUp;
        mapPictureBox.Paint += DrawMap;
        addConnButton.Click += StartConnection;
        removeButton.Click += RemoveNode;
        floorFilter.ValueChanged += (s, e) => { mapPictureBox.Invalidate(); AdjustView(); };
        buildingFilter.ValueChanged += (s, e) => { mapPictureBox.Invalidate(); AdjustView(); };
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

    private void SelectBackground(object sender, EventArgs e)
    {
        using (var ofd = new OpenFileDialog { Filter = "Изображения (*.jpg;*.png;*.gif)|*.jpg;*.png;*.gif|Все файлы (*.*)|*.*" })
        {
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string filePath = ofd.FileName;
                int currentBuilding = (int)buildingFilter.Value;
                int currentFloor = (int)floorFilter.Value;

                string appImagesFolder = Path.Combine(Application.StartupPath, "Images");
                if (!Directory.Exists(appImagesFolder))
                {
                    Directory.CreateDirectory(appImagesFolder);
                }
                string newFilePath = Path.Combine(appImagesFolder, Path.GetFileName(filePath));
                File.Copy(filePath, newFilePath, true);

                map.UpdateBackgroundImage(currentBuilding, currentFloor, newFilePath);
                mapPictureBox.Invalidate();
            }
        }
    }

    private void StartAddCorridor(object sender, EventArgs e)
    {
        isAddingCorridor = true;
        corridorNodes.Clear();
        lastCorridorNode = null;
        MessageBox.Show("Кликните для добавления точек коридора. Для завершения нажмите 'Завершить коридор'.");
        finishCorridorButton.Enabled = true;
    }

    private void FinishCorridor(object sender, EventArgs e)
    {
        isAddingCorridor = false;
        if (corridorNodes.Count > 1)
        {
            for (int i = 0; i < corridorNodes.Count - 1; i++)
            {
                int distance = CalculateDistance(corridorNodes[i], corridorNodes[i + 1]);
                map.AddConnection(corridorNodes[i], corridorNodes[i + 1], distance);
            }
        }
        corridorNodes.Clear();
        lastCorridorNode = null;
        finishCorridorButton.Enabled = false;
        mapPictureBox.Invalidate();
    }

    private void MapClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (isAddingCorridor)
            {
                int x = (int)((e.X - offsetX) / zoomLevel);
                int y = (int)((e.Y - offsetY) / zoomLevel);
                var newNode = new Node($"Corridor_{corridorNodes.Count + 1}", x, y, (int)floorFilter.Value, (int)buildingFilter.Value);
                newNode.IsCorridor = true;
                map.AddNode(newNode);
                corridorNodes.Add(newNode);
                if (lastCorridorNode != null)
                {
                    int distance = CalculateDistance(lastCorridorNode, newNode);
                    map.AddConnection(lastCorridorNode, newNode, distance);
                }
                lastCorridorNode = newNode;
                mapPictureBox.Invalidate();
            }
            else
            {
                Node clickedNode = GetClickedNode(e);
                if (clickedNode != null)
                {
                    if (selectedNode == null)
                    {
                        selectedNode = clickedNode;
                        selectedNode.IsSelected = true;
                        UpdateControls();
                    }
                    else if (selectedNode != clickedNode)
                    {
                        if (connectionStart == null)
                        {
                            connectionStart = selectedNode;
                            selectedNode = clickedNode;
                            selectedNode.IsSelected = true;
                        }
                        else
                        {
                            int distance = CalculateDistance(selectedNode, clickedNode);
                            map.AddConnection(selectedNode, clickedNode, distance);
                            connectionStart = null;
                            selectedNode.IsSelected = false;
                            selectedNode = null;
                        }
                    }
                    else
                    {
                        selectedNode.IsSelected = false;
                        selectedNode = null;
                    }
                }
                else if (connectionStart != null)
                {
                    int x = (int)((e.X - offsetX) / zoomLevel);
                    int y = (int)((e.Y - offsetY) / zoomLevel);
                    var newNode = new Node($"Node_{map.Nodes.Count + 1}", x, y, (int)floorFilter.Value, (int)buildingFilter.Value);
                    map.AddNode(newNode);
                    int distance = CalculateDistance(connectionStart, newNode);
                    map.AddConnection(connectionStart, newNode, distance);
                    connectionStart.IsSelected = false;
                    connectionStart = null;
                }
                mapPictureBox.Invalidate();
            }
        }
    }

    private void StartConnection(object sender, EventArgs e)
    {
        if (selectedNode != null)
        {
            connectionStart = selectedNode;
            selectedNode.IsSelected = true;
            MessageBox.Show("Выберите второй узел для связи или кликните для добавления нового.");
        }
    }

    private void RemoveNode(object sender, EventArgs e)
    {
        if (selectedNode != null)
        {
            map.RemoveNode(selectedNode);
            selectedNode = null;
            mapPictureBox.Invalidate();
        }
    }

    private Node GetClickedNode(MouseEventArgs e)
    {
        foreach (var node in map.Nodes)
        {
            if (Math.Abs((node.X * zoomLevel) + offsetX - e.X) < 10 * zoomLevel &&
                Math.Abs((node.Y * zoomLevel) + offsetY - e.Y) < 10 * zoomLevel)
            {
                return node;
            }
        }
        return null;
    }

    private Tuple<Node, Node> GetClickedConnection(MouseEventArgs e)
    {
        int tolerance = 5;
        foreach (var node in map.Nodes)
        {
            foreach (var conn in node.Connections)
            {
                PointF p1 = new PointF((float)((node.X * zoomLevel) + offsetX), (float)((node.Y * zoomLevel) + offsetY));
                PointF p2 = new PointF((float)((conn.Key.X * zoomLevel) + offsetX), (float)((conn.Key.Y * zoomLevel) + offsetY));
                float dist = DistanceToLineSegment(new PointF(e.X, e.Y), p1, p2);
                if (dist <= tolerance)
                {
                    return new Tuple<Node, Node>(node, conn.Key);
                }
            }
        }
        return null;
    }

    private float DistanceToLineSegment(PointF p, PointF a, PointF b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        if (dx == 0 && dy == 0) return (float)Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));

        float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Max(0, Math.Min(1, t));

        float projX = a.X + t * dx;
        float projY = a.Y + t * dy;

        return (float)Math.Sqrt(Math.Pow(p.X - projX, 2) + Math.Pow(p.Y - projY, 2));
    }

    private void AddMidpointToConnection(Node from, Node to, MouseEventArgs e)
    {
        int midX = (from.X + to.X) / 2;
        int midY = (from.Y + to.Y) / 2;

        Node midNode = new Node($"MidNode{map.Nodes.Count + 1}", midX, midY, from.Floor, from.Building);
        midNode.IsCorridor = true;
        map.AddNode(midNode);

        map.RemoveConnection(from, to);
        int dist1 = CalculateDistance(from, midNode);
        int dist2 = CalculateDistance(midNode, to);
        map.AddConnection(from, midNode, dist1);
        map.AddConnection(midNode, to, dist2);

        mapPictureBox.Invalidate();
    }

    private void MapPictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            selectedNode = GetClickedNode(e);
            if (selectedNode != null)
            {
                selectedNode.IsSelected = true;
                lastMousePosition = e.Location;
            }
            else
            {
                isDraggingMap = true;
                lastMousePosition = e.Location;
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            isDraggingMap = true;
            lastMousePosition = e.Location;
        }
    }

    private void MapPictureBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
        {
            if (selectedNode != null && e.Button == MouseButtons.Left)
            {
                int deltaX = (int)((e.X - lastMousePosition.X) / zoomLevel);
                int deltaY = (int)((e.Y - lastMousePosition.Y) / zoomLevel);
                selectedNode.X += deltaX;
                selectedNode.Y += deltaY;
                lastMousePosition = e.Location;
                mapPictureBox.Invalidate();
            }
            else if (isDraggingMap)
            {
                int deltaX = e.X - lastMousePosition.X;
                int deltaY = e.Y - lastMousePosition.Y;
                offsetX += deltaX;
                offsetY += deltaY;
                lastMousePosition = e.Location;
                mapPictureBox.Invalidate();
            }
        }
    }

    private void MapPictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
        {
            isDraggingMap = false;
            if (selectedNode != null)
            {
                selectedNode.IsSelected = false;
                selectedNode = null;
            }
        }
    }

    private void MapPictureBox_MouseWheel(object sender, MouseEventArgs e)
    {
        double oldZoom = zoomLevel;
        if (e.Delta > 0)
            zoomLevel *= 1.1;
        else
            zoomLevel /= 1.1;

        zoomLevel = Math.Max(0.5, Math.Min(zoomLevel, 3.0));

        int mouseX = e.X;
        int mouseY = e.Y;
        offsetX = (int)(mouseX - (mouseX - offsetX) * (zoomLevel / oldZoom));
        offsetY = (int)(mouseY - (mouseY - offsetY) * (zoomLevel / oldZoom));

        mapPictureBox.Invalidate();
    }

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

            Bitmap bgImage = map.GetBackgroundImage(currentBuilding, currentFloor);
            if (bgImage != null)
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                int scaledWidth = (int)(bgImage.Width * zoomLevel);
                int scaledHeight = (int)(bgImage.Height * zoomLevel);
                g.DrawImage(bgImage, new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight));
            }

            var visibleNodes = map.Nodes.Where(n => n.Floor == currentFloor && n.Building == currentBuilding).ToList();

            foreach (var node in visibleNodes)
            {
                foreach (var conn in node.Connections)
                {
                    if (visibleNodes.Contains(conn.Key))
                    {
                        using (var pen = new Pen(Color.Black, 2))
                        {
                            g.DrawLine(pen,
                                (float)((node.X * zoomLevel) + offsetX),
                                (float)((node.Y * zoomLevel) + offsetY),
                                (float)((conn.Key.X * zoomLevel) + offsetX),
                                (float)((conn.Key.Y * zoomLevel) + offsetY));
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
                float drawX = (float)((node.X * zoomLevel) + offsetX - nodeSize / 2);
                float drawY = (float)((node.Y * zoomLevel) + offsetY - nodeSize / 2);
                if (node.Type == NodeType.Staircase)
                    g.FillRectangle(brush, drawX, drawY, nodeSize, nodeSize);
                else
                    g.FillEllipse(brush, drawX, drawY, nodeSize, nodeSize);
                g.DrawString(node.Name, new Font("Arial", (float)(10 * zoomLevel)), Brushes.Black,
                    (float)((node.X * zoomLevel) + offsetX + nodeSize / 2), (float)((node.Y * zoomLevel) + offsetY));
            }
        }
        mapPictureBox.Image = mapBitmap;
    }

    private void AdjustView()
    {
        zoomLevel = 1.0;

        if (map.Nodes.Count == 0) return;

        int currentFloor = (int)floorFilter.Value;
        int currentBuilding = (int)buildingFilter.Value;
        var visibleNodes = map.Nodes.Where(n => n.Floor == currentFloor && n.Building == currentBuilding).ToList();

        if (visibleNodes.Count == 0) return;

        int minX = visibleNodes.Min(n => n.X);
        int minY = visibleNodes.Min(n => n.Y);
        int maxX = visibleNodes.Max(n => n.X);
        int maxY = visibleNodes.Max(n => n.Y);

        double widthRatio = (double)mapPictureBox.Width / (maxX - minX + 100);
        double heightRatio = (double)mapPictureBox.Height / (maxY - minY + 100);
        zoomLevel = Math.Min(widthRatio, heightRatio);
        zoomLevel = Math.Max(0.5, Math.Min(zoomLevel, 1.0));

        int centerX = (minX + maxX) / 2;
        int centerY = (minY + maxY) / 2;
        int targetCenterX = mapPictureBox.Width / 2;
        int targetCenterY = mapPictureBox.Height / 2;

        offsetX = targetCenterX - (int)(centerX * zoomLevel);
        offsetY = targetCenterY - (int)(centerY * zoomLevel);

        mapPictureBox.Invalidate();
    }

    private int CalculateDistance(Node from, Node to)
    {
        double dx = from.X - to.X;
        double dy = from.Y - to.Y;
        return (int)Math.Sqrt(dx * dx + dy * dy);
    }

    private void UpdateControls()
    {
        if (selectedNode != null)
        {
            nameBox.Text = selectedNode.Name;
            floorFilter.Value = selectedNode.Floor;
            buildingFilter.Value = selectedNode.Building;
            typeBox.SelectedItem = selectedNode.Type;
        }
        else
        {
            nameBox.Text = "";
            floorFilter.Value = 1;
            buildingFilter.Value = 1;
            typeBox.SelectedIndex = -1;
        }
    }
}