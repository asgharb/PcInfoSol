using System;
using System.Data.SqlClient;
using System.IO;

namespace ChackDb
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Path to connection string file
                string filePath = "connection.txt";

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("Connection string file 'connection.txt' not found!");
                    return;
                }

                // Read connection string from file
                string connectionString = File.ReadAllText(filePath).Trim();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine("Connection successful!");
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine("SQL Exception occurred:");
                Console.WriteLine("Error Number: " + sqlEx.Number);
                Console.WriteLine("Error State: " + sqlEx.State);
                Console.WriteLine("Error Class: " + sqlEx.Class);
                Console.WriteLine("Server: " + sqlEx.Server);
                Console.WriteLine("Message: " + sqlEx.Message);
                Console.WriteLine("StackTrace: " + sqlEx.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Exception occurred:");
                Console.WriteLine("Message: " + ex.Message);
                Console.WriteLine("StackTrace: " + ex.StackTrace);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
