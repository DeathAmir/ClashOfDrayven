package ir.irautox.clashofdrayven;

import android.content.Context;
import android.graphics.*;
import android.graphics.drawable.*;
import android.view.*;
import java.util.*;

final class GameView extends View {
    interface Listener{
        void shop();
        void army();
        void attack();
        void clan();
        void profile();
        void chat();
        void placeBuilding(GameCatalog.BuildingSpec s,int x,int y);
        void upgrade(GameModel.Building b);
        void dirty(String toast);
    }

    private final Paint p=new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint shadow=new Paint(Paint.ANTI_ALIAS_FLAG);
    private final RectF[] nav=new RectF[5];
    private final RectF chatButton=new RectF();
    private final RectF profileRect=new RectF();
    private final RectF builderRect=new RectF();
    private final RectF attackRect=new RectF();
    private final RectF selectedRect=new RectF();

    private final Listener listener;
    private final PackManager packs;
    private final Typeface dry;
    private String playerName;
    private final ScaleGestureDetector scaleDetector;
    private final GestureDetector gestureDetector;

    private GameModel model;
    private GameCatalog.BuildingSpec buildMode;
    private GameModel.Building selectedBuilding;

    private String toast="دهکده آماده است";
    private long toastUntil=System.currentTimeMillis()+2200;

    private float tileW=72f;
    private float tileH=36f;
    private float originX;
    private float originY;
    private float zoom=1f;
    private float panX=0f;
    private float panY=0f;

    private long pulseStart=System.currentTimeMillis();
    private boolean showBuildHint;
    private float touchDownX;
    private float touchDownY;

    GameView(
        Context c,
        GameModel m,
        PackManager packs,
        String playerName,
        Listener listener
    ){
        super(c);

        model=m;
        this.packs=packs;
        this.playerName=
            playerName==null||
            playerName.trim().isEmpty()
            ?"فرمانده"
            :playerName.trim();

        this.listener=listener;

        dry=packs.font();

        p.setTypeface(dry);
        p.setStrokeCap(Paint.Cap.ROUND);
        p.setStrokeJoin(Paint.Join.ROUND);

        shadow.setAntiAlias(true);
        shadow.setShadowLayer(
            14,
            0,
            5,
            0x70000000
        );

        setLayerType(
            View.LAYER_TYPE_SOFTWARE,
            null
        );

        setLayoutDirection(
            View.LAYOUT_DIRECTION_RTL
        );

        scaleDetector=
            new ScaleGestureDetector(
                c,
                new ScaleGestureDetector
                    .SimpleOnScaleGestureListener(){

                    @Override
                    public boolean onScale(
                        ScaleGestureDetector d
                    ){
                        zoom=Math.max(
                            .58f,
                            Math.min(
                                1.9f,
                                zoom*d.getScaleFactor()
                            )
                        );

                        invalidate();
                        return true;
                    }
                }
            );

        gestureDetector=
            new GestureDetector(
                c,
                new GestureDetector
                    .SimpleOnGestureListener(){

                    @Override
                    public boolean onDown(
                        MotionEvent e
                    ){
                        touchDownX=e.getX();
                        touchDownY=e.getY();
                        return true;
                    }

                    @Override
                    public boolean onScroll(
                        MotionEvent e1,
                        MotionEvent e2,
                        float dx,
                        float dy
                    ){
                        if(scaleDetector.isInProgress())
                            return false;

                        if(
                            Math.abs(e2.getX()-touchDownX)<8&&
                            Math.abs(e2.getY()-touchDownY)<8
                        )
                            return false;

                        panX=Math.max(
                            -getWidth()*.38f,
                            Math.min(
                                getWidth()*.38f,
                                panX-dx
                            )
                        );

                        panY=Math.max(
                            -getHeight()*.25f,
                            Math.min(
                                getHeight()*.25f,
                                panY-dy
                            )
                        );

                        invalidate();

                        return true;
                    }

                    @Override
                    public boolean onSingleTapUp(
                        MotionEvent e
                    ){
                        return handleTap(
                            e.getX(),
                            e.getY()
                        );
                    }
                }
            );
    }

    void refreshModel(GameModel fresh){
        if(fresh==null)return;

        model=fresh;

        if(fresh.username!=null&&
            !fresh.username.trim().isEmpty())
            playerName=fresh.username.trim();

        if(selectedBuilding!=null){
            GameModel.Building replacement=
                model.at(
                    selectedBuilding.x,
                    selectedBuilding.y
                );

            selectedBuilding=replacement;
        }

        invalidate();
    }

    void selectBuilding(
        GameCatalog.BuildingSpec s
    ){
        buildMode=s;
        selectedBuilding=null;
        showBuildHint=true;

        showToast(
            "محل ساخت "+s.name+
            " را روی زمین انتخاب کن"
        );

        invalidate();
    }

    void showToast(String s){
        toast=s==null?"":s;
        toastUntil=
            System.currentTimeMillis()+3200;
        invalidate();
    }

    @Override
    protected void onDraw(Canvas c){
        super.onDraw(c);

        int w=getWidth();
        int h=getHeight();

        originX=w*.50f;
        originY=Math.max(
            125,
            h*.21f
        );

        tileW=Math.max(
            58,
            Math.min(
                82,
                w/18f
            )
        );

        tileH=tileW*.50f;

        drawWorldBackground(c,w,h);

        c.save();

        c.translate(
            panX,
            panY
        );

        c.scale(
            zoom,
            zoom,
            w*.5f,
            h*.46f
        );

        drawGround(c);
        drawGrid(c);
        drawBuildings(c);

        c.restore();

        drawTopHud(c,w,h);
        drawResources(c,w);
        drawBuilderPanel(c,w,h);
        drawSideButtons(c,w,h);
        drawBottomBar(c,w,h);

        if(buildMode!=null)
            drawBuildHint(
                c,
                w,
                "ساخت "+
                buildMode.name+
                " • "+
                buildMode.cost+
                " "+
                GameCatalog.currencyName(
                    buildMode.currency
                )
            );
        else if(selectedBuilding!=null)
            drawBuildingPanel(c,w,h);

        if(
            System.currentTimeMillis()<toastUntil
        )
            drawToast(c,w,h);

        postInvalidateDelayed(500);
    }

    private void drawWorldBackground(
        Canvas c,
        int w,
        int h
    ){
        LinearGradient g=
            new LinearGradient(
                0,0,
                0,h,
                0xff72c7d9,
                0xff315b38,
                Shader.TileMode.CLAMP
            );

        p.setShader(g);
        c.drawRect(0,0,w,h,p);
        p.setShader(null);

        p.setColor(0x220d2524);
        c.drawCircle(
            w*.08f,
            h*.23f,
            w*.18f,
            p
        );

        c.drawCircle(
            w*.92f,
            h*.28f,
            w*.21f,
            p
        );

        p.setColor(0x19000000);

        for(int i=0;i<8;i++){
            float x=(i*191)%w;
            float y=100+(i*83)%Math.max(1,h-180);

            c.drawCircle(
                x,
                y,
                32+(i%3)*18,
                p
            );
        }
    }

    private void drawGround(
        Canvas c
    ){
        for(int sum=0;sum<=38;sum++){
            for(int x=0;x<20;x++){
                int y=sum-x;

                if(y<0||y>=20)
                    continue;

                PointF q=tile(x,y);

                Path path=new Path();

                path.moveTo(
                    q.x,
                    q.y-tileH/2
                );

                path.lineTo(
                    q.x+tileW/2,
                    q.y
                );

                path.lineTo(
                    q.x,
                    q.y+tileH/2
                );

                path.lineTo(
                    q.x-tileW/2,
                    q.y
                );

                path.close();

                p.setStyle(Paint.Style.FILL);
                p.setColor(
                    (x+y)%2==0
                    ?0xff6aa957
                    :0xff74b55f
                );

                c.drawPath(path,p);

                p.setStyle(Paint.Style.STROKE);
                p.setStrokeWidth(1);
                p.setColor(0x454b793f);

                c.drawPath(path,p);

                p.setStyle(Paint.Style.FILL);
            }
        }
    }

    private void drawGrid(Canvas c){
        p.setStyle(Paint.Style.STROKE);
        p.setStrokeWidth(1);

        for(int i=0;i<=20;i++){
            PointF a=tile(i,0);
            PointF b=tile(i,20);

            p.setColor(0x28436f38);

            c.drawLine(
                a.x,a.y,
                b.x,b.y,
                p
            );

            PointF d=tile(0,i);
            PointF e=tile(20,i);

            c.drawLine(
                d.x,d.y,
                e.x,e.y,
                p
            );
        }

        p.setStyle(Paint.Style.FILL);
    }

    private PointF tile(
        int x,
        int y
    ){
        return new PointF(
            originX+
            (x-y)*tileW/2f,
            originY+
            (x+y)*tileH/2f
        );
    }

    private PointF untransform(
        float sx,
        float sy
    ){
        float cx=getWidth()*.5f;
        float cy=getHeight()*.46f;

        return new PointF(
            (sx-panX-cx)/zoom+cx,
            (sy-panY-cy)/zoom+cy
        );
    }

    private int[] screen(
        float sx,
        float sy
    ){
        PointF z=untransform(
            sx,
            sy
        );

        float x=
            (
                (z.y-originY)/(tileH/2f)+
                (z.x-originX)/(tileW/2f)
            )/2f;

        float y=
            (
                (z.y-originY)/(tileH/2f)-
                (z.x-originX)/(tileW/2f)
            )/2f;

        return new int[]{
            Math.round(x),
            Math.round(y)
        };
    }

    private void drawBuildings(Canvas c){
        ArrayList<GameModel.Building> bs=
            new ArrayList<>(model.buildings);

        Collections.sort(
            bs,
            (a,b)->Integer.compare(
                a.x+a.y,
                b.x+b.y
            )
        );

        for(GameModel.Building b:bs){
            PointF q=tile(b.x,b.y);

            boolean selected=
                b==selectedBuilding;

            if(selected){
                float pulse=
                    (float)(
                        .5+
                        .5*
                        Math.sin(
                            (System.currentTimeMillis()-
                            pulseStart)/180.0
                        )
                    );

                p.setColor(
                    Color.argb(
                        (int)(80+90*pulse),
                        255,
                        215,
                        80
                    )
                );

                c.drawCircle(
                    q.x,
                    q.y-20,
                    48+8*pulse,
                    p
                );
            }

            p.setColor(0x60000000);

            c.drawOval(
                new RectF(
                    q.x-38,
                    q.y-8,
                    q.x+38,
                    q.y+17
                ),
                p
            );

            Bitmap bm=
                packs.building(b.id);

            if(bm!=null){
                drawBitmapBottom(
                    c,
                    bm,
                    q.x,
                    q.y+9,
                    "wall".equals(b.id)
                    ?54
                    :104
                );
            }else{
                p.setColor(0xff8e6449);

                c.drawRoundRect(
                    new RectF(
                        q.x-28,
                        q.y-56,
                        q.x+28,
                        q.y
                    ),
                    10,
                    10,
                    p
                );
            }

            drawLevelBadge(
                c,
                q.x+28,
                q.y-66,
                b.level
            );

            if(selected)
                drawBuildingRing(c,q);
        }
    }

    private void drawBuildingRing(
        Canvas c,
        PointF q
    ){
        p.setStyle(Paint.Style.STROKE);
        p.setStrokeWidth(3);
        p.setColor(0xffffd65a);

        c.drawOval(
            new RectF(
                q.x-42,
                q.y-17,
                q.x+42,
                q.y+17
            ),
            p
        );

        p.setStyle(Paint.Style.FILL);
    }

    private void drawLevelBadge(
        Canvas c,
        float x,
        float y,
        int level
    ){
        p.setColor(0xee1d2930);

        c.drawRoundRect(
            new RectF(
                x-22,
                y-14,
                x+22,
                y+14
            ),
            12,
            12,
            p
        );

        text(
            c,
            "L"+level,
            x,
            y+5,
            10,
            Color.WHITE,
            Paint.Align.CENTER
        );
    }

    private void drawTopHud(
        Canvas c,
        int w,
        int h
    ){
        float left=16;
        float top=15;

        profileRect.set(
            left,
            top,
            Math.min(370,w*.31f),
            top+78
        );

        shadow.setColor(0xff17242a);
        c.drawRoundRect(
            profileRect,
            21,
            21,
            shadow
        );

        shadow.clearShadowLayer();

        p.setColor(0xff263841);

        c.drawRoundRect(
            profileRect,
            21,
            21,
            p
        );

        p.setColor(0xffb8742f);

        c.drawCircle(
            left+39,
            top+39,
            27,
            p
        );

        text(
            c,
            String.valueOf(model.level),
            left+39,
            top+46,
            17,
            Color.WHITE,
            Paint.Align.CENTER
        );

        text(
            c,
            playerName,
            left+78,
            top+31,
            15,
            Color.WHITE,
            Paint.Align.LEFT
        );

        text(
            c,
            "#"+
            (model.playerId>0?
            model.playerId:
            "OFF"),
            left+78,
            top+52,
            10,
            0xffb6c6cb,
            Paint.Align.LEFT
        );

        p.setColor(0xff101a1f);

        c.drawRoundRect(
            left+78,
            top+59,
            profileRect.right-12,
            top+68,
            5,
            5,
            p
        );

        p.setColor(0xffe2a438);

        float xp=Math.max(
            0,
            Math.min(
                1,
                model.xp/Math.max(
                    1f,
                    model.level*100f
                )
            )
        );

        c.drawRoundRect(
            left+78,
            top+59,
            left+78+
            (profileRect.right-left-90)*xp,
            top+68,
            5,
            5,
            p
        );
    }

    private void drawResources(
        Canvas c,
        int w
    ){
        float width=
            Math.min(
                172,
                w*.145f
            );

        float gap=8;
        float total=
            width*3+
            gap*2;

        float start=
            w-total-18;

        resource(
            c,
            start,
            "gold",
            model.gold,
            0xffffc641,
            width
        );

        resource(
            c,
            start+width+gap,
            "elixir",
            model.elixir,
            0xffdc50d5,
            width
        );

        resource(
            c,
            start+(width+gap)*2,
            "gem",
            model.gems,
            0xff35dfa0,
            width
        );
    }

    private void resource(
        Canvas c,
        float x,
        String key,
        int value,
        int color,
        float width
    ){
        p.setColor(0xe81a292f);

        c.drawRoundRect(
            new RectF(
                x,
                18,
                x+width,
                68
            ),
            16,
            16,
            p
        );

        Bitmap bm=packs.ui(key);

        if(bm!=null){
            c.drawBitmap(
                bm,
                null,
                new RectF(
                    x+7,
                    24,
                    x+43,
                    60
                ),
                p
            );
        }else{
            p.setColor(color);

            c.drawCircle(
                x+25,
                43,
                16,
                p
            );
        }

        text(
            c,
            formatNumber(value),
            x+50,
            51,
            13,
            Color.WHITE,
            Paint.Align.LEFT
        );
    }

    private String formatNumber(int n){
        if(n>=1000000)
            return String.format(
                Locale.US,
                "%.1fM",
                n/1000000f
            );

        if(n>=1000)
            return String.format(
                Locale.US,
                "%.1fK",
                n/1000f
            );

        return String.valueOf(n);
    }

    private void drawBuilderPanel(
        Canvas c,
        int w,
        int h
    ){
        builderRect.set(
            18,
            h*.28f,
            100,
            h*.28f+72
        );

        p.setColor(0xec23363e);

        c.drawRoundRect(
            builderRect,
            19,
            19,
            p
        );

        Bitmap b=packs.ui("builder");

        if(b!=null){
            c.drawBitmap(
                b,
                null,
                new RectF(
                    builderRect.left+15,
                    builderRect.top+10,
                    builderRect.left+58,
                    builderRect.top+53
                ),
                p
            );
        }

        text(
            c,
            "سازنده",
            builderRect.centerX(),
            builderRect.bottom-12,
            10,
            Color.WHITE,
            Paint.Align.CENTER
        );

        p.setColor(0xffd48b2e);

        c.drawCircle(
            builderRect.right-9,
            builderRect.top+10,
            9,
            p
        );

        text(
            c,
            "2",
            builderRect.right-9,
            builderRect.top+14,
            8,
            Color.WHITE,
            Paint.Align.CENTER
        );
    }

    private void drawSideButtons(
        Canvas c,
        int w,
        int h
    ){
        chatButton.set(
            14,
            h*.43f,
            86,
            h*.43f+70
        );

        p.setColor(0xed243a43);

        c.drawRoundRect(
            chatButton,
            19,
            19,
            p
        );

        Bitmap chat=packs.ui("chat");

        if(chat!=null){
            c.drawBitmap(
                chat,
                null,
                new RectF(
                    chatButton.left+15,
                    chatButton.top+9,
                    chatButton.left+57,
                    chatButton.top+51
                ),
                p
            );
        }

        text(
            c,
            "چت",
            chatButton.centerX(),
            chatButton.bottom-10,
            10,
            Color.WHITE,
            Paint.Align.CENTER
        );

        p.setColor(0xffdb3f45);

        c.drawCircle(
            chatButton.right-7,
            chatButton.top+7,
            8,
            p
        );

        text(
            c,
            "!",
            chatButton.right-7,
            chatButton.top+11,
            9,
            Color.WHITE,
            Paint.Align.CENTER
        );
    }

    private void drawBottomBar(
        Canvas c,
        int w,
        int h
    ){
        float barH=Math.max(
            102,
            h*.145f
        );

        p.setColor(0xf218272e);

        c.drawRect(
            0,
            h-barH,
            w,
            h,
            p
        );

        String[] keys={
            "attack",
            "shop",
            "army",
            "clan",
            "profile"
        };

        String[] labels={
            "حمله",
            "فروشگاه",
            "ارتش",
            "کلن",
            "پروفایل"
        };

        float bw=Math.min(
            150,
            (w-70)/5f
        );

        float gap=8;

        float total=
            bw*5+
            gap*4;

        float start=(w-total)/2f;

        float top=
            h-barH+10;

        for(int i=0;i<5;i++){
            RectF r=new RectF(
                start+i*(bw+gap),
                top,
                start+i*(bw+gap)+bw,
                h-12
            );

            nav[i]=r;

            boolean attack=i==0;

            p.setColor(
                attack
                ?0xffc8752b
                :0xff30454e
            );

            c.drawRoundRect(
                r,
                19,
                19,
                p
            );

            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(2);

            p.setColor(
                attack
                ?0xffffb85b
                :0xff526b75
            );

            c.drawRoundRect(
                r,
                19,
                19,
                p
            );

            p.setStyle(Paint.Style.FILL);

            Bitmap bm=packs.ui(keys[i]);

            float icon=
                Math.min(
                    44,
                    r.height()*.45f
                );

            if(bm!=null){
                c.drawBitmap(
                    bm,
                    null,
                    new RectF(
                        r.left+10,
                        r.top+10,
                        r.left+10+icon,
                        r.top+10+icon
                    ),
                    p
                );
            }

            text(
                c,
                labels[i],
                r.right-11,
                r.centerY()+5,
                11,
                Color.WHITE,
                Paint.Align.RIGHT
            );
        }

        attackRect.set(nav[0]);

        p.setColor(0x30ffffff);

        c.drawCircle(
            attackRect.centerX(),
            attackRect.top+10,
            20,
            p
        );
    }

    private void drawBuildHint(
        Canvas c,
        int w,
        String value
    ){
        p.setColor(0xf025363e);

        RectF r=new RectF(
            w/2f-270,
            92,
            w/2f+270,
            145
        );

        c.drawRoundRect(
            r,
            18,
            18,
            p
        );

        text(
            c,
            value,
            w/2f,
            125,
            13,
            Color.WHITE,
            Paint.Align.CENTER
        );
    }

    private void drawBuildingPanel(
        Canvas c,
        int w,
        int h
    ){
        if(selectedBuilding==null)
            return;

        float left=
            w*.5f-185;

        float right=
            w*.5f+185;

        selectedRect.set(
            left,
            h*.66f,
            right,
            h*.66f+82
        );

        p.setColor(0xf01d2d34);

        c.drawRoundRect(
            selectedRect,
            22,
            22,
            p
        );

        String name;

        try{
            name=
                GameCatalog
                .building(
                    selectedBuilding.id
                ).name;
        }catch(Exception e){
            name=selectedBuilding.id;
        }

        text(
            c,
            name+
            "   •   سطح "+
            selectedBuilding.level,
            left+18,
            selectedRect.top+27,
            13,
            Color.WHITE,
            Paint.Align.LEFT
        );

        int cost=
            NativeBridge.upgradeCost(
                selectedBuilding.id,
                selectedBuilding.level
            );

        if(cost<0){
            text(
                c,
                "حداکثر سطح",
                left+18,
                selectedRect.top+54,
                11,
                0xffd5a44d,
                Paint.Align.LEFT
            );
        }else{
            text(
                c,
                "ارتقا   "+cost+" طلا",
                left+18,
                selectedRect.top+54,
                11,
                0xffffc65b,
                Paint.Align.LEFT
            );
        }

        p.setColor(0xffc8752b);

        c.drawRoundRect(
            new RectF(
                right-125,
                selectedRect.top+15,
                right-18,
                selectedRect.top+65
            ),
            15,
            15,
            p
        );

        text(
            c,
            cost<0?"بستن":"ارتقا",
            right-71,
            selectedRect.top+47,
            12,
            Color.WHITE,
            Paint.Align.CENTER
        );
    }

    private void drawToast(
        Canvas c,
        int w,
        int h
    ){
        p.setColor(0xee101b21);

        RectF r=new RectF(
            w/2f-250,
            h*.80f-50,
            w/2f+250,
            h*.80f-8
        );

        c.drawRoundRect(
            r,
            15,
            15,
            p
        );

        text(
            c,
            toast,
            w/2f,
            r.centerY()+5,
            12,
            Color.WHITE,
            Paint.Align.CENTER
        );
    }

    @Override
    public boolean onTouchEvent(MotionEvent e){
        scaleDetector.onTouchEvent(e);
        gestureDetector.onTouchEvent(e);
        return true;
    }

    private boolean handleTap(
        float x,
        float y
    ){
        if(
            chatButton.contains(x,y)
        ){
            listener.chat();
            return true;
        }

        for(int i=0;i<nav.length;i++){
            if(
                nav[i]!=null&&
                nav[i].contains(x,y)
            ){
                switch(i){
                    case 0:
                        listener.attack();
                        break;

                    case 1:
                        listener.shop();
                        break;

                    case 2:
                        listener.army();
                        break;

                    case 3:
                        listener.clan();
                        break;

                    case 4:
                        listener.profile();
                        break;
                }

                return true;
            }
        }

        if(
            selectedBuilding!=null&&
            selectedRect.contains(x,y)
        ){
            int cost=
                NativeBridge.upgradeCost(
                    selectedBuilding.id,
                    selectedBuilding.level
                );

            if(
                cost>=0&&
                x>selectedRect.right-145
            ){
                listener.upgrade(
                    selectedBuilding
                );
            }else{
                selectedBuilding=null;
                invalidate();
            }

            return true;
        }

        if(y<90||y>getHeight()*.80f)
            return false;

        int[] t=screen(x,y);

        if(
            t[0]<0||
            t[0]>=20||
            t[1]<0||
            t[1]>=20
        )
            return true;

        GameModel.Building hit=
            model.at(t[0],t[1]);

        if(buildMode!=null){
            if(hit!=null){
                showToast(
                    "این خانه اشغال است"
                );
                return true;
            }

            listener.placeBuilding(
                buildMode,
                t[0],
                t[1]
            );

            buildMode=null;
            showBuildHint=false;

            invalidate();
            return true;
        }

        selectedBuilding=hit;

        if(hit!=null){
            int cost=
                NativeBridge.upgradeCost(
                    hit.id,
                    hit.level
                );

            if(cost>=0){
                showToast(
                    "برای ارتقا پنل پایین را لمس کن"
                );
            }else{
                showToast(
                    "این ساختمان در حداکثر سطح است"
                );
            }
        }

        invalidate();

        return true;
    }

    private void drawBitmapBottom(
        Canvas c,
        Bitmap b,
        float cx,
        float bottom,
        float max
    ){
        float scale=Math.min(
            max/
            Math.max(1,b.getWidth()),
            max/
            Math.max(1,b.getHeight())
        );

        float ww=b.getWidth()*scale;
        float hh=b.getHeight()*scale;

        c.drawBitmap(
            b,
            null,
            new RectF(
                cx-ww/2,
                bottom-hh,
                cx+ww/2,
                bottom
            ),
            p
        );
    }

    private void text(
        Canvas c,
        String s,
        float x,
        float y,
        float size,
        int color,
        Paint.Align a
    ){
        p.setShader(null);
        p.setColor(color);
        p.setTextSize(size);
        p.setTextAlign(a);
        p.setTypeface(dry);
        c.drawText(
            s==null?"":s,
            x,
            y,
            p
        );
    }
}
