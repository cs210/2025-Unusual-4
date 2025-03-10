# com.utilities.extensions

[![Discord](https://img.shields.io/discord/855294214065487932.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/xQgMW9ufN4) [![openupm](https://img.shields.io/npm/v/com.utilities.extensions?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.extensions/) [![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.utilities.extensions)](https://openupm.com/packages/com.utilities.extensions/)

Common extensions for for the [Unity](https://unity.com/) Game Engine types.

## Installing

Requires Unity 2021.3 LTS or higher.

The recommended installation method is though the unity package manager and [OpenUPM](https://openupm.com/packages/com.utilities.extensions).

### Via Unity Package Manager and OpenUPM

#### Terminal

```terminal
openupm add com.utilities.extensions
```

#### Manual

- Open your Unity project settings
- Select the `Package Manager`
![scoped-registries](images/package-manager-scopes.png)
- Add the OpenUPM package registry:
  - Name: `OpenUPM`
  - URL: `https://package.openupm.com`
  - Scope(s):
    - `com.utilities`
- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Utilities.Extensions` package

### Via Unity Package Manager and Git url

- Open your Unity Package Manager
- Add package from git url: `https://github.com/RageAgainstThePixel/com.utilities.extensions.git#upm`

## Documentation

### Runtime Extensions

- Addressables Extensions
- Component Extensions
- GameObject Extensions
- Transform Extensions
- Unity.Object Extensions

### Runtime Utilities

- Serialized Dictionary

### Editor Extensions

- EditorGUILayout Extensions
- SerializedProperty Extensions
- ScriptableObject Extensions
- Unity.Object Extensions

### Editor Utilities

- AbstractDashboardWindow
- Regenerate asset Guids
- Script Icon Utility
