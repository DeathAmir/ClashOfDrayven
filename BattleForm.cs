using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ClashOfDrayven
{
    internal sealed class BattleForm : Form
    {
        private readonly Dictionary<string,int> _available;
        private readonly DryUnit[] _units;
        private readonly Dictionary<string,Image> _art;
        private readonly Dictionary<string,int> _deployed=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Tuple<RectangleF,DryUnit>> _cards=new List<Tuple<RectangleF,DryUnit>>();
        private readonly Timer _timer=new Timer{Interval=500};
        private readonly Random _random=new Random();
        private DryUnit _selected;
        private double _enemyHp=1700;
        private readonly double _enemyMax=1700;
        private int _seconds=55;
        public bool Completed{get;private set;}
        public int GoldReward{get;private set;}
        public int ElixirReward{get;private set;}
        public int XpReward{get;private set;}
        public Dictionary<string,int> UsedUnits{get{return new Dictionary<string,int>(_deployed);}}

        public BattleForm(Dictionary<string,int> army,DryUnit[] units,Dictionary<string,Image> art)
        {
            _available=new Dictionary<string,int>(army,StringComparer.OrdinalIgnoreCase);_units=units;_art=art;
            Text="Clash Of Drayven — Raid";ClientSize=new Size(1220,760);MinimumSize=new Size(1050,680);StartPosition=FormStartPosition.CenterParent;BackColor=DryTheme.Night;DoubleBuffered=true;
            _selected=_units.FirstOrDefault(u=>_available.GetValueOrDefault(u.Id)>0);
            _timer.Tick+=delegate{Step();};_timer.Start();MouseDown+=OnMouse;
        }

        protected override void Dispose(bool disposing){if(disposing)_timer.Dispose();base.Dispose(disposing);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            using(var sky=new LinearGradientBrush(ClientRectangle,Color.FromArgb(118,179,192),Color.FromArgb(53,83,71),90))g.FillRectangle(sky,ClientRectangle);
            DrawEnemy(g);DrawTop(g);DrawArmy(g);
        }

        private void DrawEnemy(Graphics g)
        {
            var field=new RectangleF(110,105,ClientSize.Width-220,ClientSize.Height-275);using(var grass=new SolidBrush(Color.FromArgb(89,150,72)))DryTheme.FillRound(g,grass,field,22);
            var ids=new[]{"townhall","cannon","archertower","mortar","goldstorage","elixirstorage","wall"};
            for(int i=0;i<12;i++)
            {
                var id=ids[i%ids.Length];var x=field.X+100+(i*137)%((int)field.Width-180);var y=field.Y+105+(i*83)%((int)field.Height-155);
                Image img;if(_art.TryGetValue(id,out img)){var sc=System.Math.Min(78f/img.Width,78f/img.Height);g.DrawImage(img,x-img.Width*sc/2,y-img.Height*sc,img.Width*sc,img.Height*sc);}else using(var b=new SolidBrush(Color.FromArgb(130,85,59)))g.FillRectangle(b,x-22,y-45,44,42);
            }
            foreach(var pair in _deployed)
            {
                var u=_units.FirstOrDefault(x=>x.Id==pair.Key);if(u==null)continue;Image img;if(!_art.TryGetValue("unit:"+u.Id,out img))continue;
                for(int i=0;i<System.Math.Min(pair.Value,7);i++){float x=field.X+40+(pair.Key.GetHashCode()+i*79&0x7fffffff)%((int)field.Width-80);float y=field.Bottom-45-(i%3)*24;g.DrawImage(img,x,y,36,36);}
            }
        }

        private void DrawTop(Graphics g)
        {
            using(var bg=new SolidBrush(Color.FromArgb(225,24,36,42)))DryTheme.FillRound(g,bg,new RectangleF(22,18,ClientSize.Width-44,68),18);
            using(var f=DryTheme.Font(20))g.DrawString("RAID THE RIVAL STRONGHOLD",f,Brushes.White,45,39);
            using(var s=DryTheme.Font(15))g.DrawString(_seconds+"s",s,Brushes.White,ClientSize.Width-100,42);
            var hp=new RectangleF(400,44,ClientSize.Width-560,16);using(var b=new SolidBrush(Color.FromArgb(80,255,255,255)))DryTheme.FillRound(g,b,hp,8);
            var pct=(float)System.Math.Max(0,_enemyHp/_enemyMax);using(var b=new SolidBrush(DryTheme.Danger))DryTheme.FillRound(g,b,new RectangleF(hp.X,hp.Y,hp.Width*pct,hp.Height),8);
        }

        private void DrawArmy(Graphics g)
        {
            var y=ClientSize.Height-142;using(var bg=new SolidBrush(Color.FromArgb(235,20,30,35)))g.FillRectangle(bg,0,y-15,ClientSize.Width,157);_cards.Clear();float x=28;
            foreach(var u in _units)
            {
                var r=new RectangleF(x,y,132,96);_cards.Add(Tuple.Create(r,u));bool active=_selected==u;using(var b=new SolidBrush(active?Color.FromArgb(107,124,132):Color.FromArgb(57,70,77)))DryTheme.FillRound(g,b,r,14);
                Image img;if(_art.TryGetValue("unit:"+u.Id,out img))g.DrawImage(img,r.X+8,r.Y+8,48,48);using(var f=DryTheme.Font(10))g.DrawString(u.Name,f,Brushes.White,r.X+60,r.Y+12);
                int left=_available.GetValueOrDefault(u.Id)-_deployed.GetValueOrDefault(u.Id);using(var s=DryTheme.Font(11))using(var gb=new SolidBrush(DryTheme.Gold))g.DrawString("x"+System.Math.Max(0,left),s,gb,r.X+60,r.Y+40);using(var p=DryTheme.Font(9))g.DrawString("PWR "+u.Power,p,Brushes.Gainsboro,r.X+12,r.Bottom-24);x+=140;if(x+132>ClientSize.Width-20)break;
            }
        }

        private void OnMouse(object sender,MouseEventArgs e)
        {
            foreach(var c in _cards)if(c.Item1.Contains(e.Location)){_selected=c.Item2;Invalidate();return;}
            if(e.Y<105||e.Y>ClientSize.Height-170||_selected==null)return;int used=_deployed.GetValueOrDefault(_selected.Id);int have=_available.GetValueOrDefault(_selected.Id);if(used>=have)return;
            _deployed[_selected.Id]=used+1;Invalidate();
        }

        private void Step()
        {
            _seconds--;int power=0;foreach(var pair in _deployed){var u=_units.FirstOrDefault(x=>x.Id==pair.Key);if(u!=null)power+=u.Power*pair.Value;}
            if(power>0)_enemyHp-=System.Math.Max(2,power*(0.045+_random.NextDouble()*0.025));
            if(_enemyHp<=0){Finish(true);return;}if(_seconds<=0){Finish(_enemyHp<_enemyMax*0.55);return;}Invalidate();
        }

        private void Finish(bool success)
        {
            _timer.Stop();double damage=1.0-System.Math.Max(0,_enemyHp)/_enemyMax;Completed=true;GoldReward=(int)(1200+damage*5200);ElixirReward=(int)(900+damage*4300);XpReward=(int)(35+damage*120);
            MessageBox.Show(success?"Victory! Rival base crushed.":"Raid ended. You still recovered part of the loot.","Clash Of Drayven",MessageBoxButtons.OK,success?MessageBoxIcon.Information:MessageBoxIcon.Warning);Close();
        }
    }
}
