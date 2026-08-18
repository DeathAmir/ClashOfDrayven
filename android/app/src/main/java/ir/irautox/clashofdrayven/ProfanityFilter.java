package ir.irautox.clashofdrayven;

import java.text.Normalizer;
import java.util.*;
import java.util.regex.*;

final class ProfanityFilter {
    private static final String[] WORDS={
        "کص","کسکش","کون","کونی","کیر","کیری","جنده","جندە","حرومزاده","حرامزاده","مادرجنده","مادر قحبه","قحبه","فاحشه","سیکتیر","گوه","عن","لاشی","پفیوز","بیناموس","بی ناموس","بی‌ناموس","ننه جنده","خارکسه","خایه","تخم سگ","ولدزنا","دیوث","جاکش","جاکش","کثافت","آشغال","مرتیکه","هرزه","شهوتی",
        "fuck","fucker","fucking","motherfucker","shit","bullshit","bitch","sonofabitch","asshole","dick","cock","pussy","cunt","whore","slut","bastard","wanker","twat","prick","nigger","nigga","faggot","retard","rape","rapist"
    };
    private static final ArrayList<Pattern> PATTERNS=new ArrayList<>();
    static{for(String w:WORDS){String n=normalize(w);if(n.length()<2)continue;StringBuilder p=new StringBuilder();for(int i=0;i<n.length();i++){char c=n.charAt(i);if(Character.isWhitespace(c)){p.append("[\\s._\\-‌]*");continue;}p.append(Pattern.quote(String.valueOf(c))).append("[\\s._\\-‌]*");}PATTERNS.add(Pattern.compile(p.toString(),Pattern.CASE_INSENSITIVE|Pattern.UNICODE_CASE));}}
    static String clean(String source){if(source==null)return"";String out=source;String normalized=normalize(source);for(String w:WORDS)if(normalized.contains(normalize(w))){out=maskMatches(out);break;}for(Pattern p:PATTERNS)out=p.matcher(normalizeVisible(out)).replaceAll("***");return out.trim();}
    static boolean containsBlocked(String source){if(source==null)return false;String n=normalize(source);for(String w:WORDS)if(n.contains(normalize(w)))return true;for(Pattern p:PATTERNS)if(p.matcher(n).find())return true;return false;}
    private static String maskMatches(String s){String out=s;for(String w:WORDS){String q=Pattern.quote(w);out=out.replaceAll("(?iu)"+q,"***");}return out;}
    private static String normalizeVisible(String s){return normalize(s).replace('ي','ی').replace('ك','ک');}
    private static String normalize(String s){String n=Normalizer.normalize(s,Normalizer.Form.NFKC).toLowerCase(Locale.ROOT).replace("\u200c","").replace("\u200d","").replace("\u200e","").replace("\u200f","").replace('ي','ی').replace('ى','ی').replace('ك','ک').replace('ة','ه');n=n.replaceAll("(.)\\1{2,}","$1$1");return n;}
    private ProfanityFilter(){}
}
