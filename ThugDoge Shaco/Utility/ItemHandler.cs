using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Shaco
{
    public class ItemHandler
    {
        public static AIHeroClient player = ObjectManager.Player;
        public static Item botrk = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item tiamat = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item hydra = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item titanic = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item randuins = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item odins = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item bilgewater = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item hexgun = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Dfg = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Bft = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Ludens = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Muramana = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Muramana2 = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item sheen = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item gaunlet = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item trinity = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item lich = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item youmuu = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item frost = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item mountain = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item solari = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Qss = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Mercurial = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Dervish = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Zhonya = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static Item Woooglet = new Item((int) ItemId.Blade_of_the_Ruined_King);
        public static bool QssUsed;
        public static float MuramanaTime;
        public static Obj_AI_Base hydraTarget;

        public static void UseItems(Obj_AI_Base target, float comboDmg = 0f, bool cleanseSpell = false)
        {                   
                hydraTarget = target;             
      
            if (Item.HasItem(randuins.Id) && Item.CanUseItem(randuins.Id))
            {
                if (target != null && player.Distance(target) < randuins.Range &&
                    player.CountEnemiesInRange(randuins.Range) >= 2)
                {
                    Item.UseItem(randuins.Id);
                }
            }
            if (target != null && Item.HasItem(odins.Id) &&
                Item.CanUseItem(odins.Id))
            {
                var odinDmg = player.GetItemDamage(target, ItemId.Odyns_Veil);

                if (odinDmg > target.Health)
                    {
                        odins.Cast(target);
                    }            
               
            }
            if (Item.HasItem(bilgewater.Id) &&
                Item.CanUseItem(bilgewater.Id))
            {
                var bilDmg = player.GetItemDamage(target, ItemId.Bilgewater_Cutlass);
                if (bilDmg > target.Health)
                    {
                        bilgewater.Cast(target);
                    }             
               
            }
            if (target != null && Item.HasItem(botrk.Id) &&
                Item.CanUseItem(botrk.Id))
            {
                var botrDmg = player.GetItemDamage(target, ItemId.Blade_of_the_Ruined_King);
               
                    if (botrDmg > target.Health)
                    {
                        botrk.Cast(target);
                    }              
                
            }
            if (target != null && Item.HasItem(hexgun.Id) &&
                Item.CanUseItem(hexgun.Id))
            {
                var hexDmg = player.GetItemDamage(target, ItemId.Hextech_Gunblade);

                if (hexDmg > target.Health)
                    {
                        hexgun.Cast(target);
                    }
               
            }
            if (
                ((!MuramanaEnabled && 40 < player.ManaPercent) ||
                 (MuramanaEnabled && 40 > player.ManaPercent)))
            {
                if (Muramana.IsOwned() && Muramana.IsReady())
                {
                    Muramana.Cast();
                }
                if (Muramana2.IsOwned() && Muramana2.IsReady())
                {
                    Muramana2.Cast();
                }
            }
            MuramanaTime = System.Environment.TickCount;
            if (Item.HasItem(youmuu.Id) && Item.CanUseItem(youmuu.Id) &&
                target != null && player.Distance(target) < player.AttackRange + 50 && target.HealthPercent < 65)
            {
                youmuu.Cast();
            }

            if (Item.HasItem(frost.Id) && Item.CanUseItem(frost.Id) && target != null)
            {
                if (player.Distance(target) < frost.Range &&
                    (2 <= target.CountEnemiesInRange(225f) &&
                     ((target.Health / target.MaxHealth * 100f) < 40 && true ||
                      !true)))
                {
                    frost.Cast(target);
                }
            }
            if (Item.HasItem(solari.Id) && Item.CanUseItem(solari.Id))
            {
                if ((2 <= player.CountAlliesInRange(solari.Range) &&
                     2 <= player.CountEnemiesInRange(solari.Range)) ||
                    ObjectManager.Get<Obj_AI_Base>()
                        .FirstOrDefault(
                            h => h.IsAlly && !h.IsDead && solari.IsInRange(h) && CombatHelper.CheckCriticalBuffs(h)) !=
                    null)
                {
                    solari.Cast();
                }
            }
                  
                UseCleanse(cleanseSpell);
            
        }

        public static bool MuramanaEnabled
        {
            get { return player.HasBuff("Muramana"); }
        }

        public static void UseCleanse(bool cleanseSpell)
        {
            if (QssUsed)
            {
                return;
            }
            if (Item.CanUseItem(Qss.Id) && Item.HasItem(Qss.Id))
            {
                Cleanse(Qss);
            }
            if (Item.CanUseItem(Mercurial.Id) && Item.HasItem(Mercurial.Id))
            {
                Cleanse(Mercurial);
            }
            if (Item.CanUseItem(Dervish.Id) && Item.HasItem(Dervish.Id))
            {
                Cleanse(Dervish);
            }
        }


        private static void Cleanse(Item Item)
        {
            var delay = 600;
            foreach (var buff in player.Buffs)
            {
                if (buff.Type == BuffType.Slow)
                {
                    CastQSS(delay, Item);
                    return;
                }
                if (buff.Type == BuffType.Blind)
                {
                    CastQSS(delay, Item);
                    return;
                }
                if (buff.Type == BuffType.Silence)
                {
                    CastQSS(delay, Item);
                    return;
                }
                if (buff.Type == BuffType.Snare)
                {
                    CastQSS(delay, Item);
                    return;
                }
                if (buff.Type == BuffType.Stun)
                {
                    CastQSS(delay, Item);
                    return;
                }
                if (buff.Type == BuffType.Charm)
                {
                    CastQSS(delay, Item);
                    return;
                }
                if (buff.Type == BuffType.Taunt)
                {
                    CastQSS(delay, Item);
                    return;
                }
                if ((buff.Type == BuffType.Fear || buff.Type == BuffType.Flee))
                {
                    CastQSS(delay, Item);
                    return;
                }
                if (buff.Type == BuffType.Suppression)
                {
                    CastQSS(delay, Item);
                    return;
                }
                if (buff.Type == BuffType.Polymorph)
                {
                    CastQSS(delay, Item);
                    return;
                }             
                    switch (buff.Name)
                    {
                        case "zedulttargetmark":
                            CastQSS(2900, Item);
                            break;
                        case "VladimirHemoplague":
                            CastQSS(4900, Item);
                            break;
                        case "MordekaiserChildrenOfTheGrave":
                            CastQSS(delay, Item);
                            break;
                        case "urgotswap2":
                            CastQSS(delay, Item);
                            break;
                        case "skarnerimpale":
                            CastQSS(delay, Item);
                            break;
                        case "poppydiplomaticimmunity":
                            CastQSS(delay, Item);
                            break;
                    }              
            }
        }

        private static void CastQSS(int delay, Item item)
        {
            QssUsed = true;
                    Core.DelayAction(
                    () =>
                    {
                        Item.UseItem(item.Id, player);
                        QssUsed = false; 
                    }, delay);
                return;
            
        }


        public static void castHydra(Obj_AI_Base target)
        {
            if (target != null && player.Distance(target) < hydra.Range && !player.Spellbook.IsAutoAttacking)
            {
                if (Item.HasItem(tiamat.Id) && Item.CanUseItem(tiamat.Id))
                {
                    Item.UseItem(tiamat.Id);
                }
                if (Item.HasItem(hydra.Id) && Item.CanUseItem(hydra.Id))
                {
                    Item.UseItem(hydra.Id);
                }
            }
        }


        public static float GetItemsDamage(Obj_AI_Base target)
        {
            double damage = 0;
            if (Item.HasItem(odins.Id) && Item.CanUseItem(odins.Id))
            {
                damage += player.GetItemDamage(target, ItemId.Odyns_Veil);
            }
            if (Item.HasItem(hexgun.Id) && Item.CanUseItem(hexgun.Id))
            {
                damage += player.GetItemDamage(target, ItemId.Hextech_Gunblade);
            }
            var ludenStacks = player.Buffs.FirstOrDefault(buff => buff.Name == "itemmagicshankcharge");
            if (ludenStacks != null && (Item.HasItem(Ludens.Id) && ludenStacks.Count == 100))
            {   
                damage += player.CalculateDamageOnUnit(target, DamageType.Magical, Convert.ToInt32(100 + player.FlatMagicDamageMod * 0.15));
            }
            if (Item.HasItem(lich.Id) && Item.CanUseItem(lich.Id))
            {
                damage += player.CalculateDamageOnUnit(target, DamageType.Magical, Convert.ToInt32(player.BaseAttackDamage * 0.75 + player.FlatMagicDamageMod * 0.5));
            }        
            if (Item.HasItem(Bft.Id) && Item.CanUseItem(Bft.Id))
            {
                damage = damage * 1.2;
                damage += player.GetItemDamage(target, ItemId.Blackfire_Torch);
            }
            if (Item.HasItem(tiamat.Id) && Item.CanUseItem(tiamat.Id))
            {
                damage += player.GetItemDamage(target, ItemId.Tiamat_Melee_Only);
            }
            if (Item.HasItem(hydra.Id) && Item.CanUseItem(hydra.Id))
            {
                damage += player.GetItemDamage(target, ItemId.Ravenous_Hydra_Melee_Only);
            }
            if (Item.HasItem(bilgewater.Id) && Item.CanUseItem(bilgewater.Id))
            {
                damage += player.GetItemDamage(target, ItemId.Bilgewater_Cutlass);
            }
            if (Item.HasItem(botrk.Id) && Item.CanUseItem(botrk.Id))
            {
                damage += player.GetItemDamage(target, ItemId.Blade_of_the_Ruined_King);
            }
            if (Item.HasItem(sheen.Id) && (Item.CanUseItem(sheen.Id) || player.HasBuff("sheen")))
            {
                damage += player.CalculateDamageOnUnit(target, DamageType.Physical, player.BaseAttackDamage);
            }
            if (Item.HasItem(gaunlet.Id) && Item.CanUseItem(gaunlet.Id))
            {
                damage += player.CalculateDamageOnUnit(target, DamageType.Physical, Convert.ToInt32(player.BaseAttackDamage * 1.25));
            }
            if (Item.HasItem(trinity.Id) && Item.CanUseItem(trinity.Id))
            {
                damage += player.CalculateDamageOnUnit(target, DamageType.Physical, Convert.ToInt32(player.BaseAttackDamage * 2));
            }
            return (float)damage;
        }
        
    }
}