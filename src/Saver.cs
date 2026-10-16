using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;



namespace AfterDarkClassic {
 static class SaverProgram {
  [STAThread] static void Main(string[] args){try{string command=args.Length==0?"/c":args[0].ToLowerInvariant();if(command.StartsWith("/c")){Process.Start(Path.Combine(Paths.Root,"AfterDarkControl.exe"));return;}if(command.StartsWith("/a"))return;string engine=Path.Combine(Paths.Root,"After Dark Engine.scr");if(!File.Exists(engine))throw new IOException("The original After Dark engine was not found.");if(command=="/s"&&Paths.Int(Paths.Modern,"Randomizer",0)!=0){var modules=Catalog.Load();if(modules.Count>0){var chosen=modules[new Random().Next(modules.Count)];Paths.Write(Paths.Legacy,"SelectedFolder","After Dark 4.0");Paths.Write(Paths.Legacy+@"\Folders","After Dark 4.0",chosen.Name);}}using(var p=Process.Start(new ProcessStartInfo(engine,string.Join(" ",args)){UseShellExecute=false,WorkingDirectory=Paths.Root})){p.WaitForExit();Environment.ExitCode=p.ExitCode;}}catch(Exception ex){UI.Error(ex);Environment.ExitCode=1;}}
 }
}
