package ir.irautox.clashofdrayven;

import android.content.Context;
import android.content.pm.PackageInfo;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Typeface;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.Inflater;
import java.util.zip.InflaterInputStream;

final class PackManager {
    interface Progress { void update(int percent,String stage); }
    private static final byte[] MAGIC=new byte[]{0x43,0x4c,0x44,0x52,0x59,0x50,0x4b,0x1a};
    private final File root;private final HashMap<String,Bitmap> bitmapCache=new HashMap<>();private Typeface font,magic;
    private PackManager(File root){this.root=root;}

    static PackManager prepare(Context context)throws Exception{return prepare(context,null);}
    static PackManager prepare(Context context,Progress progress)throws Exception{
        File root=new File(context.getFilesDir(),"drayven-assets-v7");File stamp=new File(root,".stamp");PackageInfo info=context.getPackageManager().getPackageInfo(context.getPackageName(),0);String wanted="v7:"+info.lastUpdateTime;
        if(progress!=null)progress.update(12,"بررسی بسته‌های بازی");
        if(!(stamp.isFile()&&wanted.equals(readText(stamp)))){
            delete(root);if(!root.mkdirs()&&!root.isDirectory())throw new IOException("cannot create asset cache");
            for(int i=1;i<=20;i++){unpack(context,"packs/CLDRYPK"+i,root);if(progress!=null)progress.update(14+(int)(i*3.6f),"بازکردن بسته "+i+" از ۲۰");}
            writeText(stamp,wanted);
        }else if(progress!=null)progress.update(86,"بسته‌های بازی آماده‌اند");
        PackManager pm=new PackManager(root);
        try{File f=pm.exact("Fonts/Vazir.ttf");pm.font=f!=null?Typeface.createFromFile(f):Typeface.DEFAULT;}catch(Exception ignored){}
        try{File f=pm.exact("Fonts/IrAutoX-Magic.ttf");pm.magic=f!=null?Typeface.createFromFile(f):Typeface.DEFAULT_BOLD;}catch(Exception ignored){}
        if(progress!=null)progress.update(89,"ساخت کش تصویر و قلم");return pm;
    }
    File root(){return root;}
    Typeface font(){return font!=null?font:Typeface.DEFAULT;}
    Typeface magic(){return magic!=null?magic:Typeface.DEFAULT_BOLD;}
    File exact(String rel){if(rel==null)return null;try{File f=new File(root,rel.replace('/',File.separatorChar)).getCanonicalFile();String base=root.getCanonicalPath()+File.separator;if(!f.getPath().startsWith(base)||!f.isFile())return null;return f;}catch(Exception ex){return null;}}
    Bitmap bitmap(String rel){if(bitmapCache.containsKey(rel))return bitmapCache.get(rel);File f=exact(rel);Bitmap b=f==null?null:BitmapFactory.decodeFile(f.getAbsolutePath());bitmapCache.put(rel,b);return b;}
    Bitmap building(String id){return bitmap("Canonical/buildings/"+id+".png");}
    Bitmap unit(String id){return bitmap("Canonical/units/"+id+".png");}
    Bitmap ui(String id){return bitmap("Canonical/ui/"+id+".png");}
    Bitmap scenery(String id){return bitmap("Canonical/scenery/"+id+".png");}
    Bitmap brand(String id){String ext=("background".equals(id)||"duf".equals(id)||"IrAutoX".equals(id)||"COD".equals(id))?".jpg":"";return bitmap("Brand/"+id+ext);}
    Bitmap unitFrame(String id,char dir){boolean officer="ranger".equals(id)||"brute".equals(id)||"mage".equals(id)||"healer".equals(id)||"stormcaller".equals(id);char d=(dir=='b'||dir=='l'||dir=='r')?dir:'f';Bitmap b=bitmap("Canonical/units/frames/"+(officer?"officer":"soldier")+"_"+d+".png");return b!=null?b:unit(id);}

    private static void unpack(Context context,String assetName,File root)throws Exception{
        File raw=File.createTempFile("dry-pack-",".raw",context.getCacheDir());
        try(InputStream base=new BufferedInputStream(context.getAssets().open(assetName))){byte[]magic=readN(base,MAGIC.length);if(!Arrays.equals(magic,MAGIC))throw new IOException("bad CLDRYPK magic: "+assetName);int version=readI32(base);if(version!=1)throw new IOException("unsupported CLDRYPK version");long rawLength=readI64(base);int hashLen=readI32(base);byte[]expected=readN(base,hashLen);byte[]compressed=readAll(base);if(!inflate(compressed,raw,true)&&!inflate(compressed,raw,false))throw new IOException("CLDRYPK deflate decode failed");if(raw.length()!=rawLength)throw new IOException("CLDRYPK length mismatch");if(!Arrays.equals(expected,digest(raw)))throw new IOException("CLDRYPK archive hash mismatch");}
        try(InputStream in=new BufferedInputStream(new FileInputStream(raw),128*1024)){int count=readI32(in);String rootPath=root.getCanonicalPath()+File.separator;for(int i=0;i<count;i++){String rel=readDotNetString(in).replace('/',File.separatorChar);long len=readI64(in);byte[]expected=readN(in,readI32(in));File dest=new File(root,rel).getCanonicalFile();if(!dest.getPath().startsWith(rootPath))throw new IOException("unsafe pack path");File parent=dest.getParentFile();if(parent!=null)parent.mkdirs();MessageDigest md=MessageDigest.getInstance("SHA-256");try(OutputStream out=new BufferedOutputStream(new FileOutputStream(dest))){copy(in,out,len,md);}if(!Arrays.equals(expected,md.digest()))throw new IOException("file hash mismatch: "+rel);}}finally{raw.delete();}
    }
    private static boolean inflate(byte[]compressed,File raw,boolean nowrap){Inflater inflater=new Inflater(nowrap);try(InputStream zin=new InflaterInputStream(new ByteArrayInputStream(compressed),inflater,128*1024);OutputStream out=new BufferedOutputStream(new FileOutputStream(raw))){copy(zin,out,-1,null);return true;}catch(Exception ex){raw.delete();return false;}finally{inflater.end();}}
    private static byte[] readAll(InputStream in)throws IOException{ByteArrayOutputStream out=new ByteArrayOutputStream();copy(in,out,-1,null);return out.toByteArray();}
    private static void copy(InputStream in,OutputStream out,long length,MessageDigest md)throws IOException{byte[]buf=new byte[128*1024];long left=length;while(length<0||left>0){int want=length<0?buf.length:(int)Math.min(buf.length,left);int n=in.read(buf,0,want);if(n<0){if(length<0)break;throw new EOFException();}out.write(buf,0,n);if(md!=null)md.update(buf,0,n);if(length>=0)left-=n;}}
    private static byte[] digest(File file)throws Exception{MessageDigest md=MessageDigest.getInstance("SHA-256");try(InputStream in=new FileInputStream(file)){byte[]b=new byte[128*1024];int n;while((n=in.read(b))>0)md.update(b,0,n);}return md.digest();}
    private static byte[] readN(InputStream in,int n)throws IOException{byte[]b=new byte[n];int o=0;while(o<n){int r=in.read(b,o,n-o);if(r<0)throw new EOFException();o+=r;}return b;}
    private static int readI32(InputStream in)throws IOException{return(in.read()&255)|((in.read()&255)<<8)|((in.read()&255)<<16)|((in.read()&255)<<24);}
    private static long readI64(InputStream in)throws IOException{long v=0;for(int i=0;i<8;i++){int b=in.read();if(b<0)throw new EOFException();v|=((long)b)<<(8*i);}return v;}
    private static int read7(InputStream in)throws IOException{int count=0,shift=0;while(shift<35){int b=in.read();if(b<0)throw new EOFException();count|=(b&0x7f)<<shift;if((b&0x80)==0)return count;shift+=7;}throw new IOException("bad string length");}
    private static String readDotNetString(InputStream in)throws IOException{return new String(readN(in,read7(in)),StandardCharsets.UTF_8);}
    private static void delete(File f){if(!f.exists())return;if(f.isDirectory()){File[]a=f.listFiles();if(a!=null)for(File x:a)delete(x);}f.delete();}
    private static String readText(File f)throws IOException{try(FileInputStream in=new FileInputStream(f)){byte[]b=new byte[(int)f.length()];int o=0,n;while(o<b.length&&(n=in.read(b,o,b.length-o))>0)o+=n;return new String(b,StandardCharsets.UTF_8);}}
    private static void writeText(File f,String s)throws IOException{try(OutputStream out=new FileOutputStream(f)){out.write(s.getBytes(StandardCharsets.UTF_8));}}
}
