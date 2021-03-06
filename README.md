twStyleGuide
========

## Introduction

twStyleGuide is an extension for VisualStudio 14. It throws warnings if the corporate design rules were not met and provides a CodeFix for that warning.

## Installation/ Usage
You find the releases at https://github.com/twdes/twStyleGuide/releases/latest .
#### Visual Studio 2015 (14.0), Installation/ Update
Just download the TwStyleGuide.vsix and execute it.
#### MSBuild
You can include the Diagnostic Analyzer in an MSBuild Project by downloading twStyleGuide.dll and including:
```xml
<ItemGroup>
    <Analyzer Include="Path\to\the\File\TwStyleGuide.dll" />
</ItemGroup>
```
in your *.csproj.

## Licence

Licensed under the [EUPL, Version 1.1] or - as soon they will be approved by the
European Commission - subsequent versions of the EUPL(the "Licence"); You may
not use this work except in compliance with the Licence.

[EUPL, Version 1.1]: https://joinup.ec.europa.eu/community/eupl/og_page/european-union-public-licence-eupl-v11