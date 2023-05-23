using System.Diagnostics;
using Npgsql;

const string connectionString = "Server=127.0.0.1;Port=5432;Database=Sportsbook;User Id=postgres;Password=Sa12345678;Timeout=30;";
const int connectionCount = 30;

for (var i = 0; i < 3; i++)
{
    Console.WriteLine($"進行第 {i + 1} 次連接...");
    await ConnectAndMeasureTime(connectionString, connectionCount);

    // 查詢目前的連線數量
    var count = await GetActiveConnectionCount(connectionString);
    Console.WriteLine($"目前的連線數量：{count}");

    // 在第三次連接後清除連接池
    if (i == 2)
    {
        Console.ReadLine();
        Console.WriteLine("清除連接池...");
        NpgsqlConnection.ClearAllPools();
    }
}

async Task ConnectAndMeasureTime(string connection, int numConnections)
{
    var tasks = Enumerable.Range(1, numConnections).Select(x => Task.Run(async () =>
    {
        var sw = new Stopwatch();
        sw.Start();

        await using var npgsqlConnection = new NpgsqlConnection(connection);
        // 開啟連線
        await npgsqlConnection.OpenAsync();
        sw.Stop();
        Console.WriteLine($"連線 {x} 共耗時 {sw.ElapsedMilliseconds} 毫秒");
    }));

    await Task.WhenAll(tasks);
}

async Task<long> GetActiveConnectionCount(string connection)
{
    await using var npgsqlConnection = new NpgsqlConnection(connection);
    await npgsqlConnection.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT count(1) FROM pg_stat_activity WHERE datname = 'Sportsbook'", npgsqlConnection);
    return (long)((await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException()));
}