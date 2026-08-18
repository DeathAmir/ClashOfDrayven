package ir.irautox.clashofdrayven;

final class NativeBridge {
    static { System.loadLibrary("clashofdrayven"); }
    private NativeBridge() {}

    static native String version();
    static native int guard(int value);
    static native String serverBase();
    static native String engineName();
}
