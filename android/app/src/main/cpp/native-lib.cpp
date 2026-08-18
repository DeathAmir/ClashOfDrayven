#include <jni.h>
#include <string>
#include <vector>
#include "DrayvenEngine.hpp"

namespace {
std::string str(JNIEnv* env,jstring value){if(!value)return{};const char*p=env->GetStringUTFChars(value,nullptr);std::string out=p?p:"";if(p)env->ReleaseStringUTFChars(value,p);return out;}
jintArray ints(JNIEnv* env,std::initializer_list<jint> values){jintArray out=env->NewIntArray(static_cast<jsize>(values.size()));if(!out)return nullptr;std::vector<jint>v(values);env->SetIntArrayRegion(out,0,static_cast<jsize>(v.size()),v.data());return out;}
jfloatArray floats(JNIEnv* env,std::initializer_list<jfloat> values){jfloatArray out=env->NewFloatArray(static_cast<jsize>(values.size()));if(!out)return nullptr;std::vector<jfloat>v(values);env->SetFloatArrayRegion(out,0,static_cast<jsize>(v.size()),v.data());return out;}
}

extern "C" JNIEXPORT jstring JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_version(JNIEnv*e,jclass){return e->NewStringUTF(drayven::Engine::version());}
extern "C" JNIEXPORT jint JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_guard(JNIEnv*,jclass,jint v){return static_cast<jint>(drayven::Engine::guard(static_cast<unsigned int>(v)));}
extern "C" JNIEXPORT jstring JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_serverBase(JNIEnv*e,jclass){auto v=drayven::Engine::serverBase();return e->NewStringUTF(v.c_str());}
extern "C" JNIEXPORT jstring JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_engineName(JNIEnv*e,jclass){return e->NewStringUTF(drayven::Engine::name());}
extern "C" JNIEXPORT jboolean JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_luaReady(JNIEnv*,jclass){return drayven::Engine::luaReady()?JNI_TRUE:JNI_FALSE;}
extern "C" JNIEXPORT jstring JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_luaVersion(JNIEnv*e,jclass){auto v=drayven::Engine::luaVersion();return e->NewStringUTF(v.c_str());}
extern "C" JNIEXPORT jstring JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_catalogJson(JNIEnv*e,jclass){auto v=drayven::Engine::catalogJson();return e->NewStringUTF(v.c_str());}
extern "C" JNIEXPORT jintArray JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_spend(JNIEnv*e,jclass,jstring c,jint g,jint x,jint m,jint a){auto r=drayven::Engine::spend(str(e,c),g,x,m,a);return ints(e,{r.ok?1:0,r.gold,r.elixir,r.gems});}
extern "C" JNIEXPORT jintArray JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_gainXp(JNIEnv*e,jclass,jint xp,jint l,jint g,jint a){auto r=drayven::Engine::gainXp(xp,l,g,a);return ints(e,{r.xp,r.level,r.gems});}
extern "C" JNIEXPORT jintArray JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_production(JNIEnv*e,jclass,jobjectArray ids,jintArray levels){if(!ids||!levels)return ints(e,{0,0});jsize n=e->GetArrayLength(ids);if(e->GetArrayLength(levels)!=n)return ints(e,{0,0});std::vector<std::string>names;names.reserve(n);std::vector<int>lv(static_cast<std::size_t>(n));jint*raw=e->GetIntArrayElements(levels,nullptr);for(jsize i=0;i<n;++i){auto s=static_cast<jstring>(e->GetObjectArrayElement(ids,i));names.push_back(str(e,s));e->DeleteLocalRef(s);lv[static_cast<std::size_t>(i)]=raw?raw[i]:1;}if(raw)e->ReleaseIntArrayElements(levels,raw,JNI_ABORT);auto r=drayven::Engine::production(names.data(),lv.data(),static_cast<int>(n));return ints(e,{r.gold,r.elixir});}
extern "C" JNIEXPORT jfloatArray JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_unitCombat(JNIEnv*e,jclass,jstring id){auto r=drayven::Engine::unitCombat(str(e,id));return floats(e,{r.speed,r.range,r.damage,r.delay});}
extern "C" JNIEXPORT jfloatArray JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_buildingCombat(JNIEnv*e,jclass,jstring id,jint level){auto r=drayven::Engine::buildingCombat(str(e,id),level);return floats(e,{r.range,r.damage,r.delay});}
extern "C" JNIEXPORT jfloat JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_enemyHp(JNIEnv*e,jclass,jstring id,jint level){return drayven::Engine::enemyHp(str(e,id),level);}
extern "C" JNIEXPORT jint JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_battleStars(JNIEnv*,jclass,jint d,jboolean th){return drayven::Engine::battleStars(d,th==JNI_TRUE);}
extern "C" JNIEXPORT jintArray JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_battleReward(JNIEnv*e,jclass,jint l,jint d,jint s){auto r=drayven::Engine::battleReward(l,d,s);return ints(e,{r.gold,r.elixir});}
extern "C" JNIEXPORT jintArray JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_lootPreview(JNIEnv*e,jclass,jint l){auto r=drayven::Engine::lootPreview(l);return ints(e,{r.gold,r.elixir});}
extern "C" JNIEXPORT jint JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_upgradeCost(JNIEnv*e,jclass,jstring id,jint level){return drayven::Engine::upgradeCost(str(e,id),level);}
extern "C" JNIEXPORT jstring JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_rankName(JNIEnv*e,jclass,jint points){auto r=drayven::Engine::rankName(points);return e->NewStringUTF(r.c_str());}
extern "C" JNIEXPORT jstring JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_loadingTip(JNIEnv*e,jclass,jint seed){auto r=drayven::Engine::loadingTip(seed);return e->NewStringUTF(r.c_str());}
extern "C" JNIEXPORT jint JNICALL Java_ir_irautox_clashofdrayven_NativeBridge_loadingProgress(JNIEnv*,jclass,jint phase){return drayven::Engine::loadingProgress(phase);}
