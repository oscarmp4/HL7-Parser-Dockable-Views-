# HL7 Parser WinForms Application

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
```
; LogicalName = HL7 Path
MSHSendingApp = /MSH-3
MSHSendingFacility = /MSH-4
MSHReceivingApp = /MSH-5
MSHReceivingFacility = /MSH-6
PIDMedRec = /PID-3
PIDFamilyName = /PID-5-1
PIDGivenName = /PID-5-2
RXEGiveCodeText = /RXE-3-2
RXEGiveCodeID = /RXE-3-1
```

## Usage
1. Build the solution in Visual Studio 2019.
2. Run `HL7ParserWin_ParseView.exe`.
3. Use Paste Message to insert your HL7 text.
4. Click Get VMD to load mappings from `/VMD/VMD.ini`.
5. Click Parse:
   - Message View: Raw HL7 message (wrapped).
   - Parse View: Formatted structured output.
   - Tree View: Hierarchical view of segments.
6. Rearrange panels as desired — layout persists until cleared.
7. Clear to reset and start again.

## Example HL7 Message
```
MSH|^~\&|FrameworkLTC|DHPH|FW-TP-CST-LK|DCTIPU|20230615161633||RDE^O11^RDE_O11|67080|P|2.5||||||ASCII|||
PID|1||DCTIPU\F\1872||Ward^Creston^^^^^D|DCCST00039903|19511002000000|M|||351 Deers Head Hospital Rd^^Salisbury^MD^21801||(410)572-6166|||||39903||||||||||||N|||||||||
...
```

## Shortcuts
- Ctrl+V in Message View also pastes HL7 message.
- Click a Tree Node → highlights corresponding segment in Message View.
- Edit Parse View freely to adjust output before saving.

## Future Enhancements
- Save/restore dock layout between sessions.
- Batch parsing for multiple HL7 messages in one file.
- Full support for custom Z-segments in Parse View.
