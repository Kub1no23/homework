using SimulaceBanky.Users;
using System.Collections.Generic;
using System.Data;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace SimulaceBanky
{
    internal abstract class Menu
    {
        public abstract MenuResult Show();
        public static string GetUserInput(string label, Func<string, bool> validator, int offset)
        {
            string input;
            do
            {
                Console.WriteLine(label);
                input = Console.ReadLine() ?? "";
                int currentLine = Console.CursorTop;

                if (input == "")
                    return input;

                if (!validator(input))
                {
                    Helper.ShiftLinesUp(offset, currentLine);
                    continue;
                }

                Helper.ShiftLinesUp(offset, currentLine, input);
                return input;
            } while (true);
        }
        public static string GetUserSelection(string label, Func<ConsoleKey, string> selector, Func<string, bool> validator, int offset)
        {
            string result;
            do
            {
                Console.WriteLine(label);
                ConsoleKey key = Console.ReadKey(intercept: true).Key;

                result = selector(key);
                Console.WriteLine(result ?? "");
                int currentLine = Console.CursorTop;
                Helper.ShiftLinesUp(offset, currentLine, result ?? "");

                if (result == "") return result;

                if (!validator(result))
                {
                    Helper.ShiftLinesUp(offset, currentLine);
                    continue;
                }

                return result;

            } while (true);
        }

    }

    internal class AdminMenu : Menu
    {
        private Admin _Admin { get; }

        public AdminMenu(Admin admin)
        {
            _Admin = admin;
        }

        public override MenuResult Show()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Welcome {_Admin.Name}. [Admin]\n");
                Console.WriteLine("1. Create a new user\n2. Edit a user\n3. Log out\nEsc: Exit");
                switch (Console.ReadKey(intercept: true).Key)
                {
                    case ConsoleKey.D1:
                        CreateUserFlow();
                        break;
                    case ConsoleKey.D2:
                        EditUserFlow();
                        break;
                    case ConsoleKey.D3:
                        return MenuResult.Logout;
                    case ConsoleKey.Escape:
                        return MenuResult.Exit;
                }
            }
        }

        private void CreateUserFlow()
        {
            Console.Clear();
            Console.WriteLine("Enter user's personal information.");

            string name = GetUserInput("Name: ", s => Regex.IsMatch(s, @"^[a-zA-Z]+$"), 1);
            if (name == "") return;

            string surname = GetUserInput("Surname: ", s => Regex.IsMatch(s, @"^[a-zA-Z]+$"), 1);
            if (surname == "") return;

            string role = GetUserSelection("Select role: 1. Admin, 2. Banker, 3. Client", key =>
            {
                return key switch
                {
                    ConsoleKey.D1 => Roles.Admin.ToString(),
                    ConsoleKey.D2 => Roles.Banker.ToString(),
                    ConsoleKey.D3 => Roles.Client.ToString(),
                    ConsoleKey.Escape => "",
                    ConsoleKey.Delete => "",
                    ConsoleKey.Enter => "",
                    _ => null
                };
            }, s => s == null ? false : true, 1);

            if (role == "") return;

            string login = GetUserInput("Login: ", s => Regex.IsMatch(s, @"^[a-zA-Z0-9]+$"), 1);
            if (login == "") return;

            string password = GetUserInput("Password: ", s => !Regex.IsMatch(s, @"\s"), 1);
            if (password == "") return;

            bool changeInfo = true;
            while (changeInfo)
            {
                Console.Clear();
                Console.WriteLine("Do you want to change any of the following information?");
                Console.WriteLine($"1. Name : {name}");
                Console.WriteLine($"2. Surname : {surname}");
                Console.WriteLine($"3. Role : {role}");
                Console.WriteLine($"4. Login : {login}");
                Console.WriteLine($"5. Password : {password}");
                Console.WriteLine("Or press any key to continue.\n");

                switch (Console.ReadKey(intercept: true).Key)
                {
                    case ConsoleKey.D1:
                        name = GetUserInput("Name: ", s => Regex.IsMatch(s, @"^[a-zA-Z]+$"), 1);
                        if (name == "") return;
                        break;
                    case ConsoleKey.D2:
                        surname = GetUserInput("Surname: ", s => Regex.IsMatch(s, @"^[a-zA-Z]+$"), 1);
                        if (surname == "") return;
                        break;
                    case ConsoleKey.D3:
                        role = GetUserSelection("Select role: 1. Admin, 2. Banker, 3. Client", key =>
                        {
                            return key switch
                            {
                                ConsoleKey.D1 => Roles.Admin.ToString(),
                                ConsoleKey.D2 => Roles.Banker.ToString(),
                                ConsoleKey.D3 => Roles.Client.ToString(),
                                ConsoleKey.Escape => "",
                                ConsoleKey.Delete => "",
                                ConsoleKey.Enter => "",
                                _ => null
                            };
                        }, s => s == null ? false : true, 1);
                        if (role == "") return;
                        break;
                    case ConsoleKey.D4:
                        login = GetUserInput("Login: ", s => Regex.IsMatch(s, @"^[a-zA-Z0-9]+$"), 1);
                        if (login == "") return;
                        break;
                    case ConsoleKey.D5:
                        password = GetUserInput("Password: ", s => !Regex.IsMatch(s, @"\s"), 1);
                        if (password == "") return;
                        break;
                    default:
                        changeInfo = false;
                        break;
                }
            }

            _Admin.CreateUser(name, surname, Enum.Parse<Roles>(role), login, password);
            Console.ReadKey(intercept: true);
        }

        private void EditUserFlow()
        {
            Console.Clear();
            User[]? users = User.GetUsers(_Admin.Connection);

            if (users == null) return;

            var sorted = users.OrderBy(u => u.Role);
            foreach (User user in sorted)
            {
                if (user.Id == _Admin.Id)
                {
                    Console.WriteLine($"{user.Id}: You [{user.Role}]");
                    continue;
                }

                Console.WriteLine($"{user.Id}: {user.Login} [{user.Role}]");
            }

            string input = GetUserInput("Select a user you want to edit: ", s => Regex.IsMatch(s, @"^\d+$"), 1);
            if (input == "") return;
            int userId = int.Parse(input);

            User selUser;
            try
            {
                selUser = users.Single(u => u.Id == userId);
            } catch
            {
                Console.WriteLine("\nIncorrect user.");
                Console.ReadKey(intercept: true);
                return;
            }

            Console.Clear();
            Console.WriteLine($"Id: {userId} \n1. Name: {selUser.Name} \n2. Surname: {selUser.Surname} \n3. Role: {selUser.Role} \n4. Login: {selUser.Login} \n5. Password\n");
            switch (Console.ReadKey(intercept: true).Key)
            {
                case ConsoleKey.D1:
                    string name = GetUserInput("Name: ", s => Regex.IsMatch(s, @"^[a-zA-Z]+$"), 1);
                    if (name == "") return;
                    _Admin.EditUser(userId, name: name);
                    break;
                case ConsoleKey.D2:
                    string surname = GetUserInput("Surname: ", s => Regex.IsMatch(s, @"^[a-zA-Z]+$"), 1);
                    if (surname == "") return;
                    _Admin.EditUser(userId, surname: surname);
                    break;
                case ConsoleKey.D3:
                    string role = GetUserSelection("Select role: 1. Admin, 2. Banker, 3. Client", key =>
                    {
                        return key switch
                        {
                            ConsoleKey.D1 => Roles.Admin.ToString(),
                            ConsoleKey.D2 => Roles.Banker.ToString(),
                            ConsoleKey.D3 => Roles.Client.ToString(),
                            ConsoleKey.Escape => "",
                            ConsoleKey.Delete => "",
                            ConsoleKey.Enter => "",
                            _ => null
                        };
                    }, s => s == null ? false : true, 1);
                    if (role == "") return;
                    _Admin.EditUser(userId, role: Enum.Parse<Roles>(role));
                    break;
                case ConsoleKey.D4:
                    string login = GetUserInput("Login: ", s => Regex.IsMatch(s, @"^[a-zA-Z0-9]+$"), 1);
                    if (login == "") return;
                    _Admin.EditUser(userId, login: login);
                    break;
                case ConsoleKey.D5:
                    string password = GetUserInput("Password: ", s => !Regex.IsMatch(s, @"\s"), 1);
                    if (password == "") return;
                    _Admin.EditUser(userId, password: password);
                    break;
                default:
                    return;
            }
            Console.ReadKey(intercept: true);
        }
    }

    internal class BankerMenu : Menu
    {
        private Banker _Banker { get; }

        public BankerMenu(Banker banker)
        {
            _Banker = banker;
        }

        public override MenuResult Show()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Welcome {_Banker.Name}. [Banker]\n");
                Console.WriteLine("1. Create a new account\n2. Inspect user accounts\n3. Show incoming interests\n4. Log out\nEsc: Exit\n");

                switch (Console.ReadKey(intercept: true).Key)
                {
                    case ConsoleKey.D1:
                        OpenAccountFlow();
                        break;
                    case ConsoleKey.D2:
                        InspectAccountsFlow();
                        break;
                    case ConsoleKey.D3:
                        ShowIncInterests();
                        break;
                    case ConsoleKey.D4:
                        return MenuResult.Logout;
                    case ConsoleKey.Escape:
                        return MenuResult.Exit;
                }
            }
        }

        public void OpenAccountFlow()
        {
            Console.Clear();
            User[]? users = User.GetUsers(_Banker.Connection);

            var clients = users.Where(u => u.Role == Roles.Client);
            if (clients == null) return;

            foreach (User client in clients)
            {
                Console.WriteLine($"{client.Id}: {client.Login} [{client.Role}]");
            }

            string input = GetUserInput("Select a client for whom you want to open an account: \n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            int clientId = int.Parse(input);

            User selClient;
            try
            {
                selClient = clients.Single(c => c.Id == clientId);
            } catch
            {
                Console.WriteLine("\nIncorrect user.");
                Console.ReadKey(intercept: true);
                return;
            }

            Console.Clear();
            Console.WriteLine("Select an account type: ");
            string accountType = GetUserSelection($"1. {AccountType.Basic.ToString()} \n2. {AccountType.Credit.ToString()} \n3. {AccountType.Savings.ToString()} \n4. {AccountType.StudentSavings.ToString()}\n", key =>
            {
                return key switch
                {
                    ConsoleKey.D1 => AccountType.Basic.ToString(),
                    ConsoleKey.D2 => AccountType.Credit.ToString(),
                    ConsoleKey.D3 => AccountType.Savings.ToString(),
                    ConsoleKey.D4 => AccountType.StudentSavings.ToString(),
                    ConsoleKey.Escape => "",
                    ConsoleKey.Delete => "",
                    ConsoleKey.Enter => "",
                    _ => null
                };
            }, s => s == null ? false : true, 5);
            if (accountType == "") return;

            Console.WriteLine();
            _Banker.OpenAccount(Enum.Parse<AccountType>(accountType), clientId);
            Console.ReadKey(intercept: true);
        }

        public void InspectAccountsFlow()
        {
            Console.Clear();
            User[]? users = User.GetUsers(_Banker.Connection);

            var clients = users.Where(u => u.Role == Roles.Client);
            if (clients == null) return;

            foreach (User client in clients)
            {
                Console.WriteLine($"{client.Id}: {client.Login} [{client.Role}]");
            }

            string input = GetUserInput("Select a client whose accounts you want to inspect: \n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            int clientId = int.Parse(input);

            Client selClient;
            try
            {
                selClient = (Client)clients.Single(c => c.Id == clientId);
            }
            catch
            {
                Console.WriteLine("\nIncorrect user.");
                Console.ReadKey(intercept: true);
                return;
            }

            Account[]? accounts = _Banker.GetAccounts(selClient);
            if (accounts == null)
            {
                Console.WriteLine("\nThis client has no opened accounts.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            foreach (Account acc in accounts)
            {
                Console.WriteLine($"{acc.Id}: {acc.AccountType.ToString()} [{acc.Balance}]");
            }

            input = GetUserInput("Select an account: \n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            int accId = int.Parse(input);

            Account selAcc;
            try
            {
                selAcc = accounts.Single(a => a.Id == accId);
            }
            catch
            {
                Console.WriteLine("\nIncorrect account.");
                Console.ReadKey(intercept: true);
                return;
            }

            Console.Clear();
            Console.WriteLine($"Owner: {selClient.Name} {selClient.Surname}\n{selAcc.AccountType.ToString()} Account\nBalance: {selAcc.Balance}\nCreated at: {selAcc.CreatedAt}");
            Console.ReadKey();
        }

        public void ShowIncInterests()
        {
            Console.Clear();

            Dictionary<Account, decimal>? accInter = _Banker.GetInterests();
            if (accInter == null)
            {
                Console.WriteLine("\nThere are no accounts opened implementing interests.");
                Console.ReadKey();
                return;
            }

            foreach (Account acc in accInter.Keys)
            {
                Console.WriteLine($"Interest for {acc.AccountType.ToString()} account {acc.Id} : {Math.Abs(accInter[acc])}");
            }

            decimal interReceive = 0;
            decimal interPay = 0;
            foreach (Account acc in accInter.Keys)
            {
               if (acc.AccountType == AccountType.Credit)
               {
                    interReceive += Math.Abs(accInter[acc]);
               } else
               {
                    interPay += accInter[acc];
               }
            }
            Console.WriteLine($"\nTotal summarry:\nInterest to pay: {interPay} | Interests to receive : {interReceive}\nGain: {interReceive - interPay}");

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

    }

    internal class ClientMenu : Menu
    {
        private Client _Client { get; }

        public ClientMenu(Client client)
        {
            _Client = client;
        }

        public override MenuResult Show()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Welcome {_Client.Name}. [Client]\n");
                Console.WriteLine("1. List all your accounts\n2. Deposit or withdraw\n3. Transfer\n4. Payment\n5. See transaction history\n6. Log out\nEsc: Exit");

                switch (Console.ReadKey(intercept: true).Key)
                {
                    case ConsoleKey.D1:
                        ListAccountsFlow();
                        break;
                    case ConsoleKey.D2:
                        DepositWithdrawFlow();
                        break;
                    case ConsoleKey.D3:
                        TransferFlow();
                        break;
                    case ConsoleKey.D4:
                        PaymentFlow();
                        break;
                    case ConsoleKey.D5:
                        TransactionHistoryFlow();
                        break;
                    case ConsoleKey.D6:
                        return MenuResult.Logout;
                    case ConsoleKey.Escape:
                        return MenuResult.Exit;
                }
            }
        }
        public void ListAccountsFlow()
        {
            Account[]? accounts = _Client.GetAccounts();
            if (accounts == null)
            {
                Console.WriteLine("\nYou have no opened accounts.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            foreach (Account acc in accounts)
            {
                Console.WriteLine($"{acc.Id}: {acc.AccountType.ToString()} {acc.CreatedAt}\n[{acc.Balance}]");
            }
            Console.ReadKey(intercept: true);
        }
        public void DepositWithdrawFlow()
        {
            Account[]? accounts = _Client.GetAccounts();
            if (accounts == null)
            {
                Console.WriteLine("\nYou have no opened accounts.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            foreach (Account acc in accounts)
            {
                Console.WriteLine($"{acc.Id}: {acc.AccountType.ToString()} {acc.CreatedAt}\n[{acc.Balance}]");
            }

            string input = GetUserInput("Select an account: \n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            int accId = int.Parse(input);

            Account selAcc;
            try
            {
                selAcc = accounts.Single(a => a.Id == accId);
            }
            catch
            {
                Console.WriteLine("\nIncorrect account.");
                Console.ReadKey(intercept: true);
                return;
            }

            Console.Clear();
            input = GetUserSelection("Do you want to deposit or withdraw?\n1. Deposit\n2. Withdraw\n", key =>
            {
                return key switch
                {
                    ConsoleKey.D1 => "Deposit",
                    ConsoleKey.D2 => "Withdraw",
                    ConsoleKey.Escape => "",
                    ConsoleKey.Delete => "",
                    ConsoleKey.Enter => "",
                    _ => null
                };
            }, s => s == null ? false : true, 4);
            if (input == "") return;

            Console.Clear();
            if (input == "Deposit")
            {
                input = GetUserInput("How much would like to deposit into your account?\n", s => Regex.IsMatch(s, @"^\d+$"), 2);
                if (input == "") return;

                decimal amount = decimal.Parse(input);
                Console.Clear();

                _Client.Deposit(selAcc, amount);
            }
            else if (input == "Withdraw")
            {
                input = GetUserInput("How much would like to withdraw from your account?\n", s => Regex.IsMatch(s, @"^\d+$"), 2);
                if (input == "") return;

                decimal amount = decimal.Parse(input);
                Console.Clear();

                _Client.Withdraw(selAcc, amount);
            }

            Console.ReadKey();
        }
        public void TransferFlow()
        {
            Account[]? accounts = _Client.GetAccounts();
            if (accounts == null || accounts.Length == 1)
            {
                Console.WriteLine("\nYou need to have 2 or more accounts opened for transfers to be allowed.");
                Console.ReadKey();
                return;
            }
            Account[] transferAccounts = accounts.Where(a => a is ITransferable).ToArray();
            if (transferAccounts.Length == 0)
            {
                Console.WriteLine("\nYou have no opened accounts that allow transfer.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            foreach (Account acc in transferAccounts)
            {
                Console.WriteLine($"{acc.Id}: {acc.AccountType.ToString()} {acc.CreatedAt}\n[{acc.Balance}]");
            }
            string input = GetUserInput("Select an account to transfer from: \n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            int fromAccId = int.Parse(input);

            Account fromAcc;
            try
            {
                fromAcc = transferAccounts.Single(a => a.Id == fromAccId);
            }
            catch
            {
                Console.WriteLine("\nIncorrect account.");
                Console.ReadKey(intercept: true);
                return;
            }

            Console.Clear();
            foreach (Account acc in accounts)
            {
                if (acc.Id == fromAccId) continue;

                Console.WriteLine($"{acc.Id}: {acc.AccountType.ToString()} {acc.CreatedAt}\n[{acc.Balance}]");
            }
            input = GetUserInput("Enter the target account ID: \n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            int toAccId = int.Parse(input);

            Account toAcc;
            try
            {
                toAcc = accounts.Single(a => a.Id == toAccId);
            }
            catch
            {
                Console.WriteLine("\nIncorrect account.");
                Console.ReadKey(intercept: true);
                return;
            }
            Console.Clear();

            Console.WriteLine($"From: {fromAcc.AccountType} [{fromAcc.Balance}]");
            Console.WriteLine($"To: {toAcc.AccountType} [{toAcc.Balance}]");

            input = GetUserInput("How much would like to transfer?\n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            decimal amount = decimal.Parse(input);

            Console.WriteLine();
            _Client.Transfer((ITransferable)fromAcc, toAcc, amount);
            Console.ReadKey();
        }
        public void PaymentFlow()
        {
            Account[]? accounts = _Client.GetAccounts();
            if (accounts == null)
            {
                Console.WriteLine("\nYou have no opened accounts.");
                Console.ReadKey();
                return;
            }
            Account[] payableAccounts = accounts.Where(a => a is IPayable).ToArray();
            if (payableAccounts.Length == 0)
            {
                Console.WriteLine("\nYou have no opened accounts that allow external payments.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            foreach (Account acc in payableAccounts)
            {
                Console.WriteLine($"{acc.Id}: {acc.AccountType.ToString()} {acc.CreatedAt}\n[{acc.Balance}]");
            }
            string input = GetUserInput("Select the account from which you want to make the payment: \n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            int fromAccId = int.Parse(input);

            Account fromAcc;
            try
            {
                fromAcc = payableAccounts.Single(a => a.Id == fromAccId);
            }
            catch
            {
                Console.WriteLine("\nIncorrect account.");
                Console.ReadKey(intercept: true);
                return;
            }

            Account[]? extAccounts = Account.GetAccounts(_Client.Connection);
            var extFiltered = extAccounts?.Where(a => a._Owner.Id != fromAcc._Owner.Id && a.Id != 0).ToArray();

            if (extFiltered == null || extFiltered?.Length == 0)
            {
                Console.WriteLine("\nThere are zero accounts currently available to make payments to.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            foreach (Account acc in extFiltered)
            {
                Console.WriteLine($"{acc.Id}: {acc._Owner.Name} {acc._Owner.Surname}");
            }

            input = GetUserInput("Enter the target account ID: \n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            int toAccId = int.Parse(input);

            Account toAcc;
            try
            {
                toAcc = extFiltered.Single(a => a.Id == toAccId);
            }
            catch
            {
                Console.WriteLine("\nIncorrect account.");
                Console.ReadKey(intercept: true);
                return;
            }
            Console.Clear();

            Console.WriteLine($"From: {fromAcc.AccountType} [{fromAcc.Balance}]");
            Console.WriteLine($"To: {toAcc.Id} | {toAcc._Owner.Name} {toAcc._Owner.Surname}");

            input = GetUserInput($"How much would you like to send to {toAcc._Owner.Name}?\n", s => Regex.IsMatch(s, @"^\d+$"), 2);
            if (input == "") return;
            decimal amount = decimal.Parse(input);

            Console.WriteLine();
            _Client.Pay((IPayable)fromAcc, toAcc, amount);
            Console.ReadKey();
        }
        public void TransactionHistoryFlow()
        {
            List<LogEntry> transactions = _Client.GetTransactions();
            if(transactions.Count == 0)
            {
                Console.WriteLine("\nYou have no transactions yet.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            foreach (LogEntry t in transactions)
            {
                Console.WriteLine($"[{t.Timestamp}] {t.Type} | Amount: {t.Amount} | From: {t.SourceAccountId} | To: {t.TargetAccountId} | Message: {t.Message}");
            }

            Console.ReadKey();
        }
    }
}
