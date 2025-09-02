using System;
using System.Data.SqlClient;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hola Mundo");

        // 🚨 Código vulnerable a SQL Injection 🚨
        Console.WriteLine("Ingrese su nombre de usuario:");
        string userInput = Console.ReadLine();

        string connectionString = "Server=localhost;Database=TestDB;User Id=sa;Password=Your_password123;";
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = "SELECT * FROM Users WHERE username = '" + userInput + "'";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine("Bienvenido " + reader["username"]);
            }
        }
    }
}
