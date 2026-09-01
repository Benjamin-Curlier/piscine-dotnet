public static class Acces
{
    public static string Classer(int age)
    {
        if (age < 18)
        {
            return "mineur";
        }

        return "majeur";
    }
}
