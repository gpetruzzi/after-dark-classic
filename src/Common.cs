using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Win32;
using System.Collections.Generic;
namespace AfterDarkClassic {
 static class Paths {
  public const string Legacy=@"Software\Berkeley Systems\After Dark";
  public const string Modern=@"Software\AfterDarkClassic";
  public const string Machine=Legacy+@"\4.00";
  public static string Root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
  public static string Data { get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"AfterDarkClassic","Settings");} }
  public static string Ini {get{return Path.Combine(Data,"modules.ini");}}
  public static string Asset(string s){return Path.Combine(Root,"Assets",s);}
  public static Icon GetIcon(){string p=Asset("AfterDark.ico");return File.Exists(p)?new Icon(p):SystemIcons.Application;}
  public static string ModuleKey(string name){return Legacy+@"\Module Settings\After Dark 4.0\"+name;}
  public static object Read(string key,string name,object fallback){using(var k=Registry.CurrentUser.OpenSubKey(key)){return k==null?fallback:k.GetValue(name,fallback);}}
  public static int Int(string key,string name,int fallback){try{return Convert.ToInt32(Read(key,name,fallback));}catch{return fallback;}}
  public static void Write(string key,string name,object value){using(var k=Registry.CurrentUser.CreateSubKey(key)){k.SetValue(name,value,value is int?RegistryValueKind.DWord:RegistryValueKind.String);}}
  public static string Quote(string s){return "\""+s+"\"";}
 }
 static class Native {
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] public static extern uint GetPrivateProfileString(string section,string key,string def,System.Text.StringBuilder value,uint size,string file);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] public static extern bool WritePrivateProfileString(string section,string key,string value,string file);
  [DllImport("user32.dll")] public static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] public static extern bool SystemParametersInfo(uint action,uint parameter,IntPtr value,uint flags);
  [StructLayout(LayoutKind.Sequential)] public struct LASTINPUTINFO {public uint size;public uint tick;}
  public static uint Idle(){var x=new LASTINPUTINFO();x.size=(uint)Marshal.SizeOf(x);return GetLastInputInfo(ref x)?unchecked((uint)Environment.TickCount-x.tick):0;}
  public static string IniRead(string section,string key,string fallback,string file){var s=new System.Text.StringBuilder(4096);GetPrivateProfileString(section,key,fallback,s,4096,file);return s.ToString();}
  public static void IniWrite(string section,string key,string value,string file){if(!WritePrivateProfileString(section,key,value,file))throw new IOException("No se pudo guardar "+file+" ("+Marshal.GetLastWin32Error()+").");}
 }
 static class WindowsSaver {
  public abstract class Store {public abstract object GetValue(string name,object fallback);public object GetValue(string name){return GetValue(name,null);}public abstract void SetValue(string name,object value,RegistryValueKind kind);public void SetValue(string name,object value){SetValue(name,value,value is int?RegistryValueKind.DWord:RegistryValueKind.String);}public abstract void DeleteValue(string name,bool fail);public abstract RegistryValueKind GetValueKind(string name);}
  class RegistryStore:Store {RegistryKey key;public RegistryStore(RegistryKey value){key=value;}public override object GetValue(string name,object fallback){return key.GetValue(name,fallback);}public override void SetValue(string name,object value,RegistryValueKind kind){key.SetValue(name,value,kind);}public override void DeleteValue(string name,bool fail){key.DeleteValue(name,fail);}public override RegistryValueKind GetValueKind(string name){return key.GetValueKind(name);}}
  class MemoryStore:Store {Dictionary<string,object> values=new Dictionary<string,object>();Dictionary<string,RegistryValueKind> kinds=new Dictionary<string,RegistryValueKind>();public override object GetValue(string name,object fallback){object value;return values.TryGetValue(name,out value)?value:fallback;}public override void SetValue(string name,object value,RegistryValueKind kind){values[name]=value;kinds[name]=kind;}public override void DeleteValue(string name,bool fail){values.Remove(name);kinds.Remove(name);}public override RegistryValueKind GetValueKind(string name){return kinds[name];}}
  public const string Desktop=@"Control Panel\Desktop";
  const string Backup=Paths.Modern+@"\WindowsSaverBackup";
  static readonly string[] Names={"SCRNSAVE.EXE","ScreenSaveActive","ScreenSaveTimeOut"};
  public static int Seconds(){int seconds=Paths.Int(Desktop,"ScreenSaveTimeOut",300);return seconds>0?seconds:300;}
  static void Notify(uint action,uint value){if(!Native.SystemParametersInfo(action,value,IntPtr.Zero,3))throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error(),"Windows no pudo actualizar el salvapantallas.");}
  public static void Configure(string root,bool active,int seconds){string exe=Path.Combine(root,"After Dark.scr");if(!File.Exists(exe))throw new IOException("No se encontró After Dark.scr en la carpeta instalada.");EnsureModule(root);using(var desktop=Registry.CurrentUser.CreateSubKey(Desktop))using(var backup=Registry.CurrentUser.CreateSubKey(Backup))ConfigureCore(root,active,seconds,new RegistryStore(desktop),new RegistryStore(backup),Notify);}
  public static void ConfigureCore(string root,bool active,int seconds,Store desktop,Store backup,Action<uint,uint> notify){if(seconds<1)throw new ArgumentOutOfRangeException("seconds");string exe=Path.Combine(root,"After Dark.scr");var before=Snapshot(desktop);if(Convert.ToString(backup.GetValue("Owner",""))!=exe){SaveSnapshot(backup,before);backup.SetValue("Owner",exe);}
   try{desktop.SetValue("SCRNSAVE.EXE",exe,RegistryValueKind.String);desktop.SetValue("ScreenSaveTimeOut",seconds.ToString(),RegistryValueKind.String);desktop.SetValue("ScreenSaveActive",active?"1":"0",RegistryValueKind.String);notify(0x000f,(uint)seconds);notify(0x0011,active?1u:0u);}catch{try{int timeout;notify(0x000f,int.TryParse(before[2].Value as string,out timeout)&&timeout>0?(uint)timeout:300u);notify(0x0011,Convert.ToString(before[1].Value)=="1"?1u:0u);}catch{}RestoreSnapshot(desktop,before);throw;}}
  class SavedValue {public object ValueObject;public object Value {get{return ValueObject;}}public RegistryValueKind Kind;}
  static SavedValue[] Snapshot(Store key){var values=new SavedValue[Names.Length];for(int i=0;i<Names.Length;i++){object v=key.GetValue(Names[i],null);values[i]=new SavedValue{ValueObject=v,Kind=v==null?RegistryValueKind.String:key.GetValueKind(Names[i])};}return values;}
  static void RestoreSnapshot(Store key,SavedValue[] values){for(int i=0;i<Names.Length;i++){if(values[i].Value==null)key.DeleteValue(Names[i],false);else key.SetValue(Names[i],values[i].Value,values[i].Kind);}}
  static void SaveSnapshot(Store key,SavedValue[] values){for(int i=0;i<Names.Length;i++){key.SetValue("Had"+i,values[i].Value==null?0:1,RegistryValueKind.DWord);key.SetValue("Kind"+i,(int)values[i].Kind,RegistryValueKind.DWord);if(values[i].Value!=null)key.SetValue("Value"+i,values[i].Value,values[i].Kind);else key.DeleteValue("Value"+i,false);}}
  static SavedValue[] LoadSnapshot(Store key){var values=new SavedValue[Names.Length];for(int i=0;i<Names.Length;i++)values[i]=new SavedValue{ValueObject=Convert.ToInt32(key.GetValue("Had"+i,0))==0?null:key.GetValue("Value"+i,null),Kind=(RegistryValueKind)Convert.ToInt32(key.GetValue("Kind"+i,(int)RegistryValueKind.String))};return values;}
  public static void Restore(string root){using(var desktop=Registry.CurrentUser.CreateSubKey(Desktop))using(var backup=Registry.CurrentUser.OpenSubKey(Backup)){if(backup==null)return;RestoreCore(root,new RegistryStore(desktop),new RegistryStore(backup),Notify);}Registry.CurrentUser.DeleteSubKeyTree(Backup,false);}
  public static void RestoreCore(string root,Store desktop,Store backup,Action<uint,uint> notify){string exe=Path.Combine(root,"After Dark.scr");if(!string.Equals(Convert.ToString(backup.GetValue("Owner","")),exe,StringComparison.OrdinalIgnoreCase)||!string.Equals(Convert.ToString(desktop.GetValue("SCRNSAVE.EXE","")),exe,StringComparison.OrdinalIgnoreCase))return;var saved=LoadSnapshot(backup);int seconds;notify(0x000f,int.TryParse(Convert.ToString(saved[2].Value),out seconds)&&seconds>0?(uint)seconds:300u);notify(0x0011,Convert.ToString(saved[1].Value)=="1"?1u:0u);RestoreSnapshot(desktop,saved);}
  static void EnsureModule(string root){Paths.Write(Paths.Legacy,"SelectedFolder","After Dark 4.0");string selected=Convert.ToString(Paths.Read(Paths.Legacy+@"\Folders","After Dark 4.0",""));if(File.Exists(Path.Combine(root,"After Dark 4.0",selected+".ad")))return;string preferred=Path.Combine(root,"After Dark 4.0","Flying Toasters!.ad");string[] files=Directory.GetFiles(Path.Combine(root,"After Dark 4.0"),"*.ad");if(files.Length==0)throw new IOException("No hay módulos instalados.");selected=Path.GetFileNameWithoutExtension(File.Exists(preferred)?preferred:files[0]);Paths.Write(Paths.Legacy,"SelectedFolder","After Dark 4.0");Paths.Write(Paths.Legacy+@"\Folders","After Dark 4.0",selected);}
  public static void SelfTest(string report){var desktop=new MemoryStore();var backup=new MemoryStore();desktop.SetValue("SCRNSAVE.EXE",@"C:\Old\previous.scr");desktop.SetValue("ScreenSaveActive","0");desktop.SetValue("ScreenSaveTimeOut","720");desktop.SetValue("ScreenSaverIsSecure","1");var calls=new List<string>();Action<uint,uint> notify=(a,v)=>calls.Add(a+":"+v);string root=@"C:\Example With Spaces\After Dark Classic";ConfigureCore(root,true,720,desktop,backup,notify);if(Convert.ToString(desktop.GetValue("SCRNSAVE.EXE"))!=Path.Combine(root,"After Dark.scr")||Convert.ToString(desktop.GetValue("ScreenSaveActive"))!="1"||!calls.Contains("15:720")||!calls.Contains("17:1"))throw new Exception("Registration failed");ConfigureCore(root,false,300,desktop,backup,notify);if(Convert.ToString(desktop.GetValue("ScreenSaveActive"))!="0")throw new Exception("Off failed");RestoreCore(root,desktop,backup,notify);if(Convert.ToString(desktop.GetValue("SCRNSAVE.EXE"))!=@"C:\Old\previous.scr"||Convert.ToString(desktop.GetValue("ScreenSaveTimeOut"))!="720"||Convert.ToString(desktop.GetValue("ScreenSaverIsSecure"))!="1")throw new Exception("Restoration or security preservation failed");try{ConfigureCore(root,true,300,desktop,backup,(a,v)=>{throw new IOException("simulated API error");});throw new Exception("Expected failure");}catch(IOException){}if(Convert.ToString(desktop.GetValue("SCRNSAVE.EXE"))!=@"C:\Old\previous.scr")throw new Exception("Rollback failed");File.WriteAllText(report,"PASS: native selection, timeout, On/Off, restoration, unchanged password setting and rollback. Uses in-memory stores and a simulated Windows API; desktop settings were not changed.");}
 }
 class Choice { public string Label;public int Value;public override string ToString(){return Label;} }
 class Setting { public string Key,Label,Kind;public int Min,Max,Default;public List<Choice> Choices=new List<Choice>(); }
 class Module { public string Name,File,Image,Help;public List<Setting> Settings=new List<Setting>();public override string ToString(){return Name;} }
 static class Catalog {
  public static List<Module> Load(){var d=new XmlDocument();d.Load(Path.Combine(Paths.Root,"catalog.xml"));var result=new List<Module>();HashSet<string> selected=null;string selection=Path.Combine(Paths.Root,"installed-modules.txt");if(File.Exists(selection))selected=new HashSet<string>(File.ReadAllLines(selection),StringComparer.OrdinalIgnoreCase);foreach(XmlElement e in d.SelectNodes("/catalog/module")){if(selected!=null&&!selected.Contains(e.GetAttribute("name")))continue;var m=new Module();m.Name=e.GetAttribute("name");m.File=e.GetAttribute("file");m.Image=e.GetAttribute("image");m.Help=e.GetAttribute("help");foreach(XmlElement c in e.SelectNodes("control")){var s=new Setting();s.Key=c.GetAttribute("key");s.Label=c.GetAttribute("label");s.Kind=c.GetAttribute("kind");int.TryParse(c.GetAttribute("min"),out s.Min);int.TryParse(c.GetAttribute("max"),out s.Max);int.TryParse(c.GetAttribute("default"),out s.Default);foreach(XmlElement o in c.SelectNodes("option")){s.Choices.Add(new Choice{Label=o.GetAttribute("label"),Value=int.Parse(o.GetAttribute("value"))});}m.Settings.Add(s);}result.Add(m);}return result;}
 }
 static class UI {
  public static Font Font=new Font("Microsoft Sans Serif",8.25f);
  public static Button Button(Control parent,string text,int x,int y,int w,EventHandler click){var b=new Button{Text=text,Bounds=new Rectangle(x,y,w,25),UseVisualStyleBackColor=false};b.Click+=click;parent.Controls.Add(b);return b;}
  public static Label Label(Control parent,string text,int x,int y,int w,int h){var l=new Label{Text=text,Bounds=new Rectangle(x,y,w,h)};parent.Controls.Add(l);return l;}
  public static void Error(Exception ex){MessageBox.Show(ex.Message,"After Dark",MessageBoxButtons.OK,MessageBoxIcon.Error);}
  public static void Render(Form f,string path){f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-3000,-3000);f.ShowInTaskbar=false;f.Show();Application.DoEvents();using(var bmp=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(path,System.Drawing.Imaging.ImageFormat.Png);}f.Dispose();}
 }
}
