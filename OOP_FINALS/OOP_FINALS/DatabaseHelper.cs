using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

public class DatabaseHelper
{
    private readonly string connectionString;

    public DatabaseHelper()
    {
        var conn = ConfigurationManager.ConnectionStrings["MyDBConnectionString"];

        if (conn == null)
        {
            throw new Exception("Connection string 'MyDBConnectionString' not found in App.config");
        }

        connectionString = conn.ConnectionString;
    }

    public DataTable ExecuteQuery(string query)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("DB ERROR: " + ex.Message);
            return null;
        }
    }

    public object ExecuteScalar(string query)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            return cmd.ExecuteScalar();
        }
    }
    public DataTable ExecuteQueryDataTable(string query) // NEW METHOD
    {
        DataTable dataTable = new DataTable();

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
            {
                adapter.Fill(dataTable);
            }
        }
        return dataTable;
    }
    public object ExecuteScalarWithParam(string query, string paramName, object value)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                return cmd.ExecuteScalar();
            }
        }
    }

}