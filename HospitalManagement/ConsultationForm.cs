using System;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Doctor records the diagnosis and prescription; finishing the visit creates the bill.</summary>
    public class ConsultationForm : Form
    {
        private readonly int appointmentId;
        private TextBox diagnosisBox, prescriptionBox;

        public ConsultationForm(int appointmentId, string patientName, string reason)
        {
            this.appointmentId = appointmentId;
            UI.Setup(this, "Consultation", 620, 600);

            diagnosisBox = UI.Txt(false, true, 120);
            prescriptionBox = UI.Txt(false, true, 140);

            TableLayoutPanel f = UI.FieldTable();
            UI.AddField(f, "Patient", UI.Lbl(patientName));
            UI.AddField(f, "Reason", UI.Lbl(reason.Length == 0 ? "-" : reason));
            UI.AddField(f, "Diagnosis", diagnosisBox);
            UI.AddField(f, "Prescription", prescriptionBox);

            FlowLayoutPanel buttons = UI.Flow(
                UI.Btn("Complete visit && create bill", Save),
                UI.Btn("Close", delegate { Close(); }, 1));

            UI.Compose(this, "Consultation", "Record the diagnosis and prescription", UI.FormLayout(f, buttons));
        }

        private void Save(object sender, EventArgs e)
        {
            if (diagnosisBox.Text.Trim().Length == 0)
            {
                UI.Warn("Please write a diagnosis.");
                return;
            }

            UI.Run(delegate
            {
                int n = DatabaseConnection.Execute(
                    "SET XACT_ABORT ON; BEGIN TRANSACTION; " +
                    "UPDATE Appointments SET Status = 'Completed', Diagnosis = @dg, Prescription = @pr " +
                    "WHERE AppointmentId = @id AND DoctorId = @doc AND Status IN ('Pending','Confirmed'); " +
                    "IF @@ROWCOUNT = 1 " +
                    "  INSERT INTO Bills (AppointmentId, PatientId, Amount) " +
                    "  SELECT a.AppointmentId, a.PatientId, d.ConsultationFee " +
                    "  FROM Appointments a JOIN Doctors d ON d.DoctorId = a.DoctorId " +
                    "  WHERE a.AppointmentId = @id AND NOT EXISTS (SELECT 1 FROM Bills b WHERE b.AppointmentId = @id); " +
                    "COMMIT;",
                    "@dg", diagnosisBox.Text.Trim(), "@pr", prescriptionBox.Text.Trim(),
                    "@id", appointmentId, "@doc", Session.UserId);

                if (n == 0)
                {
                    UI.Warn("This appointment is already completed or cancelled.");
                    return;
                }
                UI.Info("Visit completed. A bill was created for the patient.");
                Close();
            });
        }
    }
}
