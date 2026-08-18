#include "DrayvenEngine.hpp"
#include "LuaRuntime.hpp"
#include <algorithm>
#include <array>
#include <cmath>
#include <iterator>
#include <sstream>

namespace {
static std::uint32_t mix(std::uint32_t x) noexcept {x^=x>>16;x*=0x7feb352dU;x^=x>>15;x*=0x846ca68bU;x^=x>>16;return x;}
static std::string decode(const std::uint8_t* p,std::size_t n,std::uint8_t key){std::string s;s.resize(n);for(std::size_t i=0;i<n;i++)s[i]=static_cast<char>(p[i]^static_cast<std::uint8_t>(key+i*13U));return s;}
struct BuildingDef{const char*id;const char*name;const char*currency;int cost;int max;const char*art1;const char*art2;};
struct UnitDef{const char*id;const char*name;const char*currency;int cost;int power;const char*art1;const char*art2;};
static constexpr BuildingDef kBuildings[]={
 {"goldmine","معدن طلا","gold",800,12,"gold","mine"},{"elixircollector","پمپ اکسیر","gold",850,12,"elixir","collector"},
 {"goldstorage","مخزن طلا","gold",1200,12,"gold","storage"},{"elixirstorage","مخزن اکسیر","gold",1250,12,"elixir","storage"},
 {"barracks","سربازخانه","elixir",1200,12,"barrack",""},{"armycamp","کمپ ارتش","elixir",1500,10,"army","camp"},
 {"cannon","توپ","gold",1100,12,"cannon",""},{"archertower","برج کماندار","gold",1600,12,"archer","tower"},
 {"mortar","خمپاره‌انداز","gold",2400,10,"mortar",""},{"airdefense","دفاع هوایی","gold",3100,10,"air","defense"},
 {"wall","دیوار","gold",180,15,"wall",""},{"clancastle","قلعه کلن","gold",5000,8,"clan","castle"}
};
static constexpr UnitDef kUnits[]={
 {"vanguard","پیشتاز","elixir",250,12,"soldier","front"},{"ranger","تیرانداز","elixir",350,15,"officer","front"},
 {"rogue","یاغی","elixir",450,17,"soldier","left"},{"breaker","دیوارشکن","elixir",600,24,"soldier","right"},
 {"brute","غول","elixir",850,30,"officer","back"},{"mage","جادوگر","elixir",1100,38,"officer","left"},
 {"healer","شفادهنده","elixir",1350,44,"officer","right"},{"stormcaller","طوفان‌خوان","elixir",4200,70,"officer","front"}
};
static std::string esc(const char*s){std::string out;for(;*s;++s){if(*s=='"'||*s=='\\')out.push_back('\\');out.push_back(*s);}return out;}
}

namespace drayven {
const char* Engine::name() noexcept{return "DrayvenEngine";}
const char* Engine::version() noexcept{return "DrayvenEngine/1.0-LuaJIT-native";}
std::string Engine::serverBase(){
    const char a[]={'h','t','t','p','s',':','/','/'};const char b[]={'i','r','a','u','t','o','x','.','i','r'};const char c[]={':','8','4','5','6'};
    return std::string(a,sizeof(a))+std::string(b,sizeof(b))+std::string(c,sizeof(c));
}
std::uint32_t Engine::guard(std::uint32_t value) noexcept{return mix(value^0x44525956U^0x49524158U^0x4c4a4954U);}
bool Engine::luaReady() noexcept{return LuaRuntime::instance().ready();}
std::string Engine::luaVersion(){return LuaRuntime::instance().version();}

std::string Engine::catalogJson(){
    std::ostringstream o;o<<"{\"buildings\":[";
    for(std::size_t i=0;i<std::size(kBuildings);++i){const auto&b=kBuildings[i];if(i)o<<',';o<<"{\"id\":\""<<esc(b.id)<<"\",\"name\":\""<<esc(b.name)<<"\",\"currency\":\""<<esc(b.currency)<<"\",\"cost\":"<<b.cost<<",\"max\":"<<b.max<<",\"art\":[\""<<esc(b.art1)<<"\"";if(b.art2&&*b.art2)o<<",\""<<esc(b.art2)<<"\"";o<<"]}";}
    o<<"],\"units\":[";
    for(std::size_t i=0;i<std::size(kUnits);++i){const auto&u=kUnits[i];if(i)o<<',';o<<"{\"id\":\""<<esc(u.id)<<"\",\"name\":\""<<esc(u.name)<<"\",\"currency\":\""<<esc(u.currency)<<"\",\"cost\":"<<u.cost<<",\"power\":"<<u.power<<",\"art\":[\""<<esc(u.art1)<<"\",\""<<esc(u.art2)<<"\"]}";}
    o<<"]}";return o.str();
}

SpendResult Engine::spend(const std::string&currency,int gold,int elixir,int gems,int amount) noexcept{
    if(amount<=0)return{true,gold,elixir,gems};if(currency=="gold"&&gold>=amount)return{true,gold-amount,elixir,gems};if(currency=="elixir"&&elixir>=amount)return{true,gold,elixir-amount,gems};if(currency=="gems"&&gems>=amount)return{true,gold,elixir,gems-amount};return{false,gold,elixir,gems};
}
XpResult Engine::gainXp(int xp,int level,int gems,int amount) noexcept{
    if(LuaRuntime::instance().ready()){auto r=LuaRuntime::instance().gainXp(xp,level,gems,amount);return{r.xp,r.level,r.gems};}
    xp+=std::max(0,amount);level=std::max(1,level);while(xp>=level*220){xp-=level*220;++level;gems+=10;}return{xp,level,gems};
}
ProductionResult Engine::production(const std::string*ids,const int*levels,int count) noexcept{
    if(LuaRuntime::instance().ready()){auto r=LuaRuntime::instance().production(ids,levels,count);return{r.gold,r.elixir};}
    int gold=0,elixir=0;for(int i=0;i<count;++i){int l=std::max(1,levels[i]);if(ids[i]=="goldmine")gold+=2+l*2;else if(ids[i]=="elixircollector")elixir+=2+l*2;}return{gold,elixir};
}
UnitCombat Engine::unitCombat(const std::string&id) noexcept{auto r=LuaRuntime::instance().unitCombat(id);return{r.a,r.b,r.c,r.d};}
BuildingCombat Engine::buildingCombat(const std::string&id,int level) noexcept{auto r=LuaRuntime::instance().buildingCombat(id,level);return{r.range,r.damage,r.delay};}
float Engine::enemyHp(const std::string&id,int playerLevel) noexcept{int level=std::max(1,playerLevel);if(id=="townhall")return 950.f+level*45.f;if(id=="mortar"||id=="clancastle")return 620.f+level*30.f;if(id=="cannon"||id=="archertower"||id=="airdefense")return 470.f+level*24.f;return 350.f+level*18.f;}
int Engine::battleStars(int destruction,bool townHallDown) noexcept{if(LuaRuntime::instance().ready())return LuaRuntime::instance().battleStars(destruction,townHallDown);destruction=std::clamp(destruction,0,100);int s=0;if(destruction>=50)++s;if(townHallDown)++s;if(destruction>=100)++s;return std::min(3,s);}
BattleReward Engine::lootPreview(int playerLevel) noexcept{if(LuaRuntime::instance().ready()){auto r=LuaRuntime::instance().lootPreview(playerLevel);return{r.gold,r.elixir};}int l=std::max(1,playerLevel);return{1200+l*130,1000+l*110};}
BattleReward Engine::battleReward(int playerLevel,int destruction,int stars) noexcept{if(LuaRuntime::instance().ready()){auto r=LuaRuntime::instance().battleReward(playerLevel,destruction,stars);return{r.gold,r.elixir};}auto b=lootPreview(playerLevel);destruction=std::clamp(destruction,0,100);stars=std::clamp(stars,0,3);return{static_cast<int>(b.gold*(destruction/100.f))+stars*180,static_cast<int>(b.elixir*(destruction/100.f))+stars*150};}
int Engine::upgradeCost(const std::string&id,int level) noexcept{return LuaRuntime::instance().upgradeCost(id,level);}
std::string Engine::rankName(int points){return LuaRuntime::instance().rankName(points);}
std::string Engine::loadingTip(int seed){return LuaRuntime::instance().loadingTip(seed);}
int Engine::loadingProgress(int phase) noexcept{return LuaRuntime::instance().loadingProgress(phase);}
}
