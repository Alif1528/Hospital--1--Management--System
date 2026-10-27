using System;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Doctor: what patients have said about them.</summary>
    public class RatingsForm : Form
    {
        private DataGridView grid;
        private Label summary;

        public RatingsForm()
        {
            UI.Setup(this, "My ratings", 900, 580);

            grid = UI.Grid();
            summary = UI.Lbl("");
            summary.Font = UI.Bold;

            UI.Compose(this, "My ratings", "Feedback from your patients",
                UI.ListLayout(UI.Flow(summary), grid, null));
            Load += delegate { LoadRatings(); };
        }

        private void LoadRatings()
        {
            UI.Run(delegate
            {
                grid.DataSource = DatabaseConnection.Query(
                    "SELECT CONVERT(VARCHAR(10), a.AppointmentDate, 23) AS [Visit date], p.FullName AS Patient, " +
                    "r.Score, r.Comment " +
                    "FROM Ratings r " +
                    "JOIN Appointments a ON a.AppointmentId = r.AppointmentId " +
                    "JOIN Users p ON p.UserId = r.PatientId " +
                    "WHERE r.DoctorId = @uid ORDER BY r.RatingId DESC",
                    "@uid", Session.UserId);
                grid.Columns["Score"].FillWeight = 30;
                grid.Columns["Comment"].FillWeight = 160;

                object avg = DatabaseConnection.Scalar("SELECT AVG(CAST(Score AS FLOAT)) FROM Ratings WHERE DoctorId = @uid", "@uid", Session.UserId);
                summary.Text = avg == null
                    ? "No ratings yet."
                    : "Average: " + Convert.ToDouble(avg).ToString("0.0") + " / 5   (" + grid.Rows.Count + " ratings)";
            });
        }
    }
}
