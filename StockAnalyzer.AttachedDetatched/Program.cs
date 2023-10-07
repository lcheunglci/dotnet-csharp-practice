
Console.WriteLine("Starting");

Task.Factory.StartNew(() =>
{
    Task.Factory.StartNew(() =>
    {
        Thread.Sleep(1000);
        Console.WriteLine("Completed 1");
    }, TaskCreationOptions.AttachedToParent);
    Task.Factory.StartNew(() =>
    {
        Thread.Sleep(2000);
        Console.WriteLine("Completed 2");
    }, TaskCreationOptions.AttachedToParent);
    Task.Factory.StartNew(() =>
    {
        Thread.Sleep(3000);
        Console.WriteLine("Completed 3");
    }, TaskCreationOptions.AttachedToParent);
});

Console.WriteLine("Completed");
Console.ReadLine();
