using System;


namespace SimulaceBanky
{
    public static class Bank
    {
        public static ReserveAccount VaultAccount { get; } = new ReserveAccount(0);
        public static decimal CreditLimit { get; set; } = 25000m;
        public static int InterestFreePeriod = 14;
        public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);
        public static decimal GetAnnualRate(AccountType type) => type switch
        {
            AccountType.Savings => 0.015m,
            AccountType.StudentSavings => 0.02m,
            AccountType.Credit => 0.12m,
            _ => 0m
        };
        public static decimal GetMaxDailyWithdraw(AccountType type) => type switch
        {
            AccountType.Basic => 20000m,
            AccountType.Savings => 20000m,
            AccountType.StudentSavings => 10000m,
            AccountType.Credit => 10000m,
            _ => 0m
        };
        public static decimal GetMaxWithdraw(AccountType type) => type switch
        {
            AccountType.Basic => 10000m,
            AccountType.Savings => 10000m,
            AccountType.StudentSavings => 5000m,
            AccountType.Credit => 5000m,
            _ => 0m
        };
        public static decimal CalculateMonthlyInterest(AccountType accType, Dictionary<DateTime, decimal> balances)
        {
            if (balances.Count == 0)
                throw new InvalidDataException("Balances can never be null");

            var ordered = balances
                .OrderBy(kv => kv.Key)
                .ToList();

            decimal weightedSum = 0;
            int totalDays = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                DateTime currentDate = ordered[i].Key;

                decimal currentBalance;
                if (accType == AccountType.Credit)
                    currentBalance = Math.Min(ordered[i].Value, 0);
                else
                    currentBalance = ordered[i].Value;

                if (i + 1 == ordered.Count)
                {
                    int remaining = 1;

                    if (accType == AccountType.Credit && totalDays + remaining <= InterestFreePeriod)
                    {
                        totalDays += remaining;
                        break;
                    }

                    weightedSum += currentBalance * remaining;
                    totalDays += remaining;
                    break;
                }
                
                int days = (ordered[i + 1].Key - currentDate).Days;

                if (accType == AccountType.Credit)
                {
                    if (days <= InterestFreePeriod)
                    {
                        totalDays += days;
                        continue;
                    }
                }

                weightedSum += currentBalance * days;
                totalDays += days;
            }

            decimal avgBalance = weightedSum / totalDays;
            decimal rate = GetAnnualRate(accType);
            decimal interest = (avgBalance * rate) / 12m;
            return Round(interest);
        }

    }
}
