/* ============================================================
   RaceDay Database
   SQL Server / SSMS
   ============================================================ */

USE master;
GO

/* ---------- Delete existing database ---------- */

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB
    SET SINGLE_USER
    WITH ROLLBACK IMMEDIATE;

    DROP DATABASE RaceDayDB;
END
GO

/* ---------- Create database ---------- */

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO


/* ============================================================
   TABLE: Roles
   ============================================================ */

CREATE TABLE dbo.Roles
(
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(20) NOT NULL UNIQUE
);
GO


/* ============================================================
   TABLE: Users
   ============================================================ */

CREATE TABLE dbo.Users
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID INT NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(20) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Users_Roles
        FOREIGN KEY (RoleID)
        REFERENCES dbo.Roles(RoleID)
);
GO


/* ============================================================
   TABLE: Events
   ============================================================ */

CREATE TABLE dbo.Events
(
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName VARCHAR(100) NOT NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(150) NOT NULL,
    Description VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Events_Users
        FOREIGN KEY (OrganiserID)
        REFERENCES dbo.Users(UserID)
);
GO


/* ============================================================
   TABLE: Categories
   ============================================================ */

CREATE TABLE dbo.Categories
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName VARCHAR(50) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    EntryFee DECIMAL(8,2) NOT NULL DEFAULT 0,

    CONSTRAINT FK_Categories_Events
        FOREIGN KEY (EventID)
        REFERENCES dbo.Events(EventID)
);
GO


/* ============================================================
   TABLE: Enrolments
   ============================================================ */

CREATE TABLE dbo.Enrolments
(
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Registered',

    CONSTRAINT FK_Enrolments_Users
        FOREIGN KEY (UserID)
        REFERENCES dbo.Users(UserID),

    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (CategoryID)
        REFERENCES dbo.Categories(CategoryID),

    CONSTRAINT UQ_Enrolments_User_Category
        UNIQUE (UserID, CategoryID)
);
GO


/* ============================================================
   TABLE: Results
   ============================================================ */

CREATE TABLE dbo.Results
(
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    Position INT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',

    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentID)
        REFERENCES dbo.Enrolments(EnrolmentID)
);
GO


/* ============================================================
   SEED DATA
   ============================================================ */


/* ---------- Roles ---------- */

INSERT INTO dbo.Roles (RoleName)
VALUES
    ('Organiser'),
    ('Participant');
GO


/* ---------- Users ---------- */

INSERT INTO dbo.Users
(
    RoleID,
    FullName,
    Email,
    PasswordHash,
    PhoneNumber
)
VALUES
(
    (SELECT RoleID
     FROM dbo.Roles
     WHERE RoleName = 'Organiser'),
    'Thandiwe Mokoena',
    'thandiwe@raceday.co.za',
    'hashed_pw_1',
    '0821234567'
),
(
    (SELECT RoleID
     FROM dbo.Roles
     WHERE RoleName = 'Organiser'),
    'James Whitfield',
    'james@raceday.co.za',
    'hashed_pw_2',
    '0837654321'
),
(
    (SELECT RoleID
     FROM dbo.Roles
     WHERE RoleName = 'Participant'),
    'Tina Jeke',
    'tina@example.com',
    'hashed_pw_3',
    '0845551122'
),
(
    (SELECT RoleID
     FROM dbo.Roles
     WHERE RoleName = 'Participant'),
    'Tshepo Tau',
    'tshepo@example.com',
    'hashed_pw_4',
    '0845552233'
),
(
    (SELECT RoleID
     FROM dbo.Roles
     WHERE RoleName = 'Participant'),
    'Amy Richards',
    'amy@example.com',
    'hashed_pw_5',
    '0845553344'
);
GO


/* ---------- Events ---------- */

INSERT INTO dbo.Events
(
    OrganiserID,
    EventName,
    EventDate,
    Location,
    Description
)
VALUES
(
    (SELECT UserID
     FROM dbo.Users
     WHERE Email = 'thandiwe@raceday.co.za'),

    'Joburg City Run',
    '2026-10-11',
    'Johannesburg, Gauteng',
    'Annual road race through the Johannesburg CBD.'
),
(
    (SELECT UserID
     FROM dbo.Users
     WHERE Email = 'thandiwe@raceday.co.za'),

    'Soweto Marathon',
    '2026-11-15',
    'Soweto, Gauteng',
    'Marathon and half-marathon through historic Soweto.'
),
(
    (SELECT UserID
     FROM dbo.Users
     WHERE Email = 'james@raceday.co.za'),

    'Cape Coastal Fun Run',
    '2026-09-27',
    'Cape Town, Western Cape',
    'Family-friendly coastal fun run and 10km race.'
);
GO


/* ---------- Categories ---------- */

INSERT INTO dbo.Categories
(
    EventID,
    CategoryName,
    DistanceKm,
    MaxParticipants,
    EntryFee
)
VALUES
(
    (SELECT EventID
     FROM dbo.Events
     WHERE EventName = 'Joburg City Run'),
    '5km Fun Run',
    5.00,
    500,
    100.00
),
(
    (SELECT EventID
     FROM dbo.Events
     WHERE EventName = 'Joburg City Run'),
    '10km Race',
    10.00,
    300,
    150.00
),
(
    (SELECT EventID
     FROM dbo.Events
     WHERE EventName = 'Soweto Marathon'),
    'Half Marathon 21km',
    21.10,
    200,
    250.00
),
(
    (SELECT EventID
     FROM dbo.Events
     WHERE EventName = 'Soweto Marathon'),
    'Full Marathon 42km',
    42.20,
    150,
    350.00
),
(
    (SELECT EventID
     FROM dbo.Events
     WHERE EventName = 'Cape Coastal Fun Run'),
    '2km Family Fun Run',
    2.00,
    400,
    50.00
),
(
    (SELECT EventID
     FROM dbo.Events
     WHERE EventName = 'Cape Coastal Fun Run'),
    '10km Coastal Race',
    10.00,
    250,
    150.00
);
GO


/* ---------- Enrolments ---------- */

INSERT INTO dbo.Enrolments
(
    UserID,
    CategoryID,
    Status
)
VALUES
(
    (SELECT UserID
     FROM dbo.Users
     WHERE Email = 'tina@example.com'),

    (SELECT CategoryID
     FROM dbo.Categories
     WHERE CategoryName = '5km Fun Run'),

    'Confirmed'
),
(
    (SELECT UserID
     FROM dbo.Users
     WHERE Email = 'tina@example.com'),

    (SELECT CategoryID
     FROM dbo.Categories
     WHERE CategoryName = 'Half Marathon 21km'),

    'Confirmed'
),
(
    (SELECT UserID
     FROM dbo.Users
     WHERE Email = 'tshepo@example.com'),

    (SELECT CategoryID
     FROM dbo.Categories
     WHERE CategoryName = '10km Race'),

    'Confirmed'
),
(
    (SELECT UserID
     FROM dbo.Users
     WHERE Email = 'tshepo@example.com'),

    (SELECT CategoryID
     FROM dbo.Categories
     WHERE CategoryName = '10km Coastal Race'),

    'Registered'
),
(
    (SELECT UserID
     FROM dbo.Users
     WHERE Email = 'amy@example.com'),

    (SELECT CategoryID
     FROM dbo.Categories
     WHERE CategoryName = '2km Family Fun Run'),

    'Confirmed'
);
GO


/* ---------- Results ---------- */

INSERT INTO dbo.Results
(
    EnrolmentID,
    FinishTime,
    Position,
    Status
)
VALUES
(
    (
        SELECT e.EnrolmentID
        FROM dbo.Enrolments e
        INNER JOIN dbo.Users u
            ON e.UserID = u.UserID
        INNER JOIN dbo.Categories c
            ON e.CategoryID = c.CategoryID
        WHERE u.Email = 'tina@example.com'
          AND c.CategoryName = '5km Fun Run'
    ),
    '00:24:35',
    1,
    'Finished'
),
(
    (
        SELECT e.EnrolmentID
        FROM dbo.Enrolments e
        INNER JOIN dbo.Users u
            ON e.UserID = u.UserID
        INNER JOIN dbo.Categories c
            ON e.CategoryID = c.CategoryID
        WHERE u.Email = 'tshepo@example.com'
          AND c.CategoryName = '10km Race'
    ),
    '00:52:10',
    1,
    'Finished'
);
GO


/* ============================================================
   VERIFICATION
   ============================================================ */

SELECT * FROM dbo.Roles;
SELECT * FROM dbo.Users;
SELECT * FROM dbo.Events;
SELECT * FROM dbo.Categories;
SELECT * FROM dbo.Enrolments;
SELECT * FROM dbo.Results;
GO
