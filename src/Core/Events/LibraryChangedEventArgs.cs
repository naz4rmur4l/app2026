namespace Core.Events;

// Дані події: що саме сталося. ReaderId і DueDate заповнені лише для видачі.
public sealed class LibraryChangedEventArgs(
    string copyId, LibraryChangeKind kind, string? readerId = null, DateOnly? dueDate = null) : EventArgs
{
    public string CopyId { get; } = copyId;
    public LibraryChangeKind Kind { get; } = kind;
    public string? ReaderId { get; } = readerId;
    public DateOnly? DueDate { get; } = dueDate;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
