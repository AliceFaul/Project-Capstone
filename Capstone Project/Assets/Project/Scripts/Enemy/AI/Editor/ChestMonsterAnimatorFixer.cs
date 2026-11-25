#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mhieu.Enemy.EditorTools
{
    /// <summary>
    /// Sửa Animator Controller của Chest Monster để animation do GAME điều khiển.
    ///
    /// Controller gốc của gói RPG Monster Partners là bản trình diễn: nối thẳng mọi state theo chuỗi
    /// (IdleChest, IdleNormal, IdleBattle, Attack01, Attack02, Taunting, Victory, Walk..., Run, SenseSomething..., GetHit, Dizzy, Die)
    /// và không có parameter, nên quái tự chạy hết chuỗi: khoe dáng (Taunting), ăn mừng (Victory), choáng (Dizzy), ngã (Die)
    /// giữa lúc đang đánh nhau. Công cụ này:
    ///  1. Xóa các transition cũ (không xóa state, không đụng clip). Đặt IdleBattle làm state mặc định.
    ///  2. Thêm parameter: Speed (float), Attack (trigger), AttackId (int), Hit (trigger), Die (trigger).
    ///  3. Tạo transition mới:
    ///       IdleBattle  -> Run         khi Speed lớn hơn 0.1;   Run -> IdleBattle khi Speed nhỏ hơn 0.1
    ///       Any State   -> Attack01    khi trigger Attack và AttackId = 0
    ///       Any State   -> Attack02    khi trigger Attack và AttackId lớn hơn 0
    ///       Any State   -> GetHit      khi trigger Hit
    ///       Any State   -> Die         khi trigger Die (bản sao xác của EnemyDeathVisual bắn trigger này)
    ///       Attack01 / Attack02 / GetHit -> IdleBattle   khi chạy xong
    ///
    /// Cách dùng: nhân đôi file ChestMonster.controller để dự phòng, chọn bản gốc trong Project, rồi menu
    /// Tools > Enemy > Sửa Animator Chest Monster. Chưa chọn gì thì công cụ tự tìm "ChestMonster.controller".
    /// Các state thừa được giữ nguyên nhưng không còn nối với ai.
    /// </summary>
    public static class ChestMonsterAnimatorFixer
    {
        [MenuItem("Tools/Enemy/Sửa Animator Chest Monster")]
        public static void Fix()
        {
            AnimatorController controller = Selection.activeObject as AnimatorController;
            if (controller == null) controller = FindController();

            if (controller == null)
            {
                EditorUtility.DisplayDialog("Sửa Animator Chest Monster",
                    "Không tìm thấy ChestMonster.controller. Hãy chọn file đó trong cửa sổ Project rồi chạy lại.", "OK");
                return;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idle = FindState(machine, "IdleBattle");
            AnimatorState run = FindState(machine, "Run");
            AnimatorState attack01 = FindState(machine, "Attack01");
            AnimatorState attack02 = FindState(machine, "Attack02");
            AnimatorState getHit = FindState(machine, "GetHit");
            AnimatorState die = FindState(machine, "Die");

            if (idle == null || run == null || attack01 == null || attack02 == null || getHit == null || die == null)
            {
                EditorUtility.DisplayDialog("Sửa Animator Chest Monster",
                    "Controller '" + controller.name + "' thiếu state. Cần đủ: IdleBattle, Run, Attack01, Attack02, GetHit, Die.", "OK");
                return;
            }

            // 1. Xóa transition cũ.
            foreach (ChildAnimatorState child in machine.states)
            {
                foreach (AnimatorStateTransition old in new List<AnimatorStateTransition>(child.state.transitions))
                    child.state.RemoveTransition(old);
            }

            foreach (AnimatorStateTransition old in new List<AnimatorStateTransition>(machine.anyStateTransitions))
                machine.RemoveAnyStateTransition(old);

            machine.defaultState = idle;

            // 2. Parameter.
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "AttackId", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);

            // 3. Transition mới.
            AnimatorStateTransition toRun = idle.AddTransition(run);
            SetupBlend(toRun);
            toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            AnimatorStateTransition toIdle = run.AddTransition(idle);
            SetupBlend(toIdle);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            AnimatorStateTransition toAttack01 = AddAny(machine, attack01);
            toAttack01.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack01.AddCondition(AnimatorConditionMode.Equals, 0f, "AttackId");

            AnimatorStateTransition toAttack02 = AddAny(machine, attack02);
            toAttack02.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack02.AddCondition(AnimatorConditionMode.Greater, 0f, "AttackId");

            AnimatorStateTransition toHit = AddAny(machine, getHit);
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

            AnimatorStateTransition toDie = AddAny(machine, die);
            toDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            toDie.canTransitionToSelf = false;

            AddReturn(attack01, idle);
            AddReturn(attack02, idle);
            AddReturn(getHit, idle);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log("[ChestMonsterAnimatorFixer] Đã sửa '" + controller.name +
                                  "'. Kiểm tra: clip Die tắt Loop Time; Animator trên model tắt Apply Root Motion.", controller);
        }

        private static AnimatorController FindController()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController ChestMonster"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("/ChestMonster.controller")) continue;

                return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }

            return null;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state.name == name) return child.state;
            }

            return null;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name != name) continue;
                if (parameter.type == type) return;

                controller.RemoveParameter(parameter); // đúng tên nhưng sai kiểu: tạo lại
                break;
            }

            controller.AddParameter(name, type);
        }

        private static void SetupBlend(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.duration = 0.1f;
        }

        private static AnimatorStateTransition AddAny(AnimatorStateMachine machine, AnimatorState target)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(target);
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            transition.canTransitionToSelf = true; // đòn liên tiếp phải phát lại được
            return transition;
        }

        private static void AddReturn(AnimatorState from, AnimatorState idle)
        {
            AnimatorStateTransition transition = from.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = 0.95f;
            transition.duration = 0.15f;
        }
    }
}
#endif
