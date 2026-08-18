package ir.irautox.clashofdrayven;

final class GameCatalog {
    static final BuildingSpec[] BUILDINGS={
        new BuildingSpec("goldmine","معدن طلا","gold",800,12,"gold","mine"),new BuildingSpec("elixircollector","پمپ اکسیر","gold",850,12,"elixir","collector"),
        new BuildingSpec("goldstorage","مخزن طلا","gold",1200,12,"gold","storage"),new BuildingSpec("elixirstorage","مخزن اکسیر","gold",1250,12,"elixir","storage"),
        new BuildingSpec("barracks","سربازخانه","elixir",1200,12,"barrack"),new BuildingSpec("armycamp","کمپ ارتش","elixir",1500,10,"army","camp"),
        new BuildingSpec("cannon","توپ","gold",1100,12,"cannon"),new BuildingSpec("archertower","برج کماندار","gold",1600,12,"archer","tower"),
        new BuildingSpec("mortar","خمپاره‌انداز","gold",2400,10,"mortar"),new BuildingSpec("airdefense","دفاع هوایی","gold",3100,10,"air","defense"),
        new BuildingSpec("wall","دیوار","gold",180,15,"wall"),new BuildingSpec("clancastle","قلعه کلن","gold",5000,8,"clan","castle")
    };
    static final UnitSpec[] UNITS={
        new UnitSpec("vanguard","پیشتاز","elixir",250,12,"barbarian"),new UnitSpec("ranger","تیرانداز","elixir",350,15,"archer"),
        new UnitSpec("rogue","یاغی","elixir",450,17,"goblin"),new UnitSpec("breaker","دیوارشکن","elixir",600,24,"wall","breaker"),
        new UnitSpec("brute","غول","elixir",850,30,"giant"),new UnitSpec("mage","جادوگر","elixir",1100,38,"wizard"),
        new UnitSpec("healer","شفادهنده","elixir",1350,44,"healer"),new UnitSpec("stormcaller","طوفان‌خوان","gems",25,70,"dragon")
    };
    static BuildingSpec building(String id){for(BuildingSpec s:BUILDINGS)if(s.id.equals(id))return s;if("townhall".equals(id))return new BuildingSpec("townhall","تالار درایون","gold",0,12,"town","hall");return null;}
    static UnitSpec unit(String id){for(UnitSpec s:UNITS)if(s.id.equals(id))return s;return null;}
    static String currencyName(String id){if("gold".equals(id))return"طلا";if("elixir".equals(id))return"اکسیر";if("gems".equals(id))return"جم";return id;}
    static final class BuildingSpec{final String id,name,currency;final int cost,max;final String[]art;BuildingSpec(String i,String n,String c,int co,int m,String...a){id=i;name=n;currency=c;cost=co;max=m;art=a;}}
    static final class UnitSpec{final String id,name,currency;final int cost,power;final String[]art;UnitSpec(String i,String n,String c,int co,int p,String...a){id=i;name=n;currency=c;cost=co;power=p;art=a;}}
}
