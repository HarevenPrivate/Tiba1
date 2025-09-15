using System.Text.RegularExpressions;

namespace IteamRepositoryAPI.Services;

public static class InputValidation
{
    static private readonly string _passwordPatternValidation = @"^[^\s'"";><&]{4,50}$";
    static private readonly string _userNamePatternValidation = @"^[a-zA-Z0-9_]{3,30}$";
    static private readonly string _emailPatternValidation = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    static public bool PasswordValidation(string password)
    {
        return Regex.IsMatch(password, _passwordPatternValidation);
    }

    static public bool UserNameValidation(string userName)
    {
        return Regex.IsMatch(userName, _userNamePatternValidation);
    }

    static public bool EmailValidation(string email)
    {
        return Regex.IsMatch(email, _emailPatternValidation);
    }

    static public bool RoleValidation(string role)
    {
        if (string.IsNullOrEmpty(role))
            return false;
        if (role != "User" && role != "Admin")
            return false;
        return true;
    }
}
