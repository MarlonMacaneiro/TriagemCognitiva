Tesseract OCR Setup
===================

This project uses Tesseract for OCR and requires language data files (traineddata).

Quick start (Windows, PowerShell)
- Run: `powershell -ExecutionPolicy Bypass -File scripts/setup-tesseract.ps1 -TessDataPath .\tools\tessdata`
- This downloads `eng.traineddata` and `por.traineddata` (from tessdata_fast by default) into `tools/tessdata`.

Using in code
- Provide the tessdata directory to `TesseractOcrService`:
  - `var ocr = new TesseractOcrService(@"<repo>\tools\tessdata", "por+eng");`
- Alternatively, set the environment variable `TESSDATA_PREFIX` to the tessdata directory and keep a single place for the path.

Quality
- The script accepts `-Quality best` to download higher-accuracy (larger) models from `tessdata_best`.
- Default is `fast` (smaller, faster models) from `tessdata_fast`.

Notes
- The NuGet package `Tesseract` provides the managed wrapper and native binaries; only the language data files are required at runtime.
- Ensure the tessdata path is readable on the machine/CI agent where the code runs.

