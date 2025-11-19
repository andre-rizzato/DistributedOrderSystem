-- Insert the migration record to mark it as applied
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251118201034_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20251118201034_InitialCreate', '9.0.0');
    PRINT 'Migration record inserted successfully.';
END
ELSE
BEGIN
    PRINT 'Migration record already exists.';
END
