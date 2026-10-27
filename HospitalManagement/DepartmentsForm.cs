using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Admin: manage hospital departments.</summary>
    public class DepartmentsForm : Form
    {
        private DataGridView grid;
        private TextBox nameBox;

        public DepartmentsForm()
        {
            UI.Setup(this, "Departments", 720, 560);

            grid = UI.Grid();
            nameBox = UI.Txt();
            nameBox.Width = 260;

            FlowLayoutPanel toolbar = UI.Flow(
                UI.Lbl("New department:"),
                nameBox,
                UI.Btn("Add", AddDepartment));
            FlowLayoutPanel footer = UI.Flow(UI.Btn("Delete selected", DeleteDepartment, 2));

            UI.Compose(this, "Departments", "Departments patients can choose when booking", UI.ListLayout(toolbar, grid, footer));
            Load += delegate { LoadDepartments(); };
        }

        private void LoadDepartments()
        {
            UI.Run(delegate
            {
                grid.DataSource = DatabaseConnection.Query(
                    "SELECT d.DepartmentId AS ID, d.Name, COUNT(doc.DoctorId) AS Doctors " +
                    "FROM Departments d LEFT JOIN Doctors doc ON doc.DepartmentId = d.DepartmentId " +
                    "GROUP BY d.DepartmentId, d.Name ORDER BY d.Name");
            });
        }

        private void AddDepartment(object sender, EventArgs e)
        {
            string name = nameBox.Text.Trim();
            if (name.Length < 2) { UI.Warn("Enter a department name."); return; }

            UI.Run(delegate
            {
                if (DatabaseConnection.Scalar("SELECT 1 FROM Departments WHERE Name = @n", "@n", name) != null)
                {
                    UI.Warn("That department already exists.");
                    return;
                }
                DatabaseConnection.Execute("INSERT INTO Departments (Name) VALUES (@n)", "@n", name);
                nameBox.Clear();
                LoadDepartments();
            });
        }

        private void DeleteDepartment(object sender, EventArgs e)
        {
            int id = UI.SelectedId(grid, "ID");
            if (id < 0) { UI.Warn("Select a department first."); return; }
            if (!UI.Confirm("Delete department " + UI.Selected(grid, "Name") + "?")) return;

            UI.Run(delegate
            {
                try
                {
                    DatabaseConnection.Execute("DELETE FROM Departments WHERE DepartmentId = @id", "@id", id);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 547)
                    {
                        UI.Warn("Doctors still belong to this department. Move or delete them first.");
                        return;
                    }
                    throw;
                }
                LoadDepartments();
            });
        }
    }
}
