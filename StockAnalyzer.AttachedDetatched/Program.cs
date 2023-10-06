
Console.WriteLine("Starting");

Task.Factory.StartNew(() =>
{
    Task.Factory.StartNew(() =>
    {
        Thread.Sleep(1000);
        Console.WriteLine("Completed 1");
    });
    Task.Factory.StartNew(() =>
    {
        Thread.Sleep(2000);
        Console.WriteLine("Completed 2");
    });
    Task.Factory.StartNew(() =>
    {
        Thread.Sleep(3000);
        Console.WriteLine("Completed 3");
    });
});

Console.WriteLine("Completed");
Console.ReadLine();
