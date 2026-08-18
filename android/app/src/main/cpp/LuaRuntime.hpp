#pragma once
#include <string>

namespace drayven {
struct LuaProduction { int gold; int elixir; };
struct LuaXp { int xp; int level; int gems; };
struct LuaCombat { float a; float b; float c; float d; };
struct LuaDefense { float range; float damage; float delay; };
struct LuaReward { int gold; int elixir; };

class LuaRuntime final {
public:
    static LuaRuntime& instance();
    bool ready() noexcept;
    std::string version();
    LuaProduction production(const std::string* ids,const int* levels,int count);
    LuaXp gainXp(int xp,int level,int gems,int amount);
    LuaCombat unitCombat(const std::string& id);
    LuaDefense buildingCombat(const std::string& id,int level);
    int battleStars(int destruction,bool townHallDown);
    LuaReward lootPreview(int level);
    LuaReward battleReward(int level,int destruction,int stars);
    int upgradeCost(const std::string& id,int level);
    std::string rankName(int points);
    std::string loadingTip(int seed);
    int loadingProgress(int phase);
private:
    LuaRuntime();
    ~LuaRuntime();
    LuaRuntime(const LuaRuntime&)=delete;
    LuaRuntime& operator=(const LuaRuntime&)=delete;
    struct Impl;
    Impl* impl_;
};
}
