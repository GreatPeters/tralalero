var results = new System.Collections.Generic.List<object>();
for (int attempt = 0; attempt < 3; attempt++) {
    var timer = System.Diagnostics.Stopwatch.StartNew();
    using var stream = GameDataWorkbook.OpenRead();
    double openMs = timer.Elapsed.TotalMilliseconds;
    timer.Restart();
    using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
    double createReaderMs = timer.Elapsed.TotalMilliseconds;
    int sheets = 0;
    do { sheets++; } while(reader.NextResult());
    results.Add(new { openMs, createReaderMs, sheets });
}
return results;
