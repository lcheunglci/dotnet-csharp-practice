using System;
using System.Collections.Generic;
using System.Linq;

namespace Budget_Desktop_App.Models
{
    public class BudgetViewModel
    {
        public string CurrentMonth { get; set; } = DateTime.Now.ToString("MMMM yyyy");
        
        public decimal CurrentMonthIncome { get; set; } = 0m;
        public decimal CurrentMonthExpenses { get; set; } = 0m;
        public decimal CurrentMonthNet { get; set; }
        
        public decimal TotalIncome { get; set; } = 0m;
        public decimal TotalExpenses { get; set; } = 0m;
        public decimal TotalNet { get; set; }
        
        public Dictionary<string, decimal> MonthlyIncome { get; set; } = new();
        public Dictionary<string, decimal> MonthlyExpenses { get; set; } = new();
        
        public string SpendingTrend { get; set; } = "Stable";
        
        public List<BudgetRow> RecentTransactions { get; set; } = new();
        
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}