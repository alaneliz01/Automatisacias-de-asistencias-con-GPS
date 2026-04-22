-- ======================================================
-- BASE DE DATOS SECORVI 
-- ======================================================
DROP DATABASE IF EXISTS secorvi_db;
CREATE DATABASE secorvi_db;
USE secorvi_db;
-- 1. ROLES
CREATE TABLE Roles (
    id_rol INT PRIMARY KEY,
    nombre_rol VARCHAR(50) NOT NULL, 
    descripcion VARCHAR(100)
);

-- 2. UBICACIONES
CREATE TABLE Ubicaciones (
    id_lugar INT AUTO_INCREMENT PRIMARY KEY,
    nombre_lugar VARCHAR(100),
    latitud DECIMAL(10, 8),
    longitud DECIMAL(11, 8),
    radio_permitido INT
);

-- 3. EMPLEADOS
CREATE TABLE Empleados (
    id_empleado INT AUTO_INCREMENT PRIMARY KEY,
    nombre_completo VARCHAR(150) NOT NULL,
    telefono VARCHAR(20) NOT NULL UNIQUE, 
    id_rol INT,
    estatus ENUM('Activo', 'Inactivo') DEFAULT 'Activo',
    usuario VARCHAR(50) NOT NULL UNIQUE, 
    contrasena VARCHAR(100) NOT NULL, 
    matricula VARCHAR(20) NOT NULL UNIQUE, 
    FOREIGN KEY (id_rol) REFERENCES roles(id_rol)
);

-- 4. TURNOS (Corregida con id_empleado para coincidir con DataService)
CREATE TABLE Turnos (
    id_turno INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(50),
    hora_inicio TIME,
    hora_fin TIME,
    id_lugar INT,
    id_empleado INT,
    FOREIGN KEY (id_lugar) REFERENCES ubicaciones(id_lugar),
    FOREIGN KEY (id_empleado) REFERENCES empleados(id_empleado)
);

-- 5. ASIGNACIONES
CREATE TABLE Asignaciones (
    id_asignaciones INT AUTO_INCREMENT PRIMARY KEY,
    id_empleado INT,
    id_ubicacion INT,
    id_turno INT, 
    fecha DATE,
    estatus VARCHAR(50) DEFAULT 'PROGRAMADO',
    FOREIGN KEY (id_empleado) REFERENCES empleados(id_empleado),
    FOREIGN KEY (id_ubicacion) REFERENCES ubicaciones(id_lugar),
    FOREIGN KEY (id_turno) REFERENCES turnos(id_turno)
);

-- 6. ASISTENCIAS
CREATE TABLE Asistencias (
    id_registro INT AUTO_INCREMENT PRIMARY KEY,
    id_empleado INT,
    id_lugar INT,
    fecha_inicio DATE NOT NULL,
    hora_inicio TIME NOT NULL,
    fecha_fin DATE,
    hora_fin TIME,
    metodo_registro VARCHAR(50), 
    latitud DECIMAL(10, 8),
    longitud DECIMAL(11, 8),
    link_mapa VARCHAR(255),
    estado ENUM('Entrada', 'Salida') DEFAULT 'Entrada',
    FOREIGN KEY (id_empleado) REFERENCES empleados(id_empleado),
    FOREIGN KEY (id_lugar) REFERENCES ubicaciones(id_lugar)
);

-- ======================================================
-- CARGA DE DATOS MAESTROS E INICIALES
-- ======================================================

-- Roles base
INSERT INTO Roles (id_rol, nombre_rol, descripcion) VALUES  
(1, 'Super Admin', 'Acceso total'),
(2, 'Admin Empleados', 'Gestión operativa'),
(3, 'Agente', 'Personal de campo');

-- Ubicación inicial
INSERT INTO Ubicaciones (id_lugar, nombre_lugar, latitud, longitud, radio_permitido)
VALUES (1, 'OFICINA CENTRAL', 25.68440000, -100.31610000, 200);

-- Empleados iniciales
INSERT INTO Empleados (nombre_completo, telefono, id_rol, estatus, usuario, contrasena, matricula)
VALUES 
('ROMMEL ADMINISTRADOR', '8100000000', 1, 'Activo', 'Rommel', '1234', 'ADMIN-001'),
('LAURA ELIZ', '8115103073', 3, 'Activo', 'Laura', '5678', 'EMP-002');

-- Turnos base
INSERT INTO Turnos (id_turno, nombre, hora_inicio, hora_fin, id_lugar, id_empleado) VALUES  
(1, 'TURNO GENERAL', '08:00:00', '18:00:00', 1, NULL),
(2, 'DÍA LIBRE', '00:00:00', '00:00:00', 1, NULL),
(3, 'VACACIONES', '00:00:00', '00:00:00', 1, NULL);

-- Crear asignación para hoy
INSERT INTO Asignaciones (id_empleado, id_ubicacion, id_turno, fecha, estatus)
VALUES (2, 1, 1, CURRENT_DATE(), 'PROGRAMADO');

-- Comprobación
SELECT 'Base de datos lista' AS Resultado;