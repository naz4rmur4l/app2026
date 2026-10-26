using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Той самий контракт, інша механіка: кеш у пам'яті, дозавантаження
// при першому зверненні, запис на диск після кожної зміни.
public sealed class FileLibraryStore(string path) : ILibraryStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly Dictionary<string, BookCopy> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            var dtos = JsonSerializer.Deserialize<List<BookCopyDto>>(File.ReadAllText(_path)) ?? [];
            foreach (var dto in dtos)
            {
                var copy = BookCopy.FromDto(dto); // ті самі перевірки, що й при створенні
                _cache[copy.Id] = copy;
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path,
            JsonSerializer.Serialize(_cache.Values.Select(c => c.ToDto()).ToList(), Options));
    }

    public IReadOnlyList<BookCopy> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public BookCopy? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}
