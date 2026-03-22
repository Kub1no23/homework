# Functional Specification (FS)
## Projekt: Simulace bankovního systému
**Verze:** 1.1 
**Autor:** — Zlámal Jakub  
**Datum:** — 22.3.2026

---

# 1. Úvod

## 1.1 Účel dokumentu
Tento dokument popisuje detailní funkční chování systému simulujícího bankovní operace.  
Navazuje na SRS a specifikuje přesné scénáře, validace, vstupy, výstupy a pravidla chování jednotlivých funkcí.

## 1.2 Rozsah
FS definuje:
- chování uživatelských operací,
- pravidla pro jednotlivé typy účtů,
- validace transakcí,
- výpočet úroků,
- logování,
- pravidla pro simulaci času.

---

# 2. Uživatelské role a jejich funkce

## 2.1 Klient
- Přihlášení
- Zobrazení vlastních účtů
- Vklad, výběr
- Převod mezi vlastními účty
- Odesílání plateb (pouze z BÚ nebo ÚÚ)
- Zobrazení historie transakcí

## 2.2 Bankéř
- Vše jako klient kromě vkladů, výběrů a plateb
- Vytváření finančních účtů
- Přístup ke všem účtům
- Přehledy a agregace
- Posun času

## 2.3 Administrátor
- Přihlášení
- Správa a tvorba uživatelských účtů

---

# 3. Funkční specifikace

# 3.1 Přihlášení

### Název funkce : **Login**

### Popis
Uživatel zadá přihlašovací údaje a systém ověří jejich platnost.

### Vstupy
| Vlastnost | Typ vstupu |
| --------- | ----------- |
| login | string |
| heslo | string |

### Validace
- login musí existovat a být unikátní
- heslo musí odpovídat hashovanému heslu v DB

### Výstupy
- úspěch → přihlášený uživatel + jeho role
- neúspěch → opakuje se do 3 chybných pokusů, potom se program ukončí 

### Logování
- úspěšné i neúspěšné přihlášení s datumem

---

# 3.2 Účty

## 3.2.1 Vytvoření uživatelského účtu

### Název funkce : **CreateUserAccount**

### Popis
Funkce je dostupná pouze uživatelům s rolí **admin**.
Administrátor vytvoří nový uživatelský účet a přiřadí mu jednu z dostupných rolí.  

### Vstupy
| Vlastnost | Typ vstupu |
| --------- | ----------- |
| Jméno příjmení | string |
| Login | string |
| Heslo | string |
| Role uživatele | výběr (admin / bankéř / klient) |

### Validace
- Jméno a přijmení musí obsahovat jen písmena
- Login nesmí být prázdný
- Heslo musí splňovat minimální požadavky (min. délka)
- Role musí být jedna z povolených hodnot: *admin*, *bankéř*, *klient*
- Systém ověří, zda uživatelské jméno již neexistuje
- Adminovi se zobrazí rekapitulace údajů, které může změnit podle řádku nebo potvrdit

### Výstupy
- Vytvoření nového uživatelského záznamu v DB
- Hashované heslo uložené v DB
- Výpis potvrzení o úspěšném vytvoření účtu

### Logování
- vytvoření uživatelského účtu: datum, administrátor, nové uživatelské jméno, přiřazená role


## 3.2.2 Vytvoření finančního účtu

### Název funkce : **CreateAccount**

### Popis
Bankéř vytvoří nový účet určitého typu podle výběru z menu aplikace.

### Vstupy
| Vlastnost | Typ Vstupu |
| --------- | ---------- |
| ID vlastníka | výběr |
| BÚ, SÚ, SSÚ, ÚÚ | výběr |

### Výstupy
- Propojení uživatelova ID s jeho nově vytvořeným účtem v DB
- Výpis potvrzení o úspěšném vytvoření bankovního účtu

### Logování
- Vytvoření typu bankovního účtu : datum, bankéř, uživatel, typ účtu, počáteční zůstatek

---

# 3.3 Transakce

## 3.3.1 Vklad

### Název funkce : **Deposit**

### Popis
Uživatel vloží příslušnou částku na účet.

### Vstupy
| Vlastnost | Typ Vstupu |
| --------- | ---------- |
| Id účtu | výběr |
| Částka | string |

### Validace
- částka musí být být kladná

### Výstupy
- konfirmace vložení na účet a výpis jeho nového zůstatku

### Logování
- transakce typu *deposit*, datum, uživatel, částka, účet

---

## 3.3.2 Výběr

### Název funkce : **Withdraw**

### Popis
Uživatel vybere peníze z účtu.

### Vstupy
| Vlastnost | Typ Vstupu |
| --------- | ---------- |
| Id účtu | výběr |
| Částka | string |

### Validace
- Částka musí být kladná
- **BÚ:** zůstatek po výběru nesmí být záporný  
- **SÚ:** výběr povolen pouze při kladném zůstatku  
- **SSÚ:**  
  - výběr povolen pouze při kladném zůstatku  
  - částka ≤ limit jednorázového výběru  
  - součet výběrů za den ≤ denní limit
- **ÚÚ:** - částka po odečtení nesmí přesáhnout úvěrový rámec

### Výstupy
- konfirmace výběru
- výpis nového zůstatku

### Logování
- transakce typu *withdraw*, datum, uživatel, částka, účet

---

## 3.3.3 Převod mezi účty

### Název funkce: **Transfer**

### Popis
Přesun peněz mezi účty jednoho klienta.

### Vstupy
| Vlastnost | Typ Vstupu |
| --------- | ---------- |
| uživatel | výběr | (bankéř)
| zdrojový účet | výběr |
| cílový účet | výběr |
| částka | string |

### Validace
- Částka musí být kladná
- Může se převádět pouze mezi účty konkrétního klienta
- převod **není platba**
- pravidla přesunu podle typu zdrojového účtu (viz Withdraw)

### Výstupy
- konfirmace přesunu peněz
- výpis nových zůstatků obou účtů

### Logování
- transakce typu *transfer*, datum, uživatel, částka, zdrojový účet, cílový účet

---

## 3.3.4 Platby

### Název funkce : **Payment**

### Popis
Klient může odesílat platby na cizí účty.

### Vstupy
| Vlastnost | Typ Vstupu |
| --------- | ---------- |
| zdrojový účet | výběr |
| cílový účet | string |
| částka | string |

### Validace
- Částka musí být kladná
- platby lze odesílat pouze z:
  - **běžného účtu (BÚ)**
  - **úvěrového účtu (ÚÚ)**
- SÚ a SSÚ platby **neumožňují**
- **BÚ:** zůstatek po platbě nesmí být záporný  
- **ÚÚ:** zůstatek po platbě nesmí překročit úvěrový rámec

### Výstupy
- konfirmace odeslané platby
- nový zůstatek zdrojového účtu

### Logování
- transakce typu *payment*, datum, uživatel, částka, zdrojový účet, cílový účet

---

# 3.4 Historie transakcí

### Název funkce : **GetTransactionHistory**

### Popis
Zobrazení transakcí účtů.

### Vstupy
- Žádné

### Validace
- klient může zobrazit pouze své účty
- bankéř může zobrazit všechny

### Výstupy
- seznam transakcí (datum, typ, částka, účty)

---

# 3.5 Úroky a čas

## 3.5.1 Simulace času

### Název funkce: **AdvanceTime**

### Popis
Posun systémového času o X dní nebo měsíců.

### Vstupy
| Vlastnost | Typ vstupu |
| --------- | ----------- |
| počet dní nebo měsíců | string |

### Validace
- musí být validní datum nebo počet dní

### Chování
- při přechodu na konec měsíce se spustí výpočet úroků
- Vypíše kolik banka musí vyplatit za úroky a kolik dostane z ÚÚ.

### Logování
- Žádné

---

## 3.5.2 Výpočet úroků – spořicí účty

### Název funkce
**CalculateSavingsInterest**

### Popis
Výpočet měsíčního úroku z váženého průměrného zůstatku.

### Vstupy
| Vlastnost | Vstup |
| --------- | ---------- |
| Účet | SÚ, SSÚ |
| roční úroková sazba | decimal |
| denní zůstatky | decimal[] |

### Výpočet
Úrok = (vážený průměrný zůstatek × roční sazba) / 12

### Validace
- účet musí být typu SÚ nebo SSÚ

### Výstupy
- transakce typu *interest*, účet, datum, částka

---

## 3.5.3 Výpočet úroků – úvěrové účty

### Název funkce
**CalculateCreditInterest**

### Popis
Výpočet úroku z dlužné částky.

### Chování
- pokud je aktivní bezúročné období → úrok = 0
- jinak stejný princip jako u SÚ, ale se záporným znaménkem

### Výstupy
- transakce typu *interest*, účet, datum, částka

---

# 3.6 Logování

### Název funkce
**LogEvent**

### Popis
Zaznamenání operace do logovacího systému.

### Logované události
- vklady
- výběry
- převody
- platby
- úroky
- přihlášení
- vytvoření účtu

### Ukládání
- databáze

---

# 4. Databázové schéma (SQLite)

Systém používá databázi **SQLite**.  
Při inicializaci aplikace se vytvoří následující tabulky:

- Users  
- AccountTypes  
- Accounts  
- Logs  

Níže je kompletní přehled všech tabulek včetně přesných datových typů.

---

## 4.1 Tabulka: Users

| Sloupec   | Typ                         | Popis |
|----------|------------------------------|-------|
| Id       | INTEGER PRIMARY KEY AUTOINCREMENT | Unikátní ID uživatele |
| Name     | TEXT NOT NULL               | Jméno |
| Surname  | TEXT NOT NULL               | Příjmení |
| Role     | TEXT NOT NULL               | Role (Admin / Banker / Client) |
| Login    | TEXT NOT NULL UNIQUE        | Přihlašovací jméno |
| Password | TEXT NOT NULL               | Hash hesla |

---

## 4.2 Tabulka: AccountTypes

| Sloupec | Typ                         | Popis |
|--------|------------------------------|--------|
| Id     | INTEGER PRIMARY KEY AUTOINCREMENT | ID typu účtu |
| Name   | TEXT NOT NULL UNIQUE         | Název typu účtu (Basic, Savings, StudentSavings, Credit) |

---

## 4.3 Tabulka: Accounts

| Sloupec          | Typ                         | Popis |
|------------------|------------------------------|-------|
| Id               | INTEGER PRIMARY KEY AUTOINCREMENT | ID účtu |
| AccountTypeId    | INTEGER                      | Typ účtu (FK → AccountTypes.Id) |
| UserId           | INTEGER                      | Vlastník účtu (FK → Users.Id) |
| Balance          | REAL NOT NULL DEFAULT 0      | Zůstatek |
| CreatedAt        | TEXT NOT NULL DEFAULT (datetime('now')) | Datum vytvoření |
| DailyLimit       | REAL                         | Denní limit (SSÚ) |
| MaxPaymentLimit  | REAL                         | Limit jednorázového výběru (SSÚ) |
| CreditLimit      | REAL                         | Úvěrový rámec (ÚÚ) |

---

## 4.4 Tabulka: Logs

| Sloupec         | Typ                         | Popis |
|-----------------|------------------------------|-------|
| Id              | INTEGER PRIMARY KEY AUTOINCREMENT | ID logu |
| Type            | TEXT NOT NULL               | Typ operace (deposit, withdraw, transfer, payment, interest, login…) |
| Timestamp       | TEXT NOT NULL DEFAULT (datetime('now')) | Datum a čas |
| InitiatorId     | INTEGER                     | Kdo akci provedl (FK → Users.Id) |
| UserId          | INTEGER                     | Kterého uživatele se akce týká (FK → Users.Id) |
| TargetAccountId | INTEGER                     | Dotčený účet (FK → Accounts.Id) |
| Amount          | REAL                        | Částka |
| Message         | TEXT                        | Volitelný popis |

---

# 5. Omezení
- Všechny měsíce mají 30 dní
- Úroky se připisují pouze na konci měsíce
- Systém je konzolová aplikace

