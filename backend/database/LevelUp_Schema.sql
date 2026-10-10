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
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE TABLE [Activity] (
        [ActivityId] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Status] nvarchar(20) NOT NULL,
        [StartDate] date NOT NULL,
        [RetiredDate] date NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Activity] PRIMARY KEY ([ActivityId]),
        CONSTRAINT [CK_Activity_RetiredDate] CHECK ([RetiredDate] IS NULL OR [RetiredDate] >= [StartDate])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE TABLE [Goal] (
        [GoalId] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [TargetDate] date NULL,
        [ProgressPercentage] int NOT NULL,
        [CompletedDate] date NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Goal] PRIMARY KEY ([GoalId]),
        CONSTRAINT [CK_Goal_ProgressPercentage] CHECK ([ProgressPercentage] BETWEEN 0 AND 100)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE TABLE [Recommendations] (
        [RecommendationId] int NOT NULL IDENTITY,
        [RecommendationText] nvarchar(500) NOT NULL,
        [Category] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Recommendations] PRIMARY KEY ([RecommendationId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE TABLE [ActivityScores] (
        [ActivityScoreId] int NOT NULL IDENTITY,
        [ActivityId] int NOT NULL,
        [EvaluationDate] date NOT NULL,
        [Score] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_ActivityScores] PRIMARY KEY ([ActivityScoreId]),
        CONSTRAINT [CK_ActivityScores_Score] CHECK ([Score] BETWEEN 0 AND 5),
        CONSTRAINT [FK_ActivityScores_Activity_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activity] ([ActivityId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE TABLE [GoalCheckpoints] (
        [GoalCheckpointId] int NOT NULL IDENTITY,
        [GoalId] int NOT NULL,
        [CheckpointDate] date NOT NULL,
        [PercentComplete] int NOT NULL,
        [Note] nvarchar(1000) NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_GoalCheckpoints] PRIMARY KEY ([GoalCheckpointId]),
        CONSTRAINT [CK_GoalCheckpoints_PercentComplete] CHECK ([PercentComplete] BETWEEN 0 AND 100),
        CONSTRAINT [FK_GoalCheckpoints_Goal_GoalId] FOREIGN KEY ([GoalId]) REFERENCES [Goal] ([GoalId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE TABLE [RecommendationHistory] (
        [RecommendationHistoryId] int NOT NULL IDENTITY,
        [RecommendationId] int NULL,
        [RecommendationText] nvarchar(500) NOT NULL,
        [ActivityId] int NULL,
        [GoalId] int NULL,
        [Trigger] nvarchar(30) NOT NULL,
        [ShownDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_RecommendationHistory] PRIMARY KEY ([RecommendationHistoryId]),
        CONSTRAINT [FK_RecommendationHistory_Activity_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activity] ([ActivityId]) ON DELETE SET NULL,
        CONSTRAINT [FK_RecommendationHistory_Goal_GoalId] FOREIGN KEY ([GoalId]) REFERENCES [Goal] ([GoalId]) ON DELETE SET NULL,
        CONSTRAINT [FK_RecommendationHistory_Recommendations_RecommendationId] FOREIGN KEY ([RecommendationId]) REFERENCES [Recommendations] ([RecommendationId]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RecommendationId', N'Category', N'CreatedBy', N'CreatedDate', N'IsActive', N'ModifiedBy', N'ModifiedDate', N'RecommendationText') AND [object_id] = OBJECT_ID(N'[Recommendations]'))
        SET IDENTITY_INSERT [Recommendations] ON;
    EXEC(N'INSERT INTO [Recommendations] ([RecommendationId], [Category], [CreatedBy], [CreatedDate], [IsActive], [ModifiedBy], [ModifiedDate], [RecommendationText])
    VALUES (1, N''Activity'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Reschedule the activity to a better time of day when you have more energy.''),
    (2, N''Activity'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Break the activity into smaller tasks you can finish in under 10 minutes.''),
    (3, N''Activity'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Set a reminder on your phone or calendar for tomorrow.''),
    (4, N''Activity'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Reduce the task complexity - do a minimal version on busy days.''),
    (5, N''Activity'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Improve consistency by doing it at the same time and place every day.''),
    (6, N''Activity'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Stack it onto an existing habit (e.g. right after your morning coffee).''),
    (7, N''Activity'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Prepare everything you need the night before to remove friction.''),
    (8, N''Goal'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Break the goal into monthly milestones and focus only on the next one.''),
    (9, N''Goal'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Schedule a weekly 15-minute review of this goal in your calendar.''),
    (10, N''Goal'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Share the goal with a friend or mentor for accountability.''),
    (11, N''Goal'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Identify the single biggest obstacle and plan one action to remove it.''),
    (12, N''Goal'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Revisit the target date - an achievable deadline beats an abandoned one.''),
    (13, N''General'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Celebrate small wins - progress is progress.''),
    (14, N''General'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Track it visibly: a streak you can see is a streak you will protect.'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RecommendationId', N'Category', N'CreatedBy', N'CreatedDate', N'IsActive', N'ModifiedBy', N'ModifiedDate', N'RecommendationText') AND [object_id] = OBJECT_ID(N'[Recommendations]'))
        SET IDENTITY_INSERT [Recommendations] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Activity_Status] ON [Activity] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Activity_Title] ON [Activity] ([Title]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ActivityScores_ActivityId_EvaluationDate] ON [ActivityScores] ([ActivityId], [EvaluationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityScores_EvaluationDate] ON [ActivityScores] ([EvaluationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Goal_ProgressPercentage] ON [Goal] ([ProgressPercentage]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GoalCheckpoints_GoalId_CheckpointDate] ON [GoalCheckpoints] ([GoalId], [CheckpointDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecommendationHistory_ActivityId] ON [RecommendationHistory] ([ActivityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecommendationHistory_GoalId] ON [RecommendationHistory] ([GoalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecommendationHistory_RecommendationId] ON [RecommendationHistory] ([RecommendationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecommendationHistory_ShownDate] ON [RecommendationHistory] ([ShownDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Recommendations_Category_IsActive] ON [Recommendations] ([Category], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010172855_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010172855_InitialCreate', N'9.0.9');
END;

COMMIT;
GO

