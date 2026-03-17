# Functional Specification (FS)
## Projekt: Simulace bankovního systému
**Verze:** 1.0  
**Autor:** — Zlámal Jakub  
**Datum:** — 17.3.2026

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
- neúspěch → chybová hláška, po 3 chybných pokusech se konzole zablokuje 

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
- Adminovi se zobrazí rekapitulace údajů, kterou musí potvrdit nebo operaci stornovat

### Výstupy
- Vytvoření nového uživatelského záznamu v DB
- Hashované heslo uložené v DB
- Přiřazení role novému uživateli
- Výpis potvrzení o úspěšném vytvoření účtu

### Logování
- vytvoření uživatelského účtu: datum, administrátor, nové uživatelské jméno, přiřazená role


## 3.2.2 Vytvoření finančního účtu

### Název funkce : **CreateFinAccount**

### Popis
Bankéř vytvoří nový účet určitého typu podle výběru z menu aplikace.

### Vstupy
| Vlastnost | Typ Vstupu |
| --------- | ---------- |
| BÚ, SÚ, SSÚ, ÚÚ | výběr |
| ID vlastníka | výběr |
| počáteční zůstatek | string (volitelné) |

### Validace
- Výpis všech informací, bankéř potvrdí nebo změní konkrétní řádek a poté znovu potvrdí či kompletně stornuje operaci
- Ověří zda zůstatek je reálné číslo nebo prázdný string

### Výstupy
- Propojení uživatelova ID s jeho nově vytvořeným účtem v DB
- Vypíše konečný stav zápisu do DB

### Logování
- Vytvoření typu finančního účtu : datum, bankéř, uživatel, typ účtu, počáteční zůstatek

---

# 3.3 Transakce

## 3.3.1 Vklad

### Název funkce : **Deposit**

### Popis
Uživatel vloží příslušnou částku na účet.

### Vstupy
| Vlastnost | Typ Vstupu |
| --------- | ---------- |
| Typ účtu | výběr |
| Částka | string |

### Validace
- částka musí být číslo a být kladná

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
| Typ účtu | výběr |
| Částka | string |

### Validace
- Částka musí být validní číslo a být kladná
- **BÚ:** zůstatek po výběru nesmí být záporný  
- **SÚ:** výběr povolen pouze při kladném zůstatku  
- **SSÚ:**  
  - výběr povolen pouze při kladném zůstatku  
  - částka ≤ limit jednorázového výběru  
  - součet výběrů za den ≤ denní limit
- **ÚÚ:** - částka ≤ úvěrový rámec

### Výstupy
- konfirmace výběru
- výpis nového zůstatku

### Logování
- transakce typu *withdraw*, datum, uživatel, částka, účet

---

## 3.3.3 Převod mezi účty

### Název funkce: **Transfer**

### Popis
Přesun peněz mezi účty jednoho klienta (nebo libovolných účtů v případě bankéře).

### Vstupy
| Vlastnost | Typ Vstupu |
| --------- | ---------- |
| uživatel | výběr | (bankéř)
| zdrojový účet | výběr |
| cílový uživatel | výběr | (bankéř)
| cílový účet | výběr |
| částka | string |

### Validace
- Částka musí být validní čístlo a být kladná
- klient může převádět pouze mezi vlastními účty
- bankéř může převádět mezi libovolnými
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
- Cílový účet musí být konvertován na dvě čísla rozdělená lomenem. Pokud lomeno chybí platba je v rámci téhle banky.
- Částka musí být validní číslo a být kladná
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
- musí být možné překonvertovat na číslo nebo datum

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

### Možnosti ukládání
- soubor
- databáze

---

# 4. Omezení
- Všechny měsíce mají 30 dní
- Úroky se připisují pouze na konci měsíce
- Systém je konzolová aplikace

