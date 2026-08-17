package ir.irautox.clashofdrayven;

final class GameCatalog {
    static final BuildingSpec[] BUILDINGS={
        new BuildingSpec("goldmine","Gold Mine","gold",800,12,"gold","mine"),new BuildingSpec("elixircollector","Elixir Pump","gold",850,12,"elixir","collector"),
        new BuildingSpec("goldstorage","Gold Storage","gold",1200,12,"gold","storage"),new BuildingSpec("elixirstorage","Elixir Storage","gold",1250,12,"elixir","storage"),
        new BuildingSpec("barracks","Barracks","elixir",1200,12,"barrack"),new BuildingSpec("armycamp","Army Camp","elixir",1500,10,"army","camp"),
        new BuildingSpec("cannon","Cannon","gold",1100,12,"cannon"),new BuildingSpec("archertower","Archer Tower","gold",1600,12,"archer","tower"),
        new BuildingSpec("mortar","Mortar","gold",2400,10,"mortar"),new BuildingSpec("airdefense","Air Defense","gold",3100,10,"air","defense"),
        new BuildingSpec("wall","Wall","gold",180,15,"wall"),new BuildingSpec("clancastle","Clan Keep","gold",5000,8,"clan","castle")
    };
    static final UnitSpec[] UNITS={
        new UnitSpec("vanguard","Vanguard","elixir",250,12,"barbarian"),new UnitSpec("ranger","Ranger","elixir",350,15,"archer"),
        new UnitSpec("rogue","Rogue","elixir",450,17,"goblin"),new UnitSpec("breaker","Breaker","elixir",600,24,"wall","breaker"),
        new UnitSpec("brute","Brute","elixir",850,30,"giant"),new UnitSpec("mage","Mage","elixir",1100,38,"wizard"),
        new UnitSpec("healer","Healer","elixir",1350,44,"healer"),new UnitSpec("stormcaller","Stormcaller","gems",25,70,"dragon")
    };
    static BuildingSpec building(String id){for(BuildingSpec s:BUILDINGS)if(s.id.equals(id))return s;if("townhall".equals(id))return new BuildingSpec("townhall","Drayven Hall","gold",0,12,"town","hall");return null;}
    static UnitSpec unit(String id){for(UnitSpec s:UNITS)if(s.id.equals(id))return s;return null;}
    static final class BuildingSpec{final String id,name,currency;final int cost,max;final String[]art;BuildingSpec(String i,String n,String c,int co,int m,String...a){id=i;name=n;currency=c;cost=co;max=m;art=a;}}
    static final class UnitSpec{final String id,name,currency;final int cost,power;final String[]art;UnitSpec(String i,String n,String c,int co,int p,String...a){id=i;name=n;currency=c;cost=co;power=p;art=a;}}
}
