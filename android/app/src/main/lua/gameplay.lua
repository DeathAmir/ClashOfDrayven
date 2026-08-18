local M = {}

local units = {
  vanguard={speed=.135,range=.052,damage=20.0,delay=.70},
  ranger={speed=.132,range=.120,damage=25.5,delay=.70},
  rogue={speed=.225,range=.052,damage=29.0,delay=.43},
  breaker={speed=.138,range=.054,damage=42.0,delay=.76},
  brute={speed=.092,range=.056,damage=51.0,delay=1.10},
  mage={speed=.130,range=.122,damage=65.0,delay=.72},
  healer={speed=.128,range=.118,damage=23.0,delay=.66},
  stormcaller={speed=.162,range=.132,damage=118.0,delay=.78}
}

local defenses = {
  cannon={range=.20,damage=17,delay=.95},
  archertower={range=.25,damage=14,delay=.72},
  mortar={range=.31,damage=30,delay=1.80},
  airdefense={range=.28,damage=24,delay=1.20},
  clancastle={range=.24,damage=19,delay=.92}
}

local base_cost = {
  townhall=6500,goldmine=800,elixircollector=850,goldstorage=1200,elixirstorage=1250,
  barracks=1200,armycamp=1500,cannon=1100,archertower=1600,mortar=2400,
  airdefense=3100,wall=180,clancastle=5000
}

local tips = {
  "آیا می‌دانستید؟ دفاع‌های نزدیک به تالار درایون معمولاً ارزش بیشتری دارند.",
  "آیا می‌دانستید؟ جم برای قهرمان‌ها و خریدهای ویژه نگه داشته شده است.",
  "آیا می‌دانستید؟ ترکیب نیروهای دوربرد و تانک‌ها در حمله پایدارتر است.",
  "آیا می‌دانستید؟ ارتقای مخزن‌ها سقف منابع قابل نگهداری را بالا می‌برد.",
  "آیا می‌دانستید؟ امتیاز نبرد رتبه جهانی فرمانده را تعیین می‌کند.",
  "آیا می‌دانستید؟ بازیکنان آفلاین هم می‌توانند به‌عنوان دهکده دفاعی پیدا شوند.",
  "آیا می‌دانستید؟ پیام‌های چت قبل از ارسال در کلاینت و دوباره روی سرور فیلتر می‌شوند."
}

function M.startup()
  if jit and jit.opt then
    jit.opt.start("hotloop=20","hotexit=6","maxtrace=2000","maxrecord=6000")
  end
  local s=0
  for i=1,800 do s=s+(i%17) end
  return s
end

function M.production(ids, levels)
  local gold, elixir = 0, 0
  for i=1,#ids do
    local id=ids[i]
    local level=math.max(1, tonumber(levels[i]) or 1)
    if id=="goldmine" then gold=gold+2+level*2
    elseif id=="elixircollector" then elixir=elixir+2+level*2 end
  end
  return gold,elixir
end

function M.gain_xp(xp,level,gems,amount)
  xp=math.max(0,tonumber(xp) or 0)+math.max(0,tonumber(amount) or 0)
  level=math.max(1,tonumber(level) or 1)
  gems=math.max(0,tonumber(gems) or 0)
  while xp>=level*220 do xp=xp-level*220 level=level+1 gems=gems+10 end
  return xp,level,gems
end

function M.unit_combat(id)
  local u=units[id] or units.vanguard
  return u.speed,u.range,u.damage,u.delay
end

function M.building_combat(id,level)
  local d=defenses[id]
  if not d then return 0,0,0 end
  level=math.max(1,tonumber(level) or 1)
  return d.range,d.damage*(1+(level-1)*.11),math.max(.35,d.delay-(level-1)*.015)
end

function M.battle_stars(destruction,townhall)
  destruction=math.max(0,math.min(100,tonumber(destruction) or 0))
  local s=0
  if destruction>=50 then s=s+1 end
  if townhall then s=s+1 end
  if destruction>=100 then s=s+1 end
  return math.min(3,s)
end

function M.loot_preview(level)
  level=math.max(1,tonumber(level) or 1)
  return 1200+level*130,1000+level*110
end

function M.battle_reward(level,destruction,stars)
  local g,e=M.loot_preview(level)
  destruction=math.max(0,math.min(100,tonumber(destruction) or 0))
  stars=math.max(0,math.min(3,tonumber(stars) or 0))
  return math.floor(g*(destruction/100)+stars*180),math.floor(e*(destruction/100)+stars*150)
end

function M.upgrade_cost(id,level)
  local b=base_cost[id]
  if not b then return -1 end
  level=math.max(1,tonumber(level) or 1)
  local max=id=="wall" and 15 or 12
  if id=="clancastle" then max=8 end
  if level>=max then return -1 end
  return math.max(100,math.floor(b*(1.72^(level-1))))
end

function M.rank_name(points)
  points=math.max(0,tonumber(points) or 0)
  if points>=6200 then return "اسطوره درایون" end
  if points>=5000 then return "تایتان" end
  if points>=4100 then return "قهرمان" end
  if points>=3200 then return "استاد" end
  if points>=2500 then return "کریستال" end
  if points>=1800 then return "طلا" end
  if points>=1200 then return "نقره" end
  return "برنز"
end

function M.loading_tip(seed)
  seed=math.abs(tonumber(seed) or 0)
  return tips[(seed % #tips)+1]
end

function M.loading_progress(phase)
  phase=math.max(0,math.min(12,tonumber(phase) or 0))
  local p={3,9,16,25,34,44,55,66,77,86,93,98,100}
  return p[phase+1]
end

for k,v in pairs(M) do _G["drayven_"..k]=v end
M.startup()
return M
