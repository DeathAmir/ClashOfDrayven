package ir.irautox.clashofdrayven;

import android.app.*;
import android.os.*;
import android.content.*;
import android.graphics.*;
import android.graphics.drawable.GradientDrawable;
import android.graphics.Typeface;
import android.text.InputType;
import android.view.*;
import android.view.animation.AlphaAnimation;
import android.view.animation.Animation;
import android.widget.*;
import org.json.JSONObject;
import java.util.*;
import java.util.concurrent.*;

public final class MainActivity extends Activity implements GameView.Listener {
    private final ExecutorService io=Executors.newSingleThreadExecutor();
    private final Handler main=new Handler(Looper.getMainLooper());
    private ApiClient api;private GameModel model;private GameView game;private PackManager packs;private boolean savePending;private String playerName="فرمانده";
    private Typeface vazir=Typeface.DEFAULT,magic=Typeface.DEFAULT_BOLD;
    private final Runnable production=new Runnable(){@Override public void run(){if(model!=null){int g=0,e=0;for(GameModel.Building b:model.buildings){if("goldmine".equals(b.id))g+=2+b.level*2;if("elixircollector".equals(b.id))e+=2+b.level*2;}if(g+e>0){model.gold=Math.min(9999999,model.gold+g);model.elixir=Math.min(9999999,model.elixir+e);dirty(null);if(game!=null)game.invalidate();}}main.postDelayed(this,1800);}};

    @Override protected void onCreate(Bundle b){
        super.onCreate(b);getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN,WindowManager.LayoutParams.FLAG_FULLSCREEN);getWindow().getDecorView().setSystemUiVisibility(5894);
        try{vazir=Typeface.createFromAsset(getAssets(),"fonts/vazir.ttf");}catch(Exception ignored){}
        try{magic=Typeface.createFromAsset(getAssets(),"fonts/irautox_magic_5.ttf");}catch(Exception ignored){}
        showBrandSplash();
        main.postDelayed(this::bootstrap,4000);
    }

    private void showBrandSplash(){
        FrameLayout root=new FrameLayout(this);root.setBackgroundColor(Color.WHITE);root.setLayoutDirection(View.LAYOUT_DIRECTION_RTL);
        ImageView logo=new ImageView(this);logo.setImageResource(R.drawable.splash_irautox);logo.setScaleType(ImageView.ScaleType.CENTER_INSIDE);logo.setAdjustViewBounds(true);
        FrameLayout.LayoutParams p=new FrameLayout.LayoutParams((int)(getResources().getDisplayMetrics().widthPixels*.58f),(int)(getResources().getDisplayMetrics().heightPixels*.58f),Gravity.CENTER);root.addView(logo,p);
        AlphaAnimation pulse=new AlphaAnimation(1f,.18f);pulse.setDuration(650);pulse.setRepeatMode(Animation.REVERSE);pulse.setRepeatCount(Animation.INFINITE);logo.startAnimation(pulse);
        setContentView(root);
    }

    private void bootstrap(){
        showLoading("در حال آماده‌سازی قلمرو درایون...");
        io.execute(()->{try{packs=PackManager.prepare(this);api=new ApiClient(this);main.post(this::authenticate);}catch(Exception ex){main.post(()->fatal(ex));}});
    }

    private void showLoading(String message){
        FrameLayout root=new FrameLayout(this);root.setLayoutDirection(View.LAYOUT_DIRECTION_RTL);root.setBackground(gradient(GradientDrawable.Orientation.TL_BR,0xff102832,0xff1b4f4a,0xff0a171d,0));
        LinearLayout box=new LinearLayout(this);box.setOrientation(LinearLayout.VERTICAL);box.setGravity(Gravity.CENTER);box.setPadding(dp(36),dp(34),dp(36),dp(34));box.setBackground(round(0xe721343d,dp(26),0x55ffffff));
        TextView logo=titleLabel("CLASH OF DRAYVEN",31,0xffffd06b,Gravity.CENTER);TextView by=label("IrAutoX",12,0xff9fc3cc,Gravity.CENTER);TextView status=label(message,15,Color.WHITE,Gravity.CENTER);ProgressBar bar=new ProgressBar(this);box.addView(logo,lp(-1,-2,0,dp(6)));box.addView(by,lp(-1,-2,0,dp(24)));box.addView(status,lp(-1,-2,0,dp(18)));box.addView(bar,new LinearLayout.LayoutParams(dp(48),dp(48)));
        FrameLayout.LayoutParams bp=new FrameLayout.LayoutParams(Math.min(dp(620),(int)(getResources().getDisplayMetrics().widthPixels*.72f)),WindowManager.LayoutParams.WRAP_CONTENT,Gravity.CENTER);root.addView(box,bp);setContentView(root);
    }

    private void authenticate(){
        if(api.token()==null){showAuth(false);return;}showLoading("در حال بازیابی دهکده شما...");io.execute(()->{try{JSONObject profile=api.profile();playerName=userFrom(profile,playerName);GameModel m=api.state();main.post(()->launch(m));}catch(Exception ex){api.clear();main.post(()->showAuth(false));}});
    }

    private void showAuth(boolean register){
        FrameLayout root=new FrameLayout(this);root.setLayoutDirection(View.LAYOUT_DIRECTION_RTL);root.setBackground(gradient(GradientDrawable.Orientation.BL_TR,0xff0e2732,0xff225f58,0xff10212a,0));
        Bitmap hero=packs==null?null:packs.scenery("hero");if(hero!=null){ImageView art=new ImageView(this);art.setImageBitmap(hero);art.setScaleType(ImageView.ScaleType.CENTER_INSIDE);art.setAlpha(.34f);FrameLayout.LayoutParams ap=new FrameLayout.LayoutParams(dp(390),dp(390),Gravity.LEFT|Gravity.CENTER_VERTICAL);ap.leftMargin=dp(42);root.addView(art,ap);}
        LinearLayout card=new LinearLayout(this);card.setLayoutDirection(View.LAYOUT_DIRECTION_RTL);card.setOrientation(LinearLayout.VERTICAL);card.setPadding(dp(34),dp(26),dp(34),dp(28));card.setGravity(Gravity.CENTER_HORIZONTAL);card.setBackground(round(0xf11b2d36,dp(26),0x33ffffff));
        TextView mark=titleLabel("CLASH OF DRAYVEN",28,0xffffd36f,Gravity.CENTER);TextView sub=label(register?"ساخت حساب فرمانده":"خوش برگشتی فرمانده",13,0xffa9c7ce,Gravity.CENTER);card.addView(mark,lp(-1,-2,0,dp(2)));card.addView(sub,lp(-1,-2,0,dp(21)));
        EditText user=field(register?"نام فرمانده":"نام فرمانده یا ایمیل",false);EditText email=field("ایمیل",false);email.setVisibility(register?View.VISIBLE:View.GONE);EditText pass=field("رمز عبور",true);card.addView(user,lp(-1,dp(52),0,dp(10)));card.addView(email,lp(-1,register?dp(52):0,0,register?dp(10):0));card.addView(pass,lp(-1,dp(52),0,dp(14)));
        TextView status=label(register?"شروع با ۷۰۰۰ طلا • ۷۰۰۰ اکسیر • ۲۵۰ جم":"ورود امن به قلمرو Drayven",11,0xff91abb1,Gravity.CENTER);card.addView(status,lp(-1,-2,0,dp(12)));
        Button go=button(register?"ساخت حساب و شروع بازی":"ورود به دهکده",0xffd98b31);Button swap=button(register?"از قبل حساب دارم":"ساخت حساب جدید",0xff344d58);card.addView(go,lp(-1,dp(53),0,dp(9)));card.addView(swap,lp(-1,dp(48),0,0));
        TextView author=label("IrAutoX • Clash Of Drayven",10,0xff688991,Gravity.CENTER);card.addView(author,lp(-1,-2,0,dp(2)));
        go.setOnClickListener(v->{String u=user.getText().toString().trim(),em=email.getText().toString().trim(),pw=pass.getText().toString();if(u.isEmpty()||pw.isEmpty()||(register&&em.isEmpty())){status.setText("همه فیلدهای لازم را کامل کن.");return;}go.setEnabled(false);swap.setEnabled(false);status.setText("در حال اتصال...");io.execute(()->{try{JSONObject auth=register?api.register(u,em,pw):api.login(u,pw);playerName=userFrom(auth,u);try{JSONObject profile=api.profile();playerName=userFrom(profile,playerName);}catch(Exception ignored){}GameModel m=api.state();main.post(()->launch(m));}catch(Exception ex){main.post(()->{status.setText(ex.getMessage());go.setEnabled(true);swap.setEnabled(true);});}});});
        swap.setOnClickListener(v->showAuth(!register));
        FrameLayout.LayoutParams cp=new FrameLayout.LayoutParams(Math.min(dp(500),(int)(getResources().getDisplayMetrics().widthPixels*.48f)),WindowManager.LayoutParams.WRAP_CONTENT,Gravity.RIGHT|Gravity.CENTER_VERTICAL);cp.rightMargin=dp(52);root.addView(card,cp);setContentView(root);
    }

    private String userFrom(JSONObject root,String fallback){if(root==null)return fallback;JSONObject u=root.optJSONObject("user");String n=u==null?null:u.optString("username",null);return n==null||n.trim().isEmpty()?fallback:n.trim();}
    private void launch(GameModel m){model=m;game=new GameView(this,model,packs,playerName,this);setContentView(game);main.removeCallbacks(production);main.postDelayed(production,1800);Toast.makeText(this,"خوش آمدی "+playerName,Toast.LENGTH_SHORT).show();}

    @Override public void shop(){String[] items=new String[GameCatalog.BUILDINGS.length];for(int i=0;i<items.length;i++){GameCatalog.BuildingSpec s=GameCatalog.BUILDINGS[i];items[i]=s.name+" • "+s.cost+" "+s.currency;}new AlertDialog.Builder(this).setTitle("فروشگاه ساخت‌وساز").setItems(items,(d,which)->game.selectBuilding(GameCatalog.BUILDINGS[which])).setNegativeButton("بستن",null).show();}
    @Override public void army(){String[] items=new String[GameCatalog.UNITS.length];for(int i=0;i<items.length;i++){GameCatalog.UnitSpec u=GameCatalog.UNITS[i];items[i]=u.name+"   ×"+model.units.getOrDefault(u.id,0)+"   •   "+u.cost+" "+u.currency;}new AlertDialog.Builder(this).setTitle("آموزش نیروها").setItems(items,(d,which)->recruit(GameCatalog.UNITS[which])).setNegativeButton("بستن",null).show();}
    private void recruit(GameCatalog.UnitSpec u){boolean barracks=false;for(GameModel.Building b:model.buildings)if("barracks".equals(b.id))barracks=true;if(!barracks){game.showToast("اول سربازخانه بساز");return;}if(!model.spend(u.currency,u.cost)){game.showToast("منابع کافی نیست");return;}model.units.put(u.id,model.units.getOrDefault(u.id,0)+1);model.gainXp(4);dirty("نیرو آموزش داده شد");game.invalidate();}
    @Override public void attack(){int count=0;for(int n:model.units.values())count+=n;if(count<=0){game.showToast("قبل از حمله نیرو آموزش بده");return;}new AlertDialog.Builder(this).setTitle("دهکده دشمن پیدا شد").setMessage("یک کارت نیرو انتخاب کن و روی میدان نبرد بزن تا اعزام شود.\n\nنیروهای آماده: "+count).setPositiveButton("حمله",(d,w)->startBattle()).setNegativeButton("بعداً",null).show();}
    private void startBattle(){main.removeCallbacks(production);BattleView battle=new BattleView(this,model,packs,playerName,(gold,elixir,stars,destruction)->{savePending=true;saveNow();launch(model);if(game!=null)game.showToast("حمله: "+stars+" ستاره • "+destruction+"٪ • +"+gold+" طلا");});setContentView(battle);}
    @Override public void clan(){if(model.clanName!=null){new AlertDialog.Builder(this).setTitle(model.clanName+" ["+model.clanTag+"]").setMessage("اطلاعات کلن روی قلمرو Drayven ذخیره می‌شود.").setPositiveButton("بستن",null).show();return;}LinearLayout l=new LinearLayout(this);l.setOrientation(LinearLayout.VERTICAL);l.setPadding(dp(28),dp(10),dp(28),dp(10));EditText name=field("نام کلن",false),tag=field("تگ",false);l.addView(name);l.addView(tag);new AlertDialog.Builder(this).setTitle("ساخت کلن").setView(l).setPositiveButton("ساخت",(d,w)->io.execute(()->{try{JSONObject r=api.createClan(name.getText().toString().trim(),tag.getText().toString().trim().toUpperCase(Locale.ROOT));JSONObject c=r.getJSONObject("clan");model.clanName=c.optString("name");model.clanTag=c.optString("tag");main.post(()->{dirty("کلن ساخته شد");game.invalidate();});}catch(Exception ex){main.post(()->game.showToast(ex.getMessage()));}})).setNegativeButton("لغو",null).show();}
    @Override public void profile(){new AlertDialog.Builder(this).setTitle(playerName).setMessage("سطح فرمانده: "+model.level+"\nXP: "+model.xp+"\n\nسازنده: IrAutoX\nموتور: "+NativeBridge.engineName()+"\nNative: "+NativeBridge.version()+"\nGuard: "+NativeBridge.guard(model.level)).setPositiveButton("همگام‌سازی",(d,w)->{savePending=true;saveNow();}).setNeutralButton("خروج از حساب",(d,w)->{api.logout();recreate();}).setNegativeButton("بستن",null).show();}

    @Override public void dirty(String toast){savePending=true;if(toast!=null&&game!=null)game.showToast(toast);main.removeCallbacks(saveDebounce);main.postDelayed(saveDebounce,1200);}
    private final Runnable saveDebounce=()->saveNow();
    private void saveNow(){if(!savePending||model==null||api==null)return;savePending=false;io.execute(()->{try{api.save(model);}catch(Exception ex){savePending=true;main.post(()->{if(game!=null)game.showToast("خطا در همگام‌سازی: "+ex.getMessage());});}});}

    private EditText field(String hint,boolean password){EditText e=new EditText(this);e.setLayoutDirection(View.LAYOUT_DIRECTION_RTL);e.setGravity(Gravity.RIGHT|Gravity.CENTER_VERTICAL);e.setTypeface(vazir);e.setHint(hint);e.setHintTextColor(0xff829ba3);e.setTextColor(Color.WHITE);e.setSingleLine(true);e.setTextSize(14);e.setPadding(dp(16),0,dp(16),0);e.setBackground(round(0xff10222a,dp(13),0x554d7580));e.setInputType(password?InputType.TYPE_CLASS_TEXT|InputType.TYPE_TEXT_VARIATION_PASSWORD:InputType.TYPE_CLASS_TEXT);return e;}
    private Button button(String text,int color){Button b=new Button(this);b.setTypeface(vazir);b.setText(text);b.setTextColor(Color.WHITE);b.setTextSize(12);b.setAllCaps(false);b.setBackground(round(color,dp(14),0x33ffffff));return b;}
    private TextView label(String text,int size,int color,int gravity){TextView t=new TextView(this);t.setLayoutDirection(View.LAYOUT_DIRECTION_RTL);t.setTextDirection(View.TEXT_DIRECTION_RTL);t.setTypeface(vazir);t.setText(text);t.setTextColor(color);t.setTextSize(size);t.setGravity(gravity);return t;}
    private TextView titleLabel(String text,int size,int color,int gravity){TextView t=label(text,size,color,gravity);t.setTypeface(magic);t.setTextDirection(View.TEXT_DIRECTION_LTR);return t;}
    private GradientDrawable round(int fill,float radius,int stroke){GradientDrawable d=new GradientDrawable();d.setColor(fill);d.setCornerRadius(radius);if(stroke!=0)d.setStroke(dp(1),stroke);return d;}
    private GradientDrawable gradient(GradientDrawable.Orientation o,int a,int b,int c,float radius){GradientDrawable d=new GradientDrawable(o,new int[]{a,b,c});d.setCornerRadius(radius);return d;}
    private LinearLayout.LayoutParams lp(int w,int h,int top,int bottom){LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(w,h);p.topMargin=top;p.bottomMargin=bottom;return p;}
    private int dp(int x){return Math.round(x*getResources().getDisplayMetrics().density);}
    private void fatal(Exception ex){new AlertDialog.Builder(this).setTitle("Clash Of Drayven").setMessage("راه‌اندازی بازی ناموفق بود:\n"+ex).setCancelable(false).setPositiveButton("خروج",(d,w)->finish()).show();}
    @Override protected void onPause(){saveNow();super.onPause();}
    @Override protected void onDestroy(){main.removeCallbacks(production);main.removeCallbacks(saveDebounce);io.shutdownNow();super.onDestroy();}
}
