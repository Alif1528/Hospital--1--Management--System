using System;
using System.Data;
using System.Data.SqlClient;

namespace HospitalManagement
{
    /// <summary>
    /// Central place for the connection string plus small helpers so forms stay short.
    /// All queries are parameterised: pass parameters as name, value, name, value ...
    /// </summary>
    static class DatabaseConnection
    {
        // Change the server name here if yours is different (e.g. "(localdb)\\MSSQLLocalDB"
        // or "YOUR-PC\\SQLEXPRESS"). Uses Windows authentication by default.
        public static readonly string ConnectionString =
            "Data Source=.\\SQLEXPRESS;Initial Catalog=HospitalManagement;Integrated Security=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        private static SqlCommand Build(SqlConnection con, string sql, object[] kv)
        {
            SqlCommand cmd = new SqlCommand(sql, con);
            for (int i = 0; i + 1 < kv.Length; i += 2)
            {
                object value = kv[i + 1];
                if (value == null) value = DBNull.Value;
                cmd.Parameters.AddWithValue((string)kv[i], value);
            }
            return cmd;
        }

        public static DataTable Query(string sql, params object[] kv)
        {
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = Build(con, sql, kv))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static int Execute(string sql, params object[] kv)
        {
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = Build(con, sql, kv))
            {
                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(string sql, params object[] kv)
        {
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = Build(con, sql, kv))
            {
                con.Open();
                object r = cmd.ExecuteScalar();
                return r == DBNull.Value ? null : r;
            }
        }

        /// <summary>First row of a query, or null when nothing matches.</summary>
        public static DataRow Row(string sql, params object[] kv)
        {
            DataTable dt = Query(sql, kv);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
    }
}
