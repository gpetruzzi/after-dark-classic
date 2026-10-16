using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;
namespace AfterDarkClassic {
 static class UpdateProgram {
  const string Key=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\AfterDarkClassic";
  [STAThread] static void Main(string[] args){try{
   if(args.Length>1&&args[0]=="/extract-test"){if(File.Exists(args[1]))throw new IOException("The test destination already exists.");File.WriteAllBytes(args[1],Payload());return;}
   if(args.Length>1&&args[0]=="/selftest"){string test=Path.GetFullPath(args[1]);if(Directory.Exists(test))throw new IOException("The test directory already exists.");Directory.CreateDirectory(test);string old="previous-test-version";File.WriteAllText(Path.Combine(test,"AfterDarkControl.exe"),old);File.WriteAllText(Path.Combine(test,"settings.ini"),"preserve-me");string backup=Swap(test);byte[] actual=File.ReadAllBytes(Path.Combine(test,"AfterDarkControl.exe"));byte[] expected=Payload();if(actual.Length!=expected.Length)throw new IOException("Payload length mismatch.");for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])throw new IOException("Payload mismatch.");if(File.ReadAllText(backup)!=old||File.ReadAllText(Path.Combine(test,"settings.ini"))!="preserve-me")throw new IOException("Backup or settings mismatch.");File.WriteAllText(Path.Combine(test,"result.txt"),"PASS: executable replaced; previous executable backed up; settings unchanged.");return;}
   string root;using(var k=Registry.LocalMachine.OpenSubKey(Key)){root=k==null?"":Convert.ToString(k.GetValue("InstallLocation",""));}
   if(string.IsNullOrEmpty(root)||!File.Exists(Path.Combine(root,"installation.xml"))||!File.Exists(Path.Combine(root,"AfterDarkControl.exe")))throw new IOException("After Dark was not found. Install the full version first.");
   root=Path.GetFullPath(root).TrimEnd('\\');
   if(args.Length>0&&args[0]=="/apply"){if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))throw new InvalidOperationException("Administrator permission is required to update the control panel.");Apply(root);return;}
   if(MessageBox.Show("Update the After Dark control panel to version 5.0.\r\n\r\nFolder: "+root+"\r\n\r\nFirst, choose Exit from the After Dark tray icon.\r\n\r\nYour modules and settings will be kept. Continue?","After Dark 5.0 — Update",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return;
   var process=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"/apply"){UseShellExecute=true,Verb="runas"});process.WaitForExit();int code=process.ExitCode;process.Dispose();if(code!=0)return;
   if(MessageBox.Show("The control panel was updated. Open After Dark now?","After Dark",MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes)Process.Start(Path.Combine(root,"AfterDarkControl.exe"));
  }catch(System.ComponentModel.Win32Exception ex){if(ex.NativeErrorCode!=1223)UI.Error(ex);Environment.ExitCode=1;}catch(Exception ex){UI.Error(ex);Environment.ExitCode=1;}}
  static byte[] Payload(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("control.exe"))using(var ms=new MemoryStream()){s.CopyTo(ms);return ms.ToArray();}}
  static void Apply(string root){string exe=Path.Combine(root,"AfterDarkControl.exe");foreach(var p in Process.GetProcessesByName("AfterDarkControl")){try{string file=null;try{file=p.MainModule.FileName;}catch{throw new IOException("Choose Exit from the After Dark tray icon before updating.");}if(string.Equals(file,exe,StringComparison.OrdinalIgnoreCase))throw new IOException("After Dark is still running. Choose Exit from its tray icon and run this updater again.");}finally{p.Dispose();}}
   Swap(root);
   using(var k=Registry.LocalMachine.OpenSubKey(Key,true)){if(k!=null)k.SetValue("DisplayVersion","5.0");}
  }
  static string Swap(string root){string exe=Path.Combine(root,"AfterDarkControl.exe");byte[] bytes=Payload();string pending=Path.Combine(root,"AfterDarkControl.update-"+Guid.NewGuid().ToString("N")+".tmp");string backup=Path.Combine(root,"AfterDarkControl.backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".bak");try{File.WriteAllBytes(pending,bytes);File.Replace(pending,exe,backup);}finally{if(File.Exists(pending))File.Delete(pending);}return backup;}
 }
}
