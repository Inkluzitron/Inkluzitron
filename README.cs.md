# Inkluzitron

## Vývoj

### [Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows)

Nainstalujte si Visual Studio a .NET 5.
Můžete použít také VS Code nebo jiné IDE, které podporuje vývoj v C# (například JetBrains Rider).

### [Linux (Debian)](https://docs.microsoft.com/en-us/dotnet/core/install/linux-debian)

```sh
wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update;
sudo apt-get install -y apt-transport-https;
sudo apt-get update;
sudo apt-get install -y dotnet-sdk-5.0;
```

### [Linux (RHEL)](https://docs.microsoft.com/en-us/dotnet/core/install/linux-rhel)

```sh
sudo dnf install dotnet-sdk-5.0
```

### [MacOS](https://docs.microsoft.com/cs-cz/dotnet/core/install/macos)

https://docs.microsoft.com/cs-cz/dotnet/core/install/macos

### Konfigurace

1) Vytvořte si na [Discord Developer](https://discord.com/developers/docs/intro) portálu aplikaci a v sekci _Bot_ si zkopírujte token.
2) Vytvořte kopii `appsettings.json` a pojmenujte ji `appsettings.Development.json`.
3) Vyplňte do `appsettings.Development.json` svůj token.
4) Spusťte bota z IDE (VS, VSC, Rider, ...) nebo v terminálu:
```
dotnet run --project <path_to_src/Inkluzitron>
```

## Produkční deployment

Pro produkční nasazení je doporučená možnost použít Docker. V adresáři `src/` se nachází potřebné soubory pro vytvoření kontejneru.

### Lokální sestavení

1) Vstupte do adresáře `src/` (`cd src/`).
2) Zkopírujte si soubor `environment.template.env` do souboru `environment.env` a vyplňte v něm požadované hodnoty.
3) Spusťte příkaz `docker-compose up`. Bot by se měl automaticky sestavit a spustit.

### DockerHub

Připravte si konfiguraci prostředí (první dva kroky v části Lokální sestavení) a spusťte následující příkazy:

Prvně si vemte `environment.template.env` soubor, přejmenujte jej na `environment.env`. Bude se používat i ve výsledném kontejneru.

```sh
docker pull misha12/inkluzitron
docker run -d --name Inkluzitron --env-file '/path/to/environment/environment.env' misha12/inkluzitron
```

## Struktura repozitáře

- `src/` - Zdrojáky.
  - `Inkluzitron` – Adresář s projektem.
    - `bin/` – Adresář s binárkami.
    - `obj/` – *Nepotřebujete vědět.*
    - `Extensions/` – Rozšiřující metody, které vám mohou zjednodušit život, ale asi je nikdy nevyužijete.
    - `Handlers/` – Třídy a metody pro zachytávání událostí. *Pravděpodobně je nebudete potřebovat.*
    - `Modules/` – Zde budou třídy s moduly, které budou obsluhovat obsluhu událostí, reakcí atd. *Většinu svého krásného nového kódu budete implementovat zde.*
    - `Services/` – Podpůrné služby, díky kterým je život krásnější. *Sem asi nikdy nebudete potřebovat.*
    - `appsettings.json` – Hlavní konfigurační soubor (a zároveň šablona konfigurace).
    - `Inkluzitron.csproj` – Soubor s projektem.
    - ... (Na další soubory se můžeš zeptat ostatních.)
  - `.editorconfig` – Nesahat!
  - `Inkluzitron.sln` – Solution soubor – popisuje skupinu projektů, tenhle soubor otevíráš ve Visual Studiu nebo v Rideru.
- `README.md`– Toto README, ale v angličtině.
- `README.cs.md` – Toto README.
- `.gitignore`

## Co je třeba vědět?

- Když vytvoříte nějakou novou konfigurační sekci, přidejte ji do `appsettings.json`, aby ostatní věděli, co jste přidali, a aby si to mohli jednoduše přidat do svých konfiguráků.
- Všechno přidávejte formou PR. **NIKDO** nebude pushovat přímo do `master` branche.
- Pokud si něčím nejste jistí nebo něco nevíte, **nebojte se zeptat ostatních**.
- Dívejte se do konzole (to znamená stdout a stderr). Najdete tam logy.
- V projektu je využit dependency injection kontejner. Vyžaduje to knihovna Discord.NET.
- Appka je napsaná tak, že by se mělo vše načítat dynamicky, tudíž pro přidání nové funkcionality stačí udělat tyto jednoduché kroky:
  - Vytvořte novou třídu v `Modules/`.
  - Poděďte z bázové třídy `ModuleBase`.
  - Bavte se! Pokud nevíte, koukněte se jinam.
