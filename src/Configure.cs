using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
namespace AfterDarkClassic {
 class ConfigureScreen:Form {
  Module module;Panel canvas;FloatingControls controls;Process engine;Timer reload;object oldAnimated;IntPtr hook;KeyHook hookProc;bool closing;
  delegate IntPtr KeyHook(int code,IntPtr message,IntPtr data);
  [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int kind,KeyHook callback,IntPtr instance,uint thread);
  [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr handle);
  [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr handle,int code,IntPtr message,IntPtr data);
  [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string module);
  public ConfigureScreen(Module selected){module=selected;BackColor=Color.Black;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=Screen.PrimaryScreen.Bounds;KeyPreview=true;ShowInTaskbar=false;canvas=new Panel{Dock=DockStyle.Fill,BackColor=Color.Black};Controls.Add(canvas);reload=new Timer{Interval=180};reload.Tick+=(s,e)=>{reload.Stop();StartEngine();};KeyDown+=(s,e)=>Close();Shown+=(s,e)=>Begin();FormClosed+=(s,e)=>Cleanup();}
  void Begin(){try{oldAnimated=Paths.Read(Paths.Legacy,"AnimatedMiniPreview",null);Paths.Write(Paths.Legacy,"AnimatedMiniPreview",1);controls=new FloatingControls(module,()=>reload.Start(),()=>Close());controls.Show(this);controls.Location=new Point(Bounds.Left+20,Bounds.Top+20);StartEngine();hookProc=Keyboard;hook=SetWindowsHookEx(13,hookProc,GetModuleHandle(null),0);controls.Activate();}catch(Exception ex){UI.Error(ex);Close();}}
  bool InControls(){IntPtr window=GetForegroundWindow();for(int i=0;i<16&&window!=IntPtr.Zero;i++){if(controls!=null&&window==controls.Handle)return true;window=GetWindow(window,4);}return false;}
  IntPtr Keyboard(int code,IntPtr message,IntPtr data){if(code>=0 && (message.ToInt32()==0x100||message.ToInt32()==0x104)&&!closing&&controls!=null&&!InControls()){BeginInvoke(new Action(()=>{if(!closing)Close();}));}return CallNextHookEx(hook,code,message,data);}
  void StartEngine(){if(closing)return;StopEngine();string file=Path.Combine(Paths.Root,"After Dark Engine.scr");engine=Process.Start(new ProcessStartInfo(file,"/p "+canvas.Handle.ToInt64()){UseShellExecute=false,WorkingDirectory=Paths.Root});if(controls!=null)controls.BringToFront();}
  void StopEngine(){if(engine!=null){try{if(!engine.HasExited){engine.Kill();engine.WaitForExit(1500);}}catch{}engine.Dispose();engine=null;}}
  void Cleanup(){if(closing)return;closing=true;reload.Stop();reload.Dispose();if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}StopEngine();if(controls!=null){controls.Dispose();controls=null;}if(oldAnimated==null){using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Paths.Legacy,true)){if(key!=null)key.DeleteValue("AnimatedMiniPreview",false);}}else Paths.Write(Paths.Legacy,"AnimatedMiniPreview",oldAnimated);}
  protected override void WndProc(ref Message m){if(m.Msg==0x112&&((long)m.WParam&0xfff0)==0xf140)return;base.WndProc(ref m);}
 }
 class FloatingControls:Form {
  Module module;Action changed,stop;bool loading=true;
  public FloatingControls(Module selected,Action refresh,Action finish){module=selected;changed=refresh;stop=finish;Font=UI.Font;Text=module.Name;FormBorderStyle=FormBorderStyle.FixedToolWindow;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;TopMost=true;BackColor=Color.White;StartPosition=FormStartPosition.Manual;int y=8;
   foreach(var c in module.Settings){var setting=c;int value=Paths.Int(Paths.ModuleKey(module.Name),c.Key,c.Default);if(c.Kind=="check"){var check=new CheckBox{Text=c.Label,Checked=value!=0,Bounds=new Rectangle(8,y,171,24)};Controls.Add(check);check.CheckedChanged+=(s,e)=>Write(setting,check.Checked?1:0);y+=29;}
    else if(c.Kind=="number"){UI.Label(this,c.Label,8,y+4,107,23);var n=new NumericUpDown{Minimum=c.Min,Maximum=c.Max,Value=Math.Max(c.Min,Math.Min(c.Max,value)),Bounds=new Rectangle(119,y,58,23)};Controls.Add(n);n.ValueChanged+=(s,e)=>Write(setting,(int)n.Value);y+=33;}
    else if(c.Kind=="slider"){int i=c.Choices.FindIndex(v=>v.Value==value);if(i<0)i=0;var slider=new TrackBar{AutoSize=false,Bounds=new Rectangle(4,y,177,24),Minimum=0,Maximum=c.Choices.Count-1,Value=i,TickStyle=TickStyle.None,BackColor=Color.White};Controls.Add(slider);var label=UI.Label(this,c.Label+":",8,y+25,107,21);label.AutoEllipsis=true;var right=UI.Label(this,c.Choices[i].Label,112,y+25,65,21);right.TextAlign=ContentAlignment.TopRight;slider.ValueChanged+=(s,e)=>{right.Text=c.Choices[slider.Value].Label;Write(setting,c.Choices[slider.Value].Value);};y+=51;}
    else {UI.Label(this,c.Label+":",8,y,171,18);var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Bounds=new Rectangle(8,y+19,169,23)};foreach(var choice in c.Choices)combo.Items.Add(choice);combo.SelectedIndex=Math.Max(0,c.Choices.FindIndex(v=>v.Value==value));Controls.Add(combo);combo.SelectedIndexChanged+=(s,e)=>Write(setting,((Choice)combo.SelectedItem).Value);y+=50;}
   }
   if(module.Name=="Fish World"||module.Name=="Rainforest"||module.Name=="Slow Burn"||module.Name=="Swirling Magic"||module.Name=="Points of View"){UI.Button(this,"More options…",8,y,169,(s,e)=>{using(var f=new AdvancedForm(module,new Dictionary<string,string>())){if(f.ShowDialog(this)==DialogResult.OK){foreach(var pair in f.Values){var key=pair.Key.Split('\n');Native.IniWrite(key[0],key[1],pair.Value,Paths.Ini);}Notify();}}});y+=33;}
   var line=new Label{BorderStyle=BorderStyle.Fixed3D,Bounds=new Rectangle(6,y+2,173,2)};Controls.Add(line);var sound=new CheckBox{Text="",Appearance=Appearance.Button,Checked=Paths.Int(Paths.Legacy,"Mutesound",0)==0,Bounds=new Rectangle(8,y+10,32,24)};using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("speaker.bmp")){if(stream!=null){var bitmap=new Bitmap(stream);bitmap.MakeTransparent(bitmap.GetPixel(0,0));sound.Image=bitmap;}}new ToolTip().SetToolTip(sound,"Toggle sound");Controls.Add(sound);sound.CheckedChanged+=(s,e)=>{if(!loading){Paths.Write(Paths.Legacy,"Mutesound",sound.Checked?0:1);Notify();}};UI.Button(this,"Stop",101,y+9,77,(s,e)=>{if(stop!=null)stop();else Close();});ClientSize=new Size(185,y+42);loading=false;FormClosing+=(s,e)=>{if(stop!=null)stop();};}
  void Write(Setting setting,int value){if(loading||Program.Rendering)return;try{Paths.Write(Paths.ModuleKey(module.Name),setting.Key,value);Notify();}catch(Exception ex){UI.Error(ex);}}
  void Notify(){if(changed!=null)changed();}
 }
}
