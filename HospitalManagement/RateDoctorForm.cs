using System;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Patient rates a doctor once per completed visit.</summary>
    public class RateDoctorForm : Form
    {
        private ComboBox visitBox, scoreBox;
        private TextBox commentBox;

        public RateDoctorForm()
        {
            UI.Setup(this, "Rate a doctor", 600, 480);

            visitBox = UI.Combo();
            visitBox.DisplayMember = "Label";
            visitBox.ValueMember = "AppointmentId";
            scoreBox = UI.Combo("5 - Excellent", "4 - Good", "3 - Okay", "2 - Poor", "1 - Very poor");
            commentBox = UI.Txt(false, true, 90);

            TableLayoutPanel f = UI.FieldTable();
            UI.AddField(f, "Visit", visitBox);
            UI.AddField(f, "Rating", scoreBox);
            UI.AddField(f, "Comment", commentBox);

            FlowLayoutPanel buttons = UI.Flow(
                UI.Btn("Submit rating", Submit),
                UI.Btn("Close", delegate { Close(); }, 1));

            UI.Compose(this, "Rate a doctor", "Tell us about your completed visits", UI.FormLayout(f, buttons));
            Load += delegate { LoadVisits(); };
        }

        private void LoadVisits()
        {
            UI.Run(delegate
            {
                visitBox.DataSource = DatabaseConnection.Query(
                    "SELECT a.AppointmentId, CONVERT(VARCHAR(10), a.AppointmentDate, 23) + '  -  ' + du.FullName AS Label " +
                    "FROM Appointments a JOIN Users du ON du.UserId = a.DoctorId " +
                    "WHERE a.PatientId = @uid AND a.Status = 'Completed' " +
                    "AND NOT EXISTS (SELECT 1 FROM Ratings r WHERE r.AppointmentId = a.AppointmentId) " +
                    "ORDER BY a.AppointmentDate DESC",
                    "@uid", Session.UserId);
            });
        }

        private void Submit(object sender, EventArgs e)
        {
            if (visitBox.SelectedValue == null || visitBox.Items.Count == 0)
            {
                UI.Warn("You have no completed visits waiting for a rating.");
                return;
            }

            int appointmentId = Convert.ToInt32(visitBox.SelectedValue);
            int score = int.Parse(scoreBox.SelectedItem.ToString().Substring(0, 1));

            UI.Run(delegate
            {
                int n = DatabaseConnection.Execute(
                    "INSERT INTO Ratings (AppointmentId, PatientId, DoctorId, Score, Comment) " +
                    "SELECT AppointmentId, PatientId, DoctorId, @s, @c FROM Appointments " +
                    "WHERE AppointmentId = @a AND PatientId = @uid AND Status = 'Completed'",
                    "@s", score, "@c", commentBox.Text.Trim(), "@a", appointmentId, "@uid", Session.UserId);
                if (n == 0) { UI.Warn("That visit cannot be rated."); return; }

                UI.Info("Thank you for your feedback!");
                commentBox.Clear();
                LoadVisits();
            });
        }
    }
}
