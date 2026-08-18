#include <jni.h>
#include "DrayvenEngine.hpp"

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
