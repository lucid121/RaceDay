Use RaceDayDB;

Create Table Role (
    Roleid Int Identity(1,1) Primary Key,
    Rolename Varchar(50) Not Null Unique
);

Create Table [User] (
    Userid Int Identity(1,1) Primary Key,
    Roleid Int Not Null,
    Name Varchar(100) Not Null,
    Surname Varchar(100) Not Null,
    Email Varchar(150) Not Null Unique,
    Passwordhash Varchar(255) Not Null,
    Datecreated Datetime Not Null Default Getdate(),
    
    Constraint Fk_User_Role Foreign Key (Roleid) References Role(Roleid)
);

Create Table Event (
    Eventid Int Identity(1,1) Primary Key,
    Organiserid Int Not Null,
    Eventname Varchar(150) Not Null,
    Eventdate Date Not Null,
    Location Varchar(200) Not Null,
    Description Varchar(1000) Null,
    Constraint Fk_Event_Organiser Foreign Key (Organiserid) References [User](Userid)
);

Create Table Category (
    Categoryid Int Identity(1,1) Primary Key,
    Eventid Int Not Null,
    Categoryname Varchar(100) Not Null,
    Distance Decimal(5,2) Not Null,
    Entryfee Decimal(10,2) Not Null Default 0.00,
    Constraint Fk_Category_Event Foreign Key (Eventid) References Event(Eventid) On Delete Cascade
);

Create Table Enrolment (
    Enrolmentid Int Identity(1,1) Primary Key,
    Userid Int Not Null,
    Categoryid Int Not Null,
    Enrolmentdate Datetime Not Null Default Getdate(),
    Status Varchar(50) Not Null Default 'Registered',
    Constraint Fk_Enrolment_User Foreign Key (Userid) References [User](Userid),
    Constraint Fk_Enrolment_Category Foreign Key (Categoryid) References Category(Categoryid) On Delete Cascade,
    Constraint Uq_User_Category Unique (Userid, Categoryid)
);

Create Table Result (
    Resultid Int Identity(1,1) Primary Key,
    Enrolmentid Int Not Null Unique,
    Finishtime Time Null,
    Position Int Null,
    Remarks Varchar(255) Null,
    Constraint Fk_Result_Enrolment Foreign Key (Enrolmentid) References Enrolment(Enrolmentid) On Delete Cascade
);

Insert Into Role (Rolename) Values ('Organiser'), ('Participant');

Insert Into [User] (Roleid, Name, Surname, Email, Passwordhash) Values 
(1, 'Thabo', 'Mbeki', 'thabo.organiser@raceday.co.za', 'hashed_pwd_123'),
(1, 'Sarah', 'Van Der Merwe', 'sarah.events@raceday.co.za', 'hashed_pwd_456'),
(2, 'Sipho', 'Nkosi', 'sipho.runner@gmail.com', 'hashed_pwd_789'),
(2, 'Jessica', 'Smith', 'jess.cycles@yahoo.com', 'hashed_pwd_012');

Insert Into Event (Organiserid, Eventname, Eventdate, Location, Description) Values 
(1, 'Cape Town Cycle Tour', '2027-03-14', 'Cape Town Stadium', 'The largest timed cycling event in the world.'),
(2, 'Soweto Marathon', '2027-11-05', 'FNB Stadium, Johannesburg', 'The Peoples Race through the historic streets of Soweto.'),
(1, 'Durban Sunrise 10km Walk', '2027-01-20', 'Moses Mabhida Stadium', 'A beautiful community walk along the Durban beachfront.');

Insert Into Category (Eventid, Categoryname, Distance, Entryfee) Values 
(1, 'Full Route', 109.00, 850.00),
(1, 'Short Route', 42.00, 450.00);

Insert Into Category (Eventid, Categoryname, Distance, Entryfee) Values 
(2, 'Full Marathon', 42.20, 380.00),
(2, 'Half Marathon', 21.10, 250.00),
(2, '10km Run', 10.00, 150.00);

Insert Into Category (Eventid, Categoryname, Distance, Entryfee) Values 
(3, '10km Walk', 10.00, 100.00),
(3, '5km Fun Walk', 5.00, 50.00);

Insert Into Enrolment (Userid, Categoryid, Status) Values 
(3, 4, 'Completed'),
(4, 1, 'Registered'),
(4, 7, 'Completed');

Insert Into Result (Enrolmentid, Finishtime, Position, Remarks) Values 
(1, '01:45:22', 145, 'Great pace, personal best.');

Insert Into Result (Enrolmentid, Finishtime, Position, Remarks) Values 
(3, '00:40:15', 12, 'Easy walk, good weather.');