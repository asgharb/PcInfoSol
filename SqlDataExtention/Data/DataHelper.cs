using SqlDataExtention.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;


namespace SqlDataExtention.Data
{
    public class DataHelper
    {
        private readonly string _connectionString;

        public DataHelper()
        {
            _connectionString = ConnctionString.GetConnctionString();
        }

        public string ConnectionString => _connectionString;

        public SqlConnection GetConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public SqlConnection GetConnectionClosed()
        {
            var conn = new SqlConnection(_connectionString);
            return conn;
        }

        public bool TestConnection()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    return conn.State == ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }

        public DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteNonQuery();
            }
        }

        public object ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteScalar();
            }
        }

        public List<T> ConvertToList<T>(DataTable table) where T : new()
        {
            var list = new List<T>();

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (DataRow row in table.Rows)
            {
                T obj = new T();

                foreach (var prop in props)
                {
                    if (Attribute.IsDefined(prop, typeof(IgnoreAttribute)))
                        //    prop.Name == "InsertDate" ||
                        //    prop.Name == "ExpireDate")
                        continue;

                    string columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
                    if (!table.Columns.Contains(columnName))
                        continue;

                    var value = row[columnName];
                    if (value == DBNull.Value)
                        continue;

                    Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    object safeValue = Convert.ChangeType(value, targetType);
                    prop.SetValue(obj, safeValue);
                }

                list.Add(obj);
            }

            return list;
        }

    }
}
