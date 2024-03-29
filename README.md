# Inkluzitron

## Development

### [Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows)

Install Visual Studio and .NET 5.
It's also possible to use VS Code or another IDE that supports C# development (JetBrains Rider, for example).

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

### [macOS](https://docs.microsoft.com/cs-cz/dotnet/core/install/macos)

https://docs.microsoft.com/en-US/dotnet/core/install/macos

### Configuration

1) Create an app on the [Discord Developer](https://discord.com/developers/docs/intro) portal and retrieve a token from the _Bot_ section.
2) Create a copy of `appsettings.json` and name it `appsettings.Development.json` .
3) Fill your bot token into `appsettings.Development.json`.
3) Run the application either in an IDE (VS, VSC, Rider, ...) or via the command prompt:
```
dotnet run --project <path_to_src/Inkluzitron>
```

## Production deployment

Using Docker is recommended for production deployment. All files necessary can be found in the `src` directory.

### Local build

1) Enter the `src/` directory (`cd src/`).
2) Create a copy of the `environment.template.env` file named `environment.env`, and fill the required values inside.
3) Run `docker-compose up`. The bot should be automatically built and run.

### DockerHub image

Prepare your environment file (first two steps in section Local build), and then run these commands:

```sh
docker pull misha12/inkluzitron
docker run -d --name Inkluzitron --env-file '/path/to/environment/environment.env' misha12/inkluzitron
```

## Repository structure

- `src/` - Source code.
  - `Inkluzitron` – Directory containing project.
    - `bin/` – Binaries.
    - `obj/` – *You don't need to know.*
    - `Data/` - Classes that constitute the bot's data model.
    - `Migrations/` - Code responsible for updating the database schema. It is generated automatically, do not edit these files manually.
    - `Extensions/` – Extension methods that can make your life easier.
    - `Handlers/` – Classes and methods for handling events. *You're probably not going to need them.*
    - `Modules/` – Classes and modules that handle commands, reactions, etc. *You're mostly going to implement your shiny new code here.*
    - `Services/` – Support services to make life nicer. *You're probably never going to modify these.*
    - `appsettings.json` – The primary configuration file (and configuration template as well).
    - `Inkluzitron.csproj` – The project file.
    - ... (You can ask the others about the other files.)
  - `.editorconfig` – DO NOT TOUCH!
  - `Inkluzitron.sln` – The solution file (this encapsulates the project and this is the file to open in VS or Rider).
- `README.md` – The thing you are reading right now.
- `README.cs.md` – The thing you are reading right now but in Czech.
- `.gitignore`

## What you need to know?

- If you add a new configuration section, remember to include it in `appsettings.json`, so that the others know what you've added and don't have a hard time adjusting their own config files.
- Use PRs (pull requests) to add features or make changes. **NO ONE** may push directly to the `master` branch.
- If you are not sure or don't know how to do something, **don't be shy about asking others** for help.
- Check the console (stdout, stderr) for any logs.
- This project uses a dependency-injection container. It's required by the Discord.NET library.
- If you want to start saving a new entity into the bot's database, you'll need to create an entity class and add it as a property of type `DbSet<TridaNoveEntity>` in `Data/BotDatabaseContext.cs`.
- If you make any changes to the data model (i.e. touch anything in the `Data`) folder, remember to also generate a migration using the below command:
  ```sh
  dotnet ef migrations add TerseSummaryOfChangesMadeToDataModel
  ```
  You might need to install the `dotnet ef` tool first [according to the EF Core documentation](https://docs.microsoft.com/cs-cz/ef/core/get-started/overview/install#get-the-net-core-cli-tools).
- If you want to add something, just follow these steps (everything should load automatically):
  1) Create a new class in the `Modules/` directory (and namespace).
  2) Inherit from the `ModuleBase` class.
  3) Enjoy!
- Each module that contains commands will be automatically promoted when the `help` command is called. If you want the commands to display correctly, follow these steps:
  1) Set the `Name` attribute to the class. If the `Name` attribute is missing, the class name is used.
  2) For each command, you can enter a summary description of the command using the `Summary` attribute. If the `Summary` attribute is missing, the string` --- `will be added instead.
