using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public static class BookCsvImporter
{
    // Роздільник — крапка з комою: не конфліктує з комою в назвах книг.
    private const char Separator = ';';

    // Перші друковані книги з'явилися близько 1450 року.
    private const int MinYear = 1450;

    public static ImportResult<BookDto> Load(string path)
    {
        var items = new List<BookDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1; // людські номери рядків: з 1
            string line = lines[i];

            // порожні рядки та коментарі — не помилки, просто пропускаємо
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // рядок заголовків (id;isbn;...) — лише перший рядок файлу
            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<BookDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        // Гілки перевіряються зверху вниз: спочатку конкретні, наприкінці _.
        return parts switch
        {
            { Length: < 4 }
                => new ParseFailed($"очікую 4 колонки, отримав {parts.Length}"),

            [_, "", _, _] or [_, _, "", _]
                => new ParseFailed("ISBN або назва порожні"),

            [_, _, _, var year] when !int.TryParse(
                    year, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                => new ParseFailed($"рік '{year}' не є числом"),

            [_, _, _, var year] when IsOutOfRange(int.Parse(year, CultureInfo.InvariantCulture))
                => new ParseFailed(
                    $"рік '{year}' поза допустимими межами ({MinYear}–{DateTime.Now.Year})"),

            [var id, var isbn, var title, var year]
                => new ParseOk(new BookDto(id, isbn, title,
                    int.Parse(year, CultureInfo.InvariantCulture))),

            _ => new ParseFailed($"забагато колонок: {parts.Length}")
        };
    }

    private static bool IsOutOfRange(int year) => year < MinYear || year > DateTime.Now.Year;

    // Внутрішня «кухня» розбору: назовні видно лише ImportResult<BookDto>.
    private abstract record ParseOutcome;
    private sealed record ParseOk(BookDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
