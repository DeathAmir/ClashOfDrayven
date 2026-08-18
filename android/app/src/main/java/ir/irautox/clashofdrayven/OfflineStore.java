package ir.irautox.clashofdrayven;

import android.content.Context;
import java.io.*;
import java.nio.charset.StandardCharsets;
import org.json.JSONObject;

final class OfflineStore {
    private final File file;
    OfflineStore(Context c){file=new File(c.getFilesDir(),"offline-village-v7.json");}
    GameModel load(){try{if(!file.isFile())return null;byte[]b=new byte[(int)file.length()];try(InputStream in=new FileInputStream(file)){int o=0,n;while(o<b.length&&(n=in.read(b,o,b.length-o))>0)o+=n;}return GameModel.from(new JSONObject(new String(b,StandardCharsets.UTF_8)));}catch(Exception ex){return null;}}
    void save(GameModel m){if(m==null)return;try{File tmp=new File(file.getParentFile(),file.getName()+".tmp");try(OutputStream out=new FileOutputStream(tmp)){out.write(m.toState().toString().getBytes(StandardCharsets.UTF_8));}if(file.exists())file.delete();tmp.renameTo(file);}catch(Exception ignored){}}
}
