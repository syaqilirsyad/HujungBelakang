using System.Text;
using System.Security.Cryptography;

namespace HujungBelakang.Helper
{
    public class EncyptionPass
    {
        public static string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return password;

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
