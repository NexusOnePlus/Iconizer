# Iconizer
[![CodeQL Advanced](https://github.com/NexusOnePlus/Iconizer/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/NexusOnePlus/Iconizer/actions/workflows/codeql.yml)
[![Desktop Installer](https://github.com/NexusOnePlus/Iconizer/actions/workflows/dotnet-desktop-installer.yml/badge.svg)](https://github.com/NexusOnePlus/Iconizer/actions/workflows/dotnet-desktop-installer.yml)

Iconizer is a modern WPF application that automatically adds programming language and application icons to Windows folders, enhancing folder navigation and visual organization.


![image](https://github.com/user-attachments/assets/36f7b841-b38a-47a4-bf7f-d7ae5f0cb310)



## Overview

Iconizer simplifies the process of customizing folder icons on Windows systems. Instead of manually changing icons one by one, Iconizer automatically identifies project folders and applies appropriate programming language or application icons based on folder content analysis. This makes it easy to visually distinguish between different project types (Python, Rust, JavaScript, etc.) or application folders.

## Features

- **Automatic Icon Assignment**: Scan and apply icons to folders based on their contents
- **Programming Language Detection**: Identify project folders and apply the appropriate language icon (Python, Rust, JavaScript, etc.)
- **Application Icon Support**: Assign recognizable icons for application folders (Gmail, VS Code, Chrome, etc.)
- **Icon Library Management**: Built-in library of programming and application icons with import/export capabilities
- **Folder Monitoring**: Optional background service to monitor folder changes
- **Preview Mode**: See icon changes before applying them
- **Batch Processing**: Apply icons to multiple folders simultaneously
- **Undo/Revert**: Easily restore original folder appearances

## Architecture

Iconizer follows Clean Architecture principles with a clear separation of concerns:

```
Iconizer/
├── Domain/
│   └── Entities, Interfaces
├── Application/
│   └── Services, UseCases
├── Infrastructure/
│   └── Implementations (e.g., file system)
├── Presentation/
│   ├── Views/
│   ├── ViewModels/
│   └── UserControls/
└── Iconizer.sln
```

## Getting Started

### Prerequisites

- Windows 10/11
- .NET 9.0 or higher
- JetBrains Rider or Visual Studio 2022 (for development)

### Installation

Iconizer is currently in development. To try it out:

1. Clone the repository
2. Build the application from source following the developer instructions below
3. We'll provide installer packages once we reach our first release

### For Developers

1. Clone the repository:
   ```
   git clone https://github.com/NexusOnePlus/Iconizer.git
   ```
2. Open `Iconizer.sln` in JetBrains Rider
3. Restore NuGet packages
4. Build the solution

## Usage

1. **Launch Iconizer** - Run the application
2. **Select Target Directories** - Choose which folders to scan and enhance
3. **Configure Detection Settings** - Adjust how Iconizer identifies programming languages and applications
4. **Apply Icons** - Let Iconizer analyze and apply appropriate icons to your folders
5. **Enjoy** - Navigate your development projects with improved visual organization

## Supported Icons

Iconizer includes icons for popular programming languages and applications such as:

### Programming Languages
- Python
- Rust
- JavaScript/TypeScript
- C#/.NET
- Java
- Bun
- Go
- And many more...

### Applications
- Gmail
- VS Code
- Visual Studio
- Chrome
- Microsoft Office applications
- And many more...

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Team

- **[NexusOnePlus](https://github.com/NexusOnePlus)** - Project Owner
- **[cenixeriadev](https://github.com/cenixeriadev)** - Collaborator

## Security

Please see our [SECURITY.md](SECURITY.md) file for information on reporting security vulnerabilities.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.


## Support

If you encounter any issues or have questions, please [open an issue](https://github.com/NexusOnePlus/Iconizer/issues) on GitHub.
