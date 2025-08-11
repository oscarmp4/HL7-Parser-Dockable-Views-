import zipfile

# Re-create the README.md content
readme_content = """# HL7 Parser WinForms Application

## Overview
This application is a Windows Forms-based HL7 Parser built for .NET Framework 4.6 and Visual Studio 2019.
It uses the nHapi HL7 library to parse HL7 v2.x messages, display them in multiple views, and extract key information into a formatted Parse View.

The UI uses dockable panels (via WeifenLuo DockPanel Suite) so you can rearrange Message View, Parse View, and Tree View interactively.

## Features
- Dockable Views: Message View, Parse View, Tree View — draggable & re-positionable.
- HL7 Parsing using NHapi.Base and NHapi.Model.V251.
- Tree View:
  - Displays each segment with its occurrence number and preview.
  - Expands into fields, components, and subcomponents.
  - Clicking highlights the segment in Message View.
- Parse View:
  - Shows grouped “Profile Information” based on RDE messages.
  - Extracts and displays patient, visit, and medication details.
  - Editable text box for review or modification.
  - Clicking an item can highlight the corresponding message section.
- VMD Mapping:
  - Reads a `.ini` file from `/VMD/VMD.ini`.
  - Maps logical field names to HL7 paths dynamically — no recompiling required.
- Buttons:
  - Paste Message: Inserts HL7 message from clipboard.
  - Get VMD: Loads field mappings from `.vmd` or `.ini` file.
  - Clear: Clears all views and resets layout.
  - Parse: Parses current HL7 message using loaded mappings.

## Requirements
- .NET Framework 4.6
- Visual Studio 2019
- NuGet Packages:
  - NHapi.Base.dll (local or NuGet)
  - NHapi.Model.V251.dll (local or NuGet)
  - WeifenLuo.WinFormsUI.Docking.dll (for dockable panels)

## Project Structure
/HL7ParserWin_ParseView
    Forms/
        MainForm.cs
        MainForm.Designer.cs
        MainForm.resx
    Services/
        Hl7ParserService.cs
    VMD/
        VMD.ini         <-- Mapping file
    SampleMessages/
        sample.hl7
    Properties/
    HL7ParserWin_ParseView.csproj
    README.md

## VMD.ini Example
Located in `/VMD/`:
