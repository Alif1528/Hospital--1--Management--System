using System;
using System.Data;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>
    /// Patients see and pay their own bills. Admins see every bill, the totals,
    /// and can record a payment taken at the counter.
    /// </summary>
    public class BillsForm : Form
    {
        private DataGridView grid;
        private ComboBox statusBox, methodBox;
        private Label totalsLabel;

        public BillsForm()
        {
            UI.Setup(this, Session.Role == "Admin" ? "Billing" : "My bills", 1100, 640);

            grid = UI.Grid();
            statusBox = UI.Combo("All", "Unpaid", "Paid");
            statusBox.SelectedIndexChanged += delegate { LoadBills(); };
            methodBox = UI.Combo("Cash", "Card", "bKash", "Nagad");
            totalsLabel = UI.Lbl("");
            totalsLabel.Font = UI.Bold;

            FlowLayoutPanel toolbar = UI.Flow(
                UI.Lbl("Show:"), statusBox,
                UI.Btn("Refresh", delegate { LoadBills(); }, 1),
                totalsLabel);

            string payText = Session.Role == "Admin" ? "Record payment" : "Pay selected bill";
            FlowLayoutPanel footer = UI.Flow(UI.Lbl("Payment method:"), methodBox, UI.Btn(payText, Pay));

            UI.Compose(this, Session.Role == "Admin" ? "Billing & revenue" : "My bills",
                "A bill is created automatically when a doctor completes a visit",
                UI.ListLayout(toolbar, grid, footer));
            Load += delegate { LoadBills(); };
        }

        private void LoadBills()
        {
            UI.Run(delegate
            {
                grid.DataSource = DatabaseConnection.Query(
                    "SELECT b.BillId AS ID, CONVERT(VARCHAR(10), a.AppointmentDate, 23) AS [Visit date], " +
                    "p.FullName AS Patient, du.FullName AS Doctor, b.Amount, b.Status, " +
                    "b.PaymentMethod AS Method, CONVERT(VARCHAR(16), b.PaidAt, 120) AS [Paid at] " +
                    "FROM Bills b " +
                    "JOIN Appointments a ON a.AppointmentId = b.AppointmentId " +
                    "JOIN Users p ON p.UserId = b.PatientId " +
                    "JOIN Users du ON du.UserId = a.DoctorId " +
                    "WHERE (@status = 'All' OR b.Status = @status) " +
                    "AND (@role = 'Admin' OR b.PatientId = @uid) " +
                    "ORDER BY b.BillId DESC",
                    "@status", statusBox.SelectedItem.ToString(),
                    "@role", Session.Role,
                    "@uid", Session.UserId);
                UI.Money(grid, "Amount");
                if (Session.Role == "Patient") grid.Columns["Patient"].Visible = false;

                DataRow t = DatabaseConnection.Row(
                    "SELECT ISNULL(SUM(CASE WHEN Status = 'Unpaid' THEN Amount END), 0) AS Unpaid, " +
                    "ISNULL(SUM(CASE WHEN Status = 'Paid' THEN Amount END), 0) AS Paid " +
                    "FROM Bills WHERE (@role = 'Admin' OR PatientId = @uid)",
                    "@role", Session.Role, "@uid", Session.UserId);
                totalsLabel.Text = "   Unpaid: Tk " + Convert.ToDecimal(t["Unpaid"]).ToString("N2") +
                                   "     Paid: Tk " + Convert.ToDecimal(t["Paid"]).ToString("N2");
            });
        }

        private void Pay(object sender, EventArgs e)
        {
            int id = UI.SelectedId(grid, "ID");
            if (id < 0) { UI.Warn("Select a bill first."); return; }
            if (Convert.ToString(UI.Selected(grid, "Status")) == "Paid") { UI.Warn("This bill is already paid."); return; }

            string method = methodBox.SelectedItem.ToString();
            string amount = Convert.ToDecimal(UI.Selected(grid, "Amount")).ToString("N2");
            if (!UI.Confirm("Pay Tk " + amount + " by " + method + "?")) return;

            UI.Run(delegate
            {
                int n = DatabaseConnection.Execute(
                    "UPDATE Bills SET Status = 'Paid', PaymentMethod = @m, PaidAt = GETDATE() " +
                    "WHERE BillId = @id AND Status = 'Unpaid' AND (@role = 'Admin' OR PatientId = @uid)",
                    "@m", method, "@id", id, "@role", Session.Role, "@uid", Session.UserId);
                if (n == 0) UI.Warn("This bill could not be paid.");
                else UI.Info("Payment recorded. Thank you!");
                LoadBills();
            });
        }
    }
}
