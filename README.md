# Inkluzitron

## Development

### [Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows)

Install Visual Studio (recommended) with .NET 5.  
It's also possible to use VScode or some other IDE that allows development in C#.

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

1) Create an app on [Discord Developer](https://discord.com/developers/docs/intro) portal and take a token from the Bot section
2) Create a copy of `appsettings.json` and name it `appsettings.Development.json` 
3) Insert token for bot into `appsettings.Development.json`
3) Start a bot either from IDE (VS, VSC, Rider, ...) or command line.
```
dotnet run --project <path_to_src/Inkluzitron>
```

## Production deployment

TBD

## Structure

- `src/` - Source codes
  - `Inkluzitron` - Directory containing project
    - `bin/` - Binaries
    - `obj/` - *You don't need to know*
    - `Extensions/` - Extending methods that can make your life easier
    - `Handlers/` - Classes and methods for catching events
    - `Modules/` - Classes and modules that will handle commands, events, reactions, ...
    - `Services/` - Support services to make life nicer
    - `appsettings.json` - Main config file and template for config
    - `Inkluzitron.csproj` - Project file
    - ... (For anything else just ask)
  - `.editorconfig` - DO NOT TOUCH
  - `Inkluzitron.sln` - Project file for VS (may work for Rider too)
- `README.md` - The thing you are reading right now
- `README.cs.md` - The thing you are reading right now but in Czech
- `.gitignore`

## What you need to know?

- Anything you add to config also add to `appsettings.json` so that others know what is inside it.
- Use PRs (Pull Requests) to add features or make changes. **NO ONE** can push directly to the `master` branch.
- If you are not sure or don't know how to do something **don't feel shy to ask others** for help.
- Check console (stdout, stderr) for logs.
- This project uses a dependency injection container. It's required by the Discord.NET library.
- If you want to add something just follow these steps (everything should load dynamically):
  - Create a new class `Modules/`.
  - Inherit from base class `ModuleBase`.
  - Enjoy!
