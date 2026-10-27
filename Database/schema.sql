-- Hospital Management System - database setup (SQL Server / SQL Server Express)
-- Run this whole script once in SQL Server Management Studio (SSMS).

IF DB_ID('HospitalManagement') IS NULL
    CREATE DATABASE HospitalManagement;
GO
USE HospitalManagement;
GO

IF OBJECT_ID('Ratings')      IS NOT NULL DROP TABLE Ratings;
IF OBJECT_ID('Bills')        IS NOT NULL DROP TABLE Bills;
IF OBJECT_ID('Appointments') IS NOT NULL DROP TABLE Appointments;
IF OBJECT_ID('Doctors')      IS NOT NULL DROP TABLE Doctors;
IF OBJECT_ID('Departments')  IS NOT NULL DROP TABLE Departments;
IF OBJECT_ID('Users')        IS NOT NULL DROP TABLE Users;
GO

CREATE TABLE Users (
    UserId       INT IDENTITY(1001,1) PRIMARY KEY,
    FullName     NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(150) NOT NULL UNIQUE,
    Phone        NVARCHAR(20)  NOT NULL,
    PasswordHash CHAR(64)      NOT NULL,          -- SHA-256 hex (uppercase)
    Role         VARCHAR(10)   NOT NULL CHECK (Role IN ('Admin','Doctor','Patient')),
    Gender       VARCHAR(10)   NULL,
    DateOfBirth  DATE          NULL,
    BloodGroup   VARCHAR(5)    NULL,
    Address      NVARCHAR(250) NULL,
    CreatedAt    DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Departments (
    DepartmentId INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Doctors (
    DoctorId        INT PRIMARY KEY REFERENCES Users(UserId),
    DepartmentId    INT NOT NULL REFERENCES Departments(DepartmentId),
    Specialization  NVARCHAR(100) NOT NULL,
    Qualification   NVARCHAR(150) NULL,
    ConsultationFee DECIMAL(10,2) NOT NULL CHECK (ConsultationFee >= 0),
    AvailableDays   NVARCHAR(60)  NOT NULL DEFAULT 'Sat,Sun,Mon,Tue,Wed,Thu',
    Room            NVARCHAR(30)  NULL
);

CREATE TABLE Appointments (
    AppointmentId   INT IDENTITY(1,1) PRIMARY KEY,
    PatientId       INT NOT NULL REFERENCES Users(UserId),
    DoctorId        INT NOT NULL REFERENCES Doctors(DoctorId),
    AppointmentDate DATE NOT NULL,
    TimeSlot        CHAR(5) NOT NULL,                 -- e.g. 09:30
    Reason          NVARCHAR(300) NULL,
    Status          VARCHAR(10) NOT NULL DEFAULT 'Pending'
                    CHECK (Status IN ('Pending','Confirmed','Completed','Cancelled')),
    Diagnosis       NVARCHAR(1000) NULL,
    Prescription    NVARCHAR(1000) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE()
);
-- A doctor cannot have two live appointments in the same slot.
CREATE UNIQUE INDEX UX_Appointments_Slot
    ON Appointments (DoctorId, AppointmentDate, TimeSlot)
    WHERE Status <> 'Cancelled';

CREATE TABLE Bills (
    BillId        INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL UNIQUE REFERENCES Appointments(AppointmentId),
    PatientId     INT NOT NULL REFERENCES Users(UserId),
    Amount        DECIMAL(10,2) NOT NULL,
    Status        VARCHAR(6) NOT NULL DEFAULT 'Unpaid' CHECK (Status IN ('Unpaid','Paid')),
    PaymentMethod VARCHAR(20) NULL,
    PaidAt        DATETIME NULL,
    CreatedAt     DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Ratings (
    RatingId      INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL UNIQUE REFERENCES Appointments(AppointmentId),
    PatientId     INT NOT NULL REFERENCES Users(UserId),
    DoctorId      INT NOT NULL REFERENCES Doctors(DoctorId),
    Score         INT NOT NULL CHECK (Score BETWEEN 1 AND 5),
    Comment       NVARCHAR(300) NULL,
    CreatedAt     DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ---------- Sample data ----------
-- Passwords are stored as SHA-256 hashes (computed here with HASHBYTES).
INSERT INTO Departments (Name) VALUES
 ('General Medicine'), ('Cardiology'), ('Pediatrics'), ('Orthopedics'),
 ('Gynecology'), ('Dermatology'), ('ENT'), ('Neurology');

SET IDENTITY_INSERT Users ON;
INSERT INTO Users (UserId, FullName, Email, Phone, PasswordHash, Role, Gender) VALUES
 (1, 'System Admin', 'admin@hospital.local', '01700000000',
     CONVERT(CHAR(64), HASHBYTES('SHA2_256','admin123'), 2), 'Admin', 'Male');
SET IDENTITY_INSERT Users OFF;

INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role, Gender) VALUES
 ('Dr. Nusrat Jahan',  'nusrat@hospital.local',  '01711111111', CONVERT(CHAR(64), HASHBYTES('SHA2_256','doctor123'),2), 'Doctor', 'Female'),
 ('Dr. Rahim Chowdhury','rahim@hospital.local',  '01722222222', CONVERT(CHAR(64), HASHBYTES('SHA2_256','doctor123'),2), 'Doctor', 'Male'),
 ('Dr. Farhana Akter', 'farhana@hospital.local', '01733333333', CONVERT(CHAR(64), HASHBYTES('SHA2_256','doctor123'),2), 'Doctor', 'Female');

INSERT INTO Doctors (DoctorId, DepartmentId, Specialization, Qualification, ConsultationFee, AvailableDays, Room)
SELECT u.UserId, d.DepartmentId, x.Spec, x.Qual, x.Fee, x.Days, x.Room
FROM (VALUES
   ('nusrat@hospital.local',  'General Medicine', 'Internal Medicine', 'MBBS, FCPS',        800, 'Sat,Sun,Mon,Tue,Wed', 'R-101'),
   ('rahim@hospital.local',   'Cardiology',       'Cardiologist',      'MBBS, MD (Cardiology)', 1500, 'Sun,Tue,Thu',    'R-205'),
   ('farhana@hospital.local', 'Pediatrics',       'Child Specialist',  'MBBS, DCH',         1000, 'Sat,Mon,Wed,Thu',   'R-110')
) AS x(Email, Dept, Spec, Qual, Fee, Days, Room)
JOIN Users u ON u.Email = x.Email
JOIN Departments d ON d.Name = x.Dept;

INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role, Gender, DateOfBirth, BloodGroup, Address) VALUES
 ('Karim Hossain', 'karim@example.com', '01811111111',
   CONVERT(CHAR(64), HASHBYTES('SHA2_256','patient123'),2), 'Patient', 'Male', '1990-05-14', 'B+', 'Mirpur, Dhaka');
GO

PRINT 'Done. Demo logins (ID / password):';
PRINT '  Admin   1    / admin123';
PRINT '  Doctor  1001 / doctor123   (also 1002, 1003)';
PRINT '  Patient 1004 / patient123';
