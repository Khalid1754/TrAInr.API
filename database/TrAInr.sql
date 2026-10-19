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
    WHERE [MigrationId] = N'20261019031421_InitialCreate'
)
BEGIN
    CREATE TABLE [Profiles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [BodyWeightKg] float NOT NULL,
        [HeightCm] int NOT NULL,
        [Age] int NOT NULL,
        [Sex] nvarchar(20) NOT NULL,
        [Goal] nvarchar(50) NOT NULL,
        [Experience] nvarchar(50) NOT NULL,
        [DaysPerWeek] int NOT NULL,
        [DietaryPreferences] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Profiles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019031421_InitialCreate'
)
BEGIN
    CREATE TABLE [Workouts] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Date] date NOT NULL,
        [Notes] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [AiCaloriesJson] nvarchar(max) NULL,
        [AiMealJson] nvarchar(max) NULL,
        CONSTRAINT [PK_Workouts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019031421_InitialCreate'
)
BEGIN
    CREATE TABLE [Exercises] (
        [Id] int NOT NULL IDENTITY,
        [WorkoutId] int NOT NULL,
        [SortOrder] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Sets] int NOT NULL,
        [Reps] int NOT NULL,
        [WeightKg] float NOT NULL,
        [DurationMin] float NULL,
        CONSTRAINT [PK_Exercises] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Exercises_Workouts_WorkoutId] FOREIGN KEY ([WorkoutId]) REFERENCES [Workouts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019031421_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Age', N'BodyWeightKg', N'DaysPerWeek', N'DietaryPreferences', N'Experience', N'Goal', N'HeightCm', N'Name', N'Sex') AND [object_id] = OBJECT_ID(N'[Profiles]'))
        SET IDENTITY_INSERT [Profiles] ON;
    EXEC(N'INSERT INTO [Profiles] ([Id], [Age], [BodyWeightKg], [DaysPerWeek], [DietaryPreferences], [Experience], [Goal], [HeightCm], [Name], [Sex])
    VALUES (1, 30, 75.0E0, 3, N'''', N''medel'', N''muskelbygge'', 178, N'''', N''unspecified'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Age', N'BodyWeightKg', N'DaysPerWeek', N'DietaryPreferences', N'Experience', N'Goal', N'HeightCm', N'Name', N'Sex') AND [object_id] = OBJECT_ID(N'[Profiles]'))
        SET IDENTITY_INSERT [Profiles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019031421_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Exercises_WorkoutId] ON [Exercises] ([WorkoutId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019031421_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Workouts_Date] ON [Workouts] ([Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019031421_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019031421_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

