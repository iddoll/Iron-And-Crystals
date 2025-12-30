using UnityEngine;
using System.Collections.Generic;

public enum ItemType { None, Pickaxe, Sword, Axe, Lance, Bow, Arrow, Crystal, Helmet, Armor, Shield, Boots, Ore }
public enum PickupMethod { OnTouch, OnEPress }
public enum DamageType { Melee, Projectile, Explosion }

[CreateAssetMenu(menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Base Info")]
    public string itemName;
    public ItemType itemType;
    public Sprite icon;
    public PickupMethod pickupMethod = PickupMethod.OnEPress;

    [Header("Visuals")]
    public Sprite equippedSprite;   // Для броні (шолом)
    public GameObject worldPrefab;    // На землі
    public GameObject equippedPrefab; // В руках

    [Header("Combat & Stats")]
    public float damage = 10f;
    public float attackCooldown = 1f;
    public DamageType damageType = DamageType.Melee;

    [Header("Stacking")]
    public bool isStackable;
    public int maxStack = 1;
    
    [Header("Animations")]
    public AnimatorOverrideController overrideController;

    [Header("Crystals (Sockets)")]
    public int socketCount = 0;
    public List<Item> socketedCrystals = new List<Item>();

    public bool CanAddCrystal() => socketedCrystals.Count < socketCount;
}