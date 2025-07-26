namespace HR_Application;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

public class OracleDataService
{
    private readonly string _connectionString;

    public OracleDataService(IConfiguration configuration)
    {
         _connectionString = configuration.GetConnectionString("OracleConnection");
    }

    public async Task<List<string>> GetSomeDataAsync()
    {
        var data = new List<string>();
        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new OracleCommand("SELECT employee_id FROM hr_employees", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        data.Add(reader.GetString(0));
                    }
                }
            }
        }
        return data;
    }

    public async Task<List<Dictionary<string, object>>> GetEmployees()
    {
        var results = new List<Dictionary<string, object>>();
        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new OracleCommand("SELECT * FROM hr_employees", connection))
            {
 
                    using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);
                            object value = reader.GetValue(i);

                            row[columnName] = value is DBNull ? null : value;
                        }

                        results.Add(row);
                    }
                }
            }
        }
        return results;
    }


    public async Task<List<Dictionary<string, object>>> GetData(int type)
    {
        var tableMap = new Dictionary<int, string>
    {
        { 0, "hr_employees" },
        { 1, "hr_jobs" },
        { 2, "hr_departments" },
    };
        if (!tableMap.TryGetValue(type, out string tableName))
        {
            throw new ArgumentException($"Invalid data type: {type}");
        }

        var results = new List<Dictionary<string, object>>();
        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = $@"
            SELECT * 
            FROM {tableName}";  

            using (var command = new OracleCommand(query, connection))
            {

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);
                            object value = reader.GetValue(i);

                            row[columnName] = value is DBNull ? null : value;
                        }

                        results.Add(row);
                    }
                }
            }
        }
        return results;
    }


}
