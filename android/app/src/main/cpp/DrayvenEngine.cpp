#include "DrayvenEngine.hpp"
#include <algorithm>
#include <array>
#include <cmath>
#include <iterator>
#include <sstream>

namespace {
static std::uint32_t mix(std::uint32_t x) noexcept {
    x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16; return x;
}

static std::string decode(const std::uint8_t* p, std::size_t n, std::uint8_t key) {
    std::string s; s.resize(n);
    for (std::size_t i=0;i<n;i++) s[i]=static_cast<char>(p[i]^static_cast<std::uint8_t>(key+i*13U));
    return s;
}

struct BuildingDef { const char* id; const char* name; const char* currency; int cost; int max; const char* art1; const char* art2; };
struct UnitDef { const char* id; const char* name; const char* currency; int cost; int power; const char* art1; const char* art2; float speed; float range; float damage; float delay; };

static constexpr BuildingDef kBuildings[] = {
    {"goldmine","معدن طلا","gold",800,12,"gold","mine"},
    {"elixircollector","پمپ اکسیر","gold",850,12,"elixir","collector"},
    {"goldstorage","مخزن طلا","gold",1200,12,"gold","storage"},
    {"elixirstorage","مخزن اکسیر","gold",1250,12,"elixir","storage"},
    {"barracks","سربازخانه","elixir",1200,12,"barrack",""},
    {"armycamp","کمپ ارتش","elixir",1500,10,"army","camp"},
    {"cannon","توپ","gold",1100,12,"cannon",""},
    {"archertower","برج کماندار","gold",1600,12,"archer","tower"},
    {"mortar","خمپاره‌انداز","gold",2400,10,"mortar",""},
    {"airdefense","دفاع هوایی","gold",3100,10,"air","defense"},
    {"wall","دیوار","gold",180,15,"wall",""},
    {"clancastle","قلعه کلن","gold",5000,8,"clan","castle"}
};

static constexpr UnitDef kUnits[] = {
    {"vanguard","پیشتاز","elixir",250,12,"barbarian","",.13f,.052f,19.8f,.72f},
    {"ranger","تیرانداز","elixir",350,15,"archer","",.13f,.115f,24.75f,.72f},
    {"rogue","یاغی","elixir",450,17,"goblin","",.22f,.052f,28.05f,.45f},
    {"breaker","دیوارشکن","elixir",600,24,"wall","breaker",.13f,.052f,39.6f,.72f},
    {"brute","غول","elixir",850,30,"giant","",.09f,.052f,49.5f,1.15f},
    {"mage","جادوگر","elixir",1100,38,"wizard","",.13f,.115f,62.7f,.72f},
    {"healer","شفادهنده","elixir",1350,44,"healer","",.13f,.115f,72.6f,.72f},
    {"stormcaller","طوفان‌خوان","gems",25,70,"dragon","",.16f,.115f,115.5f,.72f}
};

static const UnitDef* unit(const std::string& id) noexcept {
    for (const auto& u : kUnits) if (id == u.id) return &u;
    return nullptr;
}

static std::string esc(const char* s) {
    std::string out;
    for (; *s; ++s) {
        if (*s=='"' || *s=='\\') out.push_back('\\');
        out.push_back(*s);
    }
    return out;
}
}

namespace drayven {
const char* Engine::name() noexcept { return "DrayvenEngine"; }
const char* Engine::version() noexcept { return "DrayvenEngine/0.8-android-native-core"; }

std::string Engine::serverBase() {
    static constexpr std::array<std::uint8_t,22> k = {
        0x32,0x2f,0x2e,0x18,0x05,0x36,0x0d,0x54,0x27,0x2a,0x12,
        0x17,0x0b,0x1b,0x63,0x4e,0x1e,0x54,0x0c,0x19,0x1c,0x0f
    };
    auto s = decode(k.data(), k.size(), 0x5a);
    if (s.rfind("http",0)==0) return s;
    const char a[]={'h','t','t','p',':','/','/'};
    const char b[]={'i','r','a','u','t','o','x','.','i','r'};
    const char c[]={':','8','4','5','6'};
    return std::string(a,sizeof(a))+std::string(b,sizeof(b))+std::string(c,sizeof(c));
}

std::uint32_t Engine::guard(std::uint32_t value) noexcept {
    return mix(value ^ 0x44525956U ^ 0x49524158U);
}

std::string Engine::catalogJson() {
    std::ostringstream o;
    o << "{\"buildings\":[";
    for (std::size_t i=0;i<std::size(kBuildings);++i) {
        const auto& b=kBuildings[i]; if(i)o<<',';
        o << "{\"id\":\""<<esc(b.id)<<"\",\"name\":\""<<esc(b.name)<<"\",\"currency\":\""<<esc(b.currency)
          <<"\",\"cost\":"<<b.cost<<",\"max\":"<<b.max<<",\"art\":[\""<<esc(b.art1)<<"\"";
        if(b.art2 && *b.art2)o<<",\""<<esc(b.art2)<<"\"";
        o << "]}";
    }
    o << "],\"units\":[";
    for (std::size_t i=0;i<std::size(kUnits);++i) {
        const auto& u=kUnits[i]; if(i)o<<',';
        o << "{\"id\":\""<<esc(u.id)<<"\",\"name\":\""<<esc(u.name)<<"\",\"currency\":\""<<esc(u.currency)
          <<"\",\"cost\":"<<u.cost<<",\"power\":"<<u.power<<",\"art\":[\""<<esc(u.art1)<<"\"";
        if(u.art2 && *u.art2)o<<",\""<<esc(u.art2)<<"\"";
        o << "]}";
    }
    o << "]}";
    return o.str();
}

SpendResult Engine::spend(const std::string& currency, int gold, int elixir, int gems, int amount) noexcept {
    if (amount <= 0) return {true,gold,elixir,gems};
    if (currency=="gold" && gold>=amount) return {true,gold-amount,elixir,gems};
    if (currency=="elixir" && elixir>=amount) return {true,gold,elixir-amount,gems};
    if (currency=="gems" && gems>=amount) return {true,gold,elixir,gems-amount};
    return {false,gold,elixir,gems};
}

XpResult Engine::gainXp(int xp, int level, int gems, int amount) noexcept {
    xp += std::max(0, amount); level = std::max(1, level);
    while (xp >= level * 220) { xp -= level * 220; ++level; gems += 10; }
    return {xp,level,gems};
}

ProductionResult Engine::production(const std::string* ids, const int* levels, int count) noexcept {
    int gold=0,elixir=0;
    for(int i=0;i<count;++i){
        const int level=std::max(1,levels[i]);
        if(ids[i]=="goldmine")gold+=2+level*2;
        else if(ids[i]=="elixircollector")elixir+=2+level*2;
    }
    return {gold,elixir};
}

UnitCombat Engine::unitCombat(const std::string& id) noexcept {
    if (const auto* u=unit(id)) return {u->speed,u->range,u->damage,u->delay};
    return {.13f,.052f,18.f,.72f};
}

float Engine::enemyHp(const std::string& id, int playerLevel) noexcept {
    const int level=std::max(1,playerLevel);
    if(id=="townhall")return 950.f+level*45.f;
    if(id=="mortar"||id=="clancastle")return 620.f+level*30.f;
    if(id=="cannon"||id=="archertower")return 470.f+level*24.f;
    return 350.f+level*18.f;
}

int Engine::battleStars(int destruction, bool townHallDown) noexcept {
    destruction=std::clamp(destruction,0,100); int stars=0;
    if(destruction>=50)++stars; if(townHallDown)++stars; if(destruction>=100)++stars;
    return std::min(3,stars);
}

BattleReward Engine::lootPreview(int playerLevel) noexcept {
    const int level=std::max(1,playerLevel);
    return {1200+level*130,1000+level*110};
}

BattleReward Engine::battleReward(int playerLevel, int destruction, int stars) noexcept {
    const auto base=lootPreview(playerLevel);
    destruction=std::clamp(destruction,0,100); stars=std::clamp(stars,0,3);
    return {static_cast<int>(base.gold*(destruction/100.f))+stars*180,
            static_cast<int>(base.elixir*(destruction/100.f))+stars*150};
}
}
