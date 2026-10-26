using System.Text;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8; // щоб кирилиця читалась у консолі Windows

// Попередні лабораторні лишаються доступними: шлях до CSV (лаб. 3) і --domain (лаб. 4).
if (args.Length > 0 && !args[0].StartsWith("--"))
    return Demos.RunImport(args[0]);
if (args.Contains("--domain"))
    return Demos.RunDomain();

// ===== Composition root: єдине місце, де названо конкретні класи =====
bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
ILibraryStore store = useFile
    ? new FileLibraryStore(dataPath)
    : new InMemoryLibraryStore(SampleData.Copies());
var service = new LendingService(store);
// =====================================================================

Console.WriteLine($"Сховище: {store.GetType().Name}");
if (useFile)
    Console.WriteLine($"Файл даних: {Path.GetFullPath(dataPath)}");
Console.WriteLine($"Записів на старті: {service.All().Count}");

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

Console.WriteLine();
Console.WriteLine("=== Сценарій 1: додати, видати, показати, знайти, повернути ===");

BookCopy created = service.AddCopy("978-966-00-0001-4");
Console.WriteLine($"Додано: {created}");

Loan loan = service.IssueCopy(created.Id, "R-100", today);
Console.WriteLine($"Видано: {loan}");

Console.WriteLine("Список:");
foreach (BookCopy c in service.All())
    Console.WriteLine($"  {c.Id,-9} {c.Isbn,-18} {(c.IsIssued ? "видано" : "у фонді")}");

BookCopy? found = service.Find(created.Id.ToUpperInvariant()); // пошук не залежить від регістру
Console.WriteLine($"Знайдено за id {created.Id.ToUpperInvariant()}: {found?.ToString() ?? "(немає)"}");

service.ReturnCopy(created.Id);
Console.WriteLine($"Повернено: {service.Find(created.Id)}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: відмови ===");
Try("повернути вдруге", () => service.ReturnCopy(created.Id));
Try("видати неіснуючий id", () => service.IssueCopy("no-such-id", "R-101", today));
Try("додати дубль id у сховище",
    () => store.Add(BookCopy.Create(created.Id.ToUpperInvariant(), "978-966-00-0002-1")));

Console.WriteLine();
Console.WriteLine($"Записів наприкінці: {service.All().Count}");
return 0;

static void Try(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: помилка — {ex.Message}");
    }
}
