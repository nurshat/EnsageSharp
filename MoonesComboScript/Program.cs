﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ensage;

#endregion

namespace MoonesComboScript
{
    class Program
    {
        const int WM_KEYUP = 0x0101;
        const int WM_KEYDOWN = 0x0105;

        private static Hero victim;
        private static double victimHP;
        private static bool Retreat;
        private static Vector3 mePosition;
        private readonly static Timer ComboTimer = new Timer();
        private readonly static Timer AttackTimer = new Timer();
        private readonly static Timer MoveTimer = new Timer();

        static void Main(string[] args)
        {
            ComboTimer.Tick += ComboTimer_Tick;
            AttackTimer.Tick += AttackTimer_Tick;
            MoveTimer.Tick += MoveTimer_Tick;
            Game.OnUpdate += OrbWalker;
            Game.OnUpdate += AutoCombo;
        }

        static void ComboTimer_Tick(object sender, EventArgs e)
        {
            ComboTimer.Enabled = false;
        }

        static void AttackTimer_Tick(object sender, EventArgs e)
        {
            AttackTimer.Enabled = false;
        }

        static void MoveTimer_Tick(object sender, EventArgs e)
        {
            MoveTimer.Enabled = false;
        }

        static void AutoCombo(EventArgs args)
        {
            if (ComboTimer.Enabled || !Game.IsInGame || Game.IsPaused)
                return;

            var me = EntityList.Hero;
            var a1 = me.Spellbook.Spell1;
            var a2 = me.Spellbook.Spell2;
            var a3 = me.Spellbook.Spell3;
            var a4 = me.Spellbook.Spell4;
            var a5 = me.Spellbook.Spell5;
            var a6 = me.Spellbook.Spell6;
            var attackRange = GetAttackRange(me);
            var victimdistance = GetDistance2D(victim.Position, me.Position);
            var canMove = AttackAnimationData.canMove;
            var mousePosition = Game.MousePosition;
            var meDmg = me.DamageAverage+me.DamageBonus;
            var blink = me.Inventory.Items.FirstOrDefault(x => x.Name == "item_blink");
            if (victim != null && (!me.UnitState.HasFlag(UnitState.Invisible) || (a2.Name == "templar_assassin_meld" && CanBeCasted(a2) && victimdistance < attackRange+50)) && ((victim.Health > 0 && victim.Health > meDmg) || victimdistance > attackRange+200) && me.IsAlive && victim.IsAlive)
            {
                if (blink != null && CanBeCasted(blink) && victim.IsVisible && victim.IsAlive && victimdistance > 500 && victimdistance > attackRange + 100 && victimdistance < 1700)
                {
                    var blinkRange = blink.AbilityData.FirstOrDefault(x => x.Name == "blink_range").Value;
                    var blinkPos = victim.Position;
                    if (Retreat)
                        blinkPos = mousePosition;
                    if (victimdistance > blinkRange || Retreat)
                        blinkPos = (blinkPos - me.Position) * (blinkRange - 1) / GetDistance2D(blinkPos, me.Position) + me.Position;
                    blink.UseAbility(blinkPos);
                    ComboTimer.Start(GetTurnTime(me, blinkPos) * 1000 + 100);
                    AttackTimer.Start(GetTurnTime(me, blinkPos) * 1000);
                    mePosition = blinkPos;
                    return;
                }
            }
            var manaboots = me.Inventory.Items.FirstOrDefault(x => x.Name == "item_arcane_boots");
            var dagon = GetDagon();
            var ethereal = me.Inventory.Items.FirstOrDefault(x => x.Name == "item_ethereal_blade");
            if (!me.UnitState.HasFlag(UnitState.Stunned) && victim.IsVisible && ((a2 == null || (a2.Name == "templar_assassin_meld" || me.Modifiers.Any(x => (x.Name == "modifier_templar_assassin_meld")) || !MoveTimer.Enabled))))
            {
                foreach (var itemData in ItemDatabase.Items)
                {
                    var itemname = itemData.Name;
                    var stun = itemData.Stun;
                    var slow = itemData.Slow;
                    var special = itemData.Special;
                    var throughBKB = itemData.ThroughBKB;
                    var killsteal = itemData.Killsteal;
                    var range = itemData.Range;
                    var retreat = itemData.Retreat;
                    var item = me.Inventory.Items.FirstOrDefault(x => x.Name == itemname);
                    if (item != null && CanBeCasted(item))
                    {
                        var go = true;
                        if (itemname == "item_refresher" || (itemname == "item_cyclone" && me.ClassId == ClassId.CDOTA_Unit_Hero_Tinker))
                        {
                            if (me.Spellbook.Spells.Any(x => x.Name != "tinker_march_of_the_machines" && x.Name != "tinker_rearm" && x.Name != "invoker_alacrity" && x.Name != "invoker_forge_spirit" && x.Name != "invoker_ice_wall" && x.Name != "invoker_ghost_walk" && x.Name != "invoker_cold_snap" && x.Name != "invoker_quas" && x.Name != "invoker_exort" && x.Name != "invoker_wex" && x.Name != "invoker_invoke" && x.Level > 0 && x.Cooldown == 0 && !x.AbilityBehavior.HasFlag(AbilityBehavior.Passive)))
                                go = false;
                            if (me.Inventory.Items.Any(x => x.Name != "item_blink" && x.Name != "item_travel_boots" && x.Name != itemname && x.Name != "item_travel_boots_2" && x.Name != "item_tpscroll" && x.Cost > 1000 && CanBeCasted(x)))
                                go = false;
                        }
                        if ((item == dagon || item == ethereal) && ((CanBeCasted(a4) && a4.Name == "necrolyte_reapers_scythe") || (killsteal && !victim.Modifiers.Any(x => (x.Name == "modifier_item_ethereal_blade_slow")) && !victim.Modifiers.Any(x => (x.Name == "modifier_necrolyte_reapers_scythe")))))
                            go = false;
                    }
                }
            }
        }

        static void OrbWalker(EventArgs args)
        {
            if (AttackTimer.Enabled || !Game.IsInGame || Game.IsPaused)
                return;

            var me = EntityList.Hero;
            var attackRange = GetAttackRange(me);
            var victimdistance = GetDistance2D(victim.Position,me.Position);
            if (victim == null || victimdistance > attackRange+100)
                victim = GetClosestEnemyHeroToMouse();
            var canMove = AttackAnimationData.canMove;
            var mousePosition = Game.MousePosition;

            if (canMove == false && victim != null && !victim.UnitState.HasFlag(UnitState.AttackImmune) && victimdistance < attackRange+100)
            {
                me.Attack(victim);
                AttackTimer.Start(200);
                return;
            }
            else
            {
                me.Move(mousePosition);
                AttackTimer.Start(200);
                return;
            }
        }

        static Hero GetClosestEnemyHeroToMouse()
        {
            var mousePosition = Game.MousePosition;
            var enemies = EntityList.GetEntities<Hero>().Where(x => x.IsVisible && x.IsAlive && !x.IsIllusion && x.Team != EntityList.Player.Team).ToList();

            var minimumDistance = float.MaxValue;
            Hero result = null;
            foreach (var hero in enemies)
            {
                var distance = Vector3.DistanceSquared(mousePosition, hero.Position);
                if (result == null || distance < minimumDistance)
                {
                    minimumDistance = distance;
                    result = hero;
                }
            }
            if (result != null)
                victimHP = result.Health;
            return result;
        }

        static float GetAttackRange(Unit unit)
        {
            var bonus = 0;
            ClassId classId = unit.ClassId;
            if (classId == ClassId.CDOTA_Unit_Hero_TemplarAssassin)
            {
                Ability psi = unit.Spellbook.SpellW;
            } 
            else if (classId == ClassId.CDOTA_Unit_Hero_Sniper)
            {
                Ability aim = unit.Spellbook.SpellE;
            }
            else if (classId == ClassId.CDOTA_Unit_Hero_Enchantress)
            {
                Ability impetus = unit.Spellbook.SpellR;
                if (impetus.Level > 0 && unit.Inventory.Items.Any(x => (x.Name == "item_ultimate_scepter")))
                    bonus = 190;
            } 
            else if (unit.Modifiers.Any(x => (x.Name == "modifier_lone_druid_true_form")))
                bonus = -423;
            else if (unit.Modifiers.Any(x => (x.Name =="dragon_knight_elder_dragon_form")))
                bonus = 372;
            else if (unit.Modifiers.Any(x => (x.Name == "terrorblade_metamorphosis")))
                bonus = 422;
            return unit.AttackRange + bonus;
        }

        static bool CanBeCasted(Ability ability)
        {
            return ability != null && ability.AbilityState == AbilityState.Ready;
        }

        static float FindAngleR(Entity ent)
        {
            return (float)(ent.RotationRad < 0 ? Math.Abs(ent.RotationRad) : 2 * Math.PI - ent.RotationRad);
        }

        static float FindAngleBetween(Vector3 first, Vector3 second)
        {
            var xAngle = (float)(Math.Atan(Math.Abs(second.X - first.X) / Math.Abs(second.Y - first.Y)) * (180.0 / Math.PI));
            if (first.X <= second.X && first.Y >= second.Y)
                return 90 - xAngle;
            if (first.X >= second.X && first.Y >= second.Y)
                return xAngle + 90;
            if (first.X >= second.X && first.Y <= second.Y)
                return 90 - xAngle + 180;
            if (first.X <= second.X && first.Y <= second.Y)
                return xAngle + 90 + 180;
            return 0;
        }

        static Item GetDagon()
        {
            return EntityList.GetLocalPlayer().Hero.Inventory.Items.ToList().FirstOrDefault(x => x.Name.Substring(0, 10) == "item_dagon");
        }

        static double GetTurnTime(Unit unit, Vector3 position)
        {
            ClassId classId = unit.ClassId;
            String name = unit.Name;
            AttackAnimationData data = AttackAnimationDatabase.GetByClassId(classId);
            if (data == null)
                data = AttackAnimationDatabase.GetByName(name);
            if (data != null)
            {
                var turnRate = data.TurnRate;
                return (Math.Max(Math.Abs(FindAngleR(unit) - DegreeToRadian(FindAngleBetween(unit, position))) - 0.69, 0) / (turnRate * (1 / 0.03)));
            }
            return (Math.Max(Math.Abs(FindAngleR(unit) - DegreeToRadian(FindAngleBetween(unit, position))) - 0.69, 0) / (0.5 * (1 / 0.03)));
        }

        static float GetDistance2D(Vector3 p1, Vector3 p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        static double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }
           
    }
}
