#include "LuaRuntime.hpp"
#include "LuaGameplay.generated.hpp"
#include "lua.hpp"
#include "luajit.h"
#include <algorithm>
#include <cstdint>
#include <mutex>
#include <new>

namespace {
static bool fn(lua_State* L,const char* name){
    lua_settop(L,0);lua_getglobal(L,name);return lua_isfunction(L,-1)!=0;
}
static int as_int(lua_State* L,int idx,int fallback=0){return lua_isnumber(L,idx)?static_cast<int>(lua_tointeger(L,idx)):fallback;}
static float as_float(lua_State* L,int idx,float fallback=0.f){return lua_isnumber(L,idx)?static_cast<float>(lua_tonumber(L,idx)):fallback;}
static std::string as_string(lua_State* L,int idx,const char* fallback=""){
    const char* s=lua_tostring(L,idx);return s?s:fallback;
}
}

namespace drayven {
struct LuaRuntime::Impl { lua_State* L=nullptr; bool ok=false; std::mutex lock; };

LuaRuntime& LuaRuntime::instance(){static LuaRuntime r;return r;}

LuaRuntime::LuaRuntime():impl_(new(std::nothrow) Impl()){
    if(!impl_)return;
    impl_->L=luaL_newstate();if(!impl_->L)return;
    luaL_openlibs(impl_->L);
    luaJIT_setmode(impl_->L,0,LUAJIT_MODE_ENGINE|LUAJIT_MODE_ON);
    const std::uint8_t* data=nullptr;std::size_t size=0;
    if(sizeof(void*)==8){data=lua_bytecode::gameplay64;size=lua_bytecode::gameplay64_size;}
    else{data=lua_bytecode::gameplay32;size=lua_bytecode::gameplay32_size;}
    if(luaL_loadbuffer(impl_->L,reinterpret_cast<const char*>(data),size,"@drayven/gameplay.luac")!=0){lua_settop(impl_->L,0);return;}
    if(lua_pcall(impl_->L,0,1,0)!=0){lua_settop(impl_->L,0);return;}
    lua_settop(impl_->L,0);impl_->ok=true;
}
LuaRuntime::~LuaRuntime(){if(impl_){if(impl_->L)lua_close(impl_->L);delete impl_;}}
bool LuaRuntime::ready() noexcept{return impl_&&impl_->ok&&impl_->L;}
std::string LuaRuntime::version(){return ready()?std::string(LUAJIT_VERSION)+"/bytecode":"LuaJIT unavailable";}

LuaProduction LuaRuntime::production(const std::string* ids,const int* levels,int count){
    LuaProduction out{0,0};if(!ready()||!ids||!levels||count<=0)return out;std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;
    if(!fn(L,"drayven_production"))return out;
    lua_createtable(L,count,0);for(int i=0;i<count;i++){lua_pushlstring(L,ids[i].data(),ids[i].size());lua_rawseti(L,-2,i+1);} 
    lua_createtable(L,count,0);for(int i=0;i<count;i++){lua_pushinteger(L,levels[i]);lua_rawseti(L,-2,i+1);} 
    if(lua_pcall(L,2,2,0)==0){out.gold=as_int(L,-2);out.elixir=as_int(L,-1);}lua_settop(L,0);return out;
}
LuaXp LuaRuntime::gainXp(int xp,int level,int gems,int amount){
    LuaXp out{xp,level,gems};if(!ready())return out;std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_gain_xp"))return out;
    lua_pushinteger(L,xp);lua_pushinteger(L,level);lua_pushinteger(L,gems);lua_pushinteger(L,amount);
    if(lua_pcall(L,4,3,0)==0){out.xp=as_int(L,-3,xp);out.level=as_int(L,-2,level);out.gems=as_int(L,-1,gems);}lua_settop(L,0);return out;
}
LuaCombat LuaRuntime::unitCombat(const std::string& id){
    LuaCombat out{.13f,.052f,18.f,.72f};if(!ready())return out;std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_unit_combat"))return out;
    lua_pushlstring(L,id.data(),id.size());if(lua_pcall(L,1,4,0)==0){out.a=as_float(L,-4,out.a);out.b=as_float(L,-3,out.b);out.c=as_float(L,-2,out.c);out.d=as_float(L,-1,out.d);}lua_settop(L,0);return out;
}
LuaDefense LuaRuntime::buildingCombat(const std::string& id,int level){
    LuaDefense out{0,0,0};if(!ready())return out;std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_building_combat"))return out;
    lua_pushlstring(L,id.data(),id.size());lua_pushinteger(L,level);if(lua_pcall(L,2,3,0)==0){out.range=as_float(L,-3);out.damage=as_float(L,-2);out.delay=as_float(L,-1);}lua_settop(L,0);return out;
}
int LuaRuntime::battleStars(int destruction,bool townHallDown){
    if(!ready())return 0;std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_battle_stars"))return 0;
    lua_pushinteger(L,destruction);lua_pushboolean(L,townHallDown?1:0);int out=0;if(lua_pcall(L,2,1,0)==0)out=as_int(L,-1);lua_settop(L,0);return out;
}
LuaReward LuaRuntime::lootPreview(int level){
    LuaReward out{0,0};if(!ready())return out;std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_loot_preview"))return out;
    lua_pushinteger(L,level);if(lua_pcall(L,1,2,0)==0){out.gold=as_int(L,-2);out.elixir=as_int(L,-1);}lua_settop(L,0);return out;
}
LuaReward LuaRuntime::battleReward(int level,int destruction,int stars){
    LuaReward out{0,0};if(!ready())return out;std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_battle_reward"))return out;
    lua_pushinteger(L,level);lua_pushinteger(L,destruction);lua_pushinteger(L,stars);if(lua_pcall(L,3,2,0)==0){out.gold=as_int(L,-2);out.elixir=as_int(L,-1);}lua_settop(L,0);return out;
}
int LuaRuntime::upgradeCost(const std::string& id,int level){
    if(!ready())return -1;std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_upgrade_cost"))return -1;
    lua_pushlstring(L,id.data(),id.size());lua_pushinteger(L,level);int out=-1;if(lua_pcall(L,2,1,0)==0)out=as_int(L,-1,-1);lua_settop(L,0);return out;
}
std::string LuaRuntime::rankName(int points){
    if(!ready())return "برنز";std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_rank_name"))return "برنز";
    lua_pushinteger(L,points);std::string out="برنز";if(lua_pcall(L,1,1,0)==0)out=as_string(L,-1,"برنز");lua_settop(L,0);return out;
}
std::string LuaRuntime::loadingTip(int seed){
    if(!ready())return "در حال آماده‌سازی قلمرو درایون";std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_loading_tip"))return "در حال آماده‌سازی قلمرو درایون";
    lua_pushinteger(L,seed);std::string out="در حال آماده‌سازی قلمرو درایون";if(lua_pcall(L,1,1,0)==0)out=as_string(L,-1,out.c_str());lua_settop(L,0);return out;
}
int LuaRuntime::loadingProgress(int phase){
    if(!ready())return std::clamp(phase*8,0,100);std::lock_guard<std::mutex> g(impl_->lock);auto* L=impl_->L;if(!fn(L,"drayven_loading_progress"))return std::clamp(phase*8,0,100);
    lua_pushinteger(L,phase);int out=0;if(lua_pcall(L,1,1,0)==0)out=as_int(L,-1);lua_settop(L,0);return std::clamp(out,0,100);
}
}
