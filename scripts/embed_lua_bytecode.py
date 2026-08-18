#!/usr/bin/env python3
from pathlib import Path
import argparse


def fmt(data: bytes) -> str:
    rows=[]
    for i in range(0,len(data),16):
        rows.append("    "+",".join(f"0x{x:02x}" for x in data[i:i+16])+",")
    return "\n".join(rows)


def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--bytecode32",required=True)
    ap.add_argument("--bytecode64",required=True)
    ap.add_argument("--out",required=True)
    a=ap.parse_args()
    b32=Path(a.bytecode32).read_bytes(); b64=Path(a.bytecode64).read_bytes()
    out=Path(a.out); out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(f'''#pragma once
#include <cstddef>
#include <cstdint>
namespace drayven::lua_bytecode {{
inline constexpr std::uint8_t gameplay32[] = {{
{fmt(b32)}
}};
inline constexpr std::size_t gameplay32_size = sizeof(gameplay32);
inline constexpr std::uint8_t gameplay64[] = {{
{fmt(b64)}
}};
inline constexpr std::size_t gameplay64_size = sizeof(gameplay64);
}}\n''',encoding="utf-8")
    print(f"Embedded LuaJIT bytecode: 32={len(b32)} 64={len(b64)} -> {out}")

if __name__=="__main__": main()
