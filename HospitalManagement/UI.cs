using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Shared look and small layout helpers, so every screen is built the same way.</summary>
    static class UI
    {
        public static readonly Color Primary = ColorTranslator.FromHtml("#0F766E");
        public static readonly Color PrimaryDark = ColorTranslator.FromHtml("#115E59");
        public static readonly Color Soft = ColorTranslator.FromHtml("#F0FDFA");
        public static readonly Color Surface = ColorTranslator.FromHtml("#F8FAFC");
        public static readonly Color Ink = ColorTranslator.FromHtml("#0F172A");
        public static readonly Color Muted = ColorTranslator.FromHtml("#64748B");
        public static readonly Color Danger = ColorTranslator.FromHtml("#B91C1C");
        public static readonly Color Selection = ColorTranslator.FromHtml("#99F6E4");

        public static readonly Font Body = new Font("Segoe UI", 10F);
        public static readonly Font Bold = new Font("Segoe UI", 10F, FontStyle.Bold);

        // ---------- windows ----------

        public static void Setup(Form f, string title, int width, int height)
        {
            f.Text = title + " - Hospital Management System";
            f.ClientSize = new Size(width, height);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Font = Body;
            f.BackColor = Surface;
            f.ForeColor = Ink;
        }

        /// <summary>Teal header bar on top, content fills the rest.</summary>
        public static void Compose(Form f, string title, string subtitle, Control content)
        {
            content.Dock = DockStyle.Fill;

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 72;
            header.BackColor = Primary;

            Label t = new Label();
            t.Text = title;
            t.ForeColor = Color.White;
            t.Font = new Font("Segoe UI Semibold", 16F);
            t.AutoSize = true;
            t.UseMnemonic = false;
            t.Location = new Point(20, 8);

            Label s = new Label();
            s.Text = subtitle;
            s.ForeColor = ColorTranslator.FromHtml("#CCFBF1");
            s.Font = new Font("Segoe UI", 9.5F);
            s.AutoSize = true;
            s.UseMnemonic = false;
            s.Location = new Point(22, 44);

            header.Controls.Add(t);
            header.Controls.Add(s);

            // Fill control first, then the docked header (WinForms docks last-added first).
            f.Controls.Add(content);
            f.Controls.Add(header);
        }

        // ---------- controls ----------

        public static Label Lbl(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.UseMnemonic = false;
            l.Anchor = AnchorStyles.Left;
            l.Margin = new Padding(3, 8, 8, 8);
            return l;
        }

        public static Label MutedLabel(string text)
        {
            Label l = Lbl(text);
            l.ForeColor = Muted;
            return l;
        }

        public static TextBox Txt(bool password, bool multiline, int height)
        {
            TextBox t = new TextBox();
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Margin = new Padding(3, 6, 3, 6);
            t.UseSystemPasswordChar = password;
            if (multiline)
            {
                t.Multiline = true;
                t.ScrollBars = ScrollBars.Vertical;
                t.Height = height;
                t.MinimumSize = new Size(0, height);
            }
            return t;
        }

        public static TextBox Txt()
        {
            return Txt(false, false, 0);
        }

        public static ComboBox Combo(params string[] items)
        {
            ComboBox c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Margin = new Padding(3, 6, 3, 6);
            c.Items.AddRange(items);
            if (items.Length > 0) c.SelectedIndex = 0;
            return c;
        }

        /// <summary>kind: 0 = primary, 1 = outline, 2 = danger.</summary>
        public static Button Btn(string text, EventHandler click, int kind)
        {
            Button b = new Button();
            b.Text = text;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(110, 36);
            b.Padding = new Padding(10, 2, 10, 2);
            b.Margin = new Padding(4);
            b.FlatStyle = FlatStyle.Flat;
            b.Cursor = Cursors.Hand;
            b.Font = Bold;
            if (kind == 1)
            {
                b.BackColor = Color.White;
                b.ForeColor = Primary;
                b.FlatAppearance.BorderColor = Primary;
            }
            else if (kind == 2)
            {
                b.BackColor = Danger;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderColor = Danger;
            }
            else
            {
                b.BackColor = Primary;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderColor = Primary;
            }
            if (click != null) b.Click += click;
            return b;
        }

        public static Button Btn(string text, EventHandler click)
        {
            return Btn(text, click, 0);
        }

        public static DataGridView Grid()
        {
            DataGridView g = new DataGridView();
            g.Dock = DockStyle.Fill;
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.RowHeadersVisible = false;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 38;
            g.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
            g.ColumnHeadersDefaultCellStyle.Font = Bold;
            g.RowTemplate.Height = 30;
            g.AlternatingRowsDefaultCellStyle.BackColor = Soft;
            g.DefaultCellStyle.SelectionBackColor = Selection;
            g.DefaultCellStyle.SelectionForeColor = Ink;
            return g;
        }

        public static FlowLayoutPanel Flow(params Control[] items)
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.FlowDirection = FlowDirection.LeftToRight;
            p.WrapContents = true;
            p.AutoSize = true;
            p.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            p.Dock = DockStyle.Fill;
            p.Margin = new Padding(0);
            p.Controls.AddRange(items);
            return p;
        }

        // ---------- layouts ----------

        /// <summary>Toolbar on top, grid in the middle, optional footer buttons at the bottom.</summary>
        public static TableLayoutPanel ListLayout(Control toolbar, DataGridView grid, Control footer)
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.ColumnCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.Padding = new Padding(16);
            int row = 0;
            if (toolbar != null)
            {
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                toolbar.Dock = DockStyle.Fill;
                t.Controls.Add(toolbar, 0, row++);
            }
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            t.Controls.Add(grid, 0, row++);
            if (footer != null)
            {
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                footer.Dock = DockStyle.Fill;
                t.Controls.Add(footer, 0, row++);
            }
            t.RowCount = row;
            return t;
        }

        public static TableLayoutPanel FieldTable()
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.ColumnCount = 2;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.AutoSize = true;
            t.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            t.Dock = DockStyle.Fill;
            t.RowCount = 0;
            return t;
        }

        public static void AddField(TableLayoutPanel t, string label, Control c)
        {
            int row = t.RowCount;
            t.RowCount = row + 1;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.Controls.Add(Lbl(label), 0, row);
            c.Dock = DockStyle.Fill;
            t.Controls.Add(c, 1, row);
        }

        /// <summary>Fields on top, buttons under them, free space below.</summary>
        public static TableLayoutPanel FormLayout(TableLayoutPanel fields, Control buttons)
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.ColumnCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.Padding = new Padding(24);
            t.AutoScroll = true;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fields.Dock = DockStyle.Fill;
            buttons.Dock = DockStyle.Fill;
            t.Controls.Add(fields, 0, 0);
            t.Controls.Add(buttons, 0, 1);
            t.RowCount = 3;
            return t;
        }

        public static Panel StatCard(string caption, string value)
        {
            Panel p = new Panel();
            p.Size = new Size(210, 84);
            p.BackColor = Color.White;
            p.BorderStyle = BorderStyle.FixedSingle;
            p.Margin = new Padding(6);

            Label v = new Label();
            v.Text = value;
            v.Font = new Font("Segoe UI Semibold", 20F);
            v.ForeColor = Primary;
            v.AutoSize = true;
            v.Location = new Point(12, 8);

            Label c = new Label();
            c.Text = caption;
            c.ForeColor = Muted;
            c.AutoSize = true;
            c.Location = new Point(14, 52);

            p.Controls.Add(v);
            p.Controls.Add(c);
            return p;
        }

        // ---------- grids ----------

        /// <summary>The value of a column in the selected row, or null when nothing is selected.</summary>
        public static object Selected(DataGridView g, string column)
        {
            if (g.CurrentRow == null) return null;
            return g.CurrentRow.Cells[column].Value;
        }

        public static int SelectedId(DataGridView g, string column)
        {
            object v = Selected(g, column);
            if (v == null || v == DBNull.Value) return -1;
            return Convert.ToInt32(v);
        }

        public static void Money(DataGridView g, params string[] columns)
        {
            foreach (string c in columns)
            {
                if (g.Columns.Contains(c)) g.Columns[c].DefaultCellStyle.Format = "N2";
            }
        }

        // ---------- messages ----------

        public static void Info(string message)
        {
            MessageBox.Show(message, "Hospital Management System", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Warn(string message)
        {
            MessageBox.Show(message, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Error(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool Confirm(string message)
        {
            return MessageBox.Show(message, "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>Runs an action and shows a friendly message if the database or anything else fails.</summary>
        public static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 53 || ex.Number == 2 || ex.Number == 18456 || ex.Number == 4060)
                    Error("Cannot reach the database.\n\nCheck the connection string in DatabaseConnection.cs and that you ran Database\\schema.sql.\n\n" + ex.Message);
                else
                    Error("Database error: " + ex.Message);
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }
    }
}
