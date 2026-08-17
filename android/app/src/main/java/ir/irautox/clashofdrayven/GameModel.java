package ir.irautox.clashofdrayven;

import org.json.*;
import java.util.*;

final class GameModel {
    int gold=7000, elixir=7000, gems=250, xp=0, level=1;
    final ArrayList<Building> buildings=new ArrayList<>();
    final LinkedHashMap<String,Integer> units=new LinkedHashMap<>();
    String clanName=null, clanTag=null;

    GameModel(){
        units.put("vanguard",0);units.put("ranger",0);units.put("rogue",0);units.put("breaker",0);units.put("brute",0);units.put("mage",0);units.put("healer",0);units.put("stormcaller",0);
    }

    static GameModel from(JSONObject root) throws JSONException {
        JSONObject s=root.has("state")?root.getJSONObject("state"):root; GameModel m=new GameModel();
        m.gold=s.optInt("gold",7000);m.elixir=s.optInt("elixir",7000);m.gems=s.optInt("gems",250);m.xp=s.optInt("xp",0);m.level=Math.max(1,s.optInt("level",1));
        m.buildings.clear();JSONArray bs=s.optJSONArray("buildings");if(bs!=null)for(int i=0;i<bs.length();i++){JSONObject b=bs.getJSONObject(i);m.buildings.add(new Building(b.optString("instanceId",UUID.randomUUID().toString()),b.optString("definitionId","townhall"),b.optInt("x",9),b.optInt("y",9),Math.max(1,b.optInt("level",1))));}
        if(m.buildings.stream().noneMatch(b->"townhall".equals(b.id)))m.buildings.add(new Building(UUID.randomUUID().toString(),"townhall",9,9,1));
        JSONObject us=s.optJSONObject("units");if(us!=null){Iterator<String>it=us.keys();while(it.hasNext()){String k=it.next();m.units.put(k,Math.max(0,us.optInt(k,0)));}}
        JSONObject clan=s.optJSONObject("clan");if(clan!=null){m.clanName=clan.optString("name",null);m.clanTag=clan.optString("tag",null);}return m;
    }

    JSONObject toState() throws JSONException {
        JSONObject s=new JSONObject();s.put("gold",gold);s.put("elixir",elixir);s.put("gems",gems);s.put("xp",xp);s.put("level",level);
        JSONArray bs=new JSONArray();for(Building b:buildings){JSONObject o=new JSONObject();o.put("instanceId",b.instance);o.put("definitionId",b.id);o.put("x",b.x);o.put("y",b.y);o.put("level",b.level);bs.put(o);}s.put("buildings",bs);
        JSONObject us=new JSONObject();for(Map.Entry<String,Integer>e:units.entrySet())us.put(e.getKey(),e.getValue());s.put("units",us);
        if(clanName!=null){JSONObject c=new JSONObject();c.put("name",clanName);c.put("tag",clanTag);s.put("clan",c);}else s.put("clan",JSONObject.NULL);
        return s;
    }

    boolean spend(String currency,int amount){if(amount<=0)return true;if("gold".equals(currency)&&gold>=amount){gold-=amount;return true;}if("elixir".equals(currency)&&elixir>=amount){elixir-=amount;return true;}if("gems".equals(currency)&&gems>=amount){gems-=amount;return true;}return false;}
    void gainXp(int n){xp+=Math.max(0,n);while(xp>=level*220){xp-=level*220;level++;gems+=10;}}

    static final class Building { String instance,id;int x,y,level;Building(String i,String d,int x,int y,int l){this.instance=i;this.id=d;this.x=x;this.y=y;this.level=l;} }
}
