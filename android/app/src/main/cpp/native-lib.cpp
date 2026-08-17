#include <jni.h>
#include <cstdint>
#include <string>

static uint32_t mix(uint32_t x) {
    x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16; return x;
}

extern "C" JNIEXPORT jstring JNICALL
Java_ir_irautox_clashofdrayven_MainActivity_nativeVersion(JNIEnv* env, jclass) {
    return env->NewStringUTF("libclashofdrayven.so/3.0.1");
}

extern "C" JNIEXPORT jint JNICALL
Java_ir_irautox_clashofdrayven_MainActivity_nativeGuard(JNIEnv*, jclass, jint value) {
    return static_cast<jint>(mix(static_cast<uint32_t>(value) ^ 0x44525956U));
}
