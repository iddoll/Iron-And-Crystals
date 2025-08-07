// using UnityEngine;
//
// public class WeaponHold : MonoBehaviour
// {
//     [SerializeField] private Transform handSlot; // Пустий обʼєкт у руці гравця
//     private GameObject currentWeapon;
//
//     public void Equip(Item item)
//     {
//         Unequip(); // Спочатку знімаємо, якщо щось уже в руці
//
//         if (item.equippedPrefab != null)
//         {
//             currentWeapon = Instantiate(item.equippedPrefab, handSlot);
//         }
//     }
//
//     public void Unequip()
//     {
//         if (currentWeapon != null)
//         {
//             Destroy(currentWeapon);
//         }
//     }
// }