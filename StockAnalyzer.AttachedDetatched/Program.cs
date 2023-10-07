
Console.WriteLine("Starting");

var task = Task.Factory.StartNew((async () =>
{
    await Task.Delay(2000);
    return "Completed";
}, Unwrap();

var result = task;

Console.WriteLine("Completed");
Console.ReadLine();
