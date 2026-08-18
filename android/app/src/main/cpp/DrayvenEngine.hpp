#pragma once
#include <cstdint>
#include <string>

namespace drayven {
class Engine final {
public:
    static const char* name() noexcept;
    static const char* version() noexcept;
    static std::string serverBase();
    static std::uint32_t guard(std::uint32_t value) noexcept;
};
}
