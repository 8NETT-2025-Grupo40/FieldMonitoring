IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE TABLE [Fields] (
        [FieldId] nvarchar(100) NOT NULL,
        [FarmId] nvarchar(100) NOT NULL,
        [SensorId] nvarchar(100) NULL,
        [Status] int NOT NULL,
        [StatusReason] nvarchar(500) NULL,
        [LastReadingAt] datetime2 NULL,
        [LastSoilMoisture] float NULL,
        [LastSoilTemperature] float NULL,
        [LastAirTemperature] float NULL,
        [LastAirHumidity] float NULL,
        [LastRain] float NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [LastTimeAboveDryThreshold] datetime2 NULL,
        [LastTimeBelowHeatThreshold] datetime2 NULL,
        [LastTimeAboveFrostThreshold] datetime2 NULL,
        [LastTimeAboveDryAirThreshold] datetime2 NULL,
        [LastTimeBelowHumidAirThreshold] datetime2 NULL,
        CONSTRAINT [PK_Fields] PRIMARY KEY ([FieldId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE TABLE [ProcessedReadings] (
        [ReadingId] nvarchar(200) NOT NULL,
        [FieldId] nvarchar(100) NOT NULL,
        [ProcessedAt] datetime2 NOT NULL,
        [Source] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_ProcessedReadings] PRIMARY KEY ([ReadingId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE TABLE [Alerts] (
        [AlertId] uniqueidentifier NOT NULL,
        [FarmId] nvarchar(100) NOT NULL,
        [FieldId] nvarchar(100) NOT NULL,
        [AlertType] nvarchar(50) NOT NULL,
        [Severity] int NULL,
        [Status] nvarchar(50) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [StartedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Alerts] PRIMARY KEY ([AlertId]),
        CONSTRAINT [FK_Alerts_Fields_FieldId] FOREIGN KEY ([FieldId]) REFERENCES [Fields] ([FieldId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE INDEX [IX_Alerts_FarmId_Status] ON [Alerts] ([FarmId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE INDEX [IX_Alerts_FieldId_AlertType_Status] ON [Alerts] ([FieldId], [AlertType], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE INDEX [IX_Alerts_FieldId_StartedAt] ON [Alerts] ([FieldId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE INDEX [IX_Fields_FarmId] ON [Fields] ([FarmId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE INDEX [IX_Fields_FarmId_Status] ON [Fields] ([FarmId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE INDEX [IX_Fields_Status] ON [Fields] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    CREATE INDEX [IX_ProcessedReadings_FieldId] ON [ProcessedReadings] ([FieldId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260117173851_initial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260117173851_initial', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProcessedReadings]') AND [c].[name] = N'ProcessedAt');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ProcessedReadings] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [ProcessedReadings] ALTER COLUMN [ProcessedAt] datetimeoffset NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fields]') AND [c].[name] = N'UpdatedAt');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Fields] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Fields] ALTER COLUMN [UpdatedAt] datetimeoffset NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fields]') AND [c].[name] = N'LastTimeBelowHumidAirThreshold');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Fields] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Fields] ALTER COLUMN [LastTimeBelowHumidAirThreshold] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fields]') AND [c].[name] = N'LastTimeBelowHeatThreshold');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Fields] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Fields] ALTER COLUMN [LastTimeBelowHeatThreshold] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fields]') AND [c].[name] = N'LastTimeAboveFrostThreshold');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Fields] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Fields] ALTER COLUMN [LastTimeAboveFrostThreshold] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fields]') AND [c].[name] = N'LastTimeAboveDryThreshold');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Fields] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Fields] ALTER COLUMN [LastTimeAboveDryThreshold] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fields]') AND [c].[name] = N'LastTimeAboveDryAirThreshold');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Fields] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Fields] ALTER COLUMN [LastTimeAboveDryAirThreshold] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fields]') AND [c].[name] = N'LastReadingAt');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Fields] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Fields] ALTER COLUMN [LastReadingAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DROP INDEX [IX_Alerts_FieldId_StartedAt] ON [Alerts];
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Alerts]') AND [c].[name] = N'StartedAt');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Alerts] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Alerts] ALTER COLUMN [StartedAt] datetimeoffset NOT NULL;
    CREATE INDEX [IX_Alerts_FieldId_StartedAt] ON [Alerts] ([FieldId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Alerts]') AND [c].[name] = N'ResolvedAt');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Alerts] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Alerts] ALTER COLUMN [ResolvedAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Alerts]') AND [c].[name] = N'CreatedAt');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Alerts] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [Alerts] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122230322_DatetimeOffset'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260122230322_DatetimeOffset', N'10.0.2');
END;

COMMIT;
GO

