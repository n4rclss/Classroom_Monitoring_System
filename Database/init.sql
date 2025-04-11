DROP DATABASE IF EXISTS AppDB;
CREATE DATABASE AppDB;

USE AppDB;

DROP TABLE IF EXISTS Teachers;
CREATE TABLE IF NOT EXISTS Teachers (
    TeacherId INT PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Password VARCHAR(100) NOT NULL
);


DROP TABLE IF EXISTS Rooms;
CREATE TABLE IF NOT EXISTS Rooms (
    RoomId INT AUTO_INCREMENT PRIMARY KEY,
    RoomName VARCHAR(100) NOT NULL,
    TeacherId INT NULL,
    CONSTRAINT fk_rooms_teacher FOREIGN KEY (TeacherId)
        REFERENCES Teachers(TeacherId)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    Password VARCHAR(100) NOT NULL
);

DROP TABLE IF EXISTS Students;
CREATE TABLE IF NOT EXISTS Students (
    StudentId INT PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Password VARCHAR(100) NOT NULL,
    RoomId INT NULL,
    CONSTRAINT fk_students_room FOREIGN KEY (RoomId)
        REFERENCES Rooms(RoomId)
        ON DELETE SET NULL
        ON UPDATE CASCADE
);


-- Password: room<ID>pass, teacher<ID>pass, student<ID>pass

INSERT INTO Teachers (TeacherId, FullName, Password) 
VALUES (1, 'Teacher 1', 'ru191Ll6rirSzBIdmFsYDHI/AEYyILODxxgQ9HZVOZzDrzeZWNLe0V5TdS973B75'), 
       (2, 'Teacher 2', 'XOELfoOfcnkc5JHnNISCokEz/pAHyu+fizcoa3l8G8fZxtCyDdG0ict1yNOW/I9l');
       
INSERT INTO Rooms (RoomName, TeacherId, Password) 
VALUES ('Room 1', 1, 'zcQdG5V/gAmKg3rcABhrZx+HxmEChZjcXMN1AUZJJ9vnoiM7NL9oUfv/yS5XaZL+'),
       ('Room 2', 2, 'q5P1hnZhCpMFfSHRtKEaxvkDOz0i2AsfBSGP1QS2ldd2a04fQ+QAxN1yC3WbPOmH'); 

INSERT INTO Students (StudentId, FullName, Password, RoomId)
VALUES (1, 'Student 1', 'vVb2CGw93HYF0AkRAL59JavaT2N409NWfqVgCnlBGg//pUGuaQ9/0uRYI193wgfr', 1),
       (2, 'Student 2', 'Kuk3UXJxn0wi5aU5S1EaZE55dLm1qW89VvspqwIt8C4nS+DKX6lL6q7mq2A7HCIe', 1),
       (3, 'Student 3', 'oWcyIxT7PYeOSm+rmFbQ1l5rHNq9lua6MubTi2wj6pLKkQ6mB7yUkSLMH+s6LNNp', 2), 
       (4, 'Student 4', 'ppfpjoO2nwP5S1c3wdIKxXoZt08twwD0rl1VTRTUsO/PCq4SEFpwML24b1OPnfdw', 2);