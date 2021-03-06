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

Pro produkční nasazení je doporučované použití Dockeru. V adresáři `src/` se nachází potřebné soubory pro vytvoření kontejneru.

### Lokální sestavení

1) Vstupte do adresáře `src/` (`cd src/`).
2) Zkopírujte si soubor `environment.template.env` do souboru `environment.env` a vyplňte v něm požadované hodnoty.
3) Spusťte příkaz `docker-compose up`. Bot by se měl automaticky sestavit a spustit.

### DockerHub

Připravte si konfiguraci prostředí (první dva kroky v části Lokální sestavení) a spusťte následující příkazy:

```sh
docker pull misha12/inkluzitron
docker run -d --name Inkluzitron --env-file '/path/to/environment/environment.env' misha12/inkluzitron
```

## Struktura repozitáře

- `src/` - Zdrojáky.
  - `Inkluzitron` – Adresář s projektem.
    - `bin/` – Adresář s binárkami.
    - `obj/` – *Nepotřebujete vědět.*
    - `Data/` - Třídy tvořící datový model bota.
    - `Migrations/` - Kód pro aktualizaci databázového schématu. Generuje se automaticky, obsah není určen k ručním úpravám.
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
- Pokud chcete do databáze začít ukládat novou entitu, je pro ni potřeba vytvořit třídu a přidat ji jako property typu `DbSet<TridaNoveEntity>` do třídy `Data/BotDatabaseContext.cs`.
- Pokud uděláte jakoukoliv úpravu datového modelu (tj. zásah do souborů ve složce `Data`), je nutné vygenerovat migraci následujícím příkazem:
  ```sh
  dotnet ef migrations add StrucnyPopisZmenProvedenychDatovemuModelu
  ```
  Nástroj `dotnet ef` může být nejprve potřeba doinstalovat [podle pokynů v dokumentaci EF Core](https://docs.microsoft.com/cs-cz/ef/core/get-started/overview/install#get-the-net-core-cli-tools).
- Appka je napsaná tak, že by se mělo vše načítat dynamicky, tudíž pro přidání nové funkcionality stačí udělat tyto jednoduché kroky:
  1) Vytvořte novou třídu v `Modules/`.
  2) Poděďte z bázové třídy `ModuleBase`.
  3) Bavte se! Pokud nevíte, koukněte se jinam.
- Každý modul, který bude obsahovat příkazy bude automaticky propagován při volání příkazu `help`. Pokud chcete, aby se vám příkazy zobrazovaly správně, tak proveďte následující kroky:
  1) Ke třídě nastavte atribut `Name`. Pokud bude atribut `Name` chybět, tak se použije název třídy.
  2) Ke každému příkazu lze zadat souhrnný popis příkazu pomocí atributu `Summary`. Pokud bude atribut `Summary` chybět, tak se místo něj doplní řetězec `---`.
