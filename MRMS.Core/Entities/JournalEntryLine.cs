namespace MRMS.Core.Entities;

public class JournalEntryLine
{
    public int Id { get; set; }
    public int JournalEntryId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }

    public JournalEntry JournalEntry { get; set; } = null!;
}
