using System;
using System.Drawing;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Home screen. The tiles and numbers depend on the logged-in role.</summary>
    public class DashboardForm : Form
    {
        public bool LoggedOut;

        private FlowLayoutPanel statsFlow;

        public DashboardForm()
        {
            UI.Setup(this, Session.Role + " dashboard", 1000, 640);
            MinimumSize = new Size(820, 560);

            statsFlow = new FlowLayoutPanel();
            statsFlow.AutoSize = true;
            statsFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            statsFlow.Dock = DockStyle.Fill;
            statsFlow.Margin = new Padding(0, 0, 0, 12);

            FlowLayoutPanel tiles = new FlowLayoutPanel();
            tiles.Dock = DockStyle.Fill;
            tiles.AutoScroll = true;
            tiles.Margin = new Padding(0);

            AddTiles(tiles);

            Button logout = UI.Btn("Logout", Logout, 2);
            FlowLayoutPanel footer = UI.Flow(logout);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Padding = new Padding(20);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(statsFlow, 0, 0);
            layout.Controls.Add(tiles, 0, 1);
            layout.Controls.Add(footer, 0, 2);
            layout.RowCount = 3;

            UI.Compose(this, "Hospital Management System",
                "Welcome, " + Session.FullName + "  |  " + Session.Role + "  |  ID " + Session.UserId, layout);

            Activated += delegate { LoadStats(); };
        }

        // ---------- tiles ----------

        private void Tile(FlowLayoutPanel parent, string text, Func<Form> make)
        {
            Button b = new Button();
            b.Text = text;
            b.Size = new Size(220, 84);
            b.Margin = new Padding(8);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = UI.Primary;
            b.BackColor = Color.White;
            b.ForeColor = UI.PrimaryDark;
            b.Font = new Font("Segoe UI Semibold", 11F);
            b.Cursor = Cursors.Hand;
            b.Click += delegate
            {
                Form f = make();
                f.ShowDialog(this);
                f.Dispose();
            };
            parent.Controls.Add(b);
        }

        private void AddTiles(FlowLayoutPanel t)
        {
            if (Session.Role == "Admin")
            {
                Tile(t, "Manage Doctors", delegate { return new ManageDoctorsForm(); });
                Tile(t, "Manage Patients", delegate { return new ManagePatientsForm(); });
                Tile(t, "Departments", delegate { return new DepartmentsForm(); });
                Tile(t, "All Appointments", delegate { return new AppointmentsForm(); });
                Tile(t, "Billing && Revenue", delegate { return new BillsForm(); });
            }
            else if (Session.Role == "Doctor")
            {
                Tile(t, "My Appointments", delegate { return new AppointmentsForm(); });
                Tile(t, "My Ratings", delegate { return new RatingsForm(); });
                Tile(t, "My Profile", delegate { return new ProfileForm(); });
            }
            else
            {
                Tile(t, "Book Appointment", delegate { return new BookAppointmentForm(); });
                Tile(t, "My Appointments", delegate { return new AppointmentsForm(); });
                Tile(t, "My Bills", delegate { return new BillsForm(); });
                Tile(t, "Medical History", delegate { return new MedicalHistoryForm(); });
                Tile(t, "Rate a Doctor", delegate { return new RateDoctorForm(); });
                Tile(t, "My Profile", delegate { return new ProfileForm(); });
            }
            Tile(t, "Change Password", delegate { return new ChangePasswordForm(); });
        }

        // ---------- numbers ----------

        private static string Count(string sql, params object[] kv)
        {
            object o = DatabaseConnection.Scalar(sql, kv);
            return o == null ? "0" : Convert.ToString(o);
        }

        private static string Money(string sql, params object[] kv)
        {
            object o = DatabaseConnection.Scalar(sql, kv);
            return "Tk " + (o == null ? 0m : Convert.ToDecimal(o)).ToString("N0");
        }

        private void LoadStats()
        {
            statsFlow.Controls.Clear();
            try
            {
                if (Session.Role == "Admin")
                {
                    statsFlow.Controls.Add(UI.StatCard("Doctors", Count("SELECT COUNT(*) FROM Doctors")));
                    statsFlow.Controls.Add(UI.StatCard("Patients", Count("SELECT COUNT(*) FROM Users WHERE Role = 'Patient'")));
                    statsFlow.Controls.Add(UI.StatCard("Today's appointments",
                        Count("SELECT COUNT(*) FROM Appointments WHERE AppointmentDate = CAST(GETDATE() AS DATE) AND Status <> 'Cancelled'")));
                    statsFlow.Controls.Add(UI.StatCard("Unpaid bills",
                        Money("SELECT SUM(Amount) FROM Bills WHERE Status = 'Unpaid'")));
                    statsFlow.Controls.Add(UI.StatCard("Revenue collected",
                        Money("SELECT SUM(Amount) FROM Bills WHERE Status = 'Paid'")));
                }
                else if (Session.Role == "Doctor")
                {
                    statsFlow.Controls.Add(UI.StatCard("Today's appointments",
                        Count("SELECT COUNT(*) FROM Appointments WHERE DoctorId = @id AND AppointmentDate = CAST(GETDATE() AS DATE) AND Status <> 'Cancelled'", "@id", Session.UserId)));
                    statsFlow.Controls.Add(UI.StatCard("Waiting for confirmation",
                        Count("SELECT COUNT(*) FROM Appointments WHERE DoctorId = @id AND Status = 'Pending'", "@id", Session.UserId)));
                    statsFlow.Controls.Add(UI.StatCard("Patients treated",
                        Count("SELECT COUNT(DISTINCT PatientId) FROM Appointments WHERE DoctorId = @id AND Status = 'Completed'", "@id", Session.UserId)));
                    object avg = DatabaseConnection.Scalar("SELECT AVG(CAST(Score AS FLOAT)) FROM Ratings WHERE DoctorId = @id", "@id", Session.UserId);
                    statsFlow.Controls.Add(UI.StatCard("Average rating", avg == null ? "-" : Convert.ToDouble(avg).ToString("0.0") + " / 5"));
                }
                else
                {
                    statsFlow.Controls.Add(UI.StatCard("Upcoming appointments",
                        Count("SELECT COUNT(*) FROM Appointments WHERE PatientId = @id AND AppointmentDate >= CAST(GETDATE() AS DATE) AND Status IN ('Pending','Confirmed')", "@id", Session.UserId)));
                    statsFlow.Controls.Add(UI.StatCard("Unpaid bills",
                        Money("SELECT SUM(Amount) FROM Bills WHERE PatientId = @id AND Status = 'Unpaid'", "@id", Session.UserId)));
                    statsFlow.Controls.Add(UI.StatCard("Completed visits",
                        Count("SELECT COUNT(*) FROM Appointments WHERE PatientId = @id AND Status = 'Completed'", "@id", Session.UserId)));
                }
            }
            catch (Exception ex)
            {
                statsFlow.Controls.Add(UI.MutedLabel("Could not load numbers: " + ex.Message));
            }
        }

        // ---------- logout ----------

        private void Logout(object sender, EventArgs e)
        {
            if (!UI.Confirm("Log out of the application?")) return;
            Session.Clear();
            LoggedOut = true;
            Close();
        }
    }
}
