using System;

public static class Division
{
    public static int Calculer(int a, int b)
    {
        if (b == 0)
        {
            throw new ArgumentException("Le diviseur doit être non nul.", nameof(b));
        }

        return a / b;
    }
}
