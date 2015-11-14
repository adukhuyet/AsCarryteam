using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Jinx_Genesis
{
    class Program
    {
        private static string ChampionName = "Jinx";

        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;

        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static Spell Q, W, E, R;
        private static float QMANA, WMANA, EMANA, RMANA;
        private static bool FishBoneActive = false, Combo = false, Farm = false;
        private static Obj_AI_Hero blitz = null;
        private static float WCastTime = Game.Time;

        private static string[] Spells =
        {
            "katarinar","drain","consume","absolutezero", "staticfield","reapthewhirlwind","jinxw","jinxr","shenstandunited","threshe","threshrpenta","threshq","meditate","caitlynpiltoverpeacemaker", "volibearqattack",
            "cassiopeiapetrifyinggaze","ezrealtrueshotbarrage","galioidolofdurand","luxmalicecannon", "missfortunebullettime","infiniteduress","alzaharnethergrasp","lucianq","velkozr","rocketgrabmissile"
        };

        private static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>();

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampionName) return;

            LoadMenu();

            Q = new Spell(SpellSlot.Q, Player.AttackRange);
            W = new Spell(SpellSlot.W, 1490f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 2500f);

            W.SetSkillshot(0.6f, 75f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.2f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero);
                }
                else if (hero.ChampionName.Equals("Blitzcrank"))
                {
                    blitz = hero;
                }
            }

            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.PrintChat("<font color=\"#00BFFF\">GENESIS </font>Jinx<font color=\"#000000\">  </font> - <font color=\"#FFFFFF\">Loaded</font>");
        }

        private static void LoadMenu()
        {
            Config = new Menu(ChampionName + " ProJinx", ChampionName + " ProJinx", true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("Chiến", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.AddToMainMenu();

            Config.SubMenu("Hiển Thị").AddItem(new MenuItem("qRange", "Tầm đánh Q").SetValue(false));
            Config.SubMenu("Hiển Thị").AddItem(new MenuItem("wRange", "Tầm đánh W").SetValue(false));
            Config.SubMenu("Hiển Thị").AddItem(new MenuItem("eRange", "Tầm đánh E").SetValue(false));
            Config.SubMenu("Hiển Thị").AddItem(new MenuItem("rRange", "Tầm đánh R").SetValue(false));
            Config.SubMenu("Hiển Thị").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));

            Config.SubMenu("Cài Đặt Q").AddItem(new MenuItem("Qcombo", "Đánh nhau bằng  Q").SetValue(true));
            Config.SubMenu("Cài Đặt Q").AddItem(new MenuItem("Qharass", "Cấu rỉa Q").SetValue(true));
            Config.SubMenu("Cài Đặt Q").AddItem(new MenuItem("farmQout", "Đổi súng nếu ngoài tầm").SetValue(true));
            Config.SubMenu("Cài Đặt Q").AddItem(new MenuItem("Qlaneclear", "Q khi  số lượng  lính (X) = ").SetValue(new Slider(4, 10, 2)));
            Config.SubMenu("Cài Đặt Q").AddItem(new MenuItem("Qchange", "Q Đổi súng lớn thành súng nhỏ").SetValue(new StringList(new[] { "Thời gian thực", "Trước khi AA" }, 1)));
            Config.SubMenu("Cài Đặt Q").AddItem(new MenuItem("Qaoe", "Luôn Q khi trúng (X) mục tiêu = ").SetValue(new Slider(3, 5, 0)));
            Config.SubMenu("Cài Đặt Q").AddItem(new MenuItem("QmanaIgnore", "Lượng mana cho phép trong (x) AA").SetValue(new Slider(4, 10, 0)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu("Cài Đặt Q").SubMenu("Cấu rỉa bằng Q :").AddItem(new MenuItem("harasQ" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu("Cài Đặt W").AddItem(new MenuItem("Wcombo", "Dùng W trong combo").SetValue(true));
            Config.SubMenu("Cài Đặt W").AddItem(new MenuItem("Wharass", " Dùng W cấu rỉa").SetValue(true));
            Config.SubMenu("Cài Đặt W").AddItem(new MenuItem("Wks", "W KS").SetValue(true));
            Config.SubMenu("Cài Đặt W").AddItem(new MenuItem("Wts", "Chế độ cấu rỉa").SetValue(new StringList(new[] { "Lựa đối tượng", "Tất cả trong tầm đánh" }, 0)));
            Config.SubMenu("Cài Đặt W").AddItem(new MenuItem("Wmode", "Chế độ W").SetValue(new StringList(new[] { "Ngoài tầm súng nhỏ", "Ngoài tầm giật bắn", "Tùy chỉnh tầm đánh" }, 0)));
            Config.SubMenu("Cài Đặt W").AddItem(new MenuItem("Wcustome", "Tùy chỉnh tầm đánh nhỏ nhất").SetValue(new Slider(600, 1500, 0)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu("Cài Đặt W").SubMenu("Dùng W cấu rỉa :").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu("Cài Đặt E").AddItem(new MenuItem("Ecombo", "Dùng E Combo").SetValue(true));
            Config.SubMenu("Cài Đặt E").AddItem(new MenuItem("Etel", "Dùng E khi đối phương dịch chuyển").SetValue(true));
            Config.SubMenu("Cài Đặt E").AddItem(new MenuItem("Ecc", "Dùng E khi CC").SetValue(true));
            Config.SubMenu("Cài Đặt E").AddItem(new MenuItem("Eslow", "E khi bị làm chậm").SetValue(true));
            Config.SubMenu("Cài Đặt E").AddItem(new MenuItem("Edash", "E Khi hất tung").SetValue(true));
            Config.SubMenu("Cài Đặt E").AddItem(new MenuItem("Espell", "E khi trúng chiu").SetValue(true));
            Config.SubMenu("Cài Đặt E").AddItem(new MenuItem("Eaoe", "E nếu trúng mục tiêu (X) = ").SetValue(new Slider(3, 5, 0)));
            Config.SubMenu("Cài Đặt E").SubMenu("E Gap Closer").AddItem(new MenuItem("EmodeGC", "E ngay bị trí bị tấn công").SetValue(new StringList(new[] { "Dash end position", "Jinx position" }, 0)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu("Cài Đặt E").SubMenu("E Gap Closer").SubMenu("Cast on enemy:").AddItem(new MenuItem("EGCchampion" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu("Cài Đặt R").AddItem(new MenuItem("Rks", "R KS").SetValue(true));
            Config.SubMenu("Cài Đặt R").SubMenu("Tùy Chỉnh R bởi phím").AddItem(new MenuItem("useR", "Phím bắn Ultimate").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space
            Config.SubMenu("Cài Đặt R").SubMenu("Tùy Chỉnh R bởi phím").AddItem(new MenuItem("semiMode", "Chế độ dùng R").SetValue(new StringList(new[] { "Mục tiêu ít máu", "Trúng nhiều" }, 0)));
            Config.SubMenu("Cài Đặt R").AddItem(new MenuItem("Rmode", "R mode").SetValue(new StringList(new[] { "Ngoài tầm súng nhỏ ", "Ngoài tầm giật bắn ", "Tùy chỉnh " }, 0)));
            Config.SubMenu("Cài Đặt R").AddItem(new MenuItem("Rcustome", "Tầm R nhỏ nhất").SetValue(new Slider(1000, 1600, 0)));
            Config.SubMenu("Cài Đặt R").AddItem(new MenuItem("RcustomeMax", "Dùng R trong tầm ").SetValue(new Slider(3000, 20000, 0)));
            Config.SubMenu("Cài Đặt R").AddItem(new MenuItem("Raoe", "R nếu trúng X mục tiêu và có thể giết").SetValue(new Slider(2, 5, 0)));
            Config.SubMenu("Cài Đặt R").SubMenu("Tối ưu hóa ulti").AddItem(new MenuItem("Rover", "Không dùng R khi có (X) ở gần").SetValue(new Slider(500, 1000, 0)));
            Config.SubMenu("Cài Đặt R").SubMenu("Tối ưu hóa ulti").AddItem(new MenuItem("RoverAA", "Không Dùng R khi có thể giết thường").SetValue(true));
            Config.SubMenu("Cài Đặt R").SubMenu("Tối ưu hóa ulti").AddItem(new MenuItem("RoverW", "Không R nếu giết được bằng W").SetValue(true));

      
            Config.SubMenu("Quản lý Mana").AddItem(new MenuItem("QmanaCombo", "Q trong combo").SetValue(new Slider(20, 100, 0)));
            Config.SubMenu("Quản lý Mana").AddItem(new MenuItem("QmanaHarass", "Q trong cấu rỉa").SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("Quản lý Mana").AddItem(new MenuItem("QmanaLC", "Q đẩy đường").SetValue(new Slider(80, 100, 0)));
            Config.SubMenu("Quản lý Mana").AddItem(new MenuItem("WmanaCombo", "W trong combo").SetValue(new Slider(20, 100, 0)));
            Config.SubMenu("Quản lý Mana").AddItem(new MenuItem("WmanaHarass", "W trong cấu rỉa").SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("Quản lý Mana").AddItem(new MenuItem("EmanaCombo", "E mana").SetValue(new Slider(20, 100, 0)));

            Config.SubMenu("Dự đoán hack não").AddItem(new MenuItem("PredictionMODE", "Hack não đối phương").SetValue(new StringList(new[] { "Common prediction", "Custom PREDICTION" }, 1)));
            Config.SubMenu("Dự đoán hack não").AddItem(new MenuItem("Wpred", "W dự đoán").SetValue(new StringList(new[] { "Rất nhanh W", "Nhanh W" }, 0)));
            Config.SubMenu("Dự đoán hack não").AddItem(new MenuItem("Epred", "E dự đoán").SetValue(new StringList(new[] { "Rất nhanh E", "Nhanh E" }, 0)));
            Config.SubMenu("Dự đoán hack não").AddItem(new MenuItem("Rpred", "R dự đoán").SetValue(new StringList(new[] { "Rất nhanh R", "Nhanh R" }, 0)));

            Config.SubMenu("Cài đặt cấu rỉa").AddItem(new MenuItem("LaneClearHarass", "Cấu rỉa & đẩy đường").SetValue(true));
            Config.SubMenu("Cài đặt cấu rỉa").AddItem(new MenuItem("LastHitHarass", "LastHit và cấu rỉa").SetValue(true));
            Config.SubMenu("Cài đặt cấu rỉa").AddItem(new MenuItem("MixedHarass", "Cấu rỉa trong tầm đánh").SetValue(true));

           
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.ManaPercent < Config.Item("EmanaCombo").GetValue<Slider>().Value)
                return;

            if (E.IsReady())
            {
                var t = gapcloser.Sender;
                if (t.IsValidTarget(E.Range) && Config.Item("EGCchampion" + t.ChampionName).GetValue<bool>())
                {
                    if (Config.Item("EmodeGC").GetValue<StringList>().SelectedIndex == 0)
                        E.Cast(gapcloser.End);
                    else
                        E.Cast(Player.ServerPosition);
                }
            }
        }

        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (!FishBoneActive)
                return;

            if (Q.IsReady() && args.Target is Obj_AI_Hero && Config.Item("Qchange").GetValue<StringList>().SelectedIndex == 1)
            {
                var t = (Obj_AI_Hero)args.Target;
                if (t.IsValidTarget())
                {
                    FishBoneToMiniGun(t);
                }
            }

            if (!Combo && args.Target is Obj_AI_Minion)
            {
                var t = (Obj_AI_Minion)args.Target;
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Player.ManaPercent > Config.Item("QmanaLC").GetValue<Slider>().Value && CountMinionsInRange(250, t.Position) >= Config.Item("Qlaneclear").GetValue<Slider>().Value)
                {

                }
                else if (GetRealDistance(t) < GetRealPowPowRange(t))
                {
                    args.Process = false;
                    if (Q.IsReady())
                        Q.Cast();
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            if (sender.IsMinion)
                return;

            if (sender.IsMe)
            {
                if (args.SData.Name == "JinxWMissile")
                    WCastTime = Game.Time;
            }

            if (!E.IsReady() || !sender.IsEnemy || !Config.Item("Espell").GetValue<bool>() || Player.ManaPercent < Config.Item("EmanaCombo").GetValue<Slider>().Value || !sender.IsValid<Obj_AI_Hero>() || !sender.IsValidTarget(E.Range))
                return;

            var foundSpell = Spells.Find(x => args.SData.Name.ToLower() == x);
            if (foundSpell != null)
            {
                E.Cast(sender.Position);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            SetValues();

            if (Q.IsReady())
                Qlogic();
            if (W.IsReady())
                Wlogic();
            if (E.IsReady())
                Elogic();
            if (R.IsReady())
                Rlogic();
        }

        private static void Rlogic()
        {
            R.Range = Config.Item("RcustomeMax").GetValue<Slider>().Value;

            if (Config.Item("useR").GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (Config.Item("semiMode").GetValue<StringList>().SelectedIndex == 0)
                    {
                        R.Cast(t);
                    }
                    else
                    {
                        R.CastIfWillHit(t, 2);
                        R.Cast(t, true, true);
                    }
                }
            }

            if (Config.Item("Rks").GetValue<bool>())
            {
                bool cast = false;


                if (Config.Item("RoverAA").GetValue<bool>() && (!Orbwalking.CanAttack() || Player.IsWindingUp))
                    return;

                foreach (var target in Enemies.Where(target => target.IsValidTarget(R.Range) && ValidUlt(target)))
                {

                    float predictedHealth = target.Health + target.HPRegenRate * 2;
                    var Rdmg = R.GetDamage(target, 1);

                    if (Rdmg > predictedHealth)
                    {
                        cast = true;
                        PredictionOutput output = R.GetPrediction(target);
                        Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                        direction.Normalize();

                        foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget()))
                        {
                            if (enemy.SkinName == target.SkinName || !cast)
                                continue;
                            PredictionOutput prediction = R.GetPrediction(enemy);
                            Vector3 predictedPosition = prediction.CastPosition;
                            Vector3 v = output.CastPosition - Player.ServerPosition;
                            Vector3 w = predictedPosition - Player.ServerPosition;
                            double c1 = Vector3.Dot(w, v);
                            double c2 = Vector3.Dot(v, v);
                            double b = c1 / c2;
                            Vector3 pb = Player.ServerPosition + ((float)b * v);
                            float length = Vector3.Distance(predictedPosition, pb);
                            if (length < (R.Width + 150 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                cast = false;
                        }

                        if (cast)
                        {
                            if (Config.Item("RoverW").GetValue<bool>() && target.IsValidTarget(W.Range) && W.GetDamage(target) > target.Health && W.Instance.Cooldown - (W.Instance.CooldownExpires - Game.Time) < 1.1)
                                return;

                            if (target.CountEnemiesInRange(400) > Config.Item("Raoe").GetValue<Slider>().Value)
                                CastSpell(R, target);

                            if (RValidRange(target) && target.CountAlliesInRange(Config.Item("Rover").GetValue<Slider>().Value) == 0)
                                CastSpell(R, target);
                        }
                    }
                }
            }
        }

        private static bool RValidRange(Obj_AI_Base t)
        {
            var range = GetRealDistance(t);

            if (Config.Item("Rmode").GetValue<StringList>().SelectedIndex == 0)
            {
                if (range > GetRealPowPowRange(t))
                    return true;
                else
                    return false;

            }
            else if (Config.Item("Rmode").GetValue<StringList>().SelectedIndex == 1)
            {
                if (range > Q.Range)
                    return true;
                else
                    return false;
            }
            else if (Config.Item("Rmode").GetValue<StringList>().SelectedIndex == 2)
            {
                if (range > Config.Item("Rcustome").GetValue<Slider>().Value && !Orbwalking.InAutoAttackRange(t))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private static void Elogic()
        {
            if (Player.ManaPercent < Config.Item("EmanaCombo").GetValue<Slider>().Value)
                return;

            if (blitz != null && blitz.Distance(Player.Position) < E.Range)
            {
                foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(2000) && enemy.HasBuff("RocketGrab")))
                {
                    E.Cast(blitz.Position.Extend(enemy.Position, 30));
                    return;
                }
            }

            foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(E.Range)))
            {

                E.CastIfWillHit(enemy, Config.Item("Eaoe").GetValue<Slider>().Value);

                if (Config.Item("Ecc").GetValue<bool>())
                {
                    if (!CanMove(enemy))
                        E.Cast(enemy.Position);
                    E.CastIfHitchanceEquals(enemy, HitChance.Immobile);
                }

                if (enemy.MoveSpeed < 250 && Config.Item("Eslow").GetValue<bool>())
                    E.Cast(enemy);
                if (Config.Item("Edash").GetValue<bool>())
                    E.CastIfHitchanceEquals(enemy, HitChance.Dashing);
            }


            if (Config.Item("Etel").GetValue<bool>())
            {
                foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.IsEnemy && Obj.Distance(Player.ServerPosition) < E.Range && (Obj.HasBuff("teleport_target", true) || Obj.HasBuff("Pantheon_GrandSkyfall_Jump", true))))
                {
                    E.Cast(Object.Position);
                }
            }

            if (Combo && Player.IsMoving && Config.Item("Ecombo").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(E.Range) && E.GetPrediction(t).CastPosition.Distance(t.Position) > 200)
                {
                    if (Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                    {
                        if (t.Position.Distance(Player.ServerPosition) < t.Position.Distance(Player.Position))
                            CastSpell(E, t);
                    }
                    else
                    {
                        if (t.Position.Distance(Player.ServerPosition) > t.Position.Distance(Player.Position))
                            CastSpell(E, t);
                    }
                }
            }
        }

        private static bool WValidRange(Obj_AI_Base t)
        {
            var range = GetRealDistance(t);

            if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 0)
            {
                if (range > GetRealPowPowRange(t) && Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                    return true;
                else
                    return false;

            }
            else if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 1)
            {
                if (range > Q.Range + 50 && Player.CountEnemiesInRange(Q.Range + 50) == 0)
                    return true;
                else
                    return false;
            }
            else if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 2)
            {
                if (range > Config.Item("Wcustome").GetValue<Slider>().Value && Player.CountEnemiesInRange(Config.Item("Wcustome").GetValue<Slider>().Value) == 0)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private static void Wlogic()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget() && WValidRange(t))
            {
                if (Config.Item("Wks").GetValue<bool>() && GetKsDamage(t, W) > t.Health && ValidUlt(t))
                {
                    CastSpell(W, t);
                }

                if (Combo && Config.Item("Wcombo").GetValue<bool>() && Player.ManaPercent > Config.Item("WmanaCombo").GetValue<Slider>().Value)
                {
                    CastSpell(W, t);
                }
                else if (Farm && Orbwalking.CanAttack() && !Player.IsWindingUp && Config.Item("Wharass").GetValue<bool>() && Player.ManaPercent > Config.Item("WmanaHarass").GetValue<Slider>().Value)
                {
                    if (Config.Item("Wts").GetValue<StringList>().SelectedIndex == 0)
                    {
                        if (Config.Item("haras" + t.ChampionName).GetValue<bool>())
                            CastSpell(W, t);
                    }
                    else
                    {
                        foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && WValidRange(t) && Config.Item("haras" + enemy.ChampionName).GetValue<bool>()))
                            CastSpell(W, enemy);
                    }
                }

            }
        }

        private static void Qlogic()
        {
            if (FishBoneActive)
            {
                var orbT = Orbwalker.GetTarget();
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Player.ManaPercent > Config.Item("QmanaLC").GetValue<Slider>().Value && orbT.IsValid<Obj_AI_Minion>())
                {

                }
                else if (Config.Item("Qchange").GetValue<StringList>().SelectedIndex == 0 && orbT.IsValid<Obj_AI_Hero>())
                {
                    var t = (Obj_AI_Hero)Orbwalker.GetTarget();
                    FishBoneToMiniGun(t);
                }
                else
                {
                    if (!Combo && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None)
                        Q.Cast();
                }
            }
            else
            {
                var t = TargetSelector.GetTarget(Q.Range + 40, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if ((!Orbwalking.InAutoAttackRange(t) || t.CountEnemiesInRange(250) >= Config.Item("Qaoe").GetValue<Slider>().Value))
                    {
                        if (Combo && Config.Item("Qcombo").GetValue<bool>() && (Player.ManaPercent > Config.Item("QmanaCombo").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value > t.Health))
                        {
                            Q.Cast();
                        }
                        if (Farm && Orbwalking.CanAttack() && !Player.IsWindingUp && Config.Item("harasQ" + t.ChampionName).GetValue<bool>() && Config.Item("Qharass").GetValue<bool>() && (Player.ManaPercent > Config.Item("QmanaHarass").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value > t.Health))
                        {
                            Q.Cast();
                        }
                    }
                }
                else
                {
                    if (Combo && Player.ManaPercent > Config.Item("QmanaCombo").GetValue<Slider>().Value)
                    {
                        Q.Cast();
                    }
                    else if (Farm && !Player.IsWindingUp && Config.Item("farmQout").GetValue<bool>() && Orbwalking.CanAttack())
                    {
                        foreach (var minion in MinionManager.GetMinions(Q.Range + 30).Where(
                        minion => !Orbwalking.InAutoAttackRange(minion) && minion.Health < Player.GetAutoAttackDamage(minion) * 1.2 && GetRealPowPowRange(minion) < GetRealDistance(minion) && Q.Range < GetRealDistance(minion)))
                        {
                            Orbwalker.ForceTarget(minion);
                            Q.Cast();
                            return;
                        }
                    }
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Player.ManaPercent > Config.Item("QmanaLC").GetValue<Slider>().Value)
                    {
                        var orbT = Orbwalker.GetTarget();
                        if (orbT.IsValid<Obj_AI_Minion>() && CountMinionsInRange(250, orbT.Position) >= Config.Item("Qlaneclear").GetValue<Slider>().Value)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static int CountMinionsInRange(float range, Vector3 pos)
        {
            var minions = MinionManager.GetMinions(pos, range);
            int count = 0;
            foreach (var minion in minions)
            {
                count++;
            }
            return count;
        }

        public static float GetKsDamage(Obj_AI_Base t, Spell QWER)
        {
            var totalDmg = QWER.GetDamage(t);

            if (Player.HasBuff("summonerexhaust"))
                totalDmg = totalDmg * 0.6f;

            if (t.HasBuff("ferocioushowl"))
                totalDmg = totalDmg * 0.7f;

            if (t is Obj_AI_Hero)
            {
                var champion = (Obj_AI_Hero)t;
                if (champion.ChampionName == "Blitzcrank" && !champion.HasBuff("BlitzcrankManaBarrierCD") && !champion.HasBuff("ManaBarrier"))
                {
                    totalDmg -= champion.Mana / 2f;
                }
            }

            var extraHP = t.Health - HealthPrediction.GetHealthPrediction(t, 500);

            totalDmg += extraHP;
            totalDmg -= t.HPRegenRate;
            totalDmg -= t.PercentLifeStealMod * 0.005f * t.FlatPhysicalDamageMod;

            return totalDmg;
        }

        public static bool ValidUlt(Obj_AI_Base target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity)
                || target.HasBuffOfType(BuffType.SpellImmunity)
                || target.IsZombie
                || target.IsInvulnerable
                || target.HasBuffOfType(BuffType.Invulnerability)
                || target.HasBuffOfType(BuffType.SpellShield)
                || target.HasBuff("deathdefiedbuff")
                || target.HasBuff("Undying Rage")
                || target.HasBuff("Chrono Shift")
                )
                return false;
            else
                return true;
        }

        private static bool CanMove(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned || target.IsChannelingImportantSpell() || target.MoveSpeed < 50f)
            {
                return false;
            }
            else
                return true;
        }

        private static void CastSpell(Spell QWER, Obj_AI_Base target)
        {
            if (Config.Item("PredictionMODE").GetValue<StringList>().SelectedIndex == 0)
            {
                if (QWER.Slot == SpellSlot.W)
                {
                    if (Config.Item("Wpred").GetValue<StringList>().SelectedIndex == 0)
                        QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                    else
                        QWER.Cast(target);
                }
                if (QWER.Slot == SpellSlot.R)
                {
                    if (Config.Item("Rpred").GetValue<StringList>().SelectedIndex == 0)
                        QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                    else
                        QWER.Cast(target);
                }
                if (QWER.Slot == SpellSlot.E)
                {
                    if (Config.Item("Epred").GetValue<StringList>().SelectedIndex == 0)
                        QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                    else
                        QWER.Cast(target);
                }
            }
            else
            {
                Core.SkillshotType CoreType2 = Core.SkillshotType.SkillshotLine;
                bool aoe2 = false;

                if (QWER.Type == SkillshotType.SkillshotCircle)
                {
                    CoreType2 = Core.SkillshotType.SkillshotCircle;
                    aoe2 = true;
                }

                var predInput2 = new Core.PredictionInput
                {
                    Aoe = aoe2,
                    Collision = QWER.Collision,
                    Speed = QWER.Speed,
                    Delay = QWER.Delay,
                    Range = QWER.Range,
                    From = Player.ServerPosition,
                    Radius = QWER.Width,
                    Unit = target,
                    Type = CoreType2
                };

                var poutput2 = Core.Prediction.GetPrediction(predInput2);

                if (QWER.Slot == SpellSlot.W)
                {
                    if (Config.Item("Wpred").GetValue<StringList>().SelectedIndex == 0)
                    {
                        if (poutput2.Hitchance >= Core.HitChance.VeryHigh)
                            QWER.Cast(poutput2.CastPosition);
                    }
                    else
                    {
                        if (poutput2.Hitchance >= Core.HitChance.High)
                            QWER.Cast(poutput2.CastPosition);
                    }
                }
                if (QWER.Slot == SpellSlot.R)
                {
                    if (Config.Item("Rpred").GetValue<StringList>().SelectedIndex == 0)
                    {
                        if (poutput2.Hitchance >= Core.HitChance.VeryHigh)
                            QWER.Cast(poutput2.CastPosition);
                    }
                    else
                    {
                        if (poutput2.Hitchance >= Core.HitChance.High)
                            QWER.Cast(poutput2.CastPosition);
                    }
                }
                if (QWER.Slot == SpellSlot.E)
                {
                    if (Config.Item("Epred").GetValue<StringList>().SelectedIndex == 0)
                    {
                        if (poutput2.Hitchance >= Core.HitChance.VeryHigh)
                            QWER.Cast(poutput2.CastPosition);
                    }
                    else
                    {
                        if (poutput2.Hitchance >= Core.HitChance.High)
                            QWER.Cast(poutput2.CastPosition);
                    }
                }
            }
        }

        private static void FishBoneToMiniGun(Obj_AI_Base t)
        {
            var realDistance = GetRealDistance(t);

            if (realDistance < GetRealPowPowRange(t) && t.CountEnemiesInRange(250) < Config.Item("Qaoe").GetValue<Slider>().Value)
            {
                if (Player.ManaPercent < Config.Item("QmanaCombo").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value < t.Health)
                    Q.Cast();

            }
        }

        private static float GetRealDistance(Obj_AI_Base target) { return Player.ServerPosition.Distance(target.ServerPosition) + Player.BoundingRadius + target.BoundingRadius; }

        private static float GetRealPowPowRange(GameObject target) { return 650f + Player.BoundingRadius + target.BoundingRadius; }

        private static void SetValues()
        {
            if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 2)
                Config.Item("Wcustome").Show(true);
            else
                Config.Item("Wcustome").Show(false);

            if (Config.Item("Rmode").GetValue<StringList>().SelectedIndex == 2)
                Config.Item("Rcustome").Show(true);
            else
                Config.Item("Rcustome").Show(false);


            if (Player.AttackRange > 525f)
                FishBoneActive = true;
            else
                FishBoneActive = false;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo = true;
            else
                Combo = false;

            if (
                (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Config.Item("LaneClearHarass").GetValue<bool>()) ||
                (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit && Config.Item("LaneClearHarass").GetValue<bool>()) ||
                (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && Config.Item("MixedHarass").GetValue<bool>())
               )
                Farm = true;
            else
                Farm = false;

            Q.Range = 685f + Player.BoundingRadius + 25f * Player.Spellbook.GetSpell(SpellSlot.Q).Level;

            QMANA = 10f;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            RMANA = R.Instance.ManaCost;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange").GetValue<bool>())
            {
                if (FishBoneActive)
                    Utility.DrawCircle(Player.Position, 590f + Player.BoundingRadius, System.Drawing.Color.Gray, 1, 1);
                else
                    Utility.DrawCircle(Player.Position, Q.Range - 40, System.Drawing.Color.Gray, 1, 1);
            }
            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Gray, 1, 1);
            }
            if (Config.Item("eRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Gray, 1, 1);
            }
        }
    }
}


