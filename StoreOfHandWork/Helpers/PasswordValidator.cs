using System.Text.RegularExpressions;

namespace StoreOfHandWork.Helpers
{
    public static class PasswordValidator
    {
        public static (bool isValid, string errorMessage) ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (false, "Пароль не может быть пустым");

            if (password.Length < 6)
                return (false, "Пароль должен содержать минимум 6 символов");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                return (false, "Пароль должен содержать хотя бы одну заглавную букву");

            if (!Regex.IsMatch(password, @"[a-z]"))
                return (false, "Пароль должен содержать хотя бы одну строчную букву");

            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}|<>]"))
                return (false, "Пароль должен содержать хотя бы один специальный символ");

            return (true, string.Empty);
        }
    }
}
