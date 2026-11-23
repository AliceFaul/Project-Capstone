#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mhieu.Enemy.EditorTools
{
    /// <summary>
    /// Sửa Animator Controller của boss Tree Monster để animation do GAME điều khiển, không tự chạy theo chuỗi.
    ///
    /// Controller gốc nối thẳng: idle -> attack -> attack02 -> idle -> behurt -> idle -> dead (tự chuyển theo Exit Time,
    /// không có parameter) nên sau vài giây boss tự "ngã" (state dead) dù chưa chết. Script này:
    ///  1. Xóa các transition cũ giữa các state (không xóa state, không đụng clip).
    ///  2. Thêm parameter: Speed (float), Attack (trigger), AttackId (int), Die (trigger), PhaseChange (trigger).
    ///  3. Tạo transition mới:
    ///       Any State -> attack     khi trigger Attack và AttackId = 0
    ///       Any State -> attack02   khi trigger Attack và AttackId lớn hơn 0
    ///       Any State -> behurt     khi trigger PhaseChange (boss gầm / đau khi chuyển pha)
    ///       Any State -> dead       khi trigger Die (chỉ bản sao xác của EnemyDeathVisual bắn trigger này)
    ///       attack / attack02 / behurt -> idle   khi chạy xong
    ///
    /// Cách dùng: nhân đôi file .controller để dự phòng, chọn nó trong Project, rồi menu
    /// Tools > Enemy > Sửa Animator boss (Tree Monster). Chưa chọn gì thì script tự tìm "Tree Monster.controller".
    /// Tên state phải đúng: idle, attack, attack02, behurt, dead. Các state thừa (idle 0, idle 0 0) được giữ nguyên nhưng không còn nối với ai.
    /// </summary>
    public static class BossAnimatorFixer
    {
        [MenuItem("Tools/Enemy/Sửa Animator boss (Tree Monster)")]
        public static void Fix()
        {
            AnimatorController controller = Selection.activeObject as AnimatorController;
            if (controller == null) controller = FindTreeMonsterController();

            if (controller == null)
            {
                EditorUtility.DisplayDialog("Sửa Animator boss",
                    "Không tìm thấy Tree Monster.controller. Hãy chọn file đó trong cửa sổ Project rồi chạy lại.", "OK");
                return;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idle = FindState(machine, "idle");
            AnimatorState attack = FindState(machine, "attack");
            AnimatorState attack02 = FindState(machine, "attack02");
            AnimatorState hurt = FindState(machine, "behurt");
            AnimatorState dead = FindState(machine, "dead");

            if (idle == null || attack == null || attack02 == null || hurt == null || dead == null)
            {
                EditorUtility.DisplayDialog("Sửa Animator boss",
                    "Controller '" + controller.name + "' thiếu state. Cần đủ: idle, attack, attack02, behurt, dead.", "OK");
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
            EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "PhaseChange", AnimatorControllerParameterType.Trigger);

            // 3. Transition mới.
            AnimatorStateTransition toAttack = AddAny(machine, attack);
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack.AddCondition(AnimatorConditionMode.Equals, 0f, "AttackId");

            AnimatorStateTransition toAttack02 = AddAny(machine, attack02);
            toAttack02.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack02.AddCondition(AnimatorConditionMode.Greater, 0f, "AttackId");

            AnimatorStateTransition toHurt = AddAny(machine, hurt);
            toHurt.AddCondition(AnimatorConditionMode.If, 0f, "PhaseChange");

            AnimatorStateTransition toDead = AddAny(machine, dead);
            toDead.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            toDead.canTransitionToSelf = false;

            AddReturn(attack, idle);
            AddReturn(attack02, idle);
            AddReturn(hurt, idle);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log("[BossAnimatorFixer] Đã sửa '" + controller.name + "'. Kiểm tra clip 'dead': tắt Loop Time để xác nằm yên.", controller);
        }

        private static AnimatorController FindTreeMonsterController()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController Tree Monster"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("/Tree Monster.controller")) continue;

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

        private static AnimatorStateTransition AddAny(AnimatorStateMachine machine, AnimatorState target)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(target);
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            transition.canTransitionToSelf = true; // đòn đánh liên tiếp phải phát lại được
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
