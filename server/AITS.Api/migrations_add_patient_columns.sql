USE [AITS-React];
GO

-- Dodanie kolumn demograficznych i adresowych do tabeli Patient
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'DateOfBirth')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [DateOfBirth] datetime2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [Gender] nvarchar(10) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'Pesel')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [Pesel] nvarchar(11) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'Street')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [Street] nvarchar(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'StreetNumber')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [StreetNumber] nvarchar(20) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'ApartmentNumber')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [ApartmentNumber] nvarchar(20) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'City')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [City] nvarchar(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'PostalCode')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [PostalCode] nvarchar(10) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'Country')
BEGIN
    ALTER TABLE [dbo].[Patient] ADD [Country] nvarchar(100) NULL;
END
GO

-- Dodanie indeksu na PESEL
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Patient]') AND name = 'IX_Patient_Pesel')
BEGIN
    CREATE INDEX [IX_Patient_Pesel] ON [dbo].[Patient] ([Pesel]);
END
GO

PRINT 'Migracja zakończona pomyślnie';
GO




