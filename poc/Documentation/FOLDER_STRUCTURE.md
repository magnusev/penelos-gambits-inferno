# POC Folder Structure

## Overview
This document describes the organized structure of the `poc` folder after cleanup.

## Folder Organization

### 📁 **Build/**
Contains build scripts for generating rotation files.
- `BuildRotation.ps1` - Main build script that combines components and class files

### 📁 **Classes/**
Contains class-specific implementations organized by specialization.
- `PaladinHoly/` - Holy Paladin rotation files
- Each class folder contains numbered files (10_Config.cs, 11_Spells.cs, etc.)

### 📁 **Components/**
Contains shared components used across all rotations.
- `00_Core.cs` - Core functionality and base classes
- `01_Conditions.cs` - Condition checking utilities
- `02_Selectors.cs` - Target selection logic
- `03_Utilities.cs` - Helper functions and utilities

### 📁 **Documentation/**
All markdown documentation and guides.
- `README.md` - Main documentation
- `QUICKSTART.md` - Quick start guide
- `COMPONENTS_GUIDE.md` - Guide to the component system
- `NEW_CLASS_TEMPLATE.md` - Template for adding new classes
- `BUILD_SYSTEM_COMPLETE.md` - Build system documentation
- `SECURITY_VALIDATION.md` - Security validation info
- And other documentation files...

### 📁 **Tools/**
Development and validation tools.
- `Analyze/` - Code analysis tools
- `CompileCheck/` - Compilation verification
- `SecurityValidator/` - Security validation tool
- `analyze.csx` - Analysis script
- `SecurityValidator.csx` - Security validation script

### 📁 **Output/**
Generated rotation files (local builds).
- Created automatically by build script
- Contains `{ClassName}_rotation.cs` files

### 📁 **logs/**
Build and tool execution logs.

### 📄 **rotation.cs**
Original monolithic rotation file kept as backup reference.
**Important:** This file is preserved in case the new component-based system needs troubleshooting.

## Build Workflow

1. Edit files in `Components/` for shared functionality
2. Edit files in `Classes/{ClassName}/` for class-specific logic
3. Run `Build\BuildRotation.ps1` to combine and validate
4. Output is generated in `Output/` folder
5. Security validation runs automatically via `Tools/SecurityValidator/`

## Key Features

- ✅ **Modular Design** - Shared components + class-specific files
- ✅ **Security Validation** - Automatic security checks on build
- ✅ **Clean Organization** - Tools, docs, and code separated
- ✅ **Legacy Backup** - Original rotation.cs preserved

