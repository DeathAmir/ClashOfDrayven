package ir.irautox.clashofdrayven;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

final class SecureSession {
    private static final String PREFS="drayven_secure_session";
    private static final String KEY_ALIAS="IrAutoX.ClashOfDrayven.Session.v2";
    private static final String TOKEN="token_v2",SIGNING="signing_v2";
    private final SharedPreferences prefs;
    SecureSession(Context context){prefs=context.getSharedPreferences(PREFS,Context.MODE_PRIVATE);}
    String load(){return loadValue(TOKEN);}
    String loadSigningKey(){return loadValue(SIGNING);}
    void save(String token,String signingKey){saveValue(TOKEN,token);saveValue(SIGNING,signingKey);}
    void clear(){prefs.edit().remove(TOKEN).remove(SIGNING).apply();}
    private String loadValue(String name){try{String packed=prefs.getString(name,null);if(packed==null||packed.isEmpty())return null;String[]parts=packed.split(":",2);if(parts.length!=2)return null;byte[]iv=Base64.decode(parts[0],Base64.NO_WRAP),data=Base64.decode(parts[1],Base64.NO_WRAP);Cipher c=Cipher.getInstance("AES/GCM/NoPadding");c.init(Cipher.DECRYPT_MODE,key(),new GCMParameterSpec(128,iv));return new String(c.doFinal(data),StandardCharsets.UTF_8);}catch(Exception ex){return null;}}
    private void saveValue(String name,String value){if(value==null||value.isEmpty()){prefs.edit().remove(name).apply();return;}try{Cipher c=Cipher.getInstance("AES/GCM/NoPadding");c.init(Cipher.ENCRYPT_MODE,key());byte[]data=c.doFinal(value.getBytes(StandardCharsets.UTF_8));String packed=Base64.encodeToString(c.getIV(),Base64.NO_WRAP)+":"+Base64.encodeToString(data,Base64.NO_WRAP);prefs.edit().putString(name,packed).apply();}catch(Exception ex){throw new IllegalStateException("Secure session storage failed",ex);}}
    private static SecretKey key()throws Exception{KeyStore ks=KeyStore.getInstance("AndroidKeyStore");ks.load(null);java.security.Key existing=ks.getKey(KEY_ALIAS,null);if(existing instanceof SecretKey)return(SecretKey)existing;KeyGenerator gen=KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES,"AndroidKeyStore");gen.init(new KeyGenParameterSpec.Builder(KEY_ALIAS,KeyProperties.PURPOSE_ENCRYPT|KeyProperties.PURPOSE_DECRYPT).setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).setKeySize(256).build());return gen.generateKey();}
}
