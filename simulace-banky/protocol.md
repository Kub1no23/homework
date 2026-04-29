# **Protokol testování aplikace**
**Datum:** 29. 4. 2026  
**Projekt:** Simulační bankovní systém  
**Autor testování:** Jakub Zlámal  
**Testovací prostředí:** Ruční testování běžící aplikace spuštěné ve Visual Studiu (Debug/Release), testování probíhalo přímo nad implementovaným kódem.

---

# **Funkční a negativní scénáře**

Tento protokol shrnuje rozsáhlé testování aplikace z hlediska funkčnosti, stability a odolnosti vůči chybným vstupům. Testování probíhalo napříč všemi rolemi systému (Login, Admin, Banker, Client) a zaměřovalo se na to, zda aplikace správně reaguje na validní i nevalidní vstupy, zda nedochází k pádům programu a zda jsou jednotlivé operace bezpečně ukončovány v případě, že uživatel nechce pokračovat nebo zadá nesmyslné hodnoty. Cílem bylo ověřit, že aplikace je robustní, bezpečná a uživatelsky odolná vůči chybám.

---

# **1. Základní chování aplikace a login**

Při přihlášení je implementována ochrana proti opakovaným chybným pokusům. Pokud uživatel zadá nesprávné přihlašovací údaje třikrát za sebou, aplikace vynutí ukončení běhu. Toto chování zabraňuje nekonečnému zkoušení hesel a zároveň chrání aplikaci před zacyklením.

Pokud uživatel zadá prázdný vstup (například pouze stiskne Enter), aplikace to interpretuje jako rozhodnutí nepokračovat a ukončí se. Stejné chování platí i pro jiné formy „prázdného“ vstupu, jako je mezera nebo ESC — aplikace operaci ukončí, protože uživatel zjevně nechce pokračovat.

---

# **2. Obecné chování menu a vstupů**

V celé aplikaci platí jednotné pravidlo:  
**špatná volba v menu nebo nevalidní vstup způsobí reset operace**, zobrazí se chybová hláška a uživatel může volbu zopakovat. Aplikace nikdy nespadne, vždy se vrátí zpět do bezpečného stavu.

Pokud uživatel nezadá žádnou volbu (prázdný vstup, mezera, ESC), operace se ukončí. Toto chování je konzistentní napříč celou aplikací a zajišťuje, že uživatel má vždy možnost operaci bezpečně přerušit.

---

# **3. Role: Admin**

## **3.1 CreateUser**
V této části aplikace není možné zadat žádný nevalidní vstup. Všechny vstupy přes `ReadLine` i `ReadKey` jsou ošetřené. Pokud uživatel nezadá nic, operace se automaticky zruší. Pokud zadá nesmyslné hodnoty (např. čísla v poli pro jméno), aplikace to odmítne a operaci ukončí.

## **3.2 EditUser**
Uživatel nejprve vybírá uživatele podle vstupu. Pokud zadá neexistujícího uživatele, zobrazí se chybová hláška. Změna jednotlivých vlastností je ošetřená — lze změnit pouze hodnoty, které dávají smysl. Prázdný vstup nebo nesmyslný vstup operaci ukončí.

---

# **4. Role: Banker**

## **4.1 CreateFinancialAccount**
Bankéř vybírá uživatele, pro kterého chce vytvořit účet. Nelze vybrat neexistujícího uživatele. Výběr typu účtu probíhá přes menu, kde není možné zvolit neplatnou možnost. Aplikace nedovolí vytvořit nesmyslný nebo nevalidní účet.

## **4.2 InspectUserAccount**
Bankéř vybírá uživatele a následně účet, který chce zobrazit. Nelze zobrazit účet, který neexistuje. Pokud je vybrán legitimní účet, aplikace zobrazí jeho statistiky a informace.

---

# **5. Role: Client**

## **5.1 ListAccounts**
Tato funkce jednoduše vypíše všechny účty klienta. Neexistují zde žádné vstupy, které by bylo možné zneužít nebo zadat špatně.

## **5.2 Deposit / Withdraw**
Klient vybírá účet, na který chce vložit nebo z něho vybrat peníze. Nelze vybrat neexistující účet. Nelze zadat částku menší nebo rovnou nule. U účtů s limity aplikace respektuje pravidla účtu — například maximální výběry nebo limity do ztráty u kreditních účtů.

## **5.3 SeeTransactionHistory**
Aplikace vypisuje legitimní transakce, částky a datum provedení. Neexistuje způsob, jak tuto funkci rozbít chybným vstupem.

## **5.4 Transfer**
Převody lze provádět pouze mezi účty jednoho klienta. Nelze zadat částku menší nebo rovnou nule. Nelze převádět na cizí účet. Aplikace respektuje limity účtů a nedovolí provést nevalidní operaci.

## **5.5 Payment**
Klient vybírá cílového uživatele a jeho účet. Nelze vybrat neexistujícího uživatele ani účet. Nelze zadat nevalidní částku. Platby lze posílat pouze na cizí účty, nikoliv na vlastní. Aplikace respektuje limity účtů a pravidla transakcí.

---

# **6. Závěr**

Aplikace byla testována na široké škále validních i nevalidních vstupů. Během testování nebyl nalezen žádný scénář, který by způsobil pád aplikace nebo nekontrolované chování. Všechny vstupy jsou ošetřené, operace se bezpečně ukončují a aplikace vždy reaguje předvídatelně.

Aplikace:

- **nepadá** při žádném testovaném vstupu  
- **správně ukončuje operace** při prázdném nebo neplatném vstupu  
- **validuje všechny volby v menu**  
- **neumožňuje vytvořit nesmyslné účty, transakce ani uživatele**  
- **respektuje limity účtů a pravidla rolí**  
- **je stabilní, robustní a odolná vůči chybám**

