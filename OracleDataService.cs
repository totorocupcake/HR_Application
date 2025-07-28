namespace HR_Application;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using HR_Application.Models;

public class OracleDataService
{
    private readonly string? _connectionString;

    public OracleDataService(IConfiguration configuration)
    {
         _connectionString = configuration.GetConnectionString("OracleConnection");
    }


    public async Task<List<Dictionary<string, object?>>> GetData(int type)
    {
        var tableMap = new Dictionary<int, string>
    {
        { 0, "hr_employees" },
        { 1, "hr_jobs" },
        { 2, "hr_departments" },
    };
        if (!tableMap.TryGetValue(type, out string? tableName))
        {
            throw new ArgumentException($"Invalid data type: {type}");
        }

        var results = new List<Dictionary<string, object?>>();
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
                        var row = new Dictionary<string, object?>();

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


    public async Task UpdateEmployeeAsync(int id, double? salary, string? phoneNumber, string email)
    {
        using var connection = new OracleConnection(_connectionString);

        try
        {
            await connection.OpenAsync();

            using var command = new OracleCommand("Employee_update_sp", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters with proper null handling
            command.Parameters.Add(new OracleParameter("EMPLOYEE_ID", OracleDbType.Int32)).Value = id;
            command.Parameters.Add(new OracleParameter("SALARY", OracleDbType.Double)).Value = salary ?? (object)DBNull.Value;
            command.Parameters.Add(new OracleParameter("PHONE_NUMBER", OracleDbType.Varchar2)).Value = phoneNumber ?? (object)DBNull.Value;
            command.Parameters.Add(new OracleParameter("EMAIL", OracleDbType.Varchar2)).Value = email;

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            // Log the specific Oracle error
            Console.WriteLine($"Oracle Error {ex.Number}: {ex.Message}");
            throw; // Re-throw to handle in calling code
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General Error: {ex.Message}");
            throw;
        }
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
            if (targetType!.IsEnum)
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

    public async Task<bool> DeleteEmployeeAsync(int employeeId)
    {
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        // Use parameterized query to prevent SQL injection
        const string query = "DELETE FROM hr_employees WHERE employee_id = :employeeId";

        using var command = new OracleCommand(query, connection);
        command.Parameters.Add(new OracleParameter("employeeId", OracleDbType.Int32)).Value = employeeId;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        // Return true if at least one row was deleted
        return rowsAffected > 0;
    }


    public async Task<bool> addEmployeeAsync(string? firstName, string lastName, string email, double? salary, DateTime hireDate, string? phone, string jobId, int? managerId,int? departmentId)
    {
        try
        {
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new OracleCommand("Employee_hire_sp", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters with proper null handling
            command.Parameters.Add(new OracleParameter("FIRST_NAME", OracleDbType.Varchar2)).Value = firstName ?? (object)DBNull.Value;
            command.Parameters.Add(new OracleParameter("LAST_NAME", OracleDbType.Varchar2)).Value = lastName ?? (object)DBNull.Value;
            command.Parameters.Add(new OracleParameter("EMAIL", OracleDbType.Varchar2)).Value = email ?? (object)DBNull.Value;
            command.Parameters.Add(new OracleParameter("SALARY", OracleDbType.Double)).Value = salary;
            command.Parameters.Add(new OracleParameter("HIRE_DATE", OracleDbType.Date)).Value = hireDate;
            command.Parameters.Add(new OracleParameter("PHONE_NUMBER", OracleDbType.Varchar2)).Value = phone ?? (object)DBNull.Value;
            command.Parameters.Add(new OracleParameter("JOB_ID", OracleDbType.Varchar2)).Value = jobId ?? (object)DBNull.Value;
            command.Parameters.Add(new OracleParameter("MANAGER_ID", OracleDbType.Int32)).Value = managerId;
            command.Parameters.Add(new OracleParameter("DEPARTMENT_ID", OracleDbType.Int16)).Value = departmentId;

            

            await command.ExecuteNonQueryAsync();

            return true;
        }
        catch (OracleException ex)
        {
            // Log the specific Oracle error
            Console.WriteLine($"Oracle Error {ex.Number}: {ex.Message}");
            throw; // Re-throw to handle in calling code
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General Error: {ex.Message}");
            throw;
        }
    }

    public async Task<List<JobDropdownItem>> GetJobsForDropDown()
    {
        var jobs = new List<JobDropdownItem>();

        try
        {
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT distinct job_id, job_title FROM hr_jobs ORDER BY job_id";
            using var command = new OracleCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                jobs.Add(new JobDropdownItem
                {
                    JobId = reader["job_id"].ToString()!,
                    JobTitle = reader["job_title"].ToString()!
                });
            }
        }
        catch (Exception ex)
        {
            // Log error here
            Console.WriteLine($"Error fetching jobs: {ex.Message}");
            throw; // Re-throw if you want calling code to handle it
        }

        return jobs;
    }

    public async Task<List<ManagerDropDownItem>> GetManagersForDropDown()
    {
        var managers = new List<ManagerDropDownItem>();

        try
        {
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT DISTINCT employee_id, first_name, last_name " +
                      "FROM hr_employees " +
                      "WHERE employee_id IN (SELECT manager_id FROM hr_employees) " +
                      "ORDER BY last_name, first_name";

            using var command = new OracleCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                managers.Add(new ManagerDropDownItem
                {
                    ManagerId = Convert.ToInt32(reader["employee_id"]),
                    FirstName = $"{reader["first_name"]}",
                    LastName = $"{reader["last_name"]}"
                });
            }
        }
        catch (Exception ex)
        {
            // Log error here
            Console.WriteLine($"Error fetching jobs: {ex.Message}");
            throw; // Re-throw if you want calling code to handle it
        }

        return managers;
    }
    public async Task<List<DepartmentDropDownItem>> GetDepartmentsForDropDown()
    {
        var departments = new List<DepartmentDropDownItem>();

        try
        {
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT distinct department_id, department_name FROM hr_departments ORDER BY department_id";

            using var command = new OracleCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                departments.Add(new DepartmentDropDownItem
                {
                    DepartmentId = Convert.ToInt32(reader["department_id"]),
                    DepartmentName = reader["department_name"].ToString()!
                });
            }
        }
        catch (Exception ex)
        {
            // Log error here
            Console.WriteLine($"Error fetching departments: {ex.Message}");
            throw; // Re-throw if you want calling code to handle it
        }

        return departments;
    }

    public async Task<bool> DeleteJobAsync(string jobId)
    {
        return true;
    }

    public async Task UpdateJobAsync(string id, string jobTitle, int? minSalary, int? maxSalary)
    {
        await Task.CompletedTask;
    }
}


