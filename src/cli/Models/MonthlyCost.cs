namespace Washington.Models;

public record MonthlyCost(
    decimal Amount,
    string Details,
    bool IsEstimated = true
);
