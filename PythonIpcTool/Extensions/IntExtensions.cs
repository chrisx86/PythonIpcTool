namespace PythonIpcTool.Extensions;

public static class IntExtensions
{
    public static string ToOrdinal(this int number)
    {
        if (number <= 0) return number.ToString();

        int rem100 = number % 100;
        int rem10 = number % 10;

        string suffix;

        if (rem100 >= 11 && rem100 <= 13)
        {
            suffix = "th";
        }
        else
        {
            switch (rem10)
            {
                case 1:
                    suffix = "st";
                    break;
                case 2:
                    suffix = "nd";
                    break;
                case 3:
                    suffix = "rd";
                    break;
                default:
                    suffix = "th";
                    break;
            }
        }
        return number + suffix;
    }
}
