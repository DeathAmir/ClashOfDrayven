package ir.irautox.clashofdrayven;

final class NativeBridge {
    static { System.loadLibrary("IrAutoX"); }
    private NativeBridge() {}
    static native String version();
    static native int guard(int value);
    static native String serverBase();
    static native String engineName();
    static native boolean luaReady();
    static native String luaVersion();
    static native String catalogJson();
    static native int[] spend(String currency,int gold,int elixir,int gems,int amount);
    static native int[] gainXp(int xp,int level,int gems,int amount);
    static native int[] production(String[] ids,int[] levels);
    static native float[] unitCombat(String id);
    static native float[] buildingCombat(String id,int level);
    static native float enemyHp(String id,int playerLevel);
    static native int battleStars(int destruction,boolean townHallDown);
    static native int[] battleReward(int playerLevel,int destruction,int stars);
    static native int[] lootPreview(int playerLevel);
    static native int upgradeCost(String buildingId,int level);
    static native String rankName(int points);
    static native String loadingTip(int seed);
    static native int loadingProgress(int phase);
}
