#include <jni.h>
#include <string>
#include <vector>
#include "DrayvenEngine.hpp"

namespace {
std::string str(JNIEnv* env, jstring value) {
    if (!value) return {};
    const char* p=env->GetStringUTFChars(value,nullptr);
    std::string out=p?p:"";
    if(p)env->ReleaseStringUTFChars(value,p);
    return out;
}

jintArray ints(JNIEnv* env, std::initializer_list<jint> values) {
    jintArray out=env->NewIntArray(static_cast<jsize>(values.size()));
    if(!out)return nullptr;
    std::vector<jint> v(values);
    env->SetIntArrayRegion(out,0,static_cast<jsize>(v.size()),v.data());
    return out;
}

jfloatArray floats(JNIEnv* env, std::initializer_list<jfloat> values) {
    jfloatArray out=env->NewFloatArray(static_cast<jsize>(values.size()));
    if(!out)return nullptr;
    std::vector<jfloat> v(values);
    env->SetFloatArrayRegion(out,0,static_cast<jsize>(v.size()),v.data());
    return out;
}
}

extern "C" JNIEXPORT jstring JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_version(JNIEnv* env, jclass) {
    return env->NewStringUTF(drayven::Engine::version());
}

extern "C" JNIEXPORT jint JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_guard(JNIEnv*, jclass, jint value) {
    return static_cast<jint>(drayven::Engine::guard(static_cast<unsigned int>(value)));
}

extern "C" JNIEXPORT jstring JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_serverBase(JNIEnv* env, jclass) {
    const auto value=drayven::Engine::serverBase();
    return env->NewStringUTF(value.c_str());
}

extern "C" JNIEXPORT jstring JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_engineName(JNIEnv* env, jclass) {
    return env->NewStringUTF(drayven::Engine::name());
}

extern "C" JNIEXPORT jstring JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_catalogJson(JNIEnv* env, jclass) {
    const auto value=drayven::Engine::catalogJson();
    return env->NewStringUTF(value.c_str());
}

extern "C" JNIEXPORT jintArray JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_spend(JNIEnv* env,jclass,jstring currency,jint gold,jint elixir,jint gems,jint amount){
    const auto r=drayven::Engine::spend(str(env,currency),gold,elixir,gems,amount);
    return ints(env,{r.ok?1:0,r.gold,r.elixir,r.gems});
}

extern "C" JNIEXPORT jintArray JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_gainXp(JNIEnv* env,jclass,jint xp,jint level,jint gems,jint amount){
    const auto r=drayven::Engine::gainXp(xp,level,gems,amount);
    return ints(env,{r.xp,r.level,r.gems});
}

extern "C" JNIEXPORT jintArray JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_production(JNIEnv* env,jclass,jobjectArray ids,jintArray levels){
    if(!ids||!levels)return ints(env,{0,0});
    const jsize n=env->GetArrayLength(ids);
    if(env->GetArrayLength(levels)!=n)return ints(env,{0,0});
    std::vector<std::string> names; names.reserve(n);
    std::vector<int> lv(static_cast<std::size_t>(n));
    jint* raw=env->GetIntArrayElements(levels,nullptr);
    for(jsize i=0;i<n;++i){
        auto s=static_cast<jstring>(env->GetObjectArrayElement(ids,i));
        names.push_back(str(env,s));
        env->DeleteLocalRef(s);
        lv[static_cast<std::size_t>(i)]=raw?raw[i]:1;
    }
    if(raw)env->ReleaseIntArrayElements(levels,raw,JNI_ABORT);
    const auto r=drayven::Engine::production(names.data(),lv.data(),static_cast<int>(n));
    return ints(env,{r.gold,r.elixir});
}

extern "C" JNIEXPORT jfloatArray JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_unitCombat(JNIEnv* env,jclass,jstring id){
    const auto r=drayven::Engine::unitCombat(str(env,id));
    return floats(env,{r.speed,r.range,r.damage,r.delay});
}

extern "C" JNIEXPORT jfloat JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_enemyHp(JNIEnv* env,jclass,jstring id,jint level){
    return drayven::Engine::enemyHp(str(env,id),level);
}

extern "C" JNIEXPORT jint JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_battleStars(JNIEnv*,jclass,jint destruction,jboolean townHallDown){
    return drayven::Engine::battleStars(destruction,townHallDown==JNI_TRUE);
}

extern "C" JNIEXPORT jintArray JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_battleReward(JNIEnv* env,jclass,jint level,jint destruction,jint stars){
    const auto r=drayven::Engine::battleReward(level,destruction,stars);
    return ints(env,{r.gold,r.elixir});
}

extern "C" JNIEXPORT jintArray JNICALL
Java_ir_irautox_clashofdrayven_NativeBridge_lootPreview(JNIEnv* env,jclass,jint level){
    const auto r=drayven::Engine::lootPreview(level);
    return ints(env,{r.gold,r.elixir});
}
