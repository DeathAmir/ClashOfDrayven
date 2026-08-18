package ir.irautox.clashofdrayven;

import android.content.Context;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;

final class ApiClient {
    private final SecureSession session;
    private String token;
    ApiClient(Context c){session=new SecureSession(c);token=session.load();}
    String token(){return token;}
    void clear(){token=null;session.clear();}

    JSONObject register(String username,String email,String password)throws Exception{JSONObject b=new JSONObject();b.put("username",username);b.put("email",email);b.put("password",password);JSONObject r=request("POST","/api/v1/register",b);acceptToken(r);return r;}
    JSONObject login(String username,String password)throws Exception{JSONObject b=new JSONObject();b.put("username",username);b.put("password",password);JSONObject r=request("POST","/api/v1/login",b);acceptToken(r);return r;}
    JSONObject profile()throws Exception{return request("GET","/api/v1/profile",null);}
    GameModel state()throws Exception{return GameModel.from(request("GET","/api/v1/state",null));}
    void save(GameModel m)throws Exception{JSONObject b=new JSONObject();b.put("state",m.toState());request("PUT","/api/v1/state",b);}
    JSONObject createClan(String name,String tag)throws Exception{JSONObject b=new JSONObject();b.put("name",name);b.put("tag",tag);return request("POST","/api/v1/clans/create",b);}
    void logout(){try{request("POST","/api/v1/logout",new JSONObject());}catch(Exception ignored){}clear();}

    private void acceptToken(JSONObject r)throws JSONException{token=r.getString("token");session.save(token);}
    JSONObject request(String method,String path,JSONObject body)throws Exception{
        HttpURLConnection c=(HttpURLConnection)new URL(NativeBridge.serverBase()+path).openConnection();c.setRequestMethod(method);c.setConnectTimeout(12000);c.setReadTimeout(12000);c.setUseCaches(false);c.setRequestProperty("Accept","application/json");c.setRequestProperty("User-Agent","ClashOfDrayven/6 Android");
        if(token!=null)c.setRequestProperty("Authorization","Bearer "+token);
        if(body!=null){byte[]data=body.toString().getBytes(StandardCharsets.UTF_8);c.setDoOutput(true);c.setRequestProperty("Content-Type","application/json; charset=utf-8");c.setFixedLengthStreamingMode(data.length);try(OutputStream out=c.getOutputStream()){out.write(data);}}
        int code=c.getResponseCode();InputStream in=code>=400?c.getErrorStream():c.getInputStream();String text=read(in);JSONObject r=text.isEmpty()?new JSONObject():new JSONObject(text);if(code>=400||!r.optBoolean("ok",true))throw new IOException(friendly(r.optString("error","HTTP "+code)));return r;
    }
    private static String read(InputStream in)throws IOException{if(in==null)return"";try(BufferedReader br=new BufferedReader(new InputStreamReader(in,StandardCharsets.UTF_8))){StringBuilder s=new StringBuilder();String line;while((line=br.readLine())!=null)s.append(line);return s.toString();}}
    private static String friendly(String e){switch(e){case"invalid_credentials":return"نام کاربری/ایمیل یا رمز عبور اشتباه است.";case"username_or_email_exists":return"این نام کاربری یا ایمیل قبلاً ثبت شده است.";case"unauthorized":return"نشست شما منقضی شده است.";case"password_8_128":return"رمز عبور باید حداقل ۸ کاراکتر باشد.";default:return e.replace('_',' ');}}
}
