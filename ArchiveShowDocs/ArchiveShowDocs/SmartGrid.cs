using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace ArchiveShowDocs
{
    /// <summary>
    /// SmartGrid: UserControl genérico para mostrar listas/tablas con:
    /// - Definición de columnas por especificaciones (texto, imagen, botón)
    /// - Origen de datos tipado o no tipado
    /// - Filtro rápido integrado (texto) + filtro personalizado (predicate)
    /// - Estilos por fila/celda mediante delegados
    /// - Eventos de selección/activación de ítems
    /// 
    /// Filosofía: como tu smartgrid de VFP, pero portable a cualquier dataset.
    /// </summary>
    public class SmartGrid : UserControl
    {
        // ---------- API PÚBLICA ----------
        public event EventHandler<object> ItemActivated;            // Doble clic/Enter
        public event EventHandler<object> SelectedItemChanged;      // Cambio de selección

        /// <summary> Muestra la barra de filtro rápido. </summary>
        public bool QuickFilterVisible
        {
            get => _pnlTop.Visible;
            set => _pnlTop.Visible = value;
        }

        /// <summary> Delegado de filtro adicional (AND con filtro rápido). </summary>
        public Func<object, bool> CustomFilter { get; set; }

        /// <summary> Estilo por fila (texto/fore/back/font/etc.). </summary>
        public Func<object, DataGridViewCellStyle> RowStyleProvider { get; set; }

        /// <summary> Estilo por celda: (item, columnName) -> style. </summary>
        public Func<object, string, DataGridViewCellStyle> CellStyleProvider { get; set; }

        /// <summary> Ítem seleccionado (objeto del origen). </summary>
        public object SelectedItem => _bs.Current;

        /// <summary> Acceso al DataGridView por si quieres enganchar eventos finos. </summary>
        public DataGridView Grid => _dgv;

        /// <summary> Vacía y aplica nuevas columnas. </summary>
        public void SetColumns(params ColumnSpec[] columns)
        {
            _columns = columns?.ToList() ?? new List<ColumnSpec>();
            BuildColumns();
        }

        /// <summary> Establece el origen de datos (lista de lo que sea). </summary>
        public void SetData<T>(IEnumerable<T> items)
        {
            _allRows = (items ?? Enumerable.Empty<T>()).Cast<object>().ToList();
            ApplyFilter(); // aplica quick+custom
        }

        /// <summary> Refresca estilos (p.e., tras cambiar estados/colores). </summary>
        public void RefreshStyles()
        {
            _dgv.Invalidate();
        }

        // ---------- IMPLEMENTACIÓN ----------
        readonly Panel _pnlTop;
        readonly TextBox _txtFilter;
        readonly Button _btnClear;
        readonly BindingSource _bs;
        readonly DataGridView _dgv;

        List<object> _allRows = new List<object>();
        List<ColumnSpec> _columns = new List<ColumnSpec>();

        // --- CueBanner (placeholder compatible .NET Framework) ---
        const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        private static void SetCueBanner(TextBox box, string text)
        {
            if (box.IsHandleCreated)
                SendMessage(box.Handle, EM_SETCUEBANNER, 0, text);
            else
                box.HandleCreated += (s, e) => SendMessage(box.Handle, EM_SETCUEBANNER, 0, text);
        }


        public SmartGrid()
        {
            DoubleBuffered = true;

            // Top (filtro rápido)
            _pnlTop = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(6, 6, 6, 6) };
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _txtFilter = new TextBox { Dock = DockStyle.Fill };

            _txtFilter = new TextBox { Dock = DockStyle.Fill };
            SetCueBanner(_txtFilter, "Filtrar...");   // <-- sustituto de PlaceholderText

            //_txtFilter.PlaceholderText = "Filtrar…";
            _btnClear = new Button { Text = "×", Width = 32, Dock = DockStyle.Right };
            tl.Controls.Add(_txtFilter, 0, 0);
            tl.Controls.Add(_btnClear, 1, 0);
            _pnlTop.Controls.Add(tl);

            // Grid
            _bs = new BindingSource();
            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                MultiSelect = false
            };
            _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.Gainsboro;
            _dgv.DataSource = _bs;

            Controls.Add(_dgv);
            Controls.Add(_pnlTop);

            // Eventos
            _txtFilter.TextChanged += (s, e) => ApplyFilter();
            _btnClear.Click += (s, e) => _txtFilter.Text = string.Empty;

            _dgv.SelectionChanged += (s, e) =>
            {
                SelectedItemChanged?.Invoke(this, _bs.Current);
            };

            _dgv.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) ItemActivated?.Invoke(this, _bs.Current);
            };

            _dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && _bs.Current != null)
                {
                    e.Handled = true;
                    ItemActivated?.Invoke(this, _bs.Current);
                }
            };

            _dgv.CellFormatting += Dgv_CellFormatting;
            _dgv.RowPrePaint += Dgv_RowPrePaint;
        }

        void BuildColumns()
        {
            _dgv.Columns.Clear();

            foreach (var c in _columns)
            {
                DataGridViewColumn col;

                switch (c.Kind)
                {
                    case ColumnKind.Image:
                        col = new DataGridViewImageColumn
                        {
                            Name = c.Name,
                            HeaderText = c.Header,
                            ImageLayout = DataGridViewImageCellLayout.Zoom,
                            Width = c.Width > 0 ? c.Width : 28
                        };
                        break;

                    case ColumnKind.Button:
                        var bcol = new DataGridViewButtonColumn
                        {
                            Name = c.Name,
                            HeaderText = c.Header,
                            UseColumnTextForButtonValue = true,
                            Text = c.ButtonText ?? "…",
                            Width = c.Width > 0 ? c.Width : 64
                        };
                        col = bcol;
                        break;

                    default:
                        col = new DataGridViewTextBoxColumn
                        {
                            Name = c.Name,
                            HeaderText = c.Header,
                            DataPropertyName = c.PropertyName ?? c.Name,
                            AutoSizeMode = c.Fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.DisplayedCells
                        };
                        break;
                }

                col.SortMode = DataGridViewColumnSortMode.Automatic;
                col.ReadOnly = true;
                _dgv.Columns.Add(col);
            }
        }

        void ApplyFilter()
        {
            IEnumerable<object> q = _allRows;

            var text = _txtFilter.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var t = text.ToLowerInvariant();
                q = q.Where(RowMatchesQuickFilter(t));
            }
            if (CustomFilter != null)
            {
                q = q.Where(item => CustomFilter(item));
            }

            _bs.DataSource = q.ToList();
        }

        Func<object, bool> RowMatchesQuickFilter(string query)
        {
            // Estrategia simple: concatena los campos visibles de texto
            // Si una columna tiene ValueSelector, también la usamos.
            return item =>
            {
                foreach (var col in _columns)
                {
                    if (col.Kind != ColumnKind.Text) continue;

                    object value = null;

                    if (col.ValueSelector != null)
                        value = col.ValueSelector(item);
                    else if (!string.IsNullOrEmpty(col.PropertyName))
                        value = GetPropertyValue(item, col.PropertyName);
                    else
                        value = GetPropertyValue(item, col.Name);

                    if (value != null && value.ToString().ToLowerInvariant().Contains(query))
                        return true;
                }
                return false;
            };
        }

        void Dgv_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            var item = _bs.List.Count > e.RowIndex ? _bs[e.RowIndex] : null;
            if (item == null) return;

            if (RowStyleProvider != null)
            {
                var style = RowStyleProvider(item);
                if (style != null)
                {
                    var r = _dgv.Rows[e.RowIndex];
                    if (style.ForeColor != Color.Empty) r.DefaultCellStyle.ForeColor = style.ForeColor;
                    if (style.BackColor != Color.Empty) r.DefaultCellStyle.BackColor = style.BackColor;
                    if (style.SelectionForeColor != Color.Empty) r.DefaultCellStyle.SelectionForeColor = style.SelectionForeColor;
                    if (style.SelectionBackColor != Color.Empty) r.DefaultCellStyle.SelectionBackColor = style.SelectionBackColor;
                    if (style.Font != null) r.DefaultCellStyle.Font = style.Font;
                }
            }
        }

        void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var item = _bs.List.Count > e.RowIndex ? _bs[e.RowIndex] : null;
            if (item == null) return;

            var col = _columns.FirstOrDefault(c => c.Name == _dgv.Columns[e.ColumnIndex].Name);
            if (col == null) return;

            // Valor
            if (col.Kind == ColumnKind.Image)
            {
                e.Value = col.ImageSelector?.Invoke(item);
                e.FormattingApplied = true;
            }
            else if (col.ValueSelector != null)
            {
                e.Value = col.ValueSelector(item);
                e.FormattingApplied = true;
            }
            else if (!string.IsNullOrEmpty(col.Format) && e.Value != null)
            {
                try
                {
                    e.Value = string.Format("{0:" + col.Format + "}", e.Value);
                    e.FormattingApplied = true;
                }
                catch { /* ignore */ }
            }

            // Estilo por celda
            if (CellStyleProvider != null)
            {
                var style = CellStyleProvider(item, col.Name);
                if (style != null)
                {
                    var cellStyle = _dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Style;
                    if (style.ForeColor != Color.Empty) cellStyle.ForeColor = style.ForeColor;
                    if (style.BackColor != Color.Empty) cellStyle.BackColor = style.BackColor;
                    if (style.SelectionForeColor != Color.Empty) cellStyle.SelectionForeColor = style.SelectionForeColor;
                    if (style.SelectionBackColor != Color.Empty) cellStyle.SelectionBackColor = style.SelectionBackColor;
                    if (style.Font != null) cellStyle.Font = style.Font;
                }
            }
        }

        static object GetPropertyValue(object obj, string propertyPath)
        {
            if (obj == null || string.IsNullOrEmpty(propertyPath)) return null;
            var current = obj;
            foreach (var part in propertyPath.Split('.'))
            {
                if (current == null) return null;
                var t = current.GetType();
                var p = t.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p == null) return null;
                current = p.GetValue(current);
            }
            return current;
        }

        // ---------- Tipos auxiliares ----------
        public enum ColumnKind { Text, Image, Button }

        /// <summary>
        /// Especificación de columna.
        /// - Name: nombre interno único (y key para estilos).
        /// - Header: texto cabecera.
        /// - Kind: Text/Image/Button.
        /// - PropertyName: nombre de la propiedad a enlazar (para Text).
        /// - ValueSelector: proveedor alternativo del valor (si no hay PropertyName o quieres formatear).
        /// - ImageSelector: proveedor de imagen (para Image).
        /// - Format: formato string (ej. "dd/MM/yyyy", "N0").
        /// - Fill: si debe usar tamaño Fill.
        /// - Width: ancho fijo opcional.
        /// - ButtonText: texto en botones.
        /// </summary>
        public class ColumnSpec
        {
            public string Name { get; set; }
            public string Header { get; set; }
            public ColumnKind Kind { get; set; } = ColumnKind.Text;

            public string PropertyName { get; set; }      // para Text
            public Func<object, object> ValueSelector { get; set; }   // alternativa a PropertyName

            public Func<object, Image> ImageSelector { get; set; }    // para Image
            public string Format { get; set; }
            public bool Fill { get; set; }
            public int Width { get; set; }

            public string ButtonText { get; set; }        // para Button

            public static ColumnSpec Text(string name, string header, string property = null, bool fill = false, string format = null, int width = 0)
                => new ColumnSpec { Name = name, Header = header, PropertyName = property ?? name, Fill = fill, Format = format, Width = width, Kind = ColumnKind.Text };

            public static ColumnSpec Image(string name, string header, Func<object, Image> selector, int width = 28)
                => new ColumnSpec { Name = name, Header = header, ImageSelector = selector, Width = width, Kind = ColumnKind.Image };

            public static ColumnSpec Button(string name, string header, string text = "…", int width = 64)
                => new ColumnSpec { Name = name, Header = header, ButtonText = text, Width = width, Kind = ColumnKind.Button };
        }
    }
}
