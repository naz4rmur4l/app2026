using Core.Domain;
using Core.Dto;
using Core.Import;

// Демонстрації попередніх лабораторних: імпорт CSV (лаб. 3) і доменна модель (лаб. 4).
internal static class Demos
{
    public static int RunDomain()
    {
        Console.WriteLine("=== Сценарій 1: успіх ===");
        BookCopy copy = BookCopy.Create("C-001", "978-966-00-0001-4");
        Console.WriteLine(copy);

        Loan loan = Loan.Open("L-001", copy, "R-100", new DateOnly(2026, 10, 1));
        Console.WriteLine(loan);
        Console.WriteLine(copy);

        loan.Close(copy, new DateOnly(2026, 10, 15));
        Console.WriteLine(loan);
        Console.WriteLine(copy);

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

        BookCopy copy2 = BookCopy.Create("C-002", "978-966-00-0002-1");
        Loan loan2 = Loan.Open("L-002", copy2, "R-101", new DateOnly(2026, 10, 10));

        TryDo("порожній ISBN", () => BookCopy.Create("C-003", " "));
        TryDo("повторна видача виданого примірника",
            () => Loan.Open("L-003", copy2, "R-102", new DateOnly(2026, 10, 11)));
        TryDo("порожній читач", () => Loan.Open("L-004", copy, " ", new DateOnly(2026, 10, 11)));
        TryDo("повернення раніше за видачу", () => loan2.Close(copy2, new DateOnly(2026, 10, 1)));
        TryDo("закрити вже закриту видачу", () => loan.Close(copy, new DateOnly(2026, 10, 20)));
        TryDo("повернути примірник, який не видано", () => copy.Return());
        TryDo("видача не того примірника", () => loan2.Close(copy, new DateOnly(2026, 10, 20)));

        Console.WriteLine();
        Console.WriteLine("Стан після відмов (нічого не змінилось):");
        Console.WriteLine($"  {copy2}");
        Console.WriteLine($"  {loan2}");

        Console.WriteLine();
        Console.WriteLine("=== Мапінг ToDto / FromDto ===");
        LoanDto dto = loan.ToDto();
        Console.WriteLine($"  {dto}");
        Console.WriteLine($"  відновлено: {Loan.FromDto(dto)}");
        TryDo("FromDto з пошкодженим DTO",
            () => Loan.FromDto(dto with { ReturnedOn = new DateOnly(2000, 1, 1) }));

        return 0;
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }

    public static int RunImport(string path)
    {
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
    }
}
