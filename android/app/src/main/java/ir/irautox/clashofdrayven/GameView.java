package ir.irautox.clashofdrayven;

import android.content.Context;
import android.graphics.*;
import android.view.*;
import java.util.*;

final class GameView extends View {
    interface Listener{void shop();void army();void attack();void clan();void profile();void dirty(String toast);}
    private final Paint p=new Paint(Paint.ANTI_ALIAS_FLAG);
    private final RectF[] nav=new RectF[5];
    private final Listener listener;
    private final PackManager packs;
    private final Typeface dry;
    private final String playerName;
    private GameModel model;
    private GameCatalog.BuildingSpec buildMode;
    private String toast="دهکده همگام شد";
    private long toastUntil=System.currentTimeMillis()+2600;
    private float tileW=72f,tileH=36f,originX,originY;

    GameView(Context c,GameModel m,PackManager packs,String playerName,Listener listener){super(c);this.model=m;this.packs=packs;this.playerName=playerName==null||playerName.trim().isEmpty()?"فرمانده":playerName.trim();this.listener=listener;dry=packs.font();p.setTypeface(dry);setLayerType(View.LAYER_TYPE_SOFTWARE,null);setLayoutDirection(View.LAYOUT_DIRECTION_RTL);}
    void selectBuilding(GameCatalog.BuildingSpec s){buildMode=s;showToast("برای ساخت "+s.name+" روی یک خانه خالی بزن");}
    void showToast(String s){toast=s;toastUntil=System.currentTimeMillis()+3200;invalidate();}
    @Override protected void onDraw(Canvas c){super.onDraw(c);int w=getWidth(),h=getHeight();originX=w*.52f;originY=Math.max(120,h*.16f);tileW=Math.max(58,Math.min(82,w/18f));tileH=tileW*.50f;drawBackground(c,w,h);drawGrid(c);drawBuildings(c);drawHud(c,w,h);drawNav(c,w,h);if(buildMode!=null)drawHint(c,w);if(System.currentTimeMillis()<toastUntil)drawToast(c,w,h);}
    private void drawBackground(Canvas c,int w,int h){LinearGradient g=new LinearGradient(0,0,0,h,0xff68b8d2,0xff214d3d,Shader.TileMode.CLAMP);p.setShader(g);c.drawRect(0,0,w,h,p);p.setShader(null);p.setColor(0x330b1b20);c.drawCircle(w*.13f,h*.28f,w*.14f,p);c.drawCircle(w*.88f,h*.34f,w*.18f,p);}
    private PointF tile(int x,int y){return new PointF(originX+(x-y)*tileW/2f,originY+(x+y)*tileH/2f);}
    private int[] screen(float sx,float sy){float x=((sy-originY)/(tileH/2f)+(sx-originX)/(tileW/2f))/2f;float y=((sy-originY)/(tileH/2f)-(sx-originX)/(tileW/2f))/2f;return new int[]{Math.round(x),Math.round(y)};}
    private void drawGrid(Canvas c){for(int sum=0;sum<=38;sum++)for(int x=0;x<20;x++){int y=sum-x;if(y<0||y>=20)continue;PointF q=tile(x,y);Path path=new Path();path.moveTo(q.x,q.y-tileH/2);path.lineTo(q.x+tileW/2,q.y);path.lineTo(q.x,q.y+tileH/2);path.lineTo(q.x-tileW/2,q.y);path.close();p.setColor((x+y)%2==0?0xff67a957:0xff72b25d);p.setStyle(Paint.Style.FILL);c.drawPath(path,p);p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(1);p.setColor(0x55517242);c.drawPath(path,p);p.setStyle(Paint.Style.FILL);}}
    private void drawBuildings(Canvas c){ArrayList<GameModel.Building> bs=new ArrayList<>(model.buildings);Collections.sort(bs,(a,b)->Integer.compare(a.x+a.y,b.x+b.y));for(GameModel.Building b:bs){PointF q=tile(b.x,b.y);p.setColor(0x59000000);c.drawOval(new RectF(q.x-34,q.y-7,q.x+34,q.y+15),p);Bitmap bm=packs.building(b.id);if(bm!=null)drawBitmapBottom(c,bm,q.x,q.y+7,"wall".equals(b.id)?50:94);else{p.setColor(0xff8e6449);c.drawRoundRect(new RectF(q.x-25,q.y-50,q.x+25,q.y),8,8,p);}p.setColor(0xe51c2c34);c.drawRoundRect(new RectF(q.x+17,q.y-66,q.x+50,q.y-43),8,8,p);text(c,"سطح "+b.level,q.x+33.5f,q.y-49,9,Color.WHITE,Paint.Align.CENTER);}}
    private void drawHud(Canvas c,int w,int h){p.setColor(0xe817262e);c.drawRoundRect(new RectF(18,16,Math.min(330,w*.32f),88),20,20,p);p.setColor(0xff4b9dd8);c.drawCircle(54,52,25,p);text(c,String.valueOf(model.level),54,59,17,Color.WHITE,Paint.Align.CENTER);text(c,playerName,91,48,18,Color.WHITE,Paint.Align.LEFT);text(c,"XP "+model.xp+" • آنلاین",91,70,10,0xffb8c9ce,Paint.Align.LEFT);float chipW=Math.min(172,w*.145f),gap=10,total=chipW*3+gap*2,start=w-total-20;resource(c,start,"gold",model.gold,0xffffc641,chipW);resource(c,start+chipW+gap,"elixir",model.elixir,0xffda50d5,chipW);resource(c,start+(chipW+gap)*2,"gem",model.gems,0xff35dfa0,chipW);p.setColor(0xaa142229);c.drawRoundRect(new RectF(w*.42f,20,w*.58f,65),15,15,p);text(c,"CLASH OF DRAYVEN",w*.50f,49,15,0xffffd572,Paint.Align.CENTER);}
    private void resource(Canvas c,float x,String key,int value,int color,float width){p.setColor(0xe91b2b33);RectF r=new RectF(x,19,x+width,70);c.drawRoundRect(r,15,15,p);Bitmap bm=packs.ui(key);if(bm!=null)c.drawBitmap(bm,null,new RectF(x+7,25,x+43,61),p);else{p.setColor(color);c.drawCircle(x+25,44,17,p);}text(c,String.format(Locale.US,"%,d",value),x+50,52,14,Color.WHITE,Paint.Align.LEFT);}
    private void drawNav(Canvas c,int w,int h){float barH=Math.max(96,h*.14f);p.setColor(0xf3132028);c.drawRect(0,h-barH,w,h,p);String[] keys={"attack","shop","army","clan","profile"};String[] labels={"حمله","ساخت","ارتش","کلن","پروفایل"};float bw=Math.min(148,(w-70)/5f),gap=8,total=bw*5+gap*4,start=(w-total)/2f,top=h-barH+11;for(int i=0;i<5;i++){RectF r=new RectF(start+i*(bw+gap),top,start+i*(bw+gap)+bw,h-14);nav[i]=r;boolean attack=i==0;p.setColor(attack?0xffd77d2c:0xff344852);c.drawRoundRect(r,17,17,p);p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(2);p.setColor(attack?0xffffb85b:0xff526a74);c.drawRoundRect(r,17,17,p);p.setStyle(Paint.Style.FILL);Bitmap bm=packs.ui(keys[i]);float icon=Math.min(42,r.height()*.48f);if(bm!=null)c.drawBitmap(bm,null,new RectF(r.left+11,r.top+12,r.left+11+icon,r.top+12+icon),p);text(c,labels[i],r.right-11,r.centerY()+5,11,Color.WHITE,Paint.Align.RIGHT);}}
    private void drawHint(Canvas c,int w){p.setColor(0xee253943);RectF r=new RectF(w/2f-235,91,w/2f+235,142);c.drawRoundRect(r,17,17,p);text(c,"ساخت "+buildMode.name+" • "+buildMode.cost+" "+GameCatalog.currencyName(buildMode.currency),w/2f,123,13,Color.WHITE,Paint.Align.CENTER);}
    private void drawToast(Canvas c,int w,int h){p.setColor(0xee101b21);RectF r=new RectF(w/2f-250,h*.80f-50,w/2f+250,h*.80f-8);c.drawRoundRect(r,15,15,p);text(c,toast,w/2f,r.centerY()+5,12,Color.WHITE,Paint.Align.CENTER);postInvalidateDelayed(250);}
    @Override public boolean onTouchEvent(MotionEvent e){if(e.getAction()!=MotionEvent.ACTION_DOWN)return true;for(int i=0;i<nav.length;i++)if(nav[i]!=null&&nav[i].contains(e.getX(),e.getY())){switch(i){case 0:listener.attack();break;case 1:listener.shop();break;case 2:listener.army();break;case 3:listener.clan();break;case 4:listener.profile();break;}return true;}if(buildMode!=null){int[] t=screen(e.getX(),e.getY());if(t[0]<0||t[0]>=20||t[1]<0||t[1]>=20)return true;for(GameModel.Building b:model.buildings)if(b.x==t[0]&&b.y==t[1]){showToast("این خانه اشغال است");return true;}if(!model.spend(buildMode.currency,buildMode.cost)){showToast("منابع کافی نیست");return true;}model.buildings.add(new GameModel.Building(UUID.randomUUID().toString(),buildMode.id,t[0],t[1],1));model.gainXp(18);buildMode=null;listener.dirty("ساختمان قرار گرفت");invalidate();}return true;}
    private void drawBitmapBottom(Canvas c,Bitmap b,float cx,float bottom,float max){float scale=Math.min(max/Math.max(1,b.getWidth()),max/Math.max(1,b.getHeight()));float ww=b.getWidth()*scale,hh=b.getHeight()*scale;c.drawBitmap(b,null,new RectF(cx-ww/2,bottom-hh,cx+ww/2,bottom),p);}
    private void text(Canvas c,String s,float x,float y,float size,int color,Paint.Align a){p.setShader(null);p.setColor(color);p.setTextSize(size);p.setTextAlign(a);p.setTypeface(dry);c.drawText(s,x,y,p);}
}
