var n = int.Parse(System.Console.ReadLine());

for (var i = 1; i <= n; i++)
{
    if (i % 15 == 0)
        System.Console.WriteLine("FizzBuzz");
    else if (i % 3 == 0)
        System.Console.WriteLine("Fizz");
    else if (i % 5 == 0)
        System.Console.WriteLine("Buzz");
    else
        System.Console.WriteLine(i);
}
