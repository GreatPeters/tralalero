try { return GameUIFontBuilder.Build(); }
catch (System.Exception error) { System.IO.File.WriteAllText("tmp/chapter-fonts/build-error.txt",error.ToString()); throw; }
