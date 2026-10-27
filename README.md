# Hospital Management System

A Windows desktop app (C# Windows Forms, .NET Framework 4.7.2, SQL Server) built in the same style as the
Regional Product Trade Management System: login screen, role-based dashboards, and a SQL Server database.

| Reference project | This project |
|---|---|
| Admin | **Admin** - manages doctors, patients, departments, appointments, billing |
| Seller | **Doctor** - confirms appointments, records diagnosis/prescription, sees ratings |
| Buyer | **Patient** - registers, books appointments, pays bills, rates doctors |
| Product / Order | Appointment |
| Payment | Bill (created automatically when a visit is completed) |
| Rate seller | Rate doctor |

## How to run

1. Install **Visual Studio 2019/2022** (".NET desktop development" workload) and **SQL Server Express** + SSMS.
2. Open `Database\schema.sql` in SSMS and run it. It creates the `HospitalManagement` database, tables and demo data.
3. Open `HospitalManagementSystem.sln` in Visual Studio.
4. In `HospitalManagement\DatabaseConnection.cs`, change the server name if yours is not `.\SQLEXPRESS`
   (e.g. `(localdb)\MSSQLLocalDB`).
5. Press **F5**.

## Demo logins (User ID / password)

| Role | ID | Password |
|---|---|---|
| Admin | 1 | admin123 |
| Doctor | 1001 (also 1002, 1003) | doctor123 |
| Patient | 1004 | patient123 |

New patients can register from the login screen; they receive a numeric login ID.

## Typical flow

1. Patient books an appointment (department -> doctor -> date -> free time slot). Status: **Pending**.
2. Doctor (or admin) **confirms** it.
3. After the visit the doctor chooses **Complete visit**, writes the diagnosis and prescription,
   and a **bill** for the doctor's fee is created automatically.
4. Patient pays the bill (Cash / Card / bKash / Nagad), views the prescription under **Medical History**,
   and rates the doctor.

## Things worth knowing

- Passwords are stored as SHA-256 hashes (the reference project stored plain text).
- All SQL is parameterised. Double-booking a doctor's slot is blocked by a unique index in the database.
- Admin "Reset password" sets a temporary password of `123456`.
- Currency is shown as "Tk"; search for `Tk ` in the code to change it.
- The screens are built in code (no designer files), so each form is a single readable `.cs` file.

## Project layout

```
Database\schema.sql            tables, constraints, demo data
HospitalManagement\
  Program.cs                   entry point
  DatabaseConnection.cs        connection string + query helpers
  Session.cs                   logged-in user + password hashing
  UI.cs                        shared look and layout helpers
  LoginForm / RegisterForm / DashboardForm
  ManageDoctorsForm / ManagePatientsForm / DepartmentsForm        (admin)
  AppointmentsForm / ConsultationForm                             (all roles / doctor)
  BookAppointmentForm / MedicalHistoryForm / RateDoctorForm       (patient)
  BillsForm / RatingsForm / ProfileForm / ChangePasswordForm
```
