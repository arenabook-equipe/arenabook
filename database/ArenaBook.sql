
-- ==========================================
-- ARENABOOK - BANCO DE DADOS
-- SQL SERVER
-- ==========================================

IF DB_ID('ArenaBook') IS NULL
    CREATE DATABASE ArenaBook;
GO

USE ArenaBook;
GO

-- ==========================================
-- 1. CLIENTES
-- ==========================================

CREATE TABLE Clientes (
    IdCliente INT IDENTITY(1,1) PRIMARY KEY,
    Nome NVARCHAR(100) NOT NULL,
    Telefone VARCHAR(20) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE
);

-- ==========================================
-- 2. ESPORTE
-- ==========================================

CREATE TABLE Esporte (
    IdEsporte INT IDENTITY(1,1) PRIMARY KEY,
    Nome NVARCHAR(100) NOT NULL UNIQUE
);

-- ==========================================
-- 3. QUADRA
-- ==========================================

CREATE TABLE Quadra (
    IdQuadra INT IDENTITY(1,1) PRIMARY KEY,
    Nome NVARCHAR(100) NOT NULL,
    PrecoHora DECIMAL(10,2) NOT NULL,
    Ativa BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_Quadra_Preco
        CHECK (PrecoHora > 0)
);

-- ==========================================
-- 4. QUADRA / ESPORTE
-- ==========================================

CREATE TABLE Quadra_Esporte (
    IdQuadra INT NOT NULL,
    IdEsporte INT NOT NULL,

    CONSTRAINT PK_Quadra_Esporte
        PRIMARY KEY (IdQuadra, IdEsporte),

    CONSTRAINT FK_QuadraEsporte_Quadra
        FOREIGN KEY (IdQuadra)
        REFERENCES Quadra(IdQuadra),

    CONSTRAINT FK_QuadraEsporte_Esporte
        FOREIGN KEY (IdEsporte)
        REFERENCES Esporte(IdEsporte)
);

-- ==========================================
-- 5. HORÁRIO DE FUNCIONAMENTO
-- ==========================================

CREATE TABLE HorarioFuncionamento (
    IdHorario INT IDENTITY(1,1) PRIMARY KEY,
    IdQuadra INT NOT NULL,
    DiaSemana TINYINT NOT NULL,
    HoraAbertura TIME(0) NOT NULL,
    HoraFechamento TIME(0) NOT NULL,

    CONSTRAINT FK_Horario_Quadra
        FOREIGN KEY (IdQuadra)
        REFERENCES Quadra(IdQuadra),

    CONSTRAINT CK_Horario_DiaSemana
        CHECK (DiaSemana BETWEEN 1 AND 7),

    CONSTRAINT CK_Horario_Intervalo
        CHECK (HoraFechamento > HoraAbertura),

    CONSTRAINT UQ_Horario_Inicio
        UNIQUE (IdQuadra, DiaSemana, HoraAbertura)
);

-- ==========================================
-- 6. RESERVA
-- ==========================================

CREATE TABLE Reserva (
    IdReserva INT IDENTITY(1,1) PRIMARY KEY,

    IdQuadra INT NOT NULL,
    IdEsporte INT NOT NULL,
    IdCliente INT NOT NULL,

    DataReserva DATE NOT NULL,
    HoraInicio TIME(0) NOT NULL,
    HoraFim TIME(0) NOT NULL,

    ValorTotal DECIMAL(10,2) NOT NULL,
    IdStatus INT NOT NULL DEFAULT 1,

    DataCriacao DATETIME2(0) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Reserva_Cliente
        FOREIGN KEY (IdCliente)
        REFERENCES Clientes(IdCliente),

    CONSTRAINT FK_Reserva_QuadraEsporte
        FOREIGN KEY (IdQuadra, IdEsporte)
        REFERENCES Quadra_Esporte(IdQuadra, IdEsporte),

    CONSTRAINT CK_Reserva_Horario
        CHECK (HoraFim > HoraInicio),

    CONSTRAINT CK_Reserva_Valor
        CHECK (ValorTotal >= 0),

    CONSTRAINT CK_Reserva_Status
        CHECK (IdStatus IN (1, 2, 3, 4))
);

-- ==========================================
-- 7. ÍNDICES PARA CONSULTAS
-- ==========================================

CREATE INDEX IX_Reserva_Quadra_Data
ON Reserva (IdQuadra, DataReserva, IdStatus)
INCLUDE (HoraInicio, HoraFim);

CREATE INDEX IX_Reserva_Cliente
ON Reserva (IdCliente, DataReserva);
