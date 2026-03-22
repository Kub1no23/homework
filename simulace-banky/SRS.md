# Software Requirements Specification (SRS)
## Projekt: Simulace bankovního systému
**Verze:** 1.1  
**Autor:** — Zlámal Jakub  
**Datum:** —  22.3.2026

---

# 1. Úvod

## 1.1 Účel dokumentu
Tento dokument specifikuje požadavky na software simulující bankovní systém. Slouží jako závazný podklad a definuje funkční i nefunkční požadavky, uživatelské role a pravidla chování systému.

## 1.2 Rozsah systému
Systém simuluje bankovní operace pro různé typy účtů, včetně správy uživatelů, transakcí, úroků a uživatelských oprávnění. Obsahuje konzolové uživatelské rozhraní a podporuje logování a práci s databází.

## 1.3 Definice a zkratky
- **BÚ** – běžný účet  
- **SÚ** – spořicí účet  
- **SSÚ** – studentský spořicí účet  
- **ÚÚ** – úvěrový účet  
- **FR** – funkční požadavek  
- **NFR** – nefunkční požadavek  

---

# 2. Celkový popis

## 2.1 Perspektiva systému
Systém je samostatná aplikace s konzolovým rozhraním. Ukládá data do SQL databáze. Uživatelé se přihlašují pomocí účtu a mají různé úrovně oprávnění.

## 2.2 Funkce systému – přehled
- Správa uživatelských účtů
- Ověřování uživatelů a jejich práv
- Správa bankovních účtů
- (Agregované) Transakce (vklady, výběry, převody)
- Výpočet úroků (spořicí a úvěrové účty)
- Logování všech operací
- Simulace času
- Zobrazení historie transakcí

## 2.3 Typy uživatelů
| Role | Oprávnění |
|------|-----------|
| **Klient** | Přístup ke svým účtům, transakce, historie |
| **Bankéř** | Přístup ke všem účtům, přehledy, agregace |
| **Administrátor** | Správa uživatelů a oprávnění |

## 2.4 Omezení
- Všechny měsíce mají pevně 30 dní.
- Úroky se připisují pouze na konci měsíce.
- Systém je konzolová aplikace.
- Použitá databáze: SQL (SQLite)

---

# 3. Funkční požadavky

## 3.1 Uživatelské účty

### FR-1: Registrace uživatele (admin)
- Admin může vytvořit nový uživatelský účet.
- Musí zadat: jméno, příjmení, roli, login, heslo.
- Heslo se ukládá hashované.

### FR-2: Přihlášení uživatele
- Uživatel zadá login a heslo.
- Systém ověří údaje proti databázi a získá uživatelova práva.
- Při úspěšném pokusu se uživateli zobrazí konzolové menu.

---

## 3.2 Bankovní účty

### FR-3: Vytvoření bankovního účtu
- Bankéř může vytvořit BÚ, SÚ, SSÚ nebo ÚÚ.
- Každý účet má: ID, typ, vlastníka, zůstatek, datum založení.

### FR-4: Běžný účet
- Umožňuje vklady a výběry pouze při kladném zůstatku.
- Umožňuje příjem plateb, odesílání plateb a převod peněz mezi vlastními účty.
- Nemá úrok.

### FR-5: Spořicí účet
- Umožňuje vklady a výběry pouze při kladném zůstatku.
- Umožňuje převod peněz mezi vlastními účty.
- Má roční úrokovou sazbu.
- Úroky se připisují měsíčně.

### FR-6: Studentský spořicí účet
- Omezení jednorázového výběru (limit definovaný bankou).
- Omezení denního součtu výběrů.
- Jinak se chová jako SÚ.

### FR-7: Úvěrový účet
- Umožňuje čerpání do záporného zůstatku až do úvěrového rámce (limitu).
- Úroky se počítají z dlužné částky.
- Má bezúročné období.

---

## 3.3 Transakce

### FR-8: Vklad
- Uživatel může vložit libovolnou částku.

### FR-9: Výběr
- Běžný účet (BÚ): povolen vždy, pokud je zůstatek ≥ 0 po operaci.
- Úvěrový účet (ÚÚ): povolen vždy, pokud nepřekročí úvěrový rámec/limit.
- Spořicí účet (SÚ) / Studentský spořicí účet (SSÚ): povolen pouze při kladném zůstatku.  
- SSÚ: kontrolují se limity jednorázového a denního výběru.

### FR-10: Převod mezi účty
- Klient může převádět mezi svými účty.
- Bankéř může převádět mezi libovolnými účty.
- Převod mezi účty **není platební transakce**.
- Převod se vždy loguje jako interní transakce typu *transfer*.

### FR-11: Platby
- Uživatel může odesílat platby z **běžného účtu (BÚ)** nebo z **úvěrového účtu (ÚÚ)**.
- Spořicí účet (SÚ) ani studentský spořicí účet (SSÚ) **neumožňují odesílání plateb**.
- Platba se loguje jako transakce typu *payment*.
- U běžného účtu platba nesmí způsobit záporný zůstatek.
- U úvěrového účtu platba nesmí překročit úvěrový rámec (tj. zůstatek po platbě nesmí být menší než úvěrový limit).

### FR-12: Historie transakcí
- Uživatel vidí seznam transakcí svého účtu.
- Bankéř vidí transakce všech účtů.

---

## 3.4 Úroky a čas

### FR-13: Simulace času
- Uživatel (bankéř/admin) může posunout čas o X dní nebo měsíců.
- Systém provede všechny časově závislé operace.

### FR-14: Výpočet úroků – spořicí účty
- Úrok = (vážený průměrný zůstatek × roční sazba) / 12
- Počítá se z denních zůstatků.
- Zaokrouhlení dle pravidel banky.

### FR-15: Výpočet úroků – úvěrové účty
- Pokud je aktivní bezúročné období → úrok = 0.
- Jinak stejný princip jako u SÚ, ale se záporným znaménkem.

---

## 3.5 Logování

### FR-16: Logování operací
- Loguje se každá transakce, úrok, změna účtu, přihlášení.
- Log bude uložen:
  - do databáze.

---

# 4. Nefunkční požadavky

## NFR-1: Bezpečnost
- Hesla musí být hashována (např. SHA-256, bcrypt).
- Přístup k databázi musí být chráněn.

## NFR-2: Rozšiřitelnost
- Nový typ účtu musí být možné přidat bez zásahu do existujících tříd.

## NFR-3: Spolehlivost
- Systém musí zabránit nekonzistentním stavům účtů a operacím.

---

# 5. Uživatelské rozhraní (konzole)

## 5.1 Klient
- Zobrazení účtů
- Zobrazení zůstatků
- Vklad/výběr
- Převod mezi vlastními účty
- Historie transakcí

## 5.2 Bankéř
- Vše jako klient +
- Vytváření účtů
- Přehled všech účtů
- Agregace (celkové vklady, úroky)

## 5.3 Administrátor
- Správa uživatelů a oprávnění
