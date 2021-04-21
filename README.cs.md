# Inkluzitron

## Vývoj

### Windows

Nainstalujte Visual Studio a .NET 5.

### Linux (Debian)

```sh
wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update;
sudo apt-get install -y apt-transport-https;
sudo apt-get update;
sudo apt-get install -y dotnet-sdk-5.0;
```

### Linux (RHEL)

```sh
sudo dnf install dotnet-sdk-5.0
```

### MacOS

https://docs.microsoft.com/cs-cz/dotnet/core/install/macos

### Konfigurace

1) Zkopírujte si `appsettings.json` a pojmenujte ho `appsettings.Development.json`.
2) Spusťte bota z IDE (VS, VSC, Rider, ...).

## Produkční deployment

TBD

## Struktura? Who knows

- `src/` - Zdrojáky
  - `Inkluzitron` - Adresář s projektem
    - `bin/` - Adresář s binárkama
    - `obj/` - Nepotřebujete vědět
    - `Extensions/` - Rozšiřující metody, které vám mohou zjednodušit život, ale asi je nikdy nevyužijete.
    - `Handlers/` - Třídy a metody pro zachytávání událostí. Nebudete potřebovat.
    - `Modules/` - Zde budou třídy s moduly, které budou obsluhovat commandy události, reakce, ... *Většinu času budete implementovat zde.*
    - `Services/` - Podpůrné služby, aby život byl krásnější. *Asi nikdy sem nebudete potřebovat.*
    - `appsettings.json` - Hlavní konfigurace a současně šablona configu.
    - `Inkluzitron.csproj` - Projektový soubor
    - ... (Cokoliv dalšího, ptej se ostatních)
  - `.editorconfig` - Nesahat
  - `Inkluzitron.sln` - Spouštěcí soubor do projektu pro VS (možná bude fungovat i rider.)
- `README.md` (Toto readme)
- `.gitignore`

## Co je třeba vědět?

- Cokoliv přidáte do configu, takto dejte do `appsettings.json`, aby ostatní věděli, co v tom vlastně je.
- Všechno přidávejte formou PR. **NIKDO** nebude pushovat přímo do `master` branche.
- Pokud nevíte. Ptejte se.
- Dívejte se do konzole (to znamená stdout a stderr). Provádí se tam logování.
- V projektu je využit dependency injection kontejner. Vyžaduje to knihovna Discord.NET.
- Appka je napsaná, že by se mělo vše načítat dynamicky, tudíž, když budete chtít něco přidat, tak bude stačit udělat tyto jednoduché kroky:
  - Vytvořte novou třídu v `Modules/`.
  - Poděďte z bázové třídy `ModuleBase`.
  - Užijte si zábavu. Pokud nevíte, tak se koukněte jinam.
