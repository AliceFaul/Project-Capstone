using UnityEngine;

public class WeaponTest : MonoBehaviour
{
    [SerializeField] private WeaponDefinition weaponDefinition;

    private void Start()
    {
        var weaponA = new Weapon(weaponDefinition);
        var weaponB = new Weapon(weaponDefinition);

        Debug.Log($"Weapon A: {weaponA}");
        Debug.Log($"Weapon B: {weaponB}");

        Debug.Log(
            $"A ID: {weaponA.InstanceId}\n" +
            $"B ID: {weaponB.InstanceId}");

        Debug.Log(
            $"A sockets at Lv.{weaponA.Level}: " +
            $"{weaponA.UnlockedSocketCount}");
    }
}