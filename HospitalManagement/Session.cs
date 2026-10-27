using System.Security.Cryptography;
using System.Text;

namespace HospitalManagement
{
    /// <summary>Who is logged in right now.</summary>
    static class Session
    {
        public static int UserId { get; set; }
        public static string Role { get; set; }
        public static string FullName { get; set; }

        public static void Clear()
        {
            UserId = 0;
            Role = null;
            FullName = null;
        }
    }

    static class Security
    {
        /// <summary>SHA-256 as 64 uppercase hex chars (matches HASHBYTES in schema.sql).</summary>
        public static string Hash(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }
    }
}
