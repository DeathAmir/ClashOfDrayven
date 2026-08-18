-keep class ir.irautox.clashofdrayven.MainActivity { public <init>(); }
-keep class ir.irautox.clashofdrayven.NativeBridge { native <methods>; }
-keepclasseswithmembernames,includedescriptorclasses class * { native <methods>; }
-keepattributes RuntimeVisibleAnnotations,RuntimeInvisibleAnnotations,Signature,InnerClasses,EnclosingMethod
-renamesourcefileattribute SourceFile
-allowaccessmodification
-adaptclassstrings
-repackageclasses 'ir.irautox.d'
-optimizationpasses 7
-assumenosideeffects class android.util.Log {
    public static *** d(...);
    public static *** v(...);
    public static *** i(...);
}
