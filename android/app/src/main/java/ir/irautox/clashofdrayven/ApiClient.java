package ir.irautox.clashofdrayven;

import android.content.Context;
import android.util.Base64;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.UUID;
import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;

final class ApiClient {
    private final SecureSession session;
    private String token,signingKey;
    ApiClient(Context c){session=new SecureSession(c);token=session.load();signingKey=session.loadSigningKey();}
    String token(){return token;}
    boolean hasSession(){return token!=null&&!token.isEmpty();}
    void clear(){token=null;signingKey=null;session.clear();}

    JSONObject health()throws Exception{return request("GET","/health",null,false);}
    JSONObject register(String username,String email,String password)throws Exception{JSONObject b=new JSONObject().put("username",username).put("email",email).put("password",password);JSONObject r=request("POST","/api/v1/register",b,false);acceptSession(r);return r;}
    JSONObject login(String username,String password,String otp)throws Exception{JSONObject b=new JSONObject().put("username",username).put("password",password);if(otp!=null&&!otp.trim().isEmpty())b.put("otp",otp.trim());JSONObject r=request("POST","/api/v1/login",b,false);acceptSession(r);return r;}
    JSONObject profile()throws Exception{return request("GET","/api/v1/profile",null,true);}
    JSONObject player(int id)throws Exception{return request("GET","/api/v1/player?id="+id,null,true);}
    GameModel state()throws Exception{JSONObject r=request("GET","/api/v1/state",null,true);GameModel m=GameModel.from(r);m.online=true;return m;}
    JSONObject syncLayout(GameModel m)throws Exception{return request("PUT","/api/v1/state",new JSONObject().put("state",m.toState()),true);}
    JSONObject createClan(String name,String tag)throws Exception{return request("POST","/api/v1/clans/create",new JSONObject().put("name",name).put("tag",tag),true);}
    JSONObject clan()throws Exception{return request("GET","/api/v1/clans/me",null,true);}
    JSONObject chat(String scope,long after)throws Exception{return request("GET","/api/v1/chat/"+scope+"?after="+Math.max(0,after),null,true);}
    JSONObject sendChat(String scope,String message)throws Exception{return request("POST","/api/v1/chat/"+scope,new JSONObject().put("message",ProfanityFilter.clean(message)),true);}
    JSONObject upgrade(String instanceId)throws Exception{return request("POST","/api/v1/buildings/upgrade",new JSONObject().put("instanceId",instanceId),true);}
    JSONObject train(String unitId,int count)throws Exception{return request("POST","/api/v1/army/train",new JSONObject().put("unitId",unitId).put("count",Math.max(1,Math.min(20,count))),true);}
    JSONObject tutorialComplete()throws Exception{return request("POST","/api/v1/tutorial/complete",new JSONObject(),true);}
    JSONObject matchmake()throws Exception{return request("POST","/api/v1/battle/matchmake",new JSONObject(),true);}
    JSONObject finishBattle(String matchId,int destruction,int stars)throws Exception{return request("POST","/api/v1/battle/finish",new JSONObject().put("matchId",matchId).put("destruction",destruction).put("stars",stars),true);}
    JSONObject shop()throws Exception{return request("GET","/api/v1/shop",null,true);}
    JSONObject buy(String sku)throws Exception{return request("POST","/api/v1/shop/buy",new JSONObject().put("sku",sku),true);}
    JSONObject beginTotp()throws Exception{return request("POST","/api/v1/2fa/begin",new JSONObject(),true);}
    JSONObject confirmTotp(String otp)throws Exception{return request("POST","/api/v1/2fa/confirm",new JSONObject().put("otp",otp),true);}
    JSONObject disableTotp(String otp)throws Exception{return request("POST","/api/v1/2fa/disable",new JSONObject().put("otp",otp),true);}
    JSONObject linkAccount(String email,String password)throws Exception{return request("POST","/api/v1/account/link",new JSONObject().put("email",email).put("password",password),true);}
    void logout(){try{request("POST","/api/v1/logout",new JSONObject(),true);}catch(Exception ignored){}clear();}

    private void acceptSession(JSONObject r)throws JSONException{token=r.getString("token");signingKey=r.optString("sessionKey",null);session.save(token,signingKey);}
    JSONObject request(String method,String path,JSONObject body,boolean signed)throws Exception{
        URL url=new URL(NativeBridge.serverBase()+path);HttpURLConnection c=(HttpURLConnection)url.openConnection();c.setRequestMethod(method);c.setConnectTimeout(8000);c.setReadTimeout(10000);c.setUseCaches(false);c.setRequestProperty("Accept","application/json");c.setRequestProperty("User-Agent","ClashOfDrayven/7 Android IrAutoX");c.setRequestProperty("X-Drayven-Engine",NativeBridge.version());
        byte[]data=body==null?new byte[0]:body.toString().getBytes(StandardCharsets.UTF_8);
        String cleanPath=url.getPath()+(url.getQuery()==null?"":"?"+url.getQuery());
        if(token!=null)c.setRequestProperty("Authorization","Bearer "+token);
        if(signed&&token!=null&&signingKey!=null){String ts=String.valueOf(System.currentTimeMillis()/1000L),nonce=UUID.randomUUID().toString().replace("-","");String payload=method+"\n"+cleanPath+"\n"+ts+"\n"+nonce+"\n"+hex(sha256(data));c.setRequestProperty("X-Drayven-Time",ts);c.setRequestProperty("X-Drayven-Nonce",nonce);c.setRequestProperty("X-Drayven-Signature",hmac(payload,signingKey));}
        if(data.length>0){c.setDoOutput(true);c.setRequestProperty("Content-Type","application/json; charset=utf-8");c.setFixedLengthStreamingMode(data.length);try(OutputStream out=c.getOutputStream()){out.write(data);}}
        int code=c.getResponseCode();InputStream in=code>=400?c.getErrorStream():c.getInputStream();String text=read(in);JSONObject r=text.isEmpty()?new JSONObject():new JSONObject(text);if(code>=400||!r.optBoolean("ok",true))throw new IOException(friendly(r.optString("error","HTTP "+code)));return r;
    }
    private static String hmac(String value,String keyB64)throws Exception{byte[]key=Base64.decode(keyB64,Base64.NO_WRAP|Base64.URL_SAFE);Mac m=Mac.getInstance("HmacSHA256");m.init(new SecretKeySpec(key,"HmacSHA256"));return hex(m.doFinal(value.getBytes(StandardCharsets.UTF_8)));}
    private static byte[]sha256(byte[]d)throws Exception{return MessageDigest.getInstance("SHA-256").digest(d);}
    private static String hex(byte[]d){StringBuilder s=new StringBuilder(d.length*2);for(byte b:d)s.append(String.format(java.util.Locale.US,"%02x",b&255));return s.toString();}
    private static String read(InputStream in)throws IOException{if(in==null)return"";try(BufferedReader br=new BufferedReader(new InputStreamReader(in,StandardCharsets.UTF_8))){StringBuilder s=new StringBuilder();String line;while((line=br.readLine())!=null)s.append(line);return s.toString();}}
    private static String friendly(String e){switch(e){case"invalid_credentials":return"نام کاربری/ایمیل یا رمز عبور اشتباه است.";case"otp_required":return"کد تایید دو مرحله‌ای لازم است.";case"invalid_otp":return"کد تایید دو مرحله‌ای صحیح نیست.";case"username_or_email_exists":return"این نام کاربری یا ایمیل قبلاً ثبت شده است.";case"unauthorized":return"نشست شما منقضی شده است.";case"bad_signature":return"اعتبار درخواست توسط سرور رد شد.";case"profanity":return"پیام به دلیل محتوای نامناسب ارسال نشد.";case"not_enough_resources":return"منابع کافی نیست.";default:return e.replace('_',' ');}}
}
