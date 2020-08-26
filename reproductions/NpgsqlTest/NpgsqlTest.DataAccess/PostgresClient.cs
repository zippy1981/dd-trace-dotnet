using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Npgsql;

namespace NpgsqlTest.DataAccess
{
    public class PostgresClient : IDisposable
    {
        private readonly DbCommand _command;

        public PostgresClient(string connectionString)
        {
            var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            _command = new NpgsqlCommand("", connection);
        }

        public async Task<DbDataReader> Query(string commandText)
        {
            _command.CommandText = commandText;
            return await _command.ExecuteReaderAsync();
        }

        public void Dispose()
        {
            _command?.Dispose();
        }
    }
}
