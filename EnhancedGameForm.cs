using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClashOfDrayven
{
    internal sealed class DryBuilding
    {
        public string Id; public string Name; public Currency Currency; public int Cost; public int MaxLevel; public string[] Art;
        public DryBuilding(string id, string name, Currency currency, int cost, int max, params string[] art) { Id=id; Name=name; Currency=currency; Cost=cost; MaxLevel=max; Art=art; }
    }
    internal sealed class DryUnit
    {
        public string Id; public string Name; public Currency Currency; public int Cost; public int Power; public string[] Art;
        public DryUnit(string id, string name, Currency currency, int cost, int power, params string[] art) { Id=id; Name=name; Currency=currency; Cost=cost; Power=power; Art=art; }
    }

    internal sealed class EnhancedGameForm : Form
    {
        private const int Grid = 20;
        private const float TileW = 64f;
        private const float TileH = 32f;
        private readonly GameState _state;
        private readonly DrayvenApiClient _api;
        private readonly UserDto _user;
        private readonly Timer _tick = new Timer { Interval = 1800 };
        private readonly Timer _autosave = new Timer { Interval = 6500 };
        private readonly Dictionary<string, Image> _art = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Tuple<RectangleF, DryBuilding>> _shopCards = new List<Tuple<RectangleF, DryBuilding>>();
        private readonly List<Tuple<RectangleF, DryUnit>> _armyCards = new List<Tuple<RectangleF, DryUnit>>();
        private readonly Dictionary<string, RectangleF> _nav = new Dictionary<string, RectangleF>(StringComparer.OrdinalIgnoreCase);
        private RectangleF _upgradeRect;
        private RectangleF _logoutRect;
        private DryBuilding _buildMode;
        private PlacedBuilding _selected;
        private string _panel = "";
        private string _toast = "Welcome to the Drayven realm.";
        private DateTime _toastUntil = DateTime.UtcNow.AddSeconds(5);
        private bool _syncing;
        private bool _dirty;

        private static readonly DryBuilding[] Buildings =
        {
            new DryBuilding("townhall","Drayven Hall",Currency.Gold,0,12,"town","hall"),
            new DryBuilding("goldmine","Gold Mine",Currency.Gold,800,12,"gold","mine"),
            new DryBuilding("elixircollector","Elixir Pump",Currency.Gold,850,12,"elixir","collector"),
            new DryBuilding("goldstorage","Gold Storage",Currency.Gold,1200,12,"gold","storage"),
            new DryBuilding("elixirstorage","Elixir Storage",Currency.Gold,1250,12,"elixir","storage"),
            new DryBuilding("barracks","Barracks",Currency.Elixir,1200,12,"barrack"),
            new DryBuilding("armycamp","Army Camp",Currency.Elixir,1500,10,"army","camp"),
            new DryBuilding("cannon","Cannon",Currency.Gold,1100,12,"cannon"),
            new DryBuilding("archertower","Archer Tower",Currency.Gold,1600,12,"archer","tower"),
            new DryBuilding("mortar","Mortar",Currency.Gold,2400,10,"mortar"),
            new DryBuilding("airdefense","Air Defense",Currency.Gold,3100,10,"air","defense"),
            new DryBuilding("wall","Wall",Currency.Gold,180,15,"wall"),
            new DryBuilding("clancastle","Clan Keep",Currency.Gold,5000,8,"clan","castle")
        };

        private static readonly DryUnit[] Units =
        {
            new DryUnit("vanguard","Vanguard",Currency.Elixir,250,12,"barbarian"),
            new DryUnit("ranger","Ranger",Currency.Elixir,350,15,"archer"),
            new DryUnit("rogue","Rogue",Currency.Elixir,450,17,"goblin"),
            new DryUnit("breaker","Breaker",Currency.Elixir,600,24,"wall","breaker"),
            new DryUnit("brute","Brute",Currency.Elixir,850,30,"giant"),
            new DryUnit("mage","Mage",Currency.Elixir,1100,38,"wizard"),
            new DryUnit("healer","Healer",Currency.Elixir,1350,44,"healer"),
            new DryUnit("stormcaller","Stormcaller",Currency.Gems,25,70,"dragon")
        };

        public EnhancedGameForm(GameState state, DrayvenApiClient api, UserDto user)
        {
            _state = state; _api = api; _user = user ?? new UserDto { username = "Chief" };
            Text = "Clash Of Drayven — Online";
            ClientSize = new Size(1440, 900); MinimumSize = new Size(1160, 720); StartPosition = FormStartPosition.CenterScreen;
            BackColor = DryTheme.Night; DoubleBuffered = true; KeyPreview = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            EnsureExtendedDefaults();
            PreloadArt();
            _tick.Tick += delegate { Produce(); Invalidate(); };
            _autosave.Tick += async delegate { if (_dirty) await SyncAsync(false); };
            _tick.Start(); _autosave.Start();
            MouseDown += OnMouse;
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) { _buildMode=null; _selected=null; _panel=""; Invalidate(); } };
            FormClosing += delegate { try { if (_dirty) _api.PutState(_state.SaveData, OnlineStateBridge.Xp, OnlineStateBridge.Level); } catch { } _state.Save(); };
        }

        private void EnsureExtendedDefaults()
        {
            foreach (var u in Units) if (!_state.SaveData.Units.ContainsKey(u.Id)) _state.SaveData.Units[u.Id] = 0;
            if (!_state.SaveData.Buildings.Any(b => b.DefinitionId == "townhall")) _state.SaveData.Buildings.Add(new PlacedBuilding { DefinitionId="townhall", X=9, Y=9, Level=1 });
        }

        private void PreloadArt()
        {
            foreach (var b in Buildings) { var img = AssetLibrary.Find(b.Art); if (img != null) _art[b.Id] = img; }
            foreach (var u in Units) { var img = AssetLibrary.Find(u.Art); if (img != null) _art["unit:"+u.Id] = img; }
            string[] ui = { "gold", "elixir", "gem", "shop", "attack", "clan", "army", "profile", "settings", "tree", "rock", "shield", "trophy" };
            foreach (var key in ui) { var img = AssetLibrary.Find(key); if (img != null) _art["ui:"+key] = img; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            using (var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(119,187,210), Color.FromArgb(29,72,67), 90f)) g.FillRectangle(bg, ClientRectangle);
            DrawVillage(g); DrawHud(g); DrawNav(g); DrawSelection(g);
            if (_panel == "shop") DrawShop(g); else if (_panel == "army") DrawArmy(g); else if (_panel == "clan") DrawClan(g); else if (_panel == "profile") DrawProfile(g);
            if (_buildMode != null) DrawBuildHint(g);
            DrawToast(g);
        }

        private PointF Origin { get { return new PointF(System.Math.Max(430, ClientSize.Width * .52f), 140f); } }
        private PointF TileToScreen(int x, int y) { var o=Origin; return new PointF(o.X+(x-y)*TileW/2f, o.Y+(x+y)*TileH/2f); }
        private Tuple<int,int> ScreenToTile(Point p)
        {
            var o=Origin; var sx=p.X-o.X; var sy=p.Y-o.Y;
            var x=(sy/(TileH/2f)+sx/(TileW/2f))/2f; var y=(sy/(TileH/2f)-sx/(TileW/2f))/2f;
            return Tuple.Create((int)System.Math.Round(x),(int)System.Math.Round(y));
        }

        private void DrawVillage(Graphics g)
        {
            for (int sum=0; sum<=38; sum++) for (int x=0;x<Grid;x++)
            {
                int y=sum-x; if(y<0||y>=Grid) continue; var c=TileToScreen(x,y);
                var pts=new[]{new PointF(c.X,c.Y-TileH/2),new PointF(c.X+TileW/2,c.Y),new PointF(c.X,c.Y+TileH/2),new PointF(c.X-TileW/2,c.Y)};
                using(var grass=new SolidBrush((x+y)%2==0?Color.FromArgb(88,159,75):Color.FromArgb(96,171,80))) g.FillPolygon(grass,pts);
                using(var line=new Pen(Color.FromArgb(45,77,58),1)) g.DrawPolygon(line,pts);
            }
            DrawDecoration(g,"tree",1,2,76); DrawDecoration(g,"tree",17,3,70); DrawDecoration(g,"rock",2,16,58); DrawDecoration(g,"tree",16,17,66);
            foreach(var b in _state.SaveData.Buildings.OrderBy(z=>z.X+z.Y).ThenBy(z=>z.Y)) DrawBuilding(g,b,ReferenceEquals(b,_selected));
        }

        private void DrawDecoration(Graphics g,string key,int x,int y,float max)
        {
            Image img; if(!_art.TryGetValue("ui:"+key,out img)) return; var c=TileToScreen(x,y); DrawSprite(g,img,c.X,c.Y,max,max,6);
        }

        private void DrawBuilding(Graphics g, PlacedBuilding b, bool selected)
        {
            var spec=Buildings.FirstOrDefault(x=>x.Id.Equals(b.DefinitionId,StringComparison.OrdinalIgnoreCase));
            var c=TileToScreen(b.X,b.Y);
            using(var sh=new SolidBrush(Color.FromArgb(75,0,0,0))) g.FillEllipse(sh,c.X-30,c.Y-7,60,25);
            if(selected){using(var p=new Pen(Color.White,3))g.DrawEllipse(p,c.X-34,c.Y-19,68,38);}
            Image img; if(spec!=null&&_art.TryGetValue(spec.Id,out img)) DrawSprite(g,img,c.X,c.Y,spec.Id=="wall"?48:92,spec.Id=="wall"?48:92,8);
            else DrawFallback(g,c,spec==null?b.DefinitionId:spec.Id);
            using(var f=DryTheme.Font(10)) using(var br=new SolidBrush(Color.FromArgb(220,22,33,38)))
            {
                var r=new RectangleF(c.X+17,c.Y-65,33,21); DryTheme.FillRound(g,br,r,8); g.DrawString("L"+b.Level,f,Brushes.White,r.X+6,r.Y+4);
            }
        }

        private static void DrawSprite(Graphics g,Image img,float cx,float cy,float maxW,float maxH,float bottomOffset)
        {
            var scale=System.Math.Min(maxW/img.Width,maxH/img.Height); var w=img.Width*scale; var h=img.Height*scale;
            g.DrawImage(img,cx-w/2,cy-h+bottomOffset,w,h);
        }

        private static void DrawFallback(Graphics g,PointF c,string id)
        {
            using(var body=new SolidBrush(Color.FromArgb(126,91,64))) g.FillRectangle(body,c.X-23,c.Y-44,46,38);
            using(var roof=new SolidBrush(id=="cannon"?Color.DimGray:Color.FromArgb(174,72,52))) g.FillPolygon(roof,new[]{new PointF(c.X-29,c.Y-42),new PointF(c.X,c.Y-66),new PointF(c.X+29,c.Y-42)});
        }

        private void DrawHud(Graphics g)
        {
            using(var panel=new SolidBrush(Color.FromArgb(224,25,37,44)))
            {
                var profile=new RectangleF(18,18,285,76); DryTheme.FillRound(g,panel,profile,18);
                using(var level=new SolidBrush(Color.FromArgb(73,134,214)))g.FillEllipse(level,30,29,52,52);
                using(var lf=DryTheme.Font(17)) DrawCenter(g,OnlineStateBridge.Level.ToString(),lf,Brushes.White,new RectangleF(30,29,52,52));
                using(var name=DryTheme.Font(17)) g.DrawString(_user.username??"Chief",name,Brushes.White,94,29);
                using(var small=DryTheme.Font(11)) g.DrawString("XP  "+OnlineStateBridge.Xp.ToString("N0"),small,Brushes.Gainsboro,95,58);
            }
            float x=ClientSize.Width-620;
            DrawResource(g,new RectangleF(x,20,190,56),"gold",_state.SaveData.Gold,DryTheme.Gold);
            DrawResource(g,new RectangleF(x+198,20,190,56),"elixir",_state.SaveData.Elixir,DryTheme.Elixir);
            DrawResource(g,new RectangleF(x+396,20,160,56),"gem",_state.SaveData.Gems,DryTheme.Gem);
        }

        private void DrawResource(Graphics g,RectangleF r,string key,int value,Color color)
        {
            using(var bg=new SolidBrush(Color.FromArgb(226,29,42,49))) DryTheme.FillRound(g,bg,r,16);
            Image icon; if(_art.TryGetValue("ui:"+key,out icon)) g.DrawImage(icon,r.X+8,r.Y+8,40,40); else using(var b=new SolidBrush(color))g.FillEllipse(b,r.X+9,r.Y+9,38,38);
            using(var f=DryTheme.Font(17))g.DrawString(value.ToString("N0"),f,Brushes.White,r.X+56,r.Y+18);
        }

        private void DrawNav(Graphics g)
        {
            _nav.Clear();
            var actions=new[]{new[]{"attack","ATTACK"},new[]{"shop","SHOP"},new[]{"army","ARMY"},new[]{"clan","CLAN"},new[]{"profile","PROFILE"}};
            float y=ClientSize.Height-102; float total=actions.Length*150; float start=(ClientSize.Width-total)/2f;
            using(var back=new SolidBrush(Color.FromArgb(232,19,29,34)))g.FillRectangle(back,0,ClientSize.Height-122,ClientSize.Width,122);
            for(int i=0;i<actions.Length;i++)
            {
                var key=actions[i][0]; var r=new RectangleF(start+i*150+6,y,138,72); _nav[key]=r;
                var active=_panel==key;
                using(var b=new SolidBrush(active?Color.FromArgb(94,112,123):Color.FromArgb(55,69,76)))DryTheme.FillRound(g,b,r,15);
                Image icon; if(_art.TryGetValue("ui:"+key,out icon))g.DrawImage(icon,r.X+9,r.Y+11,45,45);
                using(var f=DryTheme.Font(12))g.DrawString(actions[i][1],f,Brushes.White,r.X+58,r.Y+28);
            }
        }

        private void DrawSelection(Graphics g)
        {
            _upgradeRect=RectangleF.Empty;
            if(_selected==null||!string.IsNullOrEmpty(_panel)) return;
            var spec=Buildings.FirstOrDefault(x=>x.Id==_selected.DefinitionId); if(spec==null)return;
            var r=new RectangleF(20,130,300,188); using(var bg=new SolidBrush(Color.FromArgb(232,25,38,44)))DryTheme.FillRound(g,bg,r,18);
            using(var h=DryTheme.Font(20))g.DrawString(spec.Name,h,Brushes.White,40,150);
            using(var s=DryTheme.Font(13))g.DrawString("Level "+_selected.Level+" / "+spec.MaxLevel,s,Brushes.Gainsboro,42,190);
            int cost=UpgradeCost(spec,_selected.Level);
            _upgradeRect=new RectangleF(40,235,255,55); using(var b=new SolidBrush(DryTheme.Gold))DryTheme.FillRound(g,b,_upgradeRect,14);
            using(var f=DryTheme.Font(13))g.DrawString(_selected.Level>=spec.MaxLevel?"MAX LEVEL":"UPGRADE  "+cost.ToString("N0")+" "+spec.Currency,f,Brushes.Black,_upgradeRect.X+16,_upgradeRect.Y+18);
        }

        private void DrawShop(Graphics g)
        {
            var r=new RectangleF(18,120,415,ClientSize.Height-260); Panel(g,r,"BUILD SHOP","Place defenses, economy and army buildings");
            _shopCards.Clear(); float y=r.Y+86;
            foreach(var b in Buildings.Where(x=>x.Id!="townhall"))
            {
                if(y+76>r.Bottom-12)break; var card=new RectangleF(r.X+18,y,r.Width-36,68); _shopCards.Add(Tuple.Create(card,b));
                using(var bg=new SolidBrush(Color.FromArgb(68,82,90)))DryTheme.FillRound(g,bg,card,12);
                Image img; if(_art.TryGetValue(b.Id,out img))g.DrawImage(img,card.X+8,card.Y+8,52,52);
                using(var f=DryTheme.Font(14))g.DrawString(b.Name,f,Brushes.White,card.X+70,card.Y+9);
                using(var sm=DryTheme.Font(11)) using(var cb=new SolidBrush(CurrencyColor(b.Currency)))g.DrawString(b.Cost.ToString("N0")+"  "+b.Currency,sm,cb,card.X+70,card.Y+38);
                y+=76;
            }
        }

        private void DrawArmy(Graphics g)
        {
            var r=new RectangleF(18,120,500,ClientSize.Height-260); Panel(g,r,"ARMY","Recruit troops for raids"); _armyCards.Clear();
            float y=r.Y+86; foreach(var u in Units)
            {
                var card=new RectangleF(r.X+18,y,r.Width-36,68); _armyCards.Add(Tuple.Create(card,u));
                using(var bg=new SolidBrush(Color.FromArgb(68,82,90)))DryTheme.FillRound(g,bg,card,12);
                Image img; if(_art.TryGetValue("unit:"+u.Id,out img))g.DrawImage(img,card.X+8,card.Y+8,52,52);
                using(var f=DryTheme.Font(14))g.DrawString(u.Name+"  x"+_state.SaveData.Units.GetValueOrDefault(u.Id),f,Brushes.White,card.X+70,card.Y+9);
                using(var sm=DryTheme.Font(11))using(var cb=new SolidBrush(CurrencyColor(u.Currency)))g.DrawString("Power "+u.Power+"   •   "+u.Cost.ToString("N0")+" "+u.Currency,sm,cb,card.X+70,card.Y+39);
                y+=76;
            }
        }

        private void DrawClan(Graphics g)
        {
            var r=new RectangleF(22,130,430,330); Panel(g,r,"CLAN KEEP","Realm-wide clan data is stored on the server");
            using(var f=DryTheme.Font(18)) using(var s=DryTheme.Font(13))
            {
                if(_state.SaveData.Clan==null)
                {
                    g.DrawString("No clan yet",f,Brushes.White,r.X+25,r.Y+105); g.DrawString("Click inside this panel to create one.",s,Brushes.Gainsboro,r.X+25,r.Y+145);
                }
                else
                {
                    using(var gb=new SolidBrush(DryTheme.Gold))
                    {
                        g.DrawString(_state.SaveData.Clan.Name,f,Brushes.White,r.X+25,r.Y+105); g.DrawString("["+_state.SaveData.Clan.Tag+"]",s,gb,r.X+25,r.Y+145);
                    }
                    g.DrawString("Leader tools and members sync through irautox.ir.",s,Brushes.Gainsboro,r.X+25,r.Y+185);
                }
            }
        }

        private void DrawProfile(Graphics g)
        {
            var r=new RectangleF(22,130,440,360); Panel(g,r,"CHIEF PROFILE","Online identity and progress");
            using(var h=DryTheme.Font(22))using(var s=DryTheme.Font(13))using(var gb=new SolidBrush(DryTheme.Gold))
            {
                g.DrawString(_user.username??"Chief",h,Brushes.White,r.X+25,r.Y+102);
                g.DrawString(_user.email??"",s,Brushes.Gainsboro,r.X+25,r.Y+142);
                g.DrawString("Level "+OnlineStateBridge.Level+"   •   XP "+OnlineStateBridge.Xp.ToString("N0"),s,gb,r.X+25,r.Y+180);
                g.DrawString("Server: "+AppConfig.ServerBaseUrl,s,Brushes.Gainsboro,r.X+25,r.Y+218);
            }
            _logoutRect=new RectangleF(r.X+25,r.Bottom-75,r.Width-50,48); using(var b=new SolidBrush(DryTheme.Danger))DryTheme.FillRound(g,b,_logoutRect,12);
            using(var f=DryTheme.Font(13))DrawCenter(g,"LOG OUT",f,Brushes.White,_logoutRect);
        }

        private static void Panel(Graphics g,RectangleF r,string title,string sub)
        {
            using(var bg=new SolidBrush(Color.FromArgb(242,27,40,47)))DryTheme.FillRound(g,bg,r,20);
            using(var h=DryTheme.Font(20))g.DrawString(title,h,Brushes.White,r.X+22,r.Y+19);
            using(var s=DryTheme.Font(11))g.DrawString(sub,s,Brushes.Gainsboro,r.X+22,r.Y+54);
        }

        private void DrawBuildHint(Graphics g)
        {
            var r=new RectangleF(ClientSize.Width/2f-230,92,460,55); using(var b=new SolidBrush(Color.FromArgb(235,38,51,58)))DryTheme.FillRound(g,b,r,16);
            using(var f=DryTheme.Font(13))DrawCenter(g,"PLACE "+_buildMode.Name.ToUpperInvariant()+"  •  ESC TO CANCEL",f,Brushes.White,r);
        }

        private void DrawToast(Graphics g)
        {
            if(DateTime.UtcNow>_toastUntil)return; var r=new RectangleF(ClientSize.Width/2f-260,ClientSize.Height-174,520,44);
            using(var b=new SolidBrush(Color.FromArgb(225,17,25,29)))DryTheme.FillRound(g,b,r,13); using(var f=DryTheme.Font(12))DrawCenter(g,_toast,f,Brushes.White,r);
        }

        private void OnMouse(object sender, MouseEventArgs e)
        {
            foreach(var pair in _nav) if(pair.Value.Contains(e.Location)){HandleNav(pair.Key);return;}
            if(!_upgradeRect.IsEmpty&&_upgradeRect.Contains(e.Location)){UpgradeSelected();return;}
            if(!_logoutRect.IsEmpty&&_logoutRect.Contains(e.Location)){Logout();return;}
            if(_panel=="shop")foreach(var c in _shopCards)if(c.Item1.Contains(e.Location)){_buildMode=c.Item2;_panel="";Toast("Choose a free village tile.");Invalidate();return;}
            if(_panel=="army")foreach(var c in _armyCards)if(c.Item1.Contains(e.Location)){Recruit(c.Item2);return;}
            if(_panel=="clan"&&_state.SaveData.Clan==null&&new RectangleF(22,130,430,330).Contains(e.Location)){CreateClan();return;}

            var t=ScreenToTile(e.Location); if(t.Item1<0||t.Item1>=Grid||t.Item2<0||t.Item2>=Grid)return;
            if(_buildMode!=null){Place(_buildMode,t.Item1,t.Item2);return;}
            _selected=_state.SaveData.Buildings.FirstOrDefault(b=>b.X==t.Item1&&b.Y==t.Item2); _panel=""; Invalidate();
        }

        private void HandleNav(string key)
        {
            if(key=="attack"){StartBattle();return;} _buildMode=null; _selected=null; _panel=_panel==key?"":key; Invalidate();
        }

        private void Place(DryBuilding b,int x,int y)
        {
            if(_state.SaveData.Buildings.Any(z=>z.X==x&&z.Y==y)){Toast("That tile is occupied.");return;}
            if(!Spend(b.Currency,b.Cost)){Toast("Not enough "+b.Currency+".");return;}
            _state.SaveData.Buildings.Add(new PlacedBuilding{DefinitionId=b.Id,X=x,Y=y,Level=1}); _buildMode=null; GainXp(18); MarkDirty(); Toast(b.Name+" constructed."); Invalidate();
        }

        private void UpgradeSelected()
        {
            if(_selected==null)return; var b=Buildings.FirstOrDefault(x=>x.Id==_selected.DefinitionId);if(b==null)return;
            if(_selected.Level>=b.MaxLevel){Toast("Maximum level reached.");return;} int cost=UpgradeCost(b,_selected.Level);
            if(!Spend(b.Currency,cost)){Toast("Need "+cost.ToString("N0")+" "+b.Currency+".");return;}
            _selected.Level++;GainXp(25+_selected.Level*4);MarkDirty();Toast(b.Name+" upgraded to level "+_selected.Level+".");Invalidate();
        }

        private void Recruit(DryUnit u)
        {
            if(!_state.SaveData.Buildings.Any(b=>b.DefinitionId=="barracks")){Toast("Build a Barracks first.");return;}
            if(!Spend(u.Currency,u.Cost)){Toast("Not enough "+u.Currency+".");return;}
            _state.SaveData.Units[u.Id]=_state.SaveData.Units.GetValueOrDefault(u.Id)+1;GainXp(4);MarkDirty();Toast(u.Name+" joined your army.");Invalidate();
        }

        private void CreateClan()
        {
            using(var dialog=new ClanCreateForm())
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                try
                {
                    var r=_api.CreateClan(dialog.ClanName,dialog.Tag); if(r!=null&&r.ok&&r.clan!=null){_state.SaveData.Clan=new ClanInfo{Name=r.clan.name,Tag=r.clan.tag};MarkDirty();Toast("Clan created on the server.");}
                    else Toast(r==null?"Clan request failed.":r.error);
                }
                catch(Exception ex){Toast(ex.Message);}
            }
            Invalidate();
        }

        private void StartBattle()
        {
            int count=_state.SaveData.Units.Values.Sum(); if(count<=0){Toast("Recruit troops before attacking.");return;}
            using(var battle=new BattleForm(_state.SaveData.Units,Units,_art))
            {
                battle.ShowDialog(this);
                if(battle.Completed)
                {
                    foreach(var pair in battle.UsedUnits) _state.SaveData.Units[pair.Key]=System.Math.Max(0,_state.SaveData.Units.GetValueOrDefault(pair.Key)-pair.Value);
                    _state.SaveData.Gold=System.Math.Min(9999999,_state.SaveData.Gold+battle.GoldReward);
                    _state.SaveData.Elixir=System.Math.Min(9999999,_state.SaveData.Elixir+battle.ElixirReward);
                    GainXp(battle.XpReward);MarkDirty();Toast("Raid complete: +"+battle.GoldReward.ToString("N0")+" Gold, +"+battle.ElixirReward.ToString("N0")+" Elixir.");
                }
            }
            Invalidate();
        }

        private void Produce()
        {
            int gold=_state.SaveData.Buildings.Where(b=>b.DefinitionId=="goldmine").Sum(b=>2+b.Level*2);
            int elixir=_state.SaveData.Buildings.Where(b=>b.DefinitionId=="elixircollector").Sum(b=>2+b.Level*2);
            if(gold+elixir==0)return; _state.SaveData.Gold=System.Math.Min(9999999,_state.SaveData.Gold+gold); _state.SaveData.Elixir=System.Math.Min(9999999,_state.SaveData.Elixir+elixir);MarkDirty();
        }

        private void GainXp(int amount)
        {
            OnlineStateBridge.Xp+=System.Math.Max(0,amount); int need=OnlineStateBridge.Level*220;
            while(OnlineStateBridge.Xp>=need){OnlineStateBridge.Xp-=need;OnlineStateBridge.Level++;_state.SaveData.Gems=System.Math.Min(1000000,_state.SaveData.Gems+10);need=OnlineStateBridge.Level*220;Toast("Level up! +10 Gems");}
        }

        private bool Spend(Currency c,int amount)
        {
            if(amount<=0)return true; if(c==Currency.Gold&&_state.SaveData.Gold>=amount){_state.SaveData.Gold-=amount;return true;}
            if(c==Currency.Elixir&&_state.SaveData.Elixir>=amount){_state.SaveData.Elixir-=amount;return true;}
            if(c==Currency.Gems&&_state.SaveData.Gems>=amount){_state.SaveData.Gems-=amount;return true;} return false;
        }

        private static int UpgradeCost(DryBuilding b,int level){return (int)System.Math.Round(System.Math.Max(250,b.Cost)*(0.75+level*0.85));}
        private static Color CurrencyColor(Currency c){return c==Currency.Gold?DryTheme.Gold:c==Currency.Elixir?DryTheme.Elixir:DryTheme.Gem;}
        private void MarkDirty(){_dirty=true;_state.Save();}
        private async Task SyncAsync(bool announce)
        {
            if(_syncing)return;_syncing=true;try{await Task.Run(delegate{_api.PutState(_state.SaveData,OnlineStateBridge.Xp,OnlineStateBridge.Level);});_dirty=false;if(announce)Toast("Village synced with server.");}
            catch(Exception ex){if(announce)Toast("Sync failed: "+ex.Message);}finally{_syncing=false;}
        }

        private void Logout()
        {
            try{_api.Logout();}catch{} SessionStore.Clear(); MessageBox.Show("You are signed out. Restart the game to sign in again.","Clash Of Drayven"); Close();
        }
        private void Toast(string text){_toast=text;_toastUntil=DateTime.UtcNow.AddSeconds(4);Invalidate();}
        private static void DrawCenter(Graphics g,string text,Font f,Brush b,RectangleF r){var s=g.MeasureString(text,f);g.DrawString(text,f,b,r.X+(r.Width-s.Width)/2,r.Y+(r.Height-s.Height)/2);}
    }

    internal sealed class ClanCreateForm : Form
    {
        private readonly TextBox _name=new TextBox(); private readonly TextBox _tag=new TextBox(); public string ClanName{get{return _name.Text.Trim();}} public string Tag{get{return _tag.Text.Trim().ToUpperInvariant();}}
        public ClanCreateForm()
        {
            Text="Create Clan";ClientSize=new Size(430,260);StartPosition=FormStartPosition.CenterParent;BackColor=DryTheme.Night;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
            Controls.Add(new Label{Text="CLAN NAME",ForeColor=Color.White,Location=new Point(28,24),AutoSize=true,Font=DryTheme.Font(12)});_name.SetBounds(28,50,365,36);_name.Font=DryTheme.Font(15);Controls.Add(_name);
            Controls.Add(new Label{Text="TAG (2-6)",ForeColor=Color.White,Location=new Point(28,105),AutoSize=true,Font=DryTheme.Font(12)});_tag.SetBounds(28,132,365,36);_tag.Font=DryTheme.Font(15);Controls.Add(_tag);
            var ok=new Button{Text="CREATE ON SERVER",DialogResult=DialogResult.OK,BackColor=DryTheme.Gold,FlatStyle=FlatStyle.Flat,Font=DryTheme.Font(12)};ok.FlatAppearance.BorderSize=0;ok.SetBounds(28,195,365,45);Controls.Add(ok);AcceptButton=ok;
        }
    }
}
