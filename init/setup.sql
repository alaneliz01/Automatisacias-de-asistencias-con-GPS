CREATE DATABASE IF NOT EXISTS secorvi_db;
USE secorvi_db;

CREATE TABLE IF NOT EXISTS empleados (
    id INT AUTO_INCREMENT PRIMARY KEY,
    matricula VARCHAR(50) UNIQUE NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    password VARCHAR(50) NOT NULL,
    es_admin BOOLEAN DEFAULT 0,
    es_super_admin BOOLEAN DEFAULT 0
);

CREATE TABLE IF NOT EXISTS asistencias (
    id INT AUTO_INCREMENT PRIMARY KEY,
    empleado_id INT,
    fecha DATE NOT NULL,
    hora_entrada TIME,
    hora_salida TIME,
    FOREIGN KEY (empleado_id) REFERENCES empleados(id) ON DELETE CASCADE
);

INSERT IGNORE INTO empleados (matricula, nombre, password, es_admin, es_super_admin) 
VALUES ('SEC-001', 'Rommel', '1234', 1, 1);