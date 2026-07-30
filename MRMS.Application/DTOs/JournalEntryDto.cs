namespace MRMS.Application.DTOs;

public record JournalEntryDto(
    int Id,
    DateTime Date,
    string Description,
    string ReferenceType,
    int? ReferenceId,
    DateTime CreatedAt,
    List<JournalEntryLineDto> Lines);

public record JournalEntryLineDto(
    int Id,
    string AccountName,
    decimal DebitAmount,
    decimal CreditAmount);

public record TrialBalanceRowDto(
    string AccountName,
    decimal TotalDebits,
    decimal TotalCredits,
    decimal Balance);

public record TrialBalanceDto(
    List<TrialBalanceRowDto> Rows,
    decimal GrandTotalDebits,
    decimal GrandTotalCredits,
    bool IsBalanced);
