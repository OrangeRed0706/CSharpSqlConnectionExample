using System.Diagnostics;
using Npgsql;
var dbName = "TestDb";
dbName = dbName.ToLower();
const string connectionString = "Server=127.0.0.1;Port=5432;User Id=postgres;Password=Sa12345678;Timeout=30;";
const int connectionCount = 30;
var fullConnectionString = $"{connectionString};Database={dbName};";

// Check if database exists, if not create a new database
var exists = await CheckIfDatabaseExists(connectionString);
if (!exists)
{
    await CreateDatabase(connectionString);
}

for (var i = 0; i < 3; i++)
{

    await ConnectAndMeasureTime(fullConnectionString, connectionCount);

    // Querying the current number of connections
    var count = await GetActiveConnectionCount(fullConnectionString);
    Console.WriteLine($"Current connection count: {count}");

    // Clear connection pool after the third connection
    if (i == 2)
    {
        Console.ReadLine();
        Console.WriteLine("Clearing the connection pool...");
        NpgsqlConnection.ClearAllPools();
    }
}

async Task CreateDatabase(string connection)
{
    await using var npgsqlConnection = new NpgsqlConnection(connection);
    await npgsqlConnection.OpenAsync();
    await using var cmd = new NpgsqlCommand($"CREATE DATABASE {dbName}", npgsqlConnection);
    await cmd.ExecuteNonQueryAsync();
}

async Task<bool> CheckIfDatabaseExists(string connection)
{
    await using var npgsqlConnection = new NpgsqlConnection(connection);
    await npgsqlConnection.OpenAsync();
    await using var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{dbName}'", npgsqlConnection);
    var result = await cmd.ExecuteScalarAsync();
    return result != null;
}

async Task ConnectAndMeasureTime(string connection, int numConnections)
{
    var tasks = Enumerable.Range(1, numConnections).Select(x => Task.Run(async () =>
    {
        var sw = new Stopwatch();
        sw.Start();

        await using var npgsqlConnection = new NpgsqlConnection(connection);
        // Open connection
        await npgsqlConnection.OpenAsync();
        sw.Stop();
        Console.WriteLine($"Connection {x} took {sw.ElapsedMilliseconds} milliseconds");
    }));

    await Task.WhenAll(tasks);
}

async Task<long> GetActiveConnectionCount(string connection)
{
    await using var npgsqlConnection = new NpgsqlConnection(connection);
    await npgsqlConnection.OpenAsync();
    await using var cmd = new NpgsqlCommand($"SELECT count(1) FROM pg_stat_activity WHERE datname = '{dbName}'", npgsqlConnection);
    return (long)((await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException()));
}
