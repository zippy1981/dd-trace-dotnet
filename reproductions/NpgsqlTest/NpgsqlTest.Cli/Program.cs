using System;
using System.Data.Common;
using System.Threading.Tasks;
using NpgsqlTest.DataAccess;

namespace NpgsqlTest.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            Console.WriteLine($"Opening connection to {postgresHost}...");

            using (var client = new PostgresClient($"Host={postgresHost};Username=postgres;Password=postgres;Database=postgres"))
            {
                Console.WriteLine("Excuting query...");

                using (DbDataReader reader = await client.Query("SELECT 1;"))
                {
                    while (await reader.ReadAsync())
                    {
                        object value = reader[0];
                        Console.WriteLine($"Value = {value}");
                    }
                }
            }

            Console.WriteLine("Done.");
        }
    }
}
