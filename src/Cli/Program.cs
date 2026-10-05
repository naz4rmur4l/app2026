using System.Text;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = Encoding.UTF8; // щоб кирилиця читалась у консолі Windows

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

ImportResult<BookDto> result = BookCsvImporter.Load(path);

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (BookDto b in result.Items.Take(5))
    Console.WriteLine($"  {b.Id,-6} {b.Isbn,-18} {b.Title,-34} {b.Year,5}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");
}

return 0;
