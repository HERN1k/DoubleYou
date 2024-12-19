using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DoubleYou.Infrastructure.Data.Contexts;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

using Windows.Storage;

namespace DoubleYou.Utilities
{
    public class MigrationsExtension
    {
        public static void ApplyMigrations(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDBContext>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MigrationsExtension>>();

            try
            {
                using var context = contextFactory.CreateDbContext();
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine(ex.ToString());
#endif
                logger.LogError(ex, "{AN_UNEXPECTED_ERROR_OCCURRED}{Message}", Constants.AN_UNEXPECTED_ERROR_OCCURRED, ex.Message);
                throw;
            }
        }

        public static async Task ClearDB(IServiceProvider serviceProvider)
        {
            string? executingFileName = Process.GetCurrentProcess().MainModule?.FileName;

            using var scope = serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDBContext>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MigrationsExtension>>();

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();

                var user = await context.User.ToArrayAsync();
                var words = await context.Words.ToArrayAsync();

                context.User.RemoveRange(user);
                context.Words.RemoveRange(words);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine(ex.ToString());
#endif
                logger.LogError(ex, "{AN_UNEXPECTED_ERROR_OCCURRED}{Message}", Constants.AN_UNEXPECTED_ERROR_OCCURRED, ex.Message);
                throw;
            }

            if (executingFileName != null)
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = executingFileName,
                    UseShellExecute = true
                };

                Process.Start(processStartInfo);
            }

            Application.Current.Exit();
        }

        public static void InstalDatabaseDump(string dumpFilePath)
        {
            string instaledDumpFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.BACKUP_SQL_DB_FILE_NAME);
            string? executingFileName = Process.GetCurrentProcess().MainModule?.FileName;

            if (!File.Exists(dumpFilePath))
            {
                throw new ArgumentException(Constants.FILE_NOT_EXISTS, nameof(dumpFilePath));
            }

            File.Copy(dumpFilePath, instaledDumpFilePath, overwrite: true);

            if (executingFileName != null)
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = executingFileName,
                    UseShellExecute = true
                };

                Process.Start(processStartInfo);
            }

            Application.Current.Exit();
        }

        public static void CreateDatabaseDump(string dumpDirectoryPath)
        {
            string databasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.SQL_DB_FILE_NAME);
            string dumpFilePath = Path.Combine(dumpDirectoryPath, Constants.BACKUP_SQL_DB_FILE_NAME);

            if (!Directory.Exists(dumpDirectoryPath))
            {
                throw new ArgumentException($"{Constants.INVALID_DIRECTORY_PATH}: {dumpDirectoryPath}", nameof(dumpDirectoryPath));
            }

            using var connection = new SqliteConnection($"Data Source={databasePath}");
            using var writer = new StreamWriter(dumpFilePath);

            connection.Open();

            using var getTablesCommand = connection.CreateCommand();
            getTablesCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";

            using var tablesReader = getTablesCommand.ExecuteReader();

            while (tablesReader.Read())
            {
                string tableName = tablesReader.GetString(0);

                if (tableName == Constants.EF_MIGRATION_HISTORY_TABLE_NAME)
                {
                    continue;
                }

                DumpTableData(connection, writer, tableName);
            }
        }

        public static void EnsureDatabaseRestoredFromDump()
        {
            string dumpFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.BACKUP_SQL_DB_FILE_NAME);
            string databasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.SQL_DB_FILE_NAME);

            if (!File.Exists(dumpFilePath))
            {
                return;
            }

#if DEBUG
            Debug.WriteLine(Constants.DATABASE_RESTORED_FROM_DUMP);
#endif

            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException(databasePath);
            }

            using var connection = new SqliteConnection($"Data Source={databasePath};");
            connection.Open();

            var commands = File.ReadAllText(dumpFilePath)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim() + ";")
                .ToList();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var command in commands)
                {
                    if (string.IsNullOrWhiteSpace(command) || command.Trim().StartsWith("--"))
                    {
                        continue;
                    }

                    using var sqliteCommand = connection.CreateCommand();
                    sqliteCommand.CommandText = command;
                    sqliteCommand.Transaction = transaction;

                    sqliteCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"{Constants.ERROR_IMPORTING_DUMP}: {ex.Message}");
                throw;
            }

            File.Delete(dumpFilePath);
        }

        private static void DumpTableData(SqliteConnection connection, StreamWriter writer, string tableName)
        {
            using var getColumnsCommand = connection.CreateCommand();
            getColumnsCommand.CommandText = $"PRAGMA table_info({tableName});";

            var columns = new List<string>();

            using (var columnsReader = getColumnsCommand.ExecuteReader())
            {
                while (columnsReader.Read())
                {
                    columns.Add(columnsReader.GetString(1));
                }
            }

            string columnList = string.Join(", ", columns);

            using var getDataCommand = connection.CreateCommand();
            getDataCommand.CommandText = $"SELECT * FROM {tableName};";

            using var dataReader = getDataCommand.ExecuteReader();

            while (dataReader.Read())
            {
                var values = new List<string>();

                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    if (dataReader.IsDBNull(i))
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        var value = dataReader.GetValue(i).ToString();

                        if (string.IsNullOrEmpty(value))
                        {
                            continue;
                        }

                        if (dataReader.GetFieldType(i) == typeof(string))
                        {
                            value = $"'{value.Replace("'", "''")}'";
                        }
                        values.Add(value);
                    }
                }

                string valueList = string.Join(", ", values);

                writer.WriteLine($"INSERT INTO {tableName} ({columnList}) VALUES ({valueList});");
            }
        }
    }
}