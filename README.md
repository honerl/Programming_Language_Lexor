## Setup & Installation
 
### Step 1 — Clone the Repository
 
```bash
git clone <your-repository-url>
cd PROGRAMMING_LANGUAGE_LEXOR
```
 
### Step 2 — Restore Dependencies
 
```bash
dotnet restore
```
 
### Step 3 — Build the Project
 
```bash
dotnet build
```
 
You should see:
 
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
 
### Step 4 — Create the Tests Folder
 
If it does not exist yet:
 
```bash
mkdir tests
```
 
---
 
## Running the Interpreter
 
There are **three ways** to run the interpreter:
 
### Option 1 — Interactive Terminal UI (Recommended)
 
Launch the main menu:
 
```bash
dotnet run
```
 
This opens the LEXOR Terminal UI where you can write programs interactively or pick a file to run.
 
---
 
### Option 2 — Run a Specific File Directly
 
```bash
dotnet run -- tests/sample.lexor
```
 
Replace `sample.lexor` with any `.lexor` file inside the `tests/` folder:
 
```bash
dotnet run -- tests/test_arithmetic.lexor
dotnet run -- tests/test_logical.lexor
dotnet run -- tests/test_scan.lexor
```
 
---
 
### Option 3 — Run from the Compiled Binary
 
After building, you can run the compiled binary directly:
 
```bash
# Windows
.\bin\Debug\net9.0\Programming_Language_Lexor.exe tests/sample.lexor
 
# macOS / Linux
./bin/Debug/net9.0/Programming_Language_Lexor tests/sample.lexor
```
 
---