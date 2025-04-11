using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Server.Helper;

namespace App.Server.Database
{
    public class DbHelper
    {
        private static string connectionString = "";
        private MySqlConnection connection = null;

        public DbHelper()
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\Database\"));
                MessageBox.Show(projectRoot);
                string configPath = Path.Combine(projectRoot, "db.config");
                var parser = new IniParser(configPath);
                string connectionString = parser.Get("Database", "ConnectionString");
                connection = new MySqlConnection(connectionString);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create database connection.", ex);
            }
        }

        private static DbHelper instance = null;
        public static DbHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (typeof(DbHelper))
                    {
                        if (instance == null)
                        {
                            instance = new DbHelper();
                        }
                    }
                }
                return instance;
            }
        }

        public MySqlConnection GetConnection()
        {
            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                return connection;
            }
            catch (MySqlException ex)
            {
                throw new Exception("Error connecting to the database: " + ex.Message, ex);
            }
        }

        public void CloseConnection()
        {
            try
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Error closing the database connection: " + ex.Message, ex);
            }
        }

        public bool TestConnection()
        {
            try
            {
                using (MySqlConnection testConn = new MySqlConnection(connectionString))
                {
                    testConn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void UpdateConnectionString(string server, string database, string user, string password)
        {
            connectionString = $"server={server};database={database};user={user};password={password};";
            if (instance != null)
            {
                instance.CloseConnection();
                instance = null;
            }
        }

        // Added query execution methods

        /// <summary>
        /// Executes a SQL query that returns a result set
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <returns>DataTable containing the query results</returns>
        public DataTable ExecuteQuery(string query)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (MySqlConnection conn = GetConnection())
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                {
                    adapter.Fill(dataTable);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing query: " + ex.Message, ex);
            }
            finally
            {
                CloseConnection();
            }
            return dataTable;
        }

        /// <summary>
        /// Executes a SQL query with parameters that returns a result set
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <returns>DataTable containing the query results</returns>
        public DataTable ExecuteQueryWithParams(string query, Dictionary<string, object> parameters)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (MySqlConnection conn = GetConnection())
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    // Add parameters to the command
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing parameterized query: " + ex.Message, ex);
            }
            finally
            {
                CloseConnection();
            }
            return dataTable;
        }

        /// <summary>
        /// Executes a non-query SQL statement (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="query">The SQL statement to execute</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteNonQuery(string query)
        {
            int rowsAffected = 0;
            try
            {
                using (MySqlConnection conn = GetConnection())
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing non-query: " + ex.Message, ex);
            }
            finally
            {
                CloseConnection();
            }
            return rowsAffected;
        }

        /// <summary>
        /// Executes a non-query SQL statement with parameters (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="query">The SQL statement to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteNonQuery(string query, Dictionary<string, object> parameters)
        {
            int rowsAffected = 0;
            try
            {
                using (MySqlConnection conn = GetConnection())
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    // Add parameters to the command
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing parameterized non-query: " + ex.Message, ex);
            }
            finally
            {
                CloseConnection();
            }
            return rowsAffected;
        }

        /// <summary>
        /// Executes a query that returns a single value
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <returns>The first column of the first row in the result set</returns>
        public object ExecuteScalar(string query)
        {
            object result = null;
            try
            {
                using (MySqlConnection conn = GetConnection())
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    result = cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing scalar query: " + ex.Message, ex);
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        /// <summary>
        /// Executes a query with parameters that returns a single value
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <returns>The first column of the first row in the result set</returns>
        public object ExecuteScalar(string query, Dictionary<string, object> parameters)
        {
            object result = null;
            try
            {
                using (MySqlConnection conn = GetConnection())
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    // Add parameters to the command
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    result = cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing parameterized scalar query: " + ex.Message, ex);
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }
    }
}