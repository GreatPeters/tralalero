var results = new System.Collections.Generic.List<object>();
for (int attempt = 0; attempt < 3; attempt++) {
    using var stream = System.IO.File.OpenRead("outputs/startup-performance-2026-09-12/Data.xlsx");
    var timer = System.Diagnostics.Stopwatch.StartNew();
    using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
    double createReaderMs = timer.Elapsed.TotalMilliseconds;
    int sheets = 0;
    do { sheets++; } while(reader.NextResult());
    results.Add(new { createReaderMs, sheets });
}
GameDataWorkbookSchema.Validate(System.IO.File.ReadAllBytes("outputs/startup-performance-2026-09-12/Data.xlsx"));
return results;
