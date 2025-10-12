namespace DriveHub.Services;

public static class IdNumberHelper
{
    public static (int Age, DateTime DateOfBirth) CalculateAgeFromId(string idNumber)
    {
        Console.WriteLine($"🔍 Testing ID: {idNumber}");

        // SUPER SIMPLE VALIDATION
        if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length != 13)
            throw new ArgumentException("Invalid ID number");

        // Check if all digits
        foreach (char c in idNumber)
            if (!char.IsDigit(c))
                throw new ArgumentException("Invalid ID number");

        try
        {
            // Extract YYMMDD
            int year = int.Parse(idNumber.Substring(0, 2));
            int month = int.Parse(idNumber.Substring(2, 2));
            int day = int.Parse(idNumber.Substring(4, 2));

            // Simple century logic
            int fullYear = (year <= 21) ? 2000 + year : 1900 + year;

            // Create date - will throw if invalid
            var birthDate = new DateTime(fullYear, month, day);

            // Calculate age
            int age = DateTime.Today.Year - birthDate.Year;
            if (birthDate > DateTime.Today.AddYears(-age)) age--;

            Console.WriteLine($"✅ SUCCESS: Age={age}");
            return (age, birthDate);
        }
        catch
        {
            throw new ArgumentException("Invalid ID number");
        }
    }

    public static bool Is18OrOver(string idNumber)
    {
        var (age, _) = CalculateAgeFromId(idNumber);
        return age >= 18;
    }
}