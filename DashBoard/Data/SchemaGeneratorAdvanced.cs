using DashBoard.Attributes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DashBoard.Data
{
    public class SchemaGeneratorAdvanced
    {
        private readonly string _connectionString;

        public SchemaGeneratorAdvanced()
        {
            _connectionString = ConnctionString.GetConnctionString();
        }


        public void CreateTables(params Type[] modelTypes)
        {
            var sortedTypes = SortTypesByDependencies(modelTypes);

            foreach (var type in sortedTypes)
            {
                CreateOrAlterTable(type);
            }
        }

        private void CreateOrAlterTable(Type type)
        {
            string tableName = GetTableName(type);
            var properties = type.GetProperties();

            var sb = new StringBuilder();
            sb.AppendLine($"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}')");
            sb.AppendLine("BEGIN");
            sb.AppendLine($"CREATE TABLE {tableName} (");

            foreach (var prop in properties)
            {
                if (prop.GetCustomAttribute<IgnoreAttribute>() != null)
                    continue;

                string colName = GetColumnName(prop);
                string sqlType = SqlTypeHelper.GetSqlType(prop.PropertyType);


                string key = prop.GetCustomAttribute<KeyAttribute>() != null ? " PRIMARY KEY" : "";
                string identity = prop.GetCustomAttribute<DbGeneratedAttribute>() != null ? " IDENTITY(1,1)" : "";

                sb.AppendLine($"{colName} {sqlType}{identity}{key},");
            }

            // اضافه کردن FK
            foreach (var prop in properties)
            {
                var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();
                if (fkAttr != null)
                {
                    sb.AppendLine($"FOREIGN KEY ({prop.Name}) REFERENCES {fkAttr.RelatedTable}({fkAttr.RelatedColumn}),");
                }
            }

            sb.Length--; // حذف کامای آخر
            sb.AppendLine("\n);");
            sb.AppendLine("END");

            ExecuteNonQuery(sb.ToString());

            AlterTableAddMissingColumns(type);
        }

        private void AlterTableAddMissingColumns(Type type)
        {
            string tableName = GetTableName(type);
            var properties = type.GetProperties();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                foreach (var prop in properties)
                {
                    if (prop.GetCustomAttribute<IgnoreAttribute>() != null) continue;

                    string colName = GetColumnName(prop);
                    string sqlType = SqlTypeHelper.GetSqlType(prop.PropertyType);


                    string checkColumnSql = $@"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{colName}'";
                    using (var cmd = new SqlCommand(checkColumnSql, conn))
                    {
                        int exists = (int)cmd.ExecuteScalar();
                        if (exists == 0)
                        {
                            string alterSql = $"ALTER TABLE {tableName} ADD {colName} {sqlType};";
                            using (var alterCmd = new SqlCommand(alterSql, conn))
                            {
                                alterCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<Type> SortTypesByDependencies(Type[] types)
        {
            var graph = new Dictionary<Type, List<Type>>();

            foreach (var type in types)
            {
                var deps = type.GetProperties()
                    .Select(p => p.GetCustomAttribute<ForeignKeyAttribute>())
                    .Where(fk => fk != null)
                    .Select(fk => types.FirstOrDefault(t => GetTableName(t) == fk.RelatedTable))
                    .Where(t => t != null)
                    .ToList();

                graph[type] = deps;
            }

            return TopologicalSort(graph);
        }

        private IEnumerable<Type> TopologicalSort(Dictionary<Type, List<Type>> graph)
        {
            var result = new List<Type>();
            var visited = new HashSet<Type>();

            void Visit(Type node)
            {
                if (!visited.Contains(node))
                {
                    visited.Add(node);
                    foreach (var dep in graph[node])
                        Visit(dep);
                    result.Add(node);
                }
            }

            foreach (var node in graph.Keys)
                Visit(node);

            return result;
        }

        private string GetTableName(Type type)
        {
            var attr = type.GetCustomAttribute<TableAttribute>();
            return attr != null ? attr.Name : type.Name;
        }

        private string GetColumnName(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<ColumnAttribute>();
            return attr != null ? attr.Name : prop.Name;
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}