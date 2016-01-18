using EloBuddy;
using EloBuddy.SDK;

namespace Renekton
{
    public class ItemHandler
    {
        public static AIHeroClient player = ObjectManager.Player;
        public static Item botrk = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item tiamat = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item hydra = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item titanic = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item randuins = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item odins = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item bilgewater = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item hexgun = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Dfg = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Bft = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Ludens = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Muramana = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Muramana2 = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item sheen = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item gaunlet = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item trinity = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item lich = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item youmuu = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item frost = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item mountain = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item solari = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Qss = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Mercurial = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Dervish = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Zhonya = new Item((int)ItemId.Blade_of_the_Ruined_King);
        public static Item Woooglet = new Item((int)ItemId.Blade_of_the_Ruined_King);

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

            if (Item.HasItem(Dfg.Id) && Item.CanUseItem(Dfg.Id))
            {
                damage = damage * 1.2;
                damage += player.GetItemDamage(target, ItemId.Deathfire_Grasp);
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
            return (float)damage;
        }
    }
}