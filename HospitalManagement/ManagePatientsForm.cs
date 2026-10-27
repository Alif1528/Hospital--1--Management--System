using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Admin: search, view and remove patient accounts.</summary>
    public class ManagePatientsForm : Form
    {
        private DataGridView grid;
        private TextBox searchBox;

        public ManagePatientsForm()
        {
            UI.Setup(this, "Manage patients", 1100, 640);

            grid = UI.Grid();
            searchBox = UI.Txt();
            searchBox.Width = 280;
            searchBox.KeyDown += delegate(object s, KeyEventArgs k)
            {
                if (k.KeyCode == Keys.Enter) { k.SuppressKeyPress = true; LoadPatients(); }
            };

            FlowLayoutPanel toolbar = UI.Flow(
                UI.Lbl("Search name, phone or ID:"),
                searchBox,
                UI.Btn("Search", delegate { LoadPatients(); }),
                UI.Btn("Show all", delegate { searchBox.Clear(); LoadPatients(); }, 1));

            FlowLayoutPanel footer = UI.Flow(
                UI.Btn("Reset password", ResetPassword, 1),
                UI.Btn("Delete selected", DeletePatient, 2));

            UI.Compose(this, "Patients", "Registered patient accounts", UI.ListLayout(toolbar, grid, footer));
            Load += delegate { LoadPatients(); };
        }

        private void LoadPatients()
        {
            UI.Run(delegate
            {
                string text = searchBox.Text.Trim();
                int id;
                if (!int.TryParse(text, out id)) id = -1;

                grid.DataSource = DatabaseConnection.Query(
                    "SELECT UserId AS ID, FullName AS Name, Email, Phone, Gender, " +
                    "DATEDIFF(YEAR, DateOfBirth, GETDATE()) AS Age, BloodGroup AS Blood, Address, " +
                    "CONVERT(VARCHAR(10), CreatedAt, 23) AS Registered " +
                    "FROM Users WHERE Role = 'Patient' " +
                    "AND (@q = '' OR FullName LIKE '%' + @q + '%' OR Phone LIKE '%' + @q + '%' OR UserId = @id) " +
                    "ORDER BY FullName",
                    "@q", text, "@id", id);
            });
        }

        private void DeletePatient(object sender, EventArgs e)
        {
            int id = UI.SelectedId(grid, "ID");
            if (id < 0) { UI.Warn("Select a patient first."); return; }
            if (!UI.Confirm("Delete " + UI.Selected(grid, "Name") + "? This cannot be undone.")) return;

            UI.Run(delegate
            {
                try
                {
                    DatabaseConnection.Execute("DELETE FROM Users WHERE UserId = @id AND Role = 'Patient'", "@id", id);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 547)
                    {
                        UI.Warn("This patient has appointments or bills on record, so the account cannot be deleted.");
                        return;
                    }
                    throw;
                }
                LoadPatients();
            });
        }

        private void ResetPassword(object sender, EventArgs e)
        {
            PasswordTools.Reset(UI.SelectedId(grid, "ID"), Convert.ToString(UI.Selected(grid, "Name")));
        }
    }

    /// <summary>Admin helper shared by the doctor and patient screens.</summary>
    static class PasswordTools
    {
        public const string TemporaryPassword = "123456";

        public static void Reset(int userId, string name)
        {
            if (userId < 0) { UI.Warn("Select a person first."); return; }
            if (!UI.Confirm("Reset the password for " + name + " to " + TemporaryPassword + "?")) return;

            UI.Run(delegate
            {
                DatabaseConnection.Execute("UPDATE Users SET PasswordHash = @h WHERE UserId = @id",
                    "@h", Security.Hash(TemporaryPassword), "@id", userId);
                UI.Info("Password reset to " + TemporaryPassword + ".\nAsk them to change it after logging in.");
            });
        }
    }
}
