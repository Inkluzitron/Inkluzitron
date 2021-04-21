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

### [MacOS](https://docs.microsoft.com/cs-cz/dotnet/core/install/macos)

https://docs.microsoft.com/cs-cz/dotnet/core/install/macos

### Configuration

1) Create an app on the [Discord Developer](https://discord.com/developers/docs/intro) portal and retrieve a token from the _Bot_ section.
2) Create a copy of `appsettings.json` and name it `appsettings.Development.json` .
3) Fill your bot token into `appsettings.Development.json`.
3) Run the application either in an IDE (VS, VSC, Rider, ...) or via the command prompt:
```
dotnet run --project <path_to_src/Inkluzitron>
```

## Production deployment

TBD

## Repository structure

- `src/` - Source code.
  - `Inkluzitron` – Directory containing project.
    - `bin/` – Binaries.
    - `obj/` – *You don't need to know.*
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
- Use PRs (Pull Requests) to add features or make changes. **NO ONE** may push directly to the `master` branch.
- If you are not sure or don't know how to do something, **don't be shy about asking others** for help.
- Check console (stdout, stderr) for logs.
- This project uses a dependency injection container. It's required by the Discord.NET library.
- If you want to add something, just follow these steps (everything should load automatically):
  - Create a new class in the `Modules/` directory (and namespace).
  - Inherit from the `ModuleBase` class.
  - Enjoy!
