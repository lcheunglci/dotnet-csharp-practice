using System;

namespace Budget_Desktop_App.Models
{
    public class BudgetRow
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // "Income" or "Expense"
    }
}
