namespace Core.Reports;

// Підсумок групи: ключ, кількість примірників у групі і скільки з них зараз видано.
public sealed record GroupSummary(string Key, int Count, int Issued);
