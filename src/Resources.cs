using System;
using System.IO;
using System.Text;
class Resources {
 static byte[] b; static int sections, count, root;
 static ushort U16(int o){return BitConverter.ToUInt16(b,o);} static uint U32(int o){return BitConverter.ToUInt32(b,o);}
 static int Rva(uint v){for(int i=0;i<count;i++){int s=sections+40*i; uint a=U32(s+12), size=Math.Max(U32(s+8),U32(s+16));if(v>=a && v<a+size)return (int)(v-a+U32(s+20));}throw new Exception("RVA");}
 static string Name(uint n){if((n&0x80000000)==0)return n.ToString();int p=root+(int)(n&0x7fffffff);return Encoding.Unicode.GetString(b,p+2,U16(p)*2);}
 static void Walk(int p,string path,string output){int n=U16(p+12)+U16(p+14);for(int i=0;i<n;i++){int e=p+16+8*i;string name=Name(U32(e));foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');string sub=path.Length==0?name:path+"_"+name;uint v=U32(e+4);if((v&0x80000000)!=0)Walk(root+(int)(v&0x7fffffff),sub,output);else{int d=root+(int)v;int a=Rva(U32(d)), len=(int)U32(d+4);byte[] data=new byte[len];Buffer.BlockCopy(b,a,data,0,len);string ext=".bin";if(sub.StartsWith("2_")){ext=".bmp";int hdr=BitConverter.ToInt32(data,0);if(hdr>=40 && len>=40){int bits=BitConverter.ToUInt16(data,14),colors=BitConverter.ToInt32(data,32);if(colors==0 && bits<=8)colors=1<<bits;int masks=(BitConverter.ToInt32(data,16)==3 && hdr==40)?12:0;using(var ms=new MemoryStream()){using(var w=new BinaryWriter(ms)){w.Write((ushort)0x4d42);w.Write(len+14);w.Write(0);w.Write(14+hdr+colors*4+masks);w.Write(data);data=ms.ToArray();}}}}File.WriteAllBytes(Path.Combine(output,sub+ext),data);Console.WriteLine(sub+" "+len);}}}
 static void Main(string[] args){b=File.ReadAllBytes(args[0]);int pe=(int)U32(60);count=U16(pe+6);int opt=pe+24;sections=opt+U16(pe+20);root=Rva(U32(opt+112));Directory.CreateDirectory(args[1]);Walk(root,"",args[1]);}
}
