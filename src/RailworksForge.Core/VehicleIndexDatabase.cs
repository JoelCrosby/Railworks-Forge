using Microsoft.Data.Sqlite;

using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core;

internal sealed class VehicleIndexDatabase
{
    private readonly string _databasePath;
    private readonly object _initializationLock = new();
    private bool _initialized;

    public VehicleIndexDatabase(string databasePath)
    {
        _databasePath = databasePath;

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
    }

    public RefreshSession BeginRefresh()
    {
        var connection = Open();

        try
        {
            return new RefreshSession(connection);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false,
        }.ToString());

        try
        {
            connection.Open();

            lock (_initializationLock)
            {

                if (_initialized)
                {
                    return connection;
                }

                using var command = connection.CreateCommand();
                command.CommandText =
                """
                PRAGMA journal_mode=WAL;
                CREATE TABLE IF NOT EXISTS sources(path TEXT PRIMARY KEY, stamp TEXT NOT NULL, seen INTEGER NOT NULL);
                CREATE TABLE IF NOT EXISTS vehicles(
                    source TEXT NOT NULL, identity TEXT NOT NULL, priority INTEGER NOT NULL,
                    name TEXT NOT NULL, locomotive TEXT NOT NULL, type INTEGER NOT NULL,
                    provider TEXT NOT NULL, product TEXT NOT NULL, blueprint TEXT NOT NULL);
                CREATE INDEX IF NOT EXISTS vehicle_source ON vehicles(source);
                CREATE VIRTUAL TABLE IF NOT EXISTS search USING fts5(
                    name, locomotive, provider, product, blueprint, type UNINDEXED);
                """;

                command.ExecuteNonQuery();

                _initialized = true;
            }
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        return connection;
    }

    public VehicleSearchResult Search(string? search, int limit, CancellationToken token)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction(deferred: true);
        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        var terms = (search ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var match = string.Join(" AND ", terms.Select(term => "\"" + term.Replace("\"", "\"\"") + "\"*"));
        var filter = terms.Length == 0 ? "" : " WHERE search MATCH $query";

        command.Parameters.AddWithValue("$query", match);
        command.CommandText = "SELECT count(*) FROM search" + filter;

        token.ThrowIfCancellationRequested();

        var count = Convert.ToInt32(command.ExecuteScalar());
        command.CommandText = "SELECT name, locomotive, type, provider, product, blueprint FROM search" + filter
            + " ORDER BY name COLLATE NOCASE, provider, product, blueprint LIMIT $limit";
        command.Parameters.AddWithValue("$limit", Math.Max(limit, 1));

        using var reader = command.ExecuteReader();

        var vehicles = new List<RollingStockEntry>();

        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            vehicles.Add(new RollingStockEntry
            {
                DisplayName = reader.GetString(0),
                LocomotiveName = reader.GetString(1),
                BlueprintType = (BlueprintType)reader.GetInt32(2),
                Blueprint = new Blueprint
                {
                    BlueprintSetIdProvider = reader.GetString(3),
                    BlueprintSetIdProduct = reader.GetString(4),
                    BlueprintId = reader.GetString(5),
                },
            });
        }

        return new VehicleSearchResult(vehicles, count);
    }

    internal sealed class RefreshSession : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SqliteTransaction _transaction;

        internal RefreshSession(SqliteConnection connection)
        {
            _connection = connection;
            _transaction = connection.BeginTransaction();

            try
            {
                using var command = CreateCommand("UPDATE sources SET seen=0");
                command.ExecuteNonQuery();
            }
            catch
            {
                _transaction.Dispose();
                throw;
            }
        }

        public string? GetSourceStamp(string source)
        {
            using var command = CreateCommand("SELECT stamp FROM sources WHERE path=$source");
            command.Parameters.AddWithValue("$source", source);
            var stamp = command.ExecuteScalar() as string;

            return stamp;
        }

        public void DeleteSourceVehicles(string source)
        {
            using var command = CreateCommand("DELETE FROM vehicles WHERE source=$source");
            command.Parameters.AddWithValue("$source", source);
            command.ExecuteNonQuery();
        }

        public void SaveSource(string source, string stamp)
        {
            using var command = CreateCommand("INSERT OR REPLACE INTO sources VALUES($source,$stamp,1)");
            command.Parameters.AddWithValue("$source", source);
            command.Parameters.AddWithValue("$stamp", stamp);
            command.ExecuteNonQuery();
        }

        public void AddVehicle(string source, string identity, int priority, RollingStockEntry vehicle)
        {
            using var command = CreateCommand(
            """
            INSERT INTO vehicles VALUES(
                $source,
                $identity,
                $priority,
                $name,
                $locomotive,
                $type,
                $provider,
                $product,
                $blueprint
            )
            """);

            command.Parameters.AddWithValue("$source", source);
            command.Parameters.AddWithValue("$identity", identity);
            command.Parameters.AddWithValue("$priority", priority);
            command.Parameters.AddWithValue("$name", vehicle.DisplayName);
            command.Parameters.AddWithValue("$locomotive", vehicle.LocomotiveName);
            command.Parameters.AddWithValue("$type", (int)vehicle.BlueprintType);
            command.Parameters.AddWithValue("$provider", vehicle.Blueprint.BlueprintSetIdProvider);
            command.Parameters.AddWithValue("$product", vehicle.Blueprint.BlueprintSetIdProduct);
            command.Parameters.AddWithValue("$blueprint", vehicle.Blueprint.BlueprintId);

            command.ExecuteNonQuery();
        }

        public int Commit(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using var command = CreateCommand(
            """
            DELETE FROM vehicles WHERE source IN (SELECT path FROM sources WHERE seen=0);
            DELETE FROM sources WHERE seen=0;
            DELETE FROM search;
            INSERT INTO search(name,locomotive,provider,product,blueprint,type)
            SELECT name,
                locomotive,
                provider,
                product,
                blueprint,
                type FROM (
                    SELECT *, row_number() OVER(PARTITION BY identity ORDER BY priority DESC, source) AS ordinal
                    FROM vehicles)
                WHERE ordinal=1;
            """);

            command.ExecuteNonQuery();

            token.ThrowIfCancellationRequested();

            _transaction.Commit();

            command.Transaction = null;
            command.CommandText = "SELECT count(*) FROM search";

            var total = Convert.ToInt32(command.ExecuteScalar());

            return total;
        }

        private SqliteCommand CreateCommand(string sql)
        {
            var command = _connection.CreateCommand();
            command.Transaction = _transaction;
            command.CommandText = sql;

            return command;
        }

        public void Dispose()
        {
            try
            {
                _transaction.Dispose();
            }
            finally
            {
                _connection.Dispose();
            }
        }
    }
}
