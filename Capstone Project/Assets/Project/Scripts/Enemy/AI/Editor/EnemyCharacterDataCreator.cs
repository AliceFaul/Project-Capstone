#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mhieu.Enemy.EditorTools
{
    /// <summary>
    /// Tạo sẵn 3 asset CharacterData riêng cho quái (không động vào Test Enemy hay asset của đồng đội):
    ///   EnemyData_Melee, EnemyData_Ranged, EnemyData_Boss  trong Assets/Project/Scriptable Object/Enemy/
    /// Asset đã tồn tại thì giữ nguyên (không ghi đè). Chỉnh số ngay trong Inspector sau khi tạo.
    /// Menu: Tools > Enemy > Tạo CharacterData cho quái.
    /// </summary>
    public static class EnemyCharacterDataCreator
    {
        private const string Folder = "Assets/Project/Scriptable Object/Enemy";

        [MenuItem("Tools/Enemy/Tạo CharacterData cho quái")]
        public static void Create()
        {
            EnsureFolder(Folder);

            //             tên             id   exp  gold  máu   giáp  sát thương
            Make("EnemyData_Melee",  101, 10f, 20f,  100,  0,  15);
            Make("EnemyData_Ranged", 102, 15f, 30f,   60,  0,  12);
            Make("EnemyData_Boss",   103, 200f, 500f, 1000, 0,  30);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[EnemyCharacterDataCreator] Xong. Gán từng asset vào ô Character Data của component Character Install trên prefab quái tương ứng.");
        }

        private static void Make(string assetName, int id, float exp, float gold, int health, int defense, int damage)
        {
            string path = Folder + "/" + assetName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<CharacterData>(path) != null)
            {
                UnityEngine.Debug.Log("[EnemyCharacterDataCreator] Đã có " + assetName + ", giữ nguyên.");
                return;
            }

            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = id;
            data.expOnKill = exp;
            data.goldOnKill = gold;
            data.level = 1;
            data.baseHealth = health;
            data.baseDefense = defense;
            data.baseDamage = damage;
            AssetDatabase.CreateAsset(data, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
