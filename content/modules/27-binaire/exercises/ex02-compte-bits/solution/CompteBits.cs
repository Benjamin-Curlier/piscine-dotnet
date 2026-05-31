var n = int.Parse(System.Console.ReadLine());

var compte = 0;
while (n > 0)
{
    compte += n & 1;
    n >>= 1;
}

System.Console.WriteLine(compte);
