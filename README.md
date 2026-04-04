# HMI Configuration Software (Based on .NET 8 + Avalonia UI) Detailed Module Requirements

---

## I. Screen Design Module

### 1.1 Canvas and Layout
- Customizable canvas width, height, and units (pixels, millimeters, etc.)
- Support arbitrary dragging and zooming, canvas panning and view translation
- Multi-level zoom (shortcut keys ±, mouse wheel zoom)
- Show/hide grid with configurable spacing and color
- Auto-display guide lines, manual add/delete with custom color and position
- Customizable page background: solid color, gradient, image, or transparent

### 1.2 Layer/Control Hierarchy
- Control hierarchy management (layer tree) with show/hide/lock functions
- Multi-layer operations: add, delete, rename, hierarchical indentation
- Control grouping with support for independent modification of grouped controls
- Configurable display order with shortcut keys to adjust layer levels

### 1.3 Page Navigation and Multi-Screen
- Support multi-page design with configurable initial/default screen
- One-click screen switching for controls/buttons with animation effects
- Configurable page transition animation types and durations
- Page copy, paste, batch rename, and batch delete operations

### 1.4 Alignment and Distribution
- Support alignment based on selected controls (left/center/right, top/center/bottom)
- Uniform distribution (horizontal and vertical)
- Coordinate and alignment reference line display with distance hint lines

### 1.5 Batch Operations
- Frame selection and Shift/Ctrl multi-select controls
- Support batch property modification (unified size, color, font, etc.)
- One-click copy/cut/paste/repeat paste with optional positioning
- Batch delete/lock/unlock operations for controls

---

## II. Controls and Plugins Module

### 2.1 Basic Controls
- Button with configurable text, color, icon, and optional animation
- Label with font, color, alignment, and other properties
- Input box supporting text, password, numeric mode, input validation, and min/max limits
- Image control with adaptation, zoom, fill, and crop features
- Animation control supporting frame animation, GIF, SVG animation with adjustable frame rate
- Indicator light, switch, slider, gauge with customizable scale, unit, and pointer style
- Basic shapes: line, polyline, broken line, rectangle, circle, ellipse, sector, ring, etc.
- Polygon editing with node add/delete/move operations

### 2.2 Property Panel and Extensions
- Common properties for all controls (size, position, visibility, lock, transparency, Z-Index, etc.)
- Support for extended properties (custom business fields/property comments)
- Property panel search and property history display
- Undo/Redo support for property modifications

### 2.3 Plugin Controls
- Control plugins use strong-type interfaces with load/remove/version control support
- Plugin properties auto-map to property panel with custom event binding support
- Plugins support special rendering, interactive properties, and animations
- Custom icons, thumbnails, and preview images for plugins
- Plugin documentation with example code and property descriptions

---

## III. Variables and Data System

### 3.1 Variable Data Structures
- Support for numeric types (Byte, Short, Int, Float, Double), boolean, string, time, enum, and custom structures (Record/Struct)
- Batch import/export variables to Excel/CSV with format error prompts and validation
- Quick variable grouping and tag classification (e.g., "Device Variables", "User Input", etc.)

### 3.2 Data Binding and Expressions
- Data binding for any control property (Text, Color, Value, etc.)
- Single and bidirectional binding mechanisms
- Formula expression support (e.g., "[Temperature]>50 ? 'Alarm' : 'Normal'")
- Expression function library (mathematical, string, date functions, etc.)
- Binding debug mode with real-time expression value preview

### 3.3 Variable Debugging and Monitoring
- Real-time variable monitoring curves, quick manual assignment, batch variable simulation
- Variable history records and change record export
- Variable snapshot and recovery functions
- Variable usage query and unbound variable alerts

---

## IV. Event-Driven and C# Script Module

### 4.1 Script Management
- Built-in script editor with code highlighting, auto-complete, formatting, find/replace
- Script mounting support for control events (click, focus change, value change), variable events, and page load/close events
- Global scripts, timed scripts, loop scripts, and custom function support

### 4.2 Script Runtime and Debugging
- Roslyn dynamic compilation with runtime hot-reload support
- Exception catching, error line hints, and call stack analysis
- Breakpoint debugging, step-by-step execution, and variable query window
- Output log display and script runtime performance statistics

### 4.3 Host API Support
- API support for: get/set variables, read/write control properties, trigger events, page navigation, communication sending, logging, etc.
- Accessible API help documentation
- Support for importing custom C# libraries (for interface or algorithm extensions)

---

## V. Engineering and Resource Management Module

### 5.1 Engineering File Management
- Multi-directory engineering structure (pages, resources, scripts, variables in separate folders)
- Support for engineering packaging/unpacking with incremental save and auto-save
- Recent engineering list, open history, and engineering recycle bin
- One-click engineering template import/export (with all dependencies)

### 5.2 Resource Management
- Unified file management: images (static/dynamic), audio, video, fonts, SVG
- Resource file properties (name, type, size, source, usage notes)
- One-click batch import (drag or select folder), deduplication, batch rename/delete, reference traceability query
- Auto-marking and cleaning of unreferenced resources

---

## VI. Preview, Runtime and Publishing Module

### 6.1 Real-time Preview
- Integrated edit/preview with real-time synchronization of variable values and control states
- Full-screen and windowed preview modes with resolution adjustment and terminal simulation
- Real-time debug information printing

### 6.2 Engineering Publishing
- Cross-platform packaging and publishing of edited runtime screens (not the entire software) - Windows exe, Linux elf/executable screen package

### 6.3 Runtime Features
- Load engineering files (JSON/XML) with dynamic interface rendering
- Dynamic loading of plugins, scripts, and resources
- Automatic logging of logs, alarms, and exceptions
- Automatic local data storage and scheduled backup of engineering projects

---

## VII. System, Extension and Communication Module

### 7.1 Multi-language Support
- Bilingual/multi-lingual system interface and engineering with user-switchable languages
- Multi-language entry import/export with translation comparison and validation
- Dynamic switching of engineering/control/resource multi-language properties

### 7.2 Theme and Skin
- Multiple theme styles (light, dark, custom) with one-click switching
- Global control style configuration and font package loading
- User-defined theme editing with import/export support

### 7.3 Plugin SDK and Development Support
- Complete SDK documentation, API example projects, and test projects
- Plugin auto-registration, uninstall, disable, upgrade, and rollback
- Plugin marketplace with online search and download support
- Plugin security sandbox and permission management

### 7.4 Communication and IO Interface
- Built-in support for protocol drivers (Modbus RTU/TCP, OPC UA/DA, MQTT, WebAPI, UDP/TCP, serial port, etc.)
- Communication manager with device configuration, status monitoring, and auto-reconnect
- Communication data collection with queue buffering and exception alarms
- Communication testing tools and manual send/receive debugging
- Device/variable mapping and quick binding

---

## VIII. Extended Features

### 8.1 Alarm and Logging System
- Alarm condition scripted configuration support
- Alarm grading, display, history record query/export
- Log types (system/communication/user operation/script) with level-based storage and retrieval

### 8.2 Permission and User Management
- Multi-role permission control with operation password protection
- Permission judgment for actions and scripts
- User logs and operation traceability

### 8.3 Data Persistence and Local Database
- SQLite/local file storage support for variables, events, alarms, and log data
- Data import/export to CSV/Excel
- Scheduled data backup and recovery

---

**All the above features should have good UI/UE design, an auto-adaptive interface, high usability, and efficient, stable operation. The inter-module structure should be clear, facilitating future extension and maintenance.**
