BEGIN TRANSACTION;
ALTER TABLE [Patient] ADD [LastSessionSummary] nvarchar(max) NULL;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Culture', N'Key', N'Value') AND [object_id] = OBJECT_ID(N'[Translations]'))
    SET IDENTITY_INSERT [Translations] ON;
INSERT INTO [Translations] ([Id], [Culture], [Key], [Value])
VALUES (193, N'pl', N'common.logout', N'Wyloguj'),
(194, N'en', N'common.logout', N'Log out');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Culture', N'Key', N'Value') AND [object_id] = OBJECT_ID(N'[Translations]'))
    SET IDENTITY_INSERT [Translations] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251107060835_AddPatientLastSessionSummary', N'9.0.10');

COMMIT;
GO

