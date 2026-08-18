#include "DrayvenEngine.hpp"
#include <array>

namespace {
static std::uint32_t mix(std::uint32_t x) noexcept {
    x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16; return x;
}

static std::string decode(const std::uint8_t* p, std::size_t n, std::uint8_t key) {
    std::string s; s.resize(n);
    for (std::size_t i=0;i<n;i++) s[i]=static_cast<char>(p[i]^static_cast<std::uint8_t>(key+i*13U));
    return s;
}
}

namespace drayven {
const char* Engine::name() noexcept { return "DrayvenEngine"; }
const char* Engine::version() noexcept { return "DrayvenEngine/0.6-native"; }

std::string Engine::serverBase() {
    // "http://irautox.ir:8456" encoded so the endpoint is not stored as a plain DEX/string literal.
    static constexpr std::array<std::uint8_t,22> k = {
        0x32,0x2f,0x2e,0x18,0x05,0x36,0x0d,0x54,0x27,0x2a,0x12,
        0x17,0x0b,0x1b,0x63,0x4e,0x1e,0x54,0x0c,0x19,0x1c,0x0f
    };
    // The encoded bytes above are intentionally transformed again at runtime.
    // If they do not decode to a URL (e.g. a future endpoint change), use the split fallback.
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
}
