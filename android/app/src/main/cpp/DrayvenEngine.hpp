#pragma once
#include <cstdint>
#include <string>

namespace drayven {
struct SpendResult { bool ok; int gold; int elixir; int gems; };
struct XpResult { int xp; int level; int gems; };
struct ProductionResult { int gold; int elixir; };
struct BattleReward { int gold; int elixir; };
struct UnitCombat { float speed; float range; float damage; float delay; };
struct BuildingCombat { float range; float damage; float delay; };

class Engine final {
public:
    static const char* name() noexcept;
    static const char* version() noexcept;
    static std::string serverBase();
    static std::uint32_t guard(std::uint32_t value) noexcept;
    static bool luaReady() noexcept;
    static std::string luaVersion();

    static std::string catalogJson();
    static SpendResult spend(const std::string& currency,int gold,int elixir,int gems,int amount) noexcept;
    static XpResult gainXp(int xp,int level,int gems,int amount) noexcept;
    static ProductionResult production(const std::string* ids,const int* levels,int count) noexcept;
    static UnitCombat unitCombat(const std::string& id) noexcept;
    static BuildingCombat buildingCombat(const std::string& id,int level) noexcept;
    static float enemyHp(const std::string& id,int playerLevel) noexcept;
    static int battleStars(int destruction,bool townHallDown) noexcept;
    static BattleReward battleReward(int playerLevel,int destruction,int stars) noexcept;
    static BattleReward lootPreview(int playerLevel) noexcept;
    static int upgradeCost(const std::string& id,int level) noexcept;
    static std::string rankName(int points);
    static std::string loadingTip(int seed);
    static int loadingProgress(int phase) noexcept;
};
}
