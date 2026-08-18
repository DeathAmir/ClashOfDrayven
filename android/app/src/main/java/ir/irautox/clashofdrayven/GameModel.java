package ir.irautox.clashofdrayven;

import org.json.*;
import java.util.*;

final class GameModel {
    int gold=7000,elixir=7000,gems=250,xp=0,level=1,points=1000,playerId=0,townHallLevel=1;
    boolean tutorialDone=false,starterGiftClaimed=false,online=false;
    final ArrayList<Building> buildings=new ArrayList<>();
    final LinkedHashMap<String,Integer> units=new LinkedHashMap<>();
    String clanName=null,clanTag=null,username="فرمانده";
    GameModel(){for(GameCatalog.UnitSpec u:GameCatalog.UNITS)units.put(u.id,0);}
    static GameModel freshOffline(){GameModel m=new GameModel();m.buildings.add(new Building("starter-townhall","townhall",9,9,1));return m;}
    static GameModel from(JSONObject root)throws JSONException{
        JSONObject s=root.has("state")?root.getJSONObject("state"):root;GameModel m=new GameModel();
        JSONObject user=root.optJSONObject("user");if(user!=null){m.playerId=user.optInt("id",0);m.username=user.optString("username",m.username);m.points=user.optInt("points",m.points);}
        m.gold=s.optInt("gold",7000);m.elixir=s.optInt("elixir",7000);m.gems=s.optInt("gems",250);m.xp=s.optInt("xp",0);m.level=Math.max(1,s.optInt("level",1));m.points=s.optInt("points",m.points);m.playerId=s.optInt("playerId",m.playerId);m.username=s.optString("username",m.username);m.tutorialDone=s.optBoolean("tutorialDone",false);m.starterGiftClaimed=s.optBoolean("starterGiftClaimed",false);
        m.buildings.clear();JSONArray bs=s.optJSONArray("buildings");if(bs!=null)for(int i=0;i<bs.length();i++){JSONObject b=bs.getJSONObject(i);m.buildings.add(new Building(b.optString("instanceId",UUID.randomUUID().toString()),b.optString("definitionId","townhall"),b.optInt("x",9),b.optInt("y",9),Math.max(1,b.optInt("level",1))));}
        if(m.buildings.stream().noneMatch(b->"townhall".equals(b.id)))m.buildings.add(new Building("starter-townhall","townhall",9,9,1));
        for(Building b:m.buildings)if("townhall".equals(b.id))m.townHallLevel=Math.max(m.townHallLevel,b.level);
        JSONObject us=s.optJSONObject("units");if(us!=null){Iterator<String>it=us.keys();while(it.hasNext()){String k=it.next();m.units.put(k,Math.max(0,us.optInt(k,0)));}}
        JSONObject clan=s.optJSONObject("clan");if(clan!=null){m.clanName=clan.optString("name",null);m.clanTag=clan.optString("tag",null);}return m;
    }
    JSONObject toState()throws JSONException{
        JSONObject s=new JSONObject();s.put("gold",gold);s.put("elixir",elixir);s.put("gems",gems);s.put("xp",xp);s.put("level",level);s.put("points",points);s.put("playerId",playerId);s.put("username",username);s.put("tutorialDone",tutorialDone);s.put("starterGiftClaimed",starterGiftClaimed);
        JSONArray bs=new JSONArray();for(Building b:buildings){JSONObject o=new JSONObject();o.put("instanceId",b.instance);o.put("definitionId",b.id);o.put("x",b.x);o.put("y",b.y);o.put("level",b.level);bs.put(o);}s.put("buildings",bs);
        JSONObject us=new JSONObject();for(Map.Entry<String,Integer>e:units.entrySet())us.put(e.getKey(),e.getValue());s.put("units",us);if(clanName!=null){JSONObject c=new JSONObject();c.put("name",clanName);c.put("tag",clanTag);s.put("clan",c);}else s.put("clan",JSONObject.NULL);return s;
    }
    boolean spend(String currency,int amount){int[]r=NativeBridge.spend(currency,gold,elixir,gems,amount);if(r==null||r.length<4)return false;gold=r[1];elixir=r[2];gems=r[3];return r[0]!=0;}
    void gainXp(int n){int[]r=NativeBridge.gainXp(xp,level,gems,n);if(r!=null&&r.length>=3){xp=r[0];level=r[1];gems=r[2];}}
    void applyProduction(){int n=buildings.size();String[]ids=new String[n];int[]levels=new int[n];for(int i=0;i<n;i++){Building b=buildings.get(i);ids[i]=b.id;levels[i]=b.level;}int[]r=NativeBridge.production(ids,levels);if(r!=null&&r.length>=2){gold=Math.min(9_999_999,gold+Math.max(0,r[0]));elixir=Math.min(9_999_999,elixir+Math.max(0,r[1]));}}
    Building at(int x,int y){for(Building b:buildings)if(b.x==x&&b.y==y)return b;return null;}
    String rank(){return NativeBridge.rankName(points);}
    static final class Building{String instance,id;int x,y,level;Building(String i,String d,int x,int y,int l){this.instance=i;this.id=d;this.x=x;this.y=y;this.level=l;}}
}
