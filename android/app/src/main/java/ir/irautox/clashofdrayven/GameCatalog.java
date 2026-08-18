package ir.irautox.clashofdrayven;

import org.json.*;
import java.util.*;

final class GameCatalog {
    static final BuildingSpec[] BUILDINGS;
    static final UnitSpec[] UNITS;

    static {
        try {
            JSONObject root=new JSONObject(NativeBridge.catalogJson());
            JSONArray bs=root.getJSONArray("buildings");
            BUILDINGS=new BuildingSpec[bs.length()];
            for(int i=0;i<bs.length();i++){
                JSONObject b=bs.getJSONObject(i);JSONArray a=b.optJSONArray("art");String[] art=new String[a==null?0:a.length()];
                for(int j=0;j<art.length;j++)art[j]=a.getString(j);
                BUILDINGS[i]=new BuildingSpec(b.getString("id"),b.getString("name"),b.getString("currency"),b.getInt("cost"),b.getInt("max"),art);
            }
            JSONArray us=root.getJSONArray("units");
            UNITS=new UnitSpec[us.length()];
            for(int i=0;i<us.length();i++){
                JSONObject u=us.getJSONObject(i);JSONArray a=u.optJSONArray("art");String[] art=new String[a==null?0:a.length()];
                for(int j=0;j<art.length;j++)art[j]=a.getString(j);
                float[] combat=NativeBridge.unitCombat(u.getString("id"));
                UNITS[i]=new UnitSpec(u.getString("id"),u.getString("name"),u.getString("currency"),u.getInt("cost"),u.getInt("power"),art,
                    combat!=null&&combat.length>0?combat[0]:.13f,combat!=null&&combat.length>1?combat[1]:.052f,
                    combat!=null&&combat.length>2?combat[2]:18f,combat!=null&&combat.length>3?combat[3]:.72f);
            }
        } catch(Exception ex) { throw new ExceptionInInitializerError(ex); }
    }

    static BuildingSpec building(String id){for(BuildingSpec s:BUILDINGS)if(s.id.equals(id))return s;if("townhall".equals(id))return new BuildingSpec("townhall","تالار درایون","gold",0,12,new String[]{"town","hall"});return null;}
    static UnitSpec unit(String id){for(UnitSpec s:UNITS)if(s.id.equals(id))return s;return null;}
    static String currencyName(String id){if("gold".equals(id))return"طلا";if("elixir".equals(id))return"اکسیر";if("gems".equals(id))return"جم";return id;}

    static final class BuildingSpec{
        final String id,name,currency;final int cost,max;final String[]art;
        BuildingSpec(String i,String n,String c,int co,int m,String...a){id=i;name=n;currency=c;cost=co;max=m;art=a;}
    }
    static final class UnitSpec{
        final String id,name,currency;final int cost,power;final String[]art;final float speed,range,damage,delay;
        UnitSpec(String i,String n,String c,int co,int p,String[]a,float s,float r,float d,float de){id=i;name=n;currency=c;cost=co;power=p;art=a;speed=s;range=r;damage=d;delay=de;}
    }
}
