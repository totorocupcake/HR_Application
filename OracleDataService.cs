namespace HR_Application;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Net.NetworkInformation;
using System.Reflection;
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

    // Main method to retrieve strongly-typed results
    public async Task<List<T>> GetDataForModels<T>(int type) where T : new()
    {
        var tableMap = new Dictionary<int, string>
        {
            { 0, "hr_employees" },
            { 1, "hr_jobs" },
            { 2, "hr_departments" },
        };

        if (!tableMap.TryGetValue(type, out var tableName))
        {
            throw new ArgumentException($"Invalid data type: {type}");
        }

        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        string query = $"SELECT * FROM {tableName}";
        using var command = new OracleCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        return MapDataReaderToObjects<T>(reader);
    }

    // Core mapping logic
    private List<T> MapDataReaderToObjects<T>(IDataReader reader) where T : new()
    {
        var results = new List<T>();
        var columnMappings = GetColumnMappings<T>();

        while (reader.Read())
        {
            var obj = new T();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.GetValue(i);

                if (value == DBNull.Value) continue;

                if (columnMappings.TryGetValue(columnName, out var property))
                {
                    SetPropertyValue(obj, property, value);
                }
            }
            results.Add(obj);
        }

        return results;
    }

    // Create column-to-property mapping dictionary
    private Dictionary<string, PropertyInfo> GetColumnMappings<T>()
    {
        var mappings = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttribute?.Name ?? property.Name;
            mappings[columnName] = property;
        }

        return mappings;
    }

    // Handle type conversions safely
    private void SetPropertyValue<T>(T obj, PropertyInfo property, object value)
    {
        try
        {
            var targetType = property.PropertyType;

            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null) return;
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            // Special handling for enums
            if (targetType.IsEnum)
            {
                property.SetValue(obj, Enum.ToObject(targetType, value));
                return;
            }

            // Convert to actual property type
            var convertedValue = Convert.ChangeType(value, targetType);
            property.SetValue(obj, convertedValue);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException(
                $"Error setting value for property {property.Name}. " +
                $"Value: '{value}' ({value?.GetType().Name}), " +
                $"Target: {property.PropertyType.Name}.", ex);
        }
    }
}
