-keep class ir.irautox.clashofdrayven.MainActivity { public <init>(); }
-keep class ir.irautox.clashofdrayven.NativeBridge { native <methods>; }
-keepclasseswithmembernames,includedescriptorclasses class * { native <methods>; }
-keepattributes *Annotation*,Signature,InnerClasses,EnclosingMethod
-renamesourcefileattribute SourceFile
-keepattributes SourceFile,LineNumberTable
-assumenosideeffects class android.util.Log {
    public static *** d(...);
    public static *** v(...);
    public static *** i(...);
}
